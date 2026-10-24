using System.ComponentModel.DataAnnotations;

namespace H2_Trainning.Dtos
{
    public class UserProfileDto
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Goal { get; set; }
        public int? Age { get; set; }
        public decimal? HeightCm { get; set; }
        public decimal? WeightKg { get; set; }
        public decimal? WaistCm { get; set; }
        public decimal? ArmCm { get; set; }
        public decimal? ThighCm { get; set; }
        public decimal? ChestCm { get; set; }
        public decimal? Bmi { get; set; }
        public string? BmiCategory { get; set; }
        public int? MaxHeartRate { get; set; }
        public ProgressScoreDto? Progress { get; set; }
    }

    public class UpdateProfileDto
    {
        [EmailAddress, MaxLength(256)]
        public string? Email { get; set; }

        [Range(20, 500)]
        public decimal? WeightKg { get; set; }

        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }

        [MaxLength(200)]
        public string? Goal { get; set; }

        [Range(10, 120)]
        public int? Age { get; set; }

        [Range(100, 250)]
        public decimal? HeightCm { get; set; }

        [Range(20, 300)]
        public decimal? WaistCm { get; set; }

        [Range(10, 150)]
        public decimal? ArmCm { get; set; }

        [Range(20, 200)]
        public decimal? ThighCm { get; set; }

        [Range(30, 300)]
        public decimal? ChestCm { get; set; }
    }

    public class ProgressScoreDto
    {
        public int OverallScore { get; set; }
        public int ConsistencyScore { get; set; }
        public int DevelopmentScore { get; set; }
        public int WorkoutDays { get; set; }
        public int ExpectedWorkoutDays { get; set; }
        public decimal ProgramCompletionPercent { get; set; }
        public decimal? StrengthChangePercent { get; set; }
        public string PeriodLabel { get; set; } = string.Empty;
        public string Explanation { get; set; } = string.Empty;
    }
}
