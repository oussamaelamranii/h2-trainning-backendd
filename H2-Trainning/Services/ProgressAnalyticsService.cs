using H2_Trainning.Data;
using H2_Trainning.Dtos;
using H2_Trainning.Interfaces;
using H2_Trainning.Models;
using Microsoft.EntityFrameworkCore;

namespace H2_Trainning.Services
{
    public class ProgressAnalyticsService : IProgressAnalyticsService
    {
        private readonly ApplicationDbContext _db;

        public ProgressAnalyticsService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<ClientProgressAnalyticsDto?> GetClientProgressAsync(string coachId, string clientId, int weeks, int? assignmentId = null)
        {
            var client = await _db.Users
                .AsNoTracking()
                .Where(u => u.Id == clientId && u.CoachId == coachId && u.Role == Enums.Role.Client && u.IsApproved)
                .Select(u => new ClientAnalyticsProfileDto
                {
                    Id = u.Id,
                    Name = u.FullName,
                    Goal = u.Goal
                })
                .FirstOrDefaultAsync();

            if (client == null) return null;

            var safeWeeks = Math.Clamp(weeks, 1, 52);
            var today = DateTime.UtcNow.Date;
            var assignmentWindows = await _db.Assignments
                .AsNoTracking()
                .Where(a => a.ClientId == clientId)
                .OrderBy(a => a.AssignedDate)
                .ThenBy(a => a.Id)
                .Select(a => new { a.Id, a.AssignedDate })
                .ToListAsync();

            var assignment = await _db.Assignments
                .Include(a => a.Program)
                    .ThenInclude(p => p.Days)
                        .ThenInclude(d => d.Exercises)
                .Where(a => a.ClientId == clientId && (!assignmentId.HasValue || a.Id == assignmentId.Value))
                .OrderByDescending(a => a.AssignedDate)
                .ThenByDescending(a => a.Id)
                .AsSplitQuery()
                .AsNoTracking()
                .FirstOrDefaultAsync();

            var latestAssignmentId = assignmentWindows.LastOrDefault()?.Id;
            var isHistoricalProgram = assignment != null && assignmentId.HasValue && assignment.Id != latestAssignmentId;
            var nextAssignmentDate = assignment == null
                ? null
                : assignmentWindows
                    .Where(window => window.AssignedDate > assignment.AssignedDate
                        || (window.AssignedDate == assignment.AssignedDate && window.Id > assignment.Id))
                    .Select(window => (DateTime?)window.AssignedDate.Date)
                    .FirstOrDefault();
            var rangeEnd = isHistoricalProgram
                ? (nextAssignmentDate?.AddDays(-1) ?? today)
                : today;
            var rangeStart = isHistoricalProgram
                ? assignment!.AssignedDate.Date
                : rangeEnd.AddDays(-(safeWeeks * 7 - 1));

            if (assignment != null && rangeStart < assignment.AssignedDate.Date)
                rangeStart = assignment.AssignedDate.Date;
            if (rangeEnd < rangeStart)
                rangeEnd = rangeStart;

            var periodDays = Math.Max(1, (rangeEnd - rangeStart).Days + 1);
            var reportWeeks = isHistoricalProgram
                ? Math.Clamp((int)Math.Ceiling(periodDays / 7d), 1, 52)
                : safeWeeks;

            var report = new ClientProgressAnalyticsDto
            {
                Client = client,
                AssignmentId = assignment?.Id,
                ProgramId = assignment?.ProgramId,
                Weeks = reportWeeks,
                RangeStart = rangeStart,
                RangeEnd = rangeEnd,
                ProgramPeriodEnd = rangeEnd,
                ProgramDurationDays = periodDays,
                IsHistoricalProgram = isHistoricalProgram
            };

            var checkIns = await _db.CheckIns
                .AsNoTracking()
                .Where(c => c.ClientId == clientId
                    && c.CreatedAt >= rangeStart
                    && c.CreatedAt < rangeEnd.AddDays(1))
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();

            if (assignment?.Program == null)
            {
                report.WeeklyProgress = BuildWeeklyProgress(safeWeeks, rangeStart, rangeEnd, new(), checkIns, new());
                report.CheckIns = BuildCheckInProgress(checkIns);
                return report;
            }

            report.HasActiveProgram = true;
            report.ProgramAssignedDate = assignment.AssignedDate;
            report.ProgramTitle = assignment.Program.Title;

            var days = assignment.Program.Days
                .OrderBy(d => d.DayNumber)
                .ToList();
            var exercises = days.SelectMany(d => d.Exercises.OrderBy(e => e.SortOrder)).ToList();
            var exerciseIds = exercises.Select(e => e.Id).ToList();
            var histories = exerciseIds.Count == 0
                ? new List<WeightHistoryLog>()
                : await _db.WeightHistoryLogs
                    .AsNoTracking()
                    .Where(h => h.ClientId == clientId
                        && exerciseIds.Contains(h.ExerciseId)
                        && h.LoggedAt >= rangeStart
                        && h.LoggedAt <= rangeEnd)
                    .OrderBy(h => h.LoggedAt)
                    .ToListAsync();

            var historyByExercise = histories
                .GroupBy(h => h.ExerciseId)
                .ToDictionary(g => g.Key, g => g.OrderBy(h => h.LoggedAt).ToList());

            var allExerciseProgress = exercises
                .Select(e => BuildExerciseProgress(e, historyByExercise.GetValueOrDefault(e.Id) ?? new(), reportWeeks, rangeStart))
                .ToList();

            report.Sessions = days.Select(day =>
            {
                var sessionExercises = day.Exercises
                    .OrderBy(e => e.SortOrder)
                    .Select(e => allExerciseProgress.First(p => p.ExerciseId == e.Id))
                    .ToList();
                return BuildSessionProgress(day, sessionExercises);
            }).ToList();

            report.Summary = BuildSummary(exercises.Count, allExerciseProgress, histories);
            report.WeeklyProgress = BuildWeeklyProgress(
                reportWeeks,
                rangeStart,
                rangeEnd,
                histories,
                checkIns,
                report.Sessions);
            report.CheckIns = BuildCheckInProgress(checkIns);

            return report;
        }

