using H2_Trainning.Dtos;
using H2_Trainning.Interfaces;
using H2_Trainning.Models;

namespace H2_Trainning.Services
{
    public class ProgramService : IProgramService
    {
        private readonly IProgramRepository _repo;

        public ProgramService(IProgramRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<ProgramDto>> GetByCoachIdAsync(string coachId)
        {
            var programs = await _repo.GetByCoachIdAsync(coachId);
            return programs.Select(MapToDto).ToList();
        }

        public Task<List<ProgramSummaryDto>> GetSummariesByCoachIdAsync(string coachId) =>
            _repo.GetSummariesByCoachIdAsync(coachId);

        public async Task<ProgramDto?> GetByIdAsync(int id)
        {
            var program = await _repo.GetByIdAsync(id);
            return program == null ? null : MapToDto(program);
        }

        public async Task<ProgramDto> CreateAsync(string coachId, CreateProgramDto dto)
        {
            var program = new H2_Trainning.Models.Program
            {
                Title = dto.Title,
                Description = dto.Description,
                NutritionalBases = dto.NutritionalBases,
                CoachId = coachId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Days = dto.Days.Select(d => new ProgramDay
                {
                    Name = d.Name,
                    DayNumber = d.DayNumber,
                    IsRestDay = d.IsRestDay,
                    Exercises = d.Exercises.Select((e, i) => new Exercise
                    {
                        Name = e.Name,
                        Sets = e.Sets,
                        Reps = e.Reps,
                        Notes = e.Notes,
                        MediaUrl = e.MediaUrl,
                        MediaType = ParseMediaType(e.MediaType),
                        SortOrder = e.SortOrder > 0 ? e.SortOrder : i
                    }).ToList(),
                    Meals = d.Meals.Select((m, i) => new Meal
                    {
                        Name = m.Name,
                        Macros = m.Macros,
                        Time = m.Time,
                        PhotoUrl = m.PhotoUrl,
                        Ingredients = m.Ingredients,
                        SortOrder = m.SortOrder > 0 ? m.SortOrder : i
                    }).ToList()
                }).ToList()
            };

            var created = await _repo.CreateAsync(program);
            return MapToDto(created);
        }

        public async Task<ProgramDto?> UpdateAsync(int id, string coachId, UpdateProgramDto dto)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null || existing.CoachId != coachId) return null;

            existing.Title = dto.Title;
            existing.Description = dto.Description;
            existing.NutritionalBases = dto.NutritionalBases;
            existing.UpdatedAt = DateTime.UtcNow;

            // Granular update to preserve existing Day and Exercise IDs,
            // preventing SQL foreign key constraint violations and keeping client logs intact.
            var existingDays = existing.Days.OrderBy(d => d.DayNumber).ToList();
            var incomingDays = dto.Days.OrderBy(d => d.DayNumber).ToList();

            // 1. Remove days that are no longer in the updated list
            for (int i = incomingDays.Count; i < existingDays.Count; i++)
            {
                existing.Days.Remove(existingDays[i]);
            }

            // 2. Add or update days
            for (int i = 0; i < incomingDays.Count; i++)
            {
                var incomingDay = incomingDays[i];
                ProgramDay dayToUpdate;

                if (i < existingDays.Count)
                {
                    dayToUpdate = existingDays[i];
                    dayToUpdate.Name = incomingDay.Name;
                    dayToUpdate.IsRestDay = incomingDay.IsRestDay;
                    dayToUpdate.DayNumber = incomingDay.DayNumber;
                }
                else
                {
                    dayToUpdate = new ProgramDay
                    {
                        Name = incomingDay.Name,
                        DayNumber = incomingDay.DayNumber,
                        IsRestDay = incomingDay.IsRestDay,
                        ProgramId = id,
                        Exercises = new List<Exercise>(),
                        Meals = new List<Meal>()
                    };
                    existing.Days.Add(dayToUpdate);
                }

                // 3. Update exercises for this day
                var existingExs = dayToUpdate.Exercises.OrderBy(e => e.SortOrder).ToList();
                var incomingExs = incomingDay.Exercises.OrderBy(e => e.SortOrder).ToList();

                // Delete old exercises
                for (int j = incomingExs.Count; j < existingExs.Count; j++)
                {
                    dayToUpdate.Exercises.Remove(existingExs[j]);
                }

                // Add or update exercises
                for (int j = 0; j < incomingExs.Count; j++)
                {
                    var incomingEx = incomingExs[j];
                    if (j < existingExs.Count)
                    {
                        var exToUpdate = existingExs[j];
                        exToUpdate.Name = incomingEx.Name;
                        exToUpdate.Sets = incomingEx.Sets;
                        exToUpdate.Reps = incomingEx.Reps;
                        exToUpdate.Notes = incomingEx.Notes;
                        // Media is optional on updates. The client omits
                        // unchanged base64 blobs to stay below proxy limits.
                        if (incomingEx.MediaUrl != null)
                        {
                            exToUpdate.MediaUrl = incomingEx.MediaUrl;
                            exToUpdate.MediaType = ParseMediaType(incomingEx.MediaType);
                        }
                        exToUpdate.SortOrder = incomingEx.SortOrder > 0 ? incomingEx.SortOrder : j;
                    }
                    else
                    {
                        dayToUpdate.Exercises.Add(new Exercise
                        {
                            Name = incomingEx.Name,
                            Sets = incomingEx.Sets,
                            Reps = incomingEx.Reps,
                            Notes = incomingEx.Notes,
                            MediaUrl = incomingEx.MediaUrl,
                            MediaType = ParseMediaType(incomingEx.MediaType),
                            SortOrder = incomingEx.SortOrder > 0 ? incomingEx.SortOrder : j
                        });
                    }
                }

                // 4. Update meals for this day
                var existingMeals = dayToUpdate.Meals.OrderBy(m => m.SortOrder).ToList();
                var incomingMeals = incomingDay.Meals.OrderBy(m => m.SortOrder).ToList();

                // Delete old meals
                for (int j = incomingMeals.Count; j < existingMeals.Count; j++)
                {
                    dayToUpdate.Meals.Remove(existingMeals[j]);
                }

                // Add or update meals
                for (int j = 0; j < incomingMeals.Count; j++)
                {
                    var incomingMeal = incomingMeals[j];
                    if (j < existingMeals.Count)
                    {
                        var mealToUpdate = existingMeals[j];
                        mealToUpdate.Name = incomingMeal.Name;
                        mealToUpdate.Macros = incomingMeal.Macros;
                        mealToUpdate.Time = incomingMeal.Time;
                        if (incomingMeal.PhotoUrl != null)
                        {
                            mealToUpdate.PhotoUrl = incomingMeal.PhotoUrl;
                        }
                        mealToUpdate.Ingredients = incomingMeal.Ingredients;
                        mealToUpdate.SortOrder = incomingMeal.SortOrder > 0 ? incomingMeal.SortOrder : j;
                    }
                    else
                    {
                        dayToUpdate.Meals.Add(new Meal
                        {
                            Name = incomingMeal.Name,
                            Macros = incomingMeal.Macros,
                            Time = incomingMeal.Time,
                            PhotoUrl = incomingMeal.PhotoUrl,
                            Ingredients = incomingMeal.Ingredients,
                            SortOrder = incomingMeal.SortOrder > 0 ? incomingMeal.SortOrder : j
                        });
                    }
                }
            }

