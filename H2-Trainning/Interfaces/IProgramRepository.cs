using H2_Trainning.Models;
using H2_Trainning.Dtos;

namespace H2_Trainning.Interfaces
{
    public interface IProgramRepository
    {
        Task<List<H2_Trainning.Models.Program>> GetByCoachIdAsync(string coachId);
        Task<List<ProgramSummaryDto>> GetSummariesByCoachIdAsync(string coachId);
        Task<H2_Trainning.Models.Program?> GetByIdAsync(int id);
        Task<H2_Trainning.Models.Program> CreateAsync(H2_Trainning.Models.Program program);
        Task<H2_Trainning.Models.Program> UpdateAsync(H2_Trainning.Models.Program program);
        Task<bool> UpdateExerciseMediaAsync(int programId, string coachId, int exerciseId, string dataUrl, MediaType mediaType);
        Task<bool> UpdateMealPhotoAsync(int programId, string coachId, int mealId, string dataUrl);
        Task<bool> DeleteAsync(int id);
    }
}
