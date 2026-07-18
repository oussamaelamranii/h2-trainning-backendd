using H2_Trainning.Dtos;
using H2_Trainning.Enums;
using H2_Trainning.Interfaces;
using H2_Trainning.Models;
using Microsoft.AspNetCore.Identity;

namespace H2_Trainning.Services
{
    public class ClientService : IClientService
    {
        private readonly IClientRepository _repo;
        private readonly UserManager<AppUser> _userManager;
        private readonly ICheckInRepository _checkInRepo;

        public ClientService(IClientRepository repo, UserManager<AppUser> userManager, ICheckInRepository checkInRepo)
        {
            _repo = repo;
            _userManager = userManager;
            _checkInRepo = checkInRepo;
        }

        public async Task<List<UserDto>> GetClientsByCoachIdAsync(string coachId)
        {
            var clients = await _repo.GetClientsByCoachIdAsync(coachId);
            var clientIds = clients.Select(c => c.Id).ToList();
            var weightsMap = await _checkInRepo.GetLatestWeightsForClientsAsync(clientIds);
            return MapToDtosWithWeight(clients, weightsMap);
        }

        public async Task<List<UserDto>> SearchClientsAsync(string coachId, string query)
        {
            var clients = await _repo.SearchClientsAsync(coachId, query);
            var clientIds = clients.Select(c => c.Id).ToList();
            var weightsMap = await _checkInRepo.GetLatestWeightsForClientsAsync(clientIds);
            return MapToDtosWithWeight(clients, weightsMap);
        }

        public async Task<UserDto> CreateClientAsync(string coachId, CreateClientDto dto)
        {
            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
                throw new Exception("A user with this email already exists.");

            var client = new AppUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                FullName = dto.FullName,
                Role = Role.Client,
                Goal = dto.Goal,
                CoachId = coachId,
                IsApproved = true, // Coach-created clients are auto-approved
                PlainPassword = dto.Password,
                PhoneNumber = dto.PhoneNumber
            };

            var result = await _userManager.CreateAsync(client, dto.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new Exception($"Failed to create client: {errors}");
            }

            return await MapToDtoWithWeightAsync(client);
        }

        public async Task<UserDto?> GetByIdAsync(string id)
        {
            var user = await _repo.GetByIdAsync(id);
            return user == null ? null : await MapToDtoWithWeightAsync(user);
        }

        public async Task<bool> DeleteClientAsync(string clientId, string coachId)
        {
            return await _repo.DeleteAsync(clientId, coachId);
        }

        public async Task<List<UserDto>> GetPendingClientsAsync()
        {
            var clients = await _repo.GetPendingClientsAsync();
            var clientIds = clients.Select(c => c.Id).ToList();
            var weightsMap = await _checkInRepo.GetLatestWeightsForClientsAsync(clientIds);
            return MapToDtosWithWeight(clients, weightsMap);
        }

        public async Task<UserDto> ApproveClientAsync(string clientId, string coachId)
        {
            var user = await _repo.GetByIdAsync(clientId);
            if (user == null || user.Role != Role.Client || user.IsApproved)
                throw new Exception("Pending client not found.");

            user.IsApproved = true;
            user.CoachId = coachId;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new Exception($"Failed to approve client: {errors}");
            }

            return await MapToDtoWithWeightAsync(user);
        }

        public async Task<bool> RejectClientAsync(string clientId)
        {
            return await _repo.DeletePendingAsync(clientId);
        }

        public async Task<UserDto> ResetClientPasswordAsync(string clientId, string coachId, string newPassword)
        {
            var user = await _repo.GetByIdAsync(clientId);
            if (user == null || user.Role != Role.Client)
                throw new Exception("Client not found.");

            // Verify this client belongs to the coach
            if (user.CoachId != coachId)
                throw new Exception("You can only reset passwords for your own clients.");

            // Remove old password and set new one
            var removeResult = await _userManager.RemovePasswordAsync(user);
            if (!removeResult.Succeeded)
                throw new Exception("Failed to remove old password.");

            var addResult = await _userManager.AddPasswordAsync(user, newPassword);
            if (!addResult.Succeeded)
            {
                var errors = string.Join(", ", addResult.Errors.Select(e => e.Description));
                throw new Exception($"Failed to set new password: {errors}");
            }

            // Save plain password so coach can view it
            user.PlainPassword = newPassword;
            await _userManager.UpdateAsync(user);

            return await MapToDtoWithWeightAsync(user);
        }

        private async Task<UserDto> MapToDtoWithWeightAsync(AppUser u)
        {
            var latestCheckIn = await _checkInRepo.GetLatestByClientIdAsync(u.Id);
            return new UserDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email!,
                Role = u.Role.ToString(),
                Goal = u.Goal,
                IsApproved = u.IsApproved,
                PlainPassword = u.PlainPassword,
                PhoneNumber = u.PhoneNumber,
                LastWeight = latestCheckIn?.Weight
            };
        }

        private List<UserDto> MapToDtosWithWeight(List<AppUser> users, Dictionary<string, decimal?> weightsMap)
        {
            var dtos = new List<UserDto>();
            foreach (var u in users)
            {
                weightsMap.TryGetValue(u.Id, out var weight);
                dtos.Add(new UserDto
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email!,
                    Role = u.Role.ToString(),
                    Goal = u.Goal,
                    IsApproved = u.IsApproved,
                    PlainPassword = u.PlainPassword,
                    PhoneNumber = u.PhoneNumber,
                    LastWeight = weight
                });
            }
            return dtos;
        }
    }
}
