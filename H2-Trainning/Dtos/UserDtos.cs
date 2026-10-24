using System.ComponentModel.DataAnnotations;

namespace H2_Trainning.Dtos
{
    public class UserDto
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? Goal { get; set; }
        public bool IsApproved { get; set; }
        public string? PlainPassword { get; set; }
        public string? PhoneNumber { get; set; }
        public decimal? LastWeight { get; set; }
        public int? Age { get; set; }
        public decimal? HeightCm { get; set; }
        public decimal? WaistCm { get; set; }
        public decimal? ArmCm { get; set; }
        public decimal? ThighCm { get; set; }
        public decimal? ChestCm { get; set; }
        public decimal? Bmi { get; set; }
        public int? MaxHeartRate { get; set; }
    }

    public class CreateClientDto
    {
        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Goal { get; set; }

        [Required, MinLength(6)]
        public string Password { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }

        [Range(10, 120)]
        public int? Age { get; set; }

        [Range(100, 250)]
        public decimal? HeightCm { get; set; }
    }

    public class ResetPasswordDto
    {
        [Required, MinLength(6)]
        public string NewPassword { get; set; } = string.Empty;
    }
}
