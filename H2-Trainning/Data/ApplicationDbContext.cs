using H2_Trainning.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace H2_Trainning.Data
{
    public class ApplicationDbContext : IdentityDbContext<AppUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<H2_Trainning.Models.Program> Programs { get; set; }
        public DbSet<ProgramDay> ProgramDays { get; set; }
        public DbSet<Exercise> Exercises { get; set; }
        public DbSet<Meal> Meals { get; set; }
        public DbSet<Assignment> Assignments { get; set; }
        public DbSet<ExerciseLog> ExerciseLogs { get; set; }
        public DbSet<AvailabilitySlot> AvailabilitySlots { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<CheckIn> CheckIns { get; set; }
        public DbSet<PostLike> PostLikes { get; set; }
        public DbSet<PostComment> PostComments { get; set; }
        public DbSet<WeightHistoryLog> WeightHistoryLogs { get; set; }
        public DbSet<CoachMessage> CoachMessages { get; set; }
        public DbSet<PostRead> PostReads { get; set; }
        public DbSet<BodyWeightEntry> BodyWeightEntries { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<CoachMessage>().HasOne(m => m.Coach).WithMany()
                .HasForeignKey(m => m.CoachId).OnDelete(DeleteBehavior.NoAction);
            builder.Entity<CoachMessage>().HasOne(m => m.Client).WithMany()
                .HasForeignKey(m => m.ClientId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<CoachMessage>().HasIndex(m => new { m.ClientId, m.CreatedAt });
            builder.Entity<PostRead>().HasKey(r => new { r.ClientId, r.PostId });
            builder.Entity<PostRead>().HasOne(r => r.Client).WithMany()
                .HasForeignKey(r => r.ClientId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<PostRead>().HasOne(r => r.Post).WithMany()
                .HasForeignKey(r => r.PostId).OnDelete(DeleteBehavior.NoAction);
            builder.Entity<BodyWeightEntry>().Property(w => w.WeightKg).HasPrecision(18, 2);
            builder.Entity<BodyWeightEntry>().HasIndex(w => new { w.ClientId, w.CreatedAt });

            // 1. Configuration dial les Reservations (Cascade Restrict)
            builder.Entity<Reservation>()
                .HasOne(r => r.Client)
                .WithMany(u => u.ClientReservations)
                .HasForeignKey(r => r.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Reservation>()
                .HasOne(r => r.Coach)
                .WithMany(u => u.CoachReservations)
                .HasForeignKey(r => r.CoachId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ProgramDay>()
                .HasOne(pd => pd.Program)
                .WithMany(p => p.Days)
                .HasForeignKey(pd => pd.ProgramId)
                .OnDelete(DeleteBehavior.Cascade);

            // 2. Fix Multiple Cascade Paths dial l-Assignments (L-mouchkil li kan tla3 lik)
            builder.Entity<Assignment>()
                .HasOne(a => a.Client)
                .WithMany(u => u.Assignments)
                .HasForeignKey(a => a.ClientId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Assignment>()
                .HasOne(a => a.Program)
                .WithMany(p => p.Assignments)
                .HasForeignKey(a => a.ProgramId)
                .OnDelete(DeleteBehavior.NoAction);

            // 3. Conversion dial les Enums (Bach t-hénna mn les types errors)
            builder.Entity<Exercise>().Property(e => e.MediaType).HasConversion<string>();
            builder.Entity<AppUser>().Property(u => u.Role).HasConversion<string>();
            builder.Entity<Assignment>().Property(a => a.Status).HasConversion<string>(); // <-- Zid hadi darouri
            builder.Entity<Reservation>().Property(r => r.Status).HasConversion<string>();
            builder.Entity<AppUser>().Property(u => u.HeightCm).HasPrecision(6, 2);
            builder.Entity<AppUser>().Property(u => u.WaistCm).HasPrecision(6, 2);
            builder.Entity<AppUser>().Property(u => u.ArmCm).HasPrecision(6, 2);
            builder.Entity<AppUser>().Property(u => u.ThighCm).HasPrecision(6, 2);
            builder.Entity<AppUser>().Property(u => u.ChestCm).HasPrecision(6, 2);
            builder.Entity<CheckIn>().Property(c => c.Weight).HasPrecision(18, 2);

            // 4. Performance indexes for frequently queried foreign keys
            builder.Entity<Assignment>().HasIndex(a => a.ClientId);
            builder.Entity<Assignment>().HasIndex(a => a.ProgramId);
            builder.Entity<AppUser>().HasIndex(u => u.CoachId);
            builder.Entity<Post>().HasIndex(p => p.CoachId);
            builder.Entity<CheckIn>().HasIndex(c => c.ClientId);

            // 5. Post constraints to avoid cascade delete cycles
            builder.Entity<PostLike>()
                .HasOne(l => l.Post)
                .WithMany(p => p.Likes)
                .HasForeignKey(l => l.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PostLike>()
                .HasOne(l => l.User)
                .WithMany() // AppUser doesn't need to navigate to Likes
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.NoAction); // Important to avoid multiple cascade paths

            builder.Entity<PostComment>()
                .HasOne(c => c.Post)
                .WithMany(p => p.Comments)
                .HasForeignKey(c => c.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PostComment>()
                .HasOne(c => c.User)
                .WithMany() // AppUser doesn't need to navigate to Comments
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.NoAction); // Important to avoid multiple cascade paths

            // 6. WeightHistoryLog constraints
            builder.Entity<WeightHistoryLog>()
                .HasOne(w => w.Client)
                .WithMany(u => u.WeightHistoryLogs)
                .HasForeignKey(w => w.ClientId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<WeightHistoryLog>()
                .HasOne(w => w.Exercise)
                .WithMany()
                .HasForeignKey(w => w.ExerciseId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
