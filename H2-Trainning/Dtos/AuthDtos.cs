using System.ComponentModel.DataAnnotations;

namespace H2_Trainning.Dtos
{
    // ── Register ──
    public class RegisterDto
    {
        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = "Client"; // "Coach" or "Client"

        [MaxLength(200)]
        public string? Goal { get; set; }

        public string? PhoneNumber { get; set; }

        [Range(10, 120)]
        public int? Age { get; set; }

        [Range(100, 250)]
        public decimal? HeightCm { get; set; }
    }

    // ── Login ──
    public class LoginDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    // ── Client Signup ──
    public class ClientSignupDto
    {
        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Goal { get; set; }

        public string? PhoneNumber { get; set; }

        [Range(10, 120)]
        public int? Age { get; set; }

        [Range(100, 250)]
        public decimal? HeightCm { get; set; }
    }

    // ── Auth Response ──
    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public UserDto User { get; set; } = null!;
    }

    // ── Simple Message ──
    public class MessageDto
    {
        public string Message { get; set; } = string.Empty;
    }
}
