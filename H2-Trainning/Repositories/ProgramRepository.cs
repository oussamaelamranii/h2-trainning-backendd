using H2_Trainning.Data;
using H2_Trainning.Interfaces;
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
                .Include(p => p.Days.OrderBy(d => d.DayNumber))
                    .ThenInclude(d => d.Exercises.OrderBy(e => e.SortOrder))
                .Include(p => p.Days.OrderBy(d => d.DayNumber))
                    .ThenInclude(d => d.Meals.OrderBy(m => m.SortOrder))
                .Where(p => p.CoachId == coachId)
                .OrderByDescending(p => p.UpdatedAt)
                .AsSplitQuery()
                .ToListAsync();
        }

        public async Task<List<H2_Trainning.Models.Program>> GetSummariesByCoachIdAsync(string coachId)
        {
            return await _context.Programs
                .Where(p => p.CoachId == coachId)
                .OrderByDescending(p => p.UpdatedAt)
                .ToListAsync();
        }

        public async Task<H2_Trainning.Models.Program?> GetByIdAsync(int id)
        {
            return await _context.Programs
                .Include(p => p.Days.OrderBy(d => d.DayNumber))
                    .ThenInclude(d => d.Exercises.OrderBy(e => e.SortOrder))
                .Include(p => p.Days.OrderBy(d => d.DayNumber))
                    .ThenInclude(d => d.Meals.OrderBy(m => m.SortOrder))
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
            var existingExerciseIds = await _context.Exercises
                .Where(e => e.ProgramDay.ProgramId == program.Id)
                .Select(e => e.Id)
                .ToListAsync();

            var currentExerciseIds = program.Days
                .SelectMany(d => d.Exercises)
                .Select(e => e.Id)
                .Where(id => id > 0)
                .ToList();

            var deletedExerciseIds = existingExerciseIds.Except(currentExerciseIds).ToList();

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
            }

            if (_context.Entry(program).State == EntityState.Detached)
            {
                _context.Programs.Update(program);
            }

            await _context.SaveChangesAsync();
            return program;
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