        public async Task<List<ClientProgramHistoryItemDto>?> GetClientProgramHistoryAsync(string coachId, string clientId)
        {
            var clientExists = await _db.Users
                .AsNoTracking()
                .AnyAsync(u => u.Id == clientId
                    && u.CoachId == coachId
                    && u.Role == Enums.Role.Client
                    && u.IsApproved);

            if (!clientExists) return null;

            var today = DateTime.UtcNow.Date;
            var historyRows = await _db.Assignments
                .AsNoTracking()
                .Where(a => a.ClientId == clientId && a.Program.CoachId == coachId)
                .OrderByDescending(a => a.AssignedDate)
                .ThenByDescending(a => a.Id)
                .Select(a => new
                {
                    a.Id,
                    a.ProgramId,
                    ProgramTitle = a.Program.Title,
                    a.AssignedDate,
                    a.Status,
                    a.LastChecked
                })
                .ToListAsync();

            var history = historyRows
                .Select(item => new ClientProgramHistoryItemDto
                {
                    AssignmentId = item.Id,
                    ProgramId = item.ProgramId,
                    ProgramTitle = item.ProgramTitle,
                    AssignedDate = item.AssignedDate,
                    Status = item.Status.ToString(),
                    LastChecked = item.LastChecked
                })
                .ToList();

            for (var index = 0; index < history.Count; index++)
            {
                var item = history[index];
                var nextAssignment = index + 1 < history.Count ? history[index + 1] : null;
                var periodEnd = nextAssignment?.AssignedDate.Date.AddDays(-1) ?? today;
                if (periodEnd < item.AssignedDate.Date)
                    periodEnd = item.AssignedDate.Date;

                item.PeriodEnd = periodEnd;
                item.DurationDays = Math.Max(1, (periodEnd - item.AssignedDate.Date).Days + 1);
                item.IsCurrent = index == 0;
            }

            return history;
        }

