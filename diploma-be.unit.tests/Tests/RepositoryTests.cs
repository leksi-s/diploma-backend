using diploma_be.dal;
using diploma_be.dal.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace diploma_be.unit.tests.Tests
{
	public class RepositoryTests : IDisposable
	{
		private readonly AppDbContext _context;

		public RepositoryTests()
		{
			var options = new DbContextOptionsBuilder<AppDbContext>()
				.UseInMemoryDatabase(databaseName: $"RepositoryTestDb_{Guid.NewGuid()}")
				.Options;

			_context = new AppDbContext(options);
		}

		[Fact]
		public async Task AddUser_ShouldPersistToDatabase()
		{
			// Arrange
			var user = new User
			{
				Id = Guid.NewGuid(),
				FirstName = "Test",
				LastName = "User",
				Email = "test@example.com",
				Phone = "+380501234567",
				PasswordHash = "hash",
				Role = "Client"
			};

			// Act
			_context.Users.Add(user);
			await _context.SaveChangesAsync();

			// Assert
			var savedUser = await _context.Users.FindAsync(user.Id);
			savedUser.Should().NotBeNull();
			savedUser!.Email.Should().Be("test@example.com");
		}

		[Fact]
		public async Task UpdateUser_ShouldPersistChanges()
		{
			// Arrange
			var userId = Guid.NewGuid();
			var user = new User
			{
				Id = userId,
				FirstName = "Original",
				LastName = "Name",
				Email = "original@example.com",
				Phone = "+380501234567",
				PasswordHash = "hash",
				Role = "Client"
			};

			_context.Users.Add(user);
			await _context.SaveChangesAsync();

			// Act
			user.FirstName = "Updated";
			user.LastName = "UserName";
			await _context.SaveChangesAsync();

			// Assert
			var updatedUser = await _context.Users.FindAsync(userId);
			updatedUser.Should().NotBeNull();
			updatedUser!.FirstName.Should().Be("Updated");
			updatedUser.LastName.Should().Be("UserName");
		}

		[Fact]
		public async Task DeleteUser_ShouldRemoveFromDatabase()
		{
			// Arrange
			var userId = Guid.NewGuid();
			var user = new User
			{
				Id = userId,
				FirstName = "ToDelete",
				LastName = "User",
				Email = "delete@example.com",
				Phone = "+380501234567",
				PasswordHash = "hash",
				Role = "Client"
			};

			_context.Users.Add(user);
			await _context.SaveChangesAsync();

			// Act
			_context.Users.Remove(user);
			await _context.SaveChangesAsync();

			// Assert
			var deletedUser = await _context.Users.FindAsync(userId);
			deletedUser.Should().BeNull();
		}

		[Fact]
		public async Task QueryUsers_ByRole_ShouldReturnFilteredResults()
		{
			// Arrange
			var users = new List<User>
			{
				new User { FirstName = "Client1", Role = "Client", Email = "client1@test.com", Phone = "1", PasswordHash = "" },
				new User { FirstName = "Client2", Role = "Client", Email = "client2@test.com", Phone = "2", PasswordHash = "" },
				new User { FirstName = "Specialist1", Role = "Specialist", Email = "spec1@test.com", Phone = "3", PasswordHash = "" },
				new User { FirstName = "Admin1", Role = "Admin", Email = "admin1@test.com", Phone = "4", PasswordHash = "" }
			};

			_context.Users.AddRange(users);
			await _context.SaveChangesAsync();

			// Act
			var clients = await _context.Users.Where(u => u.Role == "Client").ToListAsync();
			var specialists = await _context.Users.Where(u => u.Role == "Specialist").ToListAsync();
			var admins = await _context.Users.Where(u => u.Role == "Admin").ToListAsync();

			// Assert
			clients.Should().HaveCount(2);
			specialists.Should().HaveCount(1);
			admins.Should().HaveCount(1);
		}

		[Fact]
		public async Task Specialist_WithUser_ShouldLoadNavigationProperty()
		{
			// Arrange
			var userId = Guid.NewGuid();
			var user = new User
			{
				Id = userId,
				FirstName = "Specialist",
				LastName = "User",
				Email = "specialist@example.com",
				Phone = "+380501234567",
				PasswordHash = "hash",
				Role = "Specialist"
			};

			var specialist = new Specialist
			{
				Id = Guid.NewGuid(),
				UserId = userId,
				Education = "PhD",
				Experience = "10 years",
				Specialization = "Anxiety",
				Price = 1000,
				Online = true,
				Offline = true,
				Gender = "Female",
				Language = "Ukrainian",
				IsActive = true
			};

			_context.Users.Add(user);
			_context.Specialists.Add(specialist);
			await _context.SaveChangesAsync();

			// Act
			var loadedSpecialist = await _context.Specialists
				.Include(s => s.User)
				.FirstOrDefaultAsync(s => s.Id == specialist.Id);

			// Assert
			loadedSpecialist.Should().NotBeNull();
			loadedSpecialist!.User.Should().NotBeNull();
			loadedSpecialist.User.FirstName.Should().Be("Specialist");
		}

		[Fact]
		public async Task Appointment_WithClientAndSpecialist_ShouldLoadNavigationProperties()
		{
			// Arrange
			var clientUserId = Guid.NewGuid();
			var specialistUserId = Guid.NewGuid();
			var clientId = Guid.NewGuid();
			var specialistId = Guid.NewGuid();

			var clientUser = new User
			{
				Id = clientUserId,
				FirstName = "Client",
				LastName = "User",
				Email = "client@example.com",
				Phone = "1",
				PasswordHash = "",
				Role = "Client"
			};

			var specialistUser = new User
			{
				Id = specialistUserId,
				FirstName = "Specialist",
				LastName = "User",
				Email = "specialist@example.com",
				Phone = "2",
				PasswordHash = "hash",
				Role = "Specialist"
			};

			var client = new Client
			{
				Id = clientId,
				UserId = clientUserId,
				Budget = 1000,
				PreferOnline = true,
				PreferOffline = false,
				PreferredGender = "Any",
				PreferredLanguage = "Ukrainian",
				Issue = "General"
			};

			var specialist = new Specialist
			{
				Id = specialistId,
				UserId = specialistUserId,
				Education = "Master",
				Experience = "5 years",
				Specialization = "General",
				Price = 800,
				Online = true,
				Offline = false,
				Gender = "Female",
				Language = "Ukrainian",
				IsActive = true
			};

			var appointment = new Appointment
			{
				Id = Guid.NewGuid(),
				ClientId = clientId,
				SpecialistId = specialistId,
				AppointmentDate = DateTime.UtcNow.AddDays(7),
				IsOnline = true,
				Status = "Scheduled",
				Notes = "First session"
			};

			_context.Users.AddRange(clientUser, specialistUser);
			_context.Clients.Add(client);
			_context.Specialists.Add(specialist);
			_context.Appointments.Add(appointment);
			await _context.SaveChangesAsync();

			// Act
			var loadedAppointment = await _context.Appointments
				.Include(a => a.Client)
					.ThenInclude(c => c.User)
				.Include(a => a.Specialist)
					.ThenInclude(s => s.User)
				.FirstOrDefaultAsync(a => a.Id == appointment.Id);

			// Assert
			loadedAppointment.Should().NotBeNull();
			loadedAppointment!.Client.Should().NotBeNull();
			loadedAppointment.Client.User.Should().NotBeNull();
			loadedAppointment.Client.User.FirstName.Should().Be("Client");
			loadedAppointment.Specialist.Should().NotBeNull();
			loadedAppointment.Specialist.User.Should().NotBeNull();
			loadedAppointment.Specialist.User.FirstName.Should().Be("Specialist");
		}

		[Fact]
		public async Task ComplexQuery_FilterSpecialistsByMultipleCriteria()
		{
			// Arrange
			await SeedSpecialistsForComplexQuery();

			// Act
			var query = _context.Specialists
				.Include(s => s.User)
				.Where(s => s.IsActive)
				.Where(s => s.Price <= 1000)
				.Where(s => s.Online == true)
				.Where(s => s.Language == "Ukrainian")
				.OrderBy(s => s.Price);

			var results = await query.ToListAsync();

			// Assert
			results.Should().NotBeEmpty();
			results.Should().OnlyContain(s => s.IsActive);
			results.Should().OnlyContain(s => s.Price <= 1000);
			results.Should().OnlyContain(s => s.Online);
			results.Should().OnlyContain(s => s.Language == "Ukrainian");
			results.Should().BeInAscendingOrder(s => s.Price);
		}

		[Fact]
		public async Task CascadeDelete_RemovingUser_ShouldRemoveRelatedEntities()
		{
			// Arrange
			var userId = Guid.NewGuid();
			var clientId = Guid.NewGuid();

			var user = new User
			{
				Id = userId,
				FirstName = "ToDelete",
				LastName = "User",
				Email = "cascade@test.com",
				Phone = "+380501234567",
				PasswordHash = "",
				Role = "Client"
			};

			var client = new Client
			{
				Id = clientId,
				UserId = userId,
				Budget = 1000,
				PreferOnline = true,
				PreferOffline = false,
				PreferredGender = "Any",
				PreferredLanguage = "Ukrainian",
				Issue = "Test"
			};

			_context.Users.Add(user);
			_context.Clients.Add(client);
			await _context.SaveChangesAsync();

			// Act
			_context.Users.Remove(user);
			await _context.SaveChangesAsync();

			// Assert
			var deletedUser = await _context.Users.FindAsync(userId);
			var deletedClient = await _context.Clients.FindAsync(clientId);

			deletedUser.Should().BeNull();
			deletedClient.Should().BeNull(); // Should be cascade deleted
		}

		[Fact]
		public async Task Count_WithFilters_ShouldReturnCorrectNumber()
		{
			// Arrange
			var specialists = new List<Specialist>
			{
				CreateSpecialist(800, true, true, "Ukrainian", true),
				CreateSpecialist(1200, true, false, "English", true),
				CreateSpecialist(600, false, true, "Ukrainian", true),
				CreateSpecialist(1000, true, true, "Ukrainian", false), // Inactive
            };

			foreach (var specialist in specialists)
			{
				var user = new User
				{
					Id = specialist.UserId,
					FirstName = "Test",
					LastName = "Specialist",
					Email = $"{Guid.NewGuid()}@test.com",
					Phone = Guid.NewGuid().ToString().Substring(0, 10),
					PasswordHash = "hash",
					Role = "Specialist"
				};
				_context.Users.Add(user);
				_context.Specialists.Add(specialist);
			}

			await _context.SaveChangesAsync();

			// Act
			var totalCount = await _context.Specialists.CountAsync();
			var activeCount = await _context.Specialists.CountAsync(s => s.IsActive);
			var onlineUkrainianCount = await _context.Specialists
				.CountAsync(s => s.IsActive && s.Online && s.Language == "Ukrainian");

			// Assert
			totalCount.Should().Be(4);
			activeCount.Should().Be(3);
			onlineUkrainianCount.Should().Be(1);
		}

		[Fact]
		public async Task GroupBy_AppointmentsByStatus_ShouldGroupCorrectly()
		{
			// Arrange
			var appointments = new List<Appointment>
			{
				CreateAppointment("Scheduled"),
				CreateAppointment("Scheduled"),
				CreateAppointment("Completed"),
				CreateAppointment("Completed"),
				CreateAppointment("Completed"),
				CreateAppointment("Cancelled"),
				CreateAppointment("NoShow")
			};

			_context.Appointments.AddRange(appointments);
			await _context.SaveChangesAsync();

			// Act
			var groupedAppointments = await _context.Appointments
				.GroupBy(a => a.Status)
				.Select(g => new { Status = g.Key, Count = g.Count() })
				.ToListAsync();

			// Assert
			groupedAppointments.Should().HaveCount(4);
			groupedAppointments.Should().Contain(g => g.Status == "Scheduled" && g.Count == 2);
			groupedAppointments.Should().Contain(g => g.Status == "Completed" && g.Count == 3);
			groupedAppointments.Should().Contain(g => g.Status == "Cancelled" && g.Count == 1);
			groupedAppointments.Should().Contain(g => g.Status == "NoShow" && g.Count == 1);
		}

		[Fact]
		public async Task DateTimeQuery_ShouldFilterCorrectly()
		{
			// Arrange
			var now = DateTime.UtcNow;
			var appointments = new List<Appointment>
			{
				CreateAppointmentWithDate(now.AddDays(-7)), // Past
                CreateAppointmentWithDate(now.AddDays(-1)), // Yesterday
                CreateAppointmentWithDate(now.AddDays(1)),  // Tomorrow
                CreateAppointmentWithDate(now.AddDays(7)),  // Next week
                CreateAppointmentWithDate(now.AddDays(30))  // Next month
            };

			_context.Appointments.AddRange(appointments);
			await _context.SaveChangesAsync();

			// Act
			var futureAppointments = await _context.Appointments
				.Where(a => a.AppointmentDate > now)
				.OrderBy(a => a.AppointmentDate)
				.ToListAsync();

			var pastAppointments = await _context.Appointments
				.Where(a => a.AppointmentDate < now)
				.ToListAsync();

			// Assert
			futureAppointments.Should().HaveCount(3);
			pastAppointments.Should().HaveCount(2);
			futureAppointments.Should().BeInAscendingOrder(a => a.AppointmentDate);
		}

		// Helper methods
		private async Task SeedSpecialistsForComplexQuery()
		{
			var specialists = new List<(User user, Specialist specialist)>
			{
				(
					new User { Id = Guid.NewGuid(), FirstName = "Anna", LastName = "K", Email = "anna@test.com", Phone = "1", PasswordHash = "hash", Role = "Specialist" },
					new Specialist { Id = Guid.NewGuid(), UserId = Guid.Empty, Education = "PhD", Experience = "5y", Specialization = "Anxiety", Price = 800, Online = true, Offline = true, Gender = "Female", Language = "Ukrainian", IsActive = true }
				),
				(
					new User { Id = Guid.NewGuid(), FirstName = "Boris", LastName = "P", Email = "boris@test.com", Phone = "2", PasswordHash = "hash", Role = "Specialist" },
					new Specialist { Id = Guid.NewGuid(), UserId = Guid.Empty, Education = "Master", Experience = "10y", Specialization = "Depression", Price = 1200, Online = false, Offline = true, Gender = "Male", Language = "English", IsActive = true }
				),
				(
					new User { Id = Guid.NewGuid(), FirstName = "Clara", LastName = "M", Email = "clara@test.com", Phone = "3", PasswordHash = "hash", Role = "Specialist" },
					new Specialist { Id = Guid.NewGuid(), UserId = Guid.Empty, Education = "PhD", Experience = "3y", Specialization = "Relationships", Price = 900, Online = true, Offline = false, Gender = "Female", Language = "Ukrainian", IsActive = true }
				)
			};

			foreach (var (user, specialist) in specialists)
			{
				specialist.UserId = user.Id;
				_context.Users.Add(user);
				_context.Specialists.Add(specialist);
			}

			await _context.SaveChangesAsync();
		}

		private Specialist CreateSpecialist(decimal price, bool online, bool offline, string language, bool isActive)
		{
			return new Specialist
			{
				Id = Guid.NewGuid(),
				UserId = Guid.NewGuid(),
				Education = "Test Education",
				Experience = "Test Experience",
				Specialization = "Test Specialization",
				Price = price,
				Online = online,
				Offline = offline,
				Gender = "Other",
				Language = language,
				IsActive = isActive
			};
		}

		private Appointment CreateAppointment(string status)
		{
			return new Appointment
			{
				Id = Guid.NewGuid(),
				ClientId = Guid.NewGuid(),
				SpecialistId = Guid.NewGuid(),
				AppointmentDate = DateTime.UtcNow.AddDays(7),
				IsOnline = true,
				Status = status,
				Notes = $"Test appointment with status {status}"
			};
		}

		private Appointment CreateAppointmentWithDate(DateTime date)
		{
			return new Appointment
			{
				Id = Guid.NewGuid(),
				ClientId = Guid.NewGuid(),
				SpecialistId = Guid.NewGuid(),
				AppointmentDate = date,
				IsOnline = true,
				Status = "Scheduled",
				Notes = "Test appointment"
			};
		}

		public void Dispose()
		{
			_context.Dispose();
		}
	}
}