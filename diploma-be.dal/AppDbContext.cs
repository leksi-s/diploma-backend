using diploma_be.dal.Entities;
using Microsoft.EntityFrameworkCore;

namespace diploma_be.dal
{
	public class AppDbContext : DbContext
	{
		public DbSet<User> Users { get; set; }
		public DbSet<Specialist> Specialists { get; set; }
		public DbSet<Client> Clients { get; set; }
		public DbSet<Appointment> Appointments { get; set; }

		public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
		{
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			// Явно вказуємо імена таблиць з лапками
			modelBuilder.Entity<User>().ToTable("Users");
			modelBuilder.Entity<Specialist>().ToTable("Specialists");
			modelBuilder.Entity<Client>().ToTable("Clients");
			modelBuilder.Entity<Appointment>().ToTable("Appointments");

			// User configuration
			modelBuilder.Entity<User>()
				.HasIndex(u => u.Email)
				.IsUnique();

			// Specialist configuration
			modelBuilder.Entity<Specialist>()
				.HasOne(s => s.User)
				.WithMany()
				.HasForeignKey(s => s.UserId)
				.OnDelete(DeleteBehavior.Cascade);

			// Client configuration
			modelBuilder.Entity<Client>()
				.HasOne(c => c.User)
				.WithMany()
				.HasForeignKey(c => c.UserId)
				.OnDelete(DeleteBehavior.Cascade);

			// Appointment configuration
			modelBuilder.Entity<Appointment>()
				.HasOne(a => a.Client)
				.WithMany()
				.HasForeignKey(a => a.ClientId)
				.OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<Appointment>()
				.HasOne(a => a.Specialist)
				.WithMany()
				.HasForeignKey(a => a.SpecialistId)
				.OnDelete(DeleteBehavior.Restrict);
		}
	}
}