        private static ExerciseProgressDto BuildExerciseProgress(
            Exercise exercise,
            List<WeightHistoryLog> logs,
            int weeks,
            DateTime rangeStart)
        {
            var first = logs.FirstOrDefault()?.Weight;
            var latest = logs.LastOrDefault()?.Weight;
            double? change = first.HasValue && latest.HasValue ? latest.Value - first.Value : null;
            var changePercent = first.HasValue && latest.HasValue && Math.Abs(first.Value) > 0.001
                ? (double?)(change!.Value / first.Value * 100)
                : null;

            return new ExerciseProgressDto
            {
                ExerciseId = exercise.Id,
                Name = exercise.Name,
                Sets = exercise.Sets,
                Reps = exercise.Reps,
                FirstWeight = first,
                LatestWeight = latest,
                Change = change,
                ChangePercent = changePercent,
                Trend = ClassifyTrend(change, logs.Count),
                WeeklyProgress = Enumerable.Range(0, weeks)
                    .Select(week => new WeeklyExerciseProgressDto
                    {
                        WeekNumber = week + 1,
                        Weight = logs
                            .Where(h => h.LoggedAt.Date >= rangeStart.AddDays(week * 7)
                                && h.LoggedAt.Date < rangeStart.AddDays((week + 1) * 7))
                            .OrderBy(h => h.LoggedAt)
                            .LastOrDefault()?.Weight
                    })
                    .ToList()
            };
        }

        private static SessionProgressDto BuildSessionProgress(ProgramDay day, List<ExerciseProgressDto> exercises)
        {
            var comparable = exercises.Where(e => e.ChangePercent.HasValue).ToList();
            var progressIndex = comparable.Count > 0
                ? comparable.Average(e => 100 + e.ChangePercent!.Value)
                : (double?)null;

            return new SessionProgressDto
            {
                DayNumber = day.DayNumber,
                SessionName = string.IsNullOrWhiteSpace(day.Name) ? $"Session {day.DayNumber}" : day.Name,
                TotalExercises = exercises.Count,
                TrackedExercises = exercises.Count(e => e.FirstWeight.HasValue),
                ImprovingExercises = exercises.Count(e => e.Trend == "Improving"),
                StableExercises = exercises.Count(e => e.Trend == "Stable"),
                DecliningExercises = exercises.Count(e => e.Trend == "Declining"),
                InsufficientDataExercises = exercises.Count(e => e.Trend == "Needs data"),
                ProgressIndex = progressIndex,
                Trend = ClassifyTrend(progressIndex.HasValue ? progressIndex.Value - 100 : null, comparable.Count),
                Exercises = exercises
            };
        }

        private static ProgressSummaryDto BuildSummary(
            int totalExercises,
            List<ExerciseProgressDto> exercises,
            List<WeightHistoryLog> histories)
        {
            var comparable = exercises.Where(e => e.ChangePercent.HasValue).ToList();
            var change = comparable.Count > 0 ? (double?)comparable.Average(e => e.ChangePercent!.Value) : null;

            return new ProgressSummaryDto
            {
                TotalExercises = totalExercises,
                TrackedExercises = exercises.Count(e => e.FirstWeight.HasValue),
                ImprovingExercises = exercises.Count(e => e.Trend == "Improving"),
                StableExercises = exercises.Count(e => e.Trend == "Stable"),
                DecliningExercises = exercises.Count(e => e.Trend == "Declining"),
                InsufficientDataExercises = exercises.Count(e => e.Trend == "Needs data"),
                LoggedDays = histories.Select(h => h.LoggedAt.Date).Distinct().Count(),
                TotalExerciseLogs = histories.Count,
                OverallChangePercent = change,
                OverallTrend = ClassifyTrend(change, comparable.Count),
                DataCoveragePercent = totalExercises == 0 ? 0 : Math.Round(exercises.Count(e => e.FirstWeight.HasValue) * 100d / totalExercises, 1)
            };
        }

