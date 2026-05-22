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
    }

    public class ResetPasswordDto
    {
        [Required, MinLength(6)]
        public string NewPassword { get; set; } = string.Empty;
    }
}
