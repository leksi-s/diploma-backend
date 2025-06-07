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

			// Seed data
			SeedData(modelBuilder);
		}

		private void SeedData(ModelBuilder modelBuilder)
		{
			// Create test users
			var adminId = Guid.NewGuid();
			var specialist1Id = Guid.NewGuid();
			var specialist2Id = Guid.NewGuid();
			var client1Id = Guid.NewGuid();

			modelBuilder.Entity<User>().HasData(
				new User
				{
					Id = adminId,
					FirstName = "Admin",
					LastName = "User",
					Email = "admin@psychapp.com",
					Phone = "+380501234567",
					PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
					Role = "Admin"
				},
				new User
				{
					Id = specialist1Id,
					FirstName = "Anna",
					LastName = "Kovalenko",
					Email = "anna@psychapp.com",
					Phone = "+380507654321",
					PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
					Role = "Specialist"
				},
				new User
				{
					Id = specialist2Id,
					FirstName = "Petro",
					LastName = "Ivanov",
					Email = "petro@psychapp.com",
					Phone = "+380509876543",
					PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
					Role = "Specialist"
				},
				new User
				{
					Id = client1Id,
					FirstName = "Oleksandr",
					LastName = "Petrenko",
					Email = "client@psychapp.com",
					Phone = "+380661234567",
					PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
					Role = "Client"
				}
			);

			// Create specialists
			modelBuilder.Entity<Specialist>().HasData(
				new Specialist
				{
					Id = Guid.NewGuid(),
					UserId = specialist1Id,
					Education = "PhD Psychology, KNU",
					Experience = "8 years",
					Specialization = "Anxiety",
					Price = 800,
					Online = true,
					Offline = true,
					Gender = "Female",
					Language = "Ukrainian",
					IsActive = true
				},
				new Specialist
				{
					Id = Guid.NewGuid(),
					UserId = specialist2Id,
					Education = "Master Family Therapy",
					Experience = "12 years",
					Specialization = "Relationships",
					Price = 1200,
					Online = false,
					Offline = true,
					Gender = "Male",
					Language = "Ukrainian",
					IsActive = true
				}
			);

			// Create client
			modelBuilder.Entity<Client>().HasData(
				new Client
				{
					Id = Guid.NewGuid(),
					UserId = client1Id,
					Budget = 1000,
					PreferOnline = true,
					PreferOffline = false,
					PreferredGender = "Female",
					PreferredLanguage = "Ukrainian",
					Issue = "Anxiety"
				}
			);
		}
	}
}