        private static List<WeeklyProgressDto> BuildWeeklyProgress(
            int weeks,
            DateTime rangeStart,
            DateTime rangeEnd,
            List<WeightHistoryLog> histories,
            List<CheckIn> checkIns,
            List<SessionProgressDto> sessions)
        {
            var result = new List<WeeklyProgressDto>();
            var firstByExercise = histories
                .GroupBy(h => h.ExerciseId)
                .ToDictionary(g => g.Key, g => g.OrderBy(h => h.LoggedAt).First().Weight);

            for (var week = 0; week < weeks; week++)
            {
                var start = rangeStart.AddDays(week * 7);
                var end = week == weeks - 1 ? rangeEnd : start.AddDays(6);
                var weekEndExclusive = end.AddDays(1);
                var weekHistories = histories
                    .Where(h => h.LoggedAt >= start && h.LoggedAt < weekEndExclusive)
                    .ToList();
                var weekCheckIns = checkIns
                    .Where(c => c.CreatedAt >= start && c.CreatedAt < weekEndExclusive)
                    .ToList();
                var indices = weekHistories
                    .GroupBy(h => h.ExerciseId)
                    .Where(g => firstByExercise.ContainsKey(g.Key) && firstByExercise[g.Key] > 0)
                    .Select(g => g.OrderBy(h => h.LoggedAt).Last().Weight / firstByExercise[g.Key] * 100)
                    .ToList();

                result.Add(new WeeklyProgressDto
                {
                    WeekNumber = week + 1,
                    Label = $"Week {week + 1}",
                    StartDate = start,
                    EndDate = end,
                    LoggedExercises = weekHistories.Select(h => h.ExerciseId).Distinct().Count(),
                    ActiveSessions = sessions.Count(s => s.Exercises.Any(e => e.WeeklyProgress.ElementAtOrDefault(week)?.Weight.HasValue == true)),
                    AverageProgressIndex = indices.Count > 0 ? indices.Average() : null,
                    CheckInCount = weekCheckIns.Count,
                    AverageWeight = AverageOrNull(weekCheckIns.Where(c => c.Weight.HasValue).Select(c => (double)c.Weight!.Value)),
                    AverageEnergy = AverageOrNull(weekCheckIns.Select(c => (double)c.EnergyLevel)),
                    AverageSleep = AverageOrNull(weekCheckIns.Select(c => (double)c.SleepQuality)),
                    AverageMood = AverageOrNull(weekCheckIns.Select(c => (double)c.Mood))
                });
            }

            return result;
        }

        private static CheckInProgressDto BuildCheckInProgress(List<CheckIn> checkIns)
        {
            var weights = checkIns.Where(c => c.Weight.HasValue).Select(c => (double)c.Weight!.Value).ToList();
            return new CheckInProgressDto
            {
                Count = checkIns.Count,
                FirstWeight = weights.FirstOrDefaultOrNull(),
                LatestWeight = weights.LastOrDefaultOrNull(),
                WeightChange = weights.Count >= 2 ? weights[^1] - weights[0] : null,
                AverageSleep = AverageOrNull(checkIns.Select(c => (double)c.SleepQuality)),
                AverageEnergy = AverageOrNull(checkIns.Select(c => (double)c.EnergyLevel)),
                AverageMood = AverageOrNull(checkIns.Select(c => (double)c.Mood))
            };
        }

        private static string ClassifyTrend(double? change, int dataPoints)
        {
            if (!change.HasValue || dataPoints < 2) return "Needs data";
            var threshold = Math.Max(0.5, Math.Abs(change.Value) * 0.02);
            if (change.Value >= threshold) return "Improving";
            if (change.Value <= -threshold) return "Declining";
            return "Stable";
        }

        private static double? AverageOrNull(IEnumerable<double> values)
        {
            var list = values.ToList();
            return list.Count == 0 ? null : list.Average();
        }
    }

    internal static class EnumerableAnalyticsExtensions
    {
        public static double? FirstOrDefaultOrNull(this List<double> values)
            => values.Count == 0 ? null : values[0];

        public static double? LastOrDefaultOrNull(this List<double> values)
            => values.Count == 0 ? null : values[^1];
    }
}
