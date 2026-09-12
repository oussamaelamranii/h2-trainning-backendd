using H2_Trainning.Data;
using H2_Trainning.Interfaces;
using H2_Trainning.Models;
using Microsoft.EntityFrameworkCore;

namespace H2_Trainning.Repositories
{
    public class CheckInRepository : ICheckInRepository
    {
        private readonly ApplicationDbContext _context;

        public CheckInRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<CheckIn>> GetByClientIdAsync(string clientId)
        {
            return await _context.CheckIns
                .Include(c => c.Client)
                .Where(c => c.ClientId == clientId)
                .OrderByDescending(c => c.CreatedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<CheckIn?> GetLatestByClientIdAsync(string clientId)
        {
            return await _context.CheckIns
                .Include(c => c.Client)
                .Where(c => c.ClientId == clientId)
                .OrderByDescending(c => c.CreatedAt)
                .AsNoTracking()
                .FirstOrDefaultAsync();
        }

        public async Task<CheckIn> CreateAsync(CheckIn checkIn)
        {
            _context.CheckIns.Add(checkIn);
            await _context.SaveChangesAsync();
            return (await _context.CheckIns
                .Include(c => c.Client)
                .FirstOrDefaultAsync(c => c.Id == checkIn.Id))!;
        }

        public async Task<Dictionary<string, decimal?>> GetLatestWeightsForClientsAsync(List<string> clientIds)
        {
            if (clientIds == null || !clientIds.Any())
                return new Dictionary<string, decimal?>();

            var latestCheckIns = await _context.CheckIns
                .Where(c => clientIds.Contains(c.ClientId))
                .AsNoTracking()
                .GroupBy(c => c.ClientId)
                .Select(g => g.OrderByDescending(c => c.CreatedAt).FirstOrDefault())
                .ToListAsync();

            return latestCheckIns
                .Where(c => c != null)
                .ToDictionary(c => c!.ClientId, c => (decimal?)c!.Weight);
        }
    }
}
