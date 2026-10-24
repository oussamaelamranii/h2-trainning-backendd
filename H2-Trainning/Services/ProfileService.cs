using H2_Trainning.Data;
using H2_Trainning.Dtos;
using H2_Trainning.Enums;
using H2_Trainning.Interfaces;
using H2_Trainning.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace H2_Trainning.Services
{
    public class ProfileService : IProfileService
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<AppUser> _userManager;

        public ProfileService(ApplicationDbContext db, UserManager<AppUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<UserProfileDto?> GetAsync(string requesterId, string profileUserId, bool requesterIsCoach)
        {
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == profileUserId);
            if (user == null) return null;

            var canView = requesterId == profileUserId
                || (requesterIsCoach && user.Role == Role.Client && user.CoachId == requesterId);
            if (!canView) return null;

            var latestWeight = await _db.CheckIns
                .AsNoTracking()
                .Where(c => c.ClientId == profileUserId && c.Weight.HasValue)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => c.Weight)
                .FirstOrDefaultAsync();

            var coachWeight = await _db.BodyWeightEntries.AsNoTracking()
                .Where(w => w.ClientId == profileUserId).OrderByDescending(w => w.CreatedAt).FirstOrDefaultAsync();
            var checkInDate = await _db.CheckIns.Where(c => c.ClientId == profileUserId && c.Weight.HasValue)
                .MaxAsync(c => (DateTime?)c.CreatedAt);
            if (coachWeight != null && (!checkInDate.HasValue || coachWeight.CreatedAt > checkInDate))
                latestWeight = coachWeight.WeightKg;

            var profile = MapProfile(user, latestWeight);
            if (user.Role == Role.Client)
                profile.Progress = await CalculateProgressAsync(user.Id);

            return profile;
        }

        public async Task<UserProfileDto?> UpdateAsync(string userId, UpdateProfileDto dto)
            => await UpdateAuthorizedAsync(userId, userId, false, dto);

        public async Task<UserProfileDto?> UpdateClientAsync(string coachId, string clientId, UpdateProfileDto dto)
            => await UpdateAuthorizedAsync(coachId, clientId, true, dto);

        private async Task<UserProfileDto?> UpdateAuthorizedAsync(string requesterId, string userId, bool asCoach, UpdateProfileDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || (asCoach && (user.Role != Role.Client || user.CoachId != requesterId))) return null;
            if (string.IsNullOrWhiteSpace(dto.FullName))
                throw new InvalidOperationException("Full name is required.");

            await using var transaction = await _db.Database.BeginTransactionAsync();

            if (asCoach && !string.IsNullOrWhiteSpace(dto.Email))
            {
                var email = dto.Email.Trim();
                var existing = await _userManager.FindByEmailAsync(email);
                if (existing != null && existing.Id != userId)
                    throw new InvalidOperationException("This email is already used by another account.");
                if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
                {
                    user.Email = email;
                    user.UserName = email;
                    user.EmailConfirmed = false;
                }
            }

            user.FullName = dto.FullName.Trim();
            user.PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim();
            user.Goal = string.IsNullOrWhiteSpace(dto.Goal) ? null : dto.Goal.Trim();
            user.Age = dto.Age;
            user.HeightCm = dto.HeightCm;
            user.WaistCm = dto.WaistCm;
            user.ArmCm = dto.ArmCm;
            user.ThighCm = dto.ThighCm;
            user.ChestCm = dto.ChestCm;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));

            if (dto.WeightKg.HasValue)
            {
                _db.BodyWeightEntries.Add(new BodyWeightEntry { ClientId = userId, WeightKg = dto.WeightKg.Value });
                await _db.SaveChangesAsync();
            }
            await transaction.CommitAsync();

            return await GetAsync(userId, userId, user.Role == Role.Coach);
        }

        private async Task<ProgressScoreDto> CalculateProgressAsync(string clientId)
        {
            var assignment = await _db.Assignments
                .AsNoTracking()
                .Include(a => a.ExerciseLogs)
                .Include(a => a.Program)
                    .ThenInclude(p => p.Days)
                        .ThenInclude(d => d.Exercises)
                .Where(a => a.ClientId == clientId)
                .OrderByDescending(a => a.AssignedDate)
                .AsSplitQuery()
                .FirstOrDefaultAsync();

            if (assignment?.Program == null)
            {
                return new ProgressScoreDto
                {
                    PeriodLabel = "No active program",
                    Explanation = "A program must be assigned before progression can be calculated."
                };
            }

            var today = DateTime.UtcNow.Date;
            var start = assignment.AssignedDate.Date > today.AddDays(-27)
                ? assignment.AssignedDate.Date
                : today.AddDays(-27);
            var elapsedDays = Math.Max(1, (today - start).Days + 1);
            var scheduledDaysPerWeek = Math.Clamp(
                assignment.Program.Days.Count(d => !d.IsRestDay && d.Exercises.Count > 0), 1, 7);
            var expectedWorkoutDays = Math.Max(1,
                (int)Math.Ceiling(elapsedDays / 7d * scheduledDaysPerWeek));

            var exerciseIds = assignment.Program.Days
                .SelectMany(d => d.Exercises)
                .Select(e => e.Id)
                .ToList();
            var weights = exerciseIds.Count == 0
                ? new List<WeightHistoryLog>()
                : await _db.WeightHistoryLogs
                    .AsNoTracking()
                    .Where(w => w.ClientId == clientId
                        && exerciseIds.Contains(w.ExerciseId)
                        && w.LoggedAt.Date >= assignment.AssignedDate.Date)
                    .OrderBy(w => w.LoggedAt)
                    .ToListAsync();

            var activityDates = weights
                .Where(w => w.LoggedAt.Date >= start)
                .Select(w => w.LoggedAt.Date)
                .Concat(assignment.ExerciseLogs
                    .Where(l => l.LastActivityAt.Date >= start)
                    .Select(l => l.LastActivityAt.Date))
                .Distinct()
                .Count();
            var consistency = Math.Clamp(activityDates * 100d / expectedWorkoutDays, 0, 100);

            var totalExercises = exerciseIds.Count;
            var completedExercises = assignment.ExerciseLogs.Count(l => l.IsCompleted && exerciseIds.Contains(l.ExerciseId));
            var completion = totalExercises == 0 ? 0 : completedExercises * 100d / totalExercises;

            var comparableChanges = weights
                .GroupBy(w => w.ExerciseId)
                .Where(g => g.Count() >= 2 && g.First().Weight > 0)
                .Select(g => (g.Last().Weight - g.First().Weight) / g.First().Weight * 100d)
                .ToList();
            double? strengthChange = comparableChanges.Count > 0 ? comparableChanges.Average() : null;
            var development = strengthChange.HasValue
                ? completion * 0.6 + Math.Clamp(50 + strengthChange.Value * 5, 0, 100) * 0.4
                : completion;
            var overall = consistency * 0.6 + development * 0.4;

            return new ProgressScoreDto
            {
                OverallScore = (int)Math.Round(overall),
                ConsistencyScore = (int)Math.Round(consistency),
                DevelopmentScore = (int)Math.Round(development),
                WorkoutDays = activityDates,
                ExpectedWorkoutDays = expectedWorkoutDays,
                ProgramCompletionPercent = Math.Round((decimal)completion, 1),
                StrengthChangePercent = strengthChange.HasValue ? Math.Round((decimal)strengthChange.Value, 1) : null,
                PeriodLabel = $"Last {elapsedDays} day{(elapsedDays == 1 ? "" : "s")}",
                Explanation = "Score = 60% attendance against scheduled workout days + 40% current-program development (exercise completion and strength evolution when available)."
            };
        }

        private static UserProfileDto MapProfile(AppUser user, decimal? latestWeight)
        {
            decimal? bmi = null;
            if (latestWeight.HasValue && user.HeightCm.HasValue && user.HeightCm.Value > 0)
            {
                var heightM = user.HeightCm.Value / 100m;
                bmi = Math.Round(latestWeight.Value / (heightM * heightM), 1);
            }

            return new UserProfileDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Role = user.Role.ToString(),
                PhoneNumber = user.PhoneNumber,
                Goal = user.Goal,
                Age = user.Age,
                HeightCm = user.HeightCm,
                WeightKg = latestWeight,
                WaistCm = user.WaistCm,
                ArmCm = user.ArmCm,
                ThighCm = user.ThighCm,
                ChestCm = user.ChestCm,
                Bmi = bmi,
                BmiCategory = ClassifyBmi(bmi, user.Age),
                MaxHeartRate = user.Age.HasValue ? 220 - user.Age.Value : null
            };
        }

        private static string? ClassifyBmi(decimal? bmi, int? age)
        {
            if (!bmi.HasValue) return null;
            if (!age.HasValue) return "Add age to interpret";
            if (age.Value < 20) return "Age-specific interpretation required";
            if (bmi < 18.5m) return "Underweight";
            if (bmi < 25m) return "Healthy range";
            if (bmi < 30m) return "Overweight";
            return "Obesity range";
        }
    }
}
