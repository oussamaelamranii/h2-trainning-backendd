using System.ComponentModel.DataAnnotations;

namespace H2_Trainning.Dtos
{
    // ── Exercise ──
    public class ExerciseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Sets { get; set; }
        public string Reps { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public string? MediaUrl { get; set; }
        public string? MediaType { get; set; }
        public int SortOrder { get; set; }
        public bool? IsCompleted { get; set; }
        public string? ClientNotes { get; set; }
        public double? Weight { get; set; }
    }

    public class CreateExerciseDto
    {
        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required, Range(1, 20)]
        public int Sets { get; set; }

        [Required, MaxLength(30)]
        public string Reps { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Notes { get; set; }

        public string? MediaUrl { get; set; }
        public string? MediaType { get; set; } // "Image" or "Video"
        public int SortOrder { get; set; }
    }

    // ── Meal ──
    public class MealDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Macros { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public string? PhotoUrl { get; set; }
        public string? Ingredients { get; set; }
        public int SortOrder { get; set; }
    }

    public class CreateMealDto
    {
        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Macros { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Time { get; set; } = string.Empty;

        public string? PhotoUrl { get; set; }

        [MaxLength(500)]
        public string? Ingredients { get; set; }

        public int SortOrder { get; set; }
    }

    // ── ProgramDay ──
    public class ProgramDayDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int DayNumber { get; set; }
        public bool IsRestDay { get; set; }
        public List<ExerciseDto> Exercises { get; set; } = new();
        public List<MealDto> Meals { get; set; } = new();
    }

    public class CreateProgramDayDto
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        public int DayNumber { get; set; }
        public bool IsRestDay { get; set; }
        public List<CreateExerciseDto> Exercises { get; set; } = new();
        public List<CreateMealDto> Meals { get; set; } = new();
    }

    // ── Program ──
    public class ProgramDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? NutritionalBases { get; set; }
        public string CoachId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<ProgramDayDto> Days { get; set; } = new();
    }

    public class ProgramSummaryDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string CoachId { get; set; } = string.Empty;
    }

    public class CreateProgramDto
    {
        [Required, MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(500)]
        public string? NutritionalBases { get; set; }

        public List<CreateProgramDayDto> Days { get; set; } = new();
    }

    public class UpdateProgramDto
    {
        [Required, MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(500)]
        public string? NutritionalBases { get; set; }

        public List<CreateProgramDayDto> Days { get; set; } = new();
    }
}
