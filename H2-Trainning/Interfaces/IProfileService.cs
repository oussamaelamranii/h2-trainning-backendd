using H2_Trainning.Dtos;

namespace H2_Trainning.Interfaces
{
    public interface IProfileService
    {
        Task<UserProfileDto?> GetAsync(string requesterId, string profileUserId, bool requesterIsCoach);
        Task<UserProfileDto?> UpdateAsync(string userId, UpdateProfileDto dto);
        Task<UserProfileDto?> UpdateClientAsync(string coachId, string clientId, UpdateProfileDto dto);
    }
}