            var updated = await _repo.UpdateAsync(existing);
            return MapToDto(updated);
        }

        public Task<bool> SaveMediaAsync(int id, string coachId, SaveProgramMediaDto dto)
        {
            if (dto.Target == ProgramMediaTarget.Exercise)
            {
                var mediaType = ParseMediaType(dto.MediaType);
                if (mediaType == null)
                {
                    return Task.FromResult(false);
                }

                return _repo.UpdateExerciseMediaAsync(
                    id,
                    coachId,
                    dto.EntityId,
                    dto.DataUrl,
                    mediaType.Value);
            }

            return _repo.UpdateMealPhotoAsync(
                id,
                coachId,
                dto.EntityId,
                dto.DataUrl);
        }

        public async Task<bool> DeleteAsync(int id, string coachId)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null || existing.CoachId != coachId) return false;
            return await _repo.DeleteAsync(id);
        }

        public async Task<ProgramDto?> DuplicateAsync(int id, string coachId)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null || existing.CoachId != coachId) return null;

            var copy = new H2_Trainning.Models.Program
            {
                Title = $"{existing.Title} (Copy)",
                Description = existing.Description,
                NutritionalBases = existing.NutritionalBases,
                CoachId = coachId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Days = existing.Days.Select(d => new Models.ProgramDay
                {
                    Name = d.Name,
                    DayNumber = d.DayNumber,
                    IsRestDay = d.IsRestDay,
                    Exercises = d.Exercises.Select(e => new Models.Exercise
                    {
                        Name = e.Name,
                        Sets = e.Sets,
                        Reps = e.Reps,
                        Notes = e.Notes,
                        MediaUrl = e.MediaUrl,
                        MediaType = e.MediaType,
                        SortOrder = e.SortOrder
                    }).ToList(),
                    Meals = d.Meals.Select(m => new Models.Meal
                    {
                        Name = m.Name,
                        Macros = m.Macros,
                        Time = m.Time,
                        PhotoUrl = m.PhotoUrl,
                        Ingredients = m.Ingredients,
                        SortOrder = m.SortOrder
                    }).ToList()
                }).ToList()
            };

            var created = await _repo.CreateAsync(copy);
            return MapToDto(created);
        }

        private static Models.MediaType? ParseMediaType(string? type)
        {
            if (string.IsNullOrEmpty(type)) return null;
            return Enum.TryParse<Models.MediaType>(type, true, out var result) ? result : null;
        }

        private static ProgramDto MapToDto(H2_Trainning.Models.Program p) => new()
        {
            Id = p.Id,
            Title = p.Title,
            Description = p.Description,
            NutritionalBases = p.NutritionalBases,
            CoachId = p.CoachId,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt,
            Days = p.Days?.Select(d => new ProgramDayDto
            {
                Id = d.Id,
                Name = d.Name,
                DayNumber = d.DayNumber,
                IsRestDay = d.IsRestDay,
                Exercises = d.Exercises?.Select(e => new ExerciseDto
                {
                    Id = e.Id,
                    Name = e.Name,
                    Sets = e.Sets,
                    Reps = e.Reps,
                    Notes = e.Notes,
                    MediaUrl = e.MediaUrl,
                    MediaType = e.MediaType?.ToString(),
                    SortOrder = e.SortOrder
                }).ToList() ?? new(),
                Meals = d.Meals?.Select(m => new MealDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    Macros = m.Macros,
                    Time = m.Time,
                    PhotoUrl = m.PhotoUrl,
                    Ingredients = m.Ingredients,
                    SortOrder = m.SortOrder
                }).ToList() ?? new()
            }).ToList() ?? new()
        };
    }
}
