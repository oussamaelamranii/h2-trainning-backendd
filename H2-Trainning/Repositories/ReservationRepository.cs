using H2_Trainning.Data;
using H2_Trainning.Interfaces;
using H2_Trainning.Models;
using Microsoft.EntityFrameworkCore;

namespace H2_Trainning.Repositories
{
    public class ReservationRepository : IReservationRepository
    {
        private readonly ApplicationDbContext _context;

        public ReservationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Reservation>> GetByClientIdAsync(string clientId)
        {
            return await _context.Reservations
                .Include(r => r.Client)
                .Include(r => r.Coach)
                .Include(r => r.Slot)
                .Where(r => r.ClientId == clientId)
                .OrderByDescending(r => r.CreatedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<Reservation>> GetByCoachIdAsync(string coachId)
        {
            return await _context.Reservations
                .Include(r => r.Client)
                .Include(r => r.Coach)
                .Include(r => r.Slot)
                .Where(r => r.CoachId == coachId)
                .OrderByDescending(r => r.CreatedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Reservation?> GetByIdAsync(int id)
        {
            return await _context.Reservations
                .Include(r => r.Client)
                .Include(r => r.Coach)
                .Include(r => r.Slot)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<Reservation> CreateAsync(Reservation reservation)
        {
            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();
            return reservation;
        }

        public async Task<Reservation> UpdateAsync(Reservation reservation)
        {
            _context.Reservations.Update(reservation);
            await _context.SaveChangesAsync();
            return reservation;
        }
    }
}
