using H2_Trainning.Data;
using H2_Trainning.Dtos;
using H2_Trainning.Interfaces;
using H2_Trainning.Models;
using Microsoft.EntityFrameworkCore;

namespace H2_Trainning.Repositories
{
    public class ProgramRepository : IProgramRepository
    {
        private readonly ApplicationDbContext _context;

        public ProgramRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<H2_Trainning.Models.Program>> GetByCoachIdAsync(string coachId)
        {
            return await _context.Programs
                .Include(p => p.Days)
                    .ThenInclude(d => d.Exercises)
                .Include(p => p.Days)
                    .ThenInclude(d => d.Meals)
                .Where(p => p.CoachId == coachId)
                .OrderByDescending(p => p.UpdatedAt)
                .AsNoTracking()
                .AsSplitQuery()
                .ToListAsync();
        }

        public async Task<List<ProgramSummaryDto>> GetSummariesByCoachIdAsync(string coachId)
        {
            return await _context.Programs
                .Where(p => p.CoachId == coachId)
                .OrderByDescending(p => p.UpdatedAt)
                .AsNoTracking()
                .Select(p => new ProgramSummaryDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    Description = p.Description,
                    NutritionalBases = p.NutritionalBases,
                    CoachId = p.CoachId,
                    DayCount = p.Days.Count(),
                    ExerciseCount = p.Days.SelectMany(d => d.Exercises).Count(),
                    MealCount = p.Days.SelectMany(d => d.Meals).Count()
                })
                .ToListAsync();
        }

        public async Task<H2_Trainning.Models.Program?> GetByIdAsync(int id)
        {
            return await _context.Programs
                .Include(p => p.Days)
                    .ThenInclude(d => d.Exercises)
                .Include(p => p.Days)
                    .ThenInclude(d => d.Meals)
                .AsSplitQuery()
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<H2_Trainning.Models.Program> CreateAsync(H2_Trainning.Models.Program program)
        {
            _context.Programs.Add(program);
            await _context.SaveChangesAsync();
            return program;
        }

        public async Task<H2_Trainning.Models.Program> UpdateAsync(H2_Trainning.Models.Program program)
        {
            // Find all days in DB for this program
            var dbDayIds = await _context.ProgramDays
                .Where(d => d.ProgramId == program.Id)
                .Select(d => d.Id)
                .ToListAsync();

            var currentDayIds = program.Days
                .Select(d => d.Id)
                .Where(id => id > 0)
                .ToList();

            var deletedDayIds = dbDayIds.Except(currentDayIds).ToList();

            if (deletedDayIds.Any())
            {
                var daysToRemove = await _context.ProgramDays
                    .Where(d => deletedDayIds.Contains(d.Id))
                    .ToListAsync();
                _context.ProgramDays.RemoveRange(daysToRemove);
            }

            // Find all exercises in DB for this program's days
            var dbExerciseIds = await _context.Exercises
                .Where(e => e.ProgramDay.ProgramId == program.Id)
                .Select(e => e.Id)
                .ToListAsync();

            var currentExerciseIds = program.Days
                .SelectMany(d => d.Exercises)
                .Select(e => e.Id)
                .Where(id => id > 0)
                .ToList();

            var deletedExerciseIds = dbExerciseIds.Except(currentExerciseIds).ToList();

            if (deletedExerciseIds.Any())
            {
                var weightLogs = await _context.WeightHistoryLogs
                    .Where(w => deletedExerciseIds.Contains(w.ExerciseId))
                    .ToListAsync();
                if (weightLogs.Any())
                {
                    _context.WeightHistoryLogs.RemoveRange(weightLogs);
                }

                var exerciseLogs = await _context.ExerciseLogs
                    .Where(el => deletedExerciseIds.Contains(el.ExerciseId))
                    .ToListAsync();
                if (exerciseLogs.Any())
                {
                    _context.ExerciseLogs.RemoveRange(exerciseLogs);
                }

                var exercisesToRemove = await _context.Exercises
                    .Where(e => deletedExerciseIds.Contains(e.Id))
                    .ToListAsync();
                _context.Exercises.RemoveRange(exercisesToRemove);
            }

            // Find all meals in DB for this program's days
            var dbMealIds = await _context.Meals
                .Where(m => m.ProgramDay.ProgramId == program.Id)
                .Select(m => m.Id)
                .ToListAsync();

            var currentMealIds = program.Days
                .SelectMany(d => d.Meals)
                .Select(m => m.Id)
                .Where(id => id > 0)
                .ToList();

            var deletedMealIds = dbMealIds.Except(currentMealIds).ToList();

            if (deletedMealIds.Any())
            {
                var mealsToRemove = await _context.Meals
                    .Where(m => deletedMealIds.Contains(m.Id))
                    .ToListAsync();
                _context.Meals.RemoveRange(mealsToRemove);
            }

            if (_context.Entry(program).State == EntityState.Detached)
            {
                _context.Programs.Update(program);
            }

            await _context.SaveChangesAsync();
            return program;
        }

        public async Task<bool> UpdateExerciseMediaAsync(
            int programId,
            string coachId,
            int exerciseId,
            string dataUrl,
            MediaType mediaType)
        {
            var exercise = await _context.Exercises
                .FirstOrDefaultAsync(e =>
                    e.Id == exerciseId &&
                    e.ProgramDay.ProgramId == programId &&
                    e.ProgramDay.Program.CoachId == coachId);

            if (exercise == null) return false;

            exercise.MediaUrl = dataUrl;
            exercise.MediaType = mediaType;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateMealPhotoAsync(
            int programId,
            string coachId,
            int mealId,
            string dataUrl)
        {
            var meal = await _context.Meals
                .FirstOrDefaultAsync(m =>
                    m.Id == mealId &&
                    m.ProgramDay.ProgramId == programId &&
                    m.ProgramDay.Program.CoachId == coachId);

            if (meal == null) return false;

            meal.PhotoUrl = dataUrl;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var program = await _context.Programs
                .Include(p => p.Days)
                    .ThenInclude(d => d.Exercises)
                .Include(p => p.Days)
                    .ThenInclude(d => d.Meals)
                .Include(p => p.Assignments)
                    .ThenInclude(a => a.ExerciseLogs)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (program == null) return false;

            // Delete assignments and their associated exercise logs first
            // (EF constraint prevents cascading delete of assignments automatically)
            if (program.Assignments != null)
            {
                foreach (var assignment in program.Assignments)
                {
                    if (assignment.ExerciseLogs != null)
                    {
                        _context.ExerciseLogs.RemoveRange(assignment.ExerciseLogs);
                    }
                }
                _context.Assignments.RemoveRange(program.Assignments);
            }

            foreach (var day in program.Days)
            {
                _context.Exercises.RemoveRange(day.Exercises);
                _context.Meals.RemoveRange(day.Meals);
            }
            _context.ProgramDays.RemoveRange(program.Days);
            _context.Programs.Remove(program);
            
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
