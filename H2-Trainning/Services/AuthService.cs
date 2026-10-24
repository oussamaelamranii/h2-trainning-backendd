using H2_Trainning.Dtos;
using H2_Trainning.Enums;
using H2_Trainning.Helpers;
using H2_Trainning.Interfaces;
using H2_Trainning.Models;
using Microsoft.AspNetCore.Identity;

namespace H2_Trainning.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IConfiguration _config;

        public AuthService(UserManager<AppUser> userManager, IConfiguration config)
        {
            _userManager = userManager;
            _config = config;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
        {
            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
                throw new Exception("A user with this email already exists.");

            if (!Enum.TryParse<Role>(dto.Role, true, out var role))
                throw new Exception("Invalid role. Must be 'Coach' or 'Client'.");

            var user = new AppUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                FullName = dto.FullName,
                Role = role,
                Goal = dto.Goal,
                IsApproved = true, // Direct registration is always approved
                PlainPassword = dto.Password,
                PhoneNumber = dto.PhoneNumber,
                Age = dto.Age,
                HeightCm = dto.HeightCm
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new Exception($"Registration failed: {errors}");
            }

            var token = JwtHelper.GenerateToken(user, _config);

            return new AuthResponseDto
            {
                Token = token,
                User = MapToDto(user)
            };
        }

        public async Task<MessageDto> ClientSignupAsync(ClientSignupDto dto)
        {
            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
                throw new Exception("A user with this email already exists.");

            var user = new AppUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                FullName = dto.FullName,
                Role = Role.Client,
                Goal = dto.Goal,
                IsApproved = false, // Pending coach approval
                PlainPassword = dto.Password,
                PhoneNumber = dto.PhoneNumber,
                Age = dto.Age,
                HeightCm = dto.HeightCm
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new Exception($"Registration failed: {errors}");
            }

            return new MessageDto
            {
                Message = "Your account has been created and is pending coach approval. You will be able to log in once approved."
            };
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                throw new Exception("Invalid email or password.");

            var valid = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!valid)
                throw new Exception("Invalid email or password.");

            // Block unapproved clients
            if (user.Role == Role.Client && !user.IsApproved)
                throw new Exception("Your account is pending coach approval. Please wait for your coach to approve your registration.");

            var token = JwtHelper.GenerateToken(user, _config);

            return new AuthResponseDto
            {
                Token = token,
                User = MapToDto(user)
            };
        }

        private static UserDto MapToDto(AppUser user) => new()
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email!,
            Role = user.Role.ToString(),
            Goal = user.Goal,
            IsApproved = user.IsApproved,
            PlainPassword = user.PlainPassword,
            PhoneNumber = user.PhoneNumber,
            Age = user.Age,
            HeightCm = user.HeightCm,
            WaistCm = user.WaistCm,
            ArmCm = user.ArmCm,
            ThighCm = user.ThighCm,
            ChestCm = user.ChestCm,
            MaxHeartRate = user.Age.HasValue ? 220 - user.Age.Value : null
        };
    }
}
