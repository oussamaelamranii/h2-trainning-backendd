using H2_Trainning.Dtos;
using H2_Trainning.Enums;
using H2_Trainning.Interfaces;

namespace H2_Trainning.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IAssignmentRepository _assignmentRepo;
        private readonly IClientRepository _clientRepo;

        public DashboardService(IAssignmentRepository assignmentRepo, IClientRepository clientRepo)
        {
            _assignmentRepo = assignmentRepo;
            _clientRepo = clientRepo;
        }

        public async Task<CoachDashboardDto> GetCoachDashboardAsync(string coachId)
        {
            // Note: queries must be sequential because EF Core DbContext is not thread-safe
            var clients = await _clientRepo.GetClientsByCoachIdAsync(coachId);
            var assignments = await _assignmentRepo.GetRecentByCoachIdAsync(coachId, 20);

            var activeAssignments = assignments.Where(a => a.Status != AssignmentStatus.Completed).ToList();
            var viewedAssignments = assignments.Where(a => a.Status == AssignmentStatus.Viewed).ToList();

            return new CoachDashboardDto
            {
                TotalClients = clients.Count,
                ActiveAssignments = activeAssignments.Count,
                ViewedAssignments = viewedAssignments.Count,
                RecentAssignments = assignments.Take(5).Select(a => new AssignmentDto
                {
                    Id = a.Id,
                    ClientId = a.ClientId,
                    ClientName = a.Client?.FullName ?? "",
                    ProgramId = a.ProgramId,
                    ProgramTitle = a.Program?.Title ?? "",
                    AssignedDate = a.AssignedDate,
                    Status = a.Status.ToString(),
                    LastChecked = a.LastChecked
                }).ToList()
            };
        }

        public async Task<ClientDashboardDto> GetClientDashboardAsync(string clientId, bool includeMedia = true)
        {
            // Fetch only the latest assignment instead of all assignments
            var latest = await _assignmentRepo.GetLatestByClientIdAsync(clientId);

            if (latest == null)
            {
                return new ClientDashboardDto
                {
                    CurrentProgram = null,
                    AssignmentStatus = null,
                    AssignmentId = null
                };
            }

            var program = latest.Program;

            return new ClientDashboardDto
            {
                AssignmentId = latest.Id,
                AssignmentStatus = latest.Status.ToString(),
                LastChecked = latest.LastChecked,
                CurrentProgram = program == null ? null : new ProgramDto
                {
                    Id = program.Id,
                    Title = program.Title,
                    Description = program.Description,
                    NutritionalBases = program.NutritionalBases,
                    CoachId = program.CoachId,
                    CreatedAt = program.CreatedAt,
                    UpdatedAt = program.UpdatedAt,
                    Days = program.Days?.Select(d => new ProgramDayDto
                    {
                        Id = d.Id,
                        Name = d.Name,
                        DayNumber = d.DayNumber,
                        IsRestDay = d.IsRestDay,
                        Exercises = d.Exercises?.Select(e => {
                            var log = latest.ExerciseLogs?.FirstOrDefault(l => l.ExerciseId == e.Id);
                            return new ExerciseDto
                            {
                                Id = e.Id,
                                Name = e.Name,
                                Sets = e.Sets,
                                Reps = e.Reps,
                                Notes = e.Notes,
                                 MediaUrl = includeMedia ? e.MediaUrl : null,
                                 MediaType = includeMedia ? e.MediaType?.ToString() : null,
                                SortOrder = e.SortOrder,
                                IsCompleted = log?.IsCompleted,
                                ClientNotes = log?.ClientNotes,
                                Weight = log?.Weight
                            };
                        }).ToList() ?? new(),
                        Meals = d.Meals?.Select(m => new MealDto
                        {
                            Id = m.Id,
                            Name = m.Name,
                            Macros = m.Macros,
                            Time = m.Time,
                             PhotoUrl = includeMedia ? m.PhotoUrl : null,
                            Ingredients = m.Ingredients,
                            SortOrder = m.SortOrder
                        }).ToList() ?? new()
                    }).ToList() ?? new()
                }
            };
        }

        public async Task UpdateLastCheckedAsync(string clientId)
        {
            var latest = await _assignmentRepo.GetLatestByClientIdAsync(clientId);
            if (latest != null)
            {
                latest.LastChecked = DateTime.UtcNow;
                await _assignmentRepo.UpdateAsync(latest);
            }
        }
    }
}
