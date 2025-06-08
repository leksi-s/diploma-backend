using diploma_be.api.Controllers;
using diploma_be.bll.Models;
using diploma_be.dal;
using diploma_be.dal.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace diploma_be.integration.tests.Tests
{
	public class AdminControllerTests : IDisposable
	{
		private readonly AppDbContext _context;
		private readonly AdminController _controller;

		public AdminControllerTests()
		{
			var options = new DbContextOptionsBuilder<AppDbContext>()
				.UseInMemoryDatabase(databaseName: $"AdminTestDb_{Guid.NewGuid()}")
				.Options;

			_context = new AppDbContext(options);
			_controller = new AdminController(_context);

			SeedTestData();
		}

		private void SeedTestData()
		{
			var specialist1UserId = Guid.NewGuid();
			var specialist2UserId = Guid.NewGuid();
			var clientUserId = Guid.NewGuid();

			var users = new List<User>
			{
				new User
				{
					Id = specialist1UserId,
					FirstName = "Anna",
					LastName = "Specialist",
					Email = "anna@test.com",
					Phone = "+123456789",
					PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
					Role = "Specialist",
					CreatedAt = DateTime.UtcNow
				},
				new User
				{
					Id = specialist2UserId,
					FirstName = "John",
					LastName = "Therapist",
					Email = "john@test.com",
					Phone = "+123456790",
					PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
					Role = "Specialist",
					CreatedAt = DateTime.UtcNow
				},
				new User
				{
					Id = clientUserId,
					FirstName = "Test",
					LastName = "Client",
					Email = "client@test.com",
					Phone = "+123456791",
					PasswordHash = "",
					Role = "Client",
					CreatedAt = DateTime.UtcNow
				}
			};

			var specialists = new List<Specialist>
			{
				new Specialist
				{
					Id = Guid.NewGuid(),
					UserId = specialist1UserId,
					Education = "PhD Psychology",
					Experience = "8 years",
					Specialization = "Anxiety",
					Price = 800,
					Online = true,
					Offline = true,
					Gender = "Female",
					Language = "English",
					IsActive = true,
					CreatedAt = DateTime.UtcNow
				},
				new Specialist
				{
					Id = Guid.NewGuid(),
					UserId = specialist2UserId,
					Education = "Master Family Therapy",
					Experience = "10 years",
					Specialization = "Relationships",
					Price = 1200,
					Online = false,
					Offline = true,
					Gender = "Male",
					Language = "Ukrainian",
					IsActive = false, // Inactive specialist for testing
                    CreatedAt = DateTime.UtcNow
				}
			};

			var client = new Client
			{
				Id = Guid.NewGuid(),
				UserId = clientUserId,
				Budget = 1000,
				PreferOnline = true,
				PreferOffline = false,
				PreferredGender = "Female",
				PreferredLanguage = "English",
				Issue = "Anxiety",
				CreatedAt = DateTime.UtcNow
			};

			_context.Users.AddRange(users);
			_context.Specialists.AddRange(specialists);
			_context.Clients.Add(client);
			_context.SaveChanges();
		}

		[Fact]
		public async Task GetAllSpecialists_ShouldReturnAllSpecialists()
		{
			// Act
			var result = await _controller.GetAllSpecialists();

			// Assert
			result.Should().NotBeNull();
			var okResult = result.Result as OkObjectResult;
			okResult.Should().NotBeNull();

			var specialists = okResult!.Value as List<SpecialistDto>;
			specialists.Should().NotBeNull();
			specialists!.Should().HaveCount(2); // Both active and inactive

			// Verify both specialists are returned
			specialists.Should().Contain(s => s.FirstName == "Anna");
			specialists.Should().Contain(s => s.FirstName == "John");
		}

		[Fact]
		public async Task GetSpecialist_WithValidId_ShouldReturnSpecialist()
		{
			// Arrange
			var specialistId = _context.Specialists.First().Id;

			// Act
			var result = await _controller.GetSpecialist(specialistId);

			// Assert
			result.Should().NotBeNull();
			var okResult = result.Result as OkObjectResult;
			okResult.Should().NotBeNull();

			var specialist = okResult!.Value as SpecialistDto;
			specialist.Should().NotBeNull();
			specialist!.Id.Should().Be(specialistId);
		}

		[Fact]
		public async Task GetSpecialist_WithInvalidId_ShouldReturnNotFound()
		{
			// Arrange
			var invalidId = Guid.NewGuid();

			// Act
			var result = await _controller.GetSpecialist(invalidId);

			// Assert
			result.Should().NotBeNull();
			var notFoundResult = result.Result as NotFoundResult;
			notFoundResult.Should().NotBeNull();
		}

		[Fact]
		public async Task CreateSpecialist_WithValidData_ShouldCreateSpecialistAndUser()
		{
			// Arrange
			var createRequest = new CreateSpecialistRequest
			{
				FirstName = "New",
				LastName = "Specialist",
				Email = "newspecialist@test.com",
				Phone = "+123456799",
				Password = "password123",
				Education = "Master Psychology",
				Experience = "5 years",
				Specialization = "Depression",
				Price = 900,
				Online = true,
				Offline = false,
				Gender = "Female",
				Language = "Ukrainian"
			};

			// Act
			var result = await _controller.CreateSpecialist(createRequest);

			// Assert
			result.Should().NotBeNull();
			var okResult = result.Result as OkObjectResult;
			okResult.Should().NotBeNull();

			var specialist = okResult!.Value as SpecialistDto;
			specialist.Should().NotBeNull();
			specialist!.Email.Should().Be("newspecialist@test.com");
			specialist.Specialization.Should().Be("Depression");
			specialist.Price.Should().Be(900);

			// Verify user was created in database
			var createdUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "newspecialist@test.com");
			createdUser.Should().NotBeNull();
			createdUser!.Role.Should().Be("Specialist");

			// Verify specialist profile was created
			var createdSpecialist = await _context.Specialists.FirstOrDefaultAsync(s => s.UserId == createdUser.Id);
			createdSpecialist.Should().NotBeNull();
			createdSpecialist!.IsActive.Should().BeTrue(); // Admin-created specialists are active
		}

		[Fact]
		public async Task CreateSpecialist_WithExistingEmail_ShouldReturnBadRequest()
		{
			// Arrange
			var createRequest = new CreateSpecialistRequest
			{
				FirstName = "Duplicate",
				LastName = "Specialist",
				Email = "anna@test.com", // Email already exists
				Phone = "+123456798",
				Password = "password123",
				Education = "Master Psychology",
				Experience = "3 years",
				Specialization = "Stress",
				Price = 700,
				Online = true,
				Offline = true,
				Gender = "Female",
				Language = "English"
			};

			// Act
			var result = await _controller.CreateSpecialist(createRequest);

			// Assert
			result.Should().NotBeNull();
			var badRequestResult = result.Result as BadRequestObjectResult;
			badRequestResult.Should().NotBeNull();
			badRequestResult!.Value.Should().Be("Email already exists");
		}

		[Fact]
		public async Task UpdateSpecialist_WithValidData_ShouldUpdateSpecialist()
		{
			// Arrange
			var specialistId = _context.Specialists.First().Id;
			var updateRequest = new UpdateSpecialistRequest
			{
				Education = "Updated Education",
				Experience = "Updated Experience",
				Specialization = "Updated Specialization",
				Price = 950,
				Online = false,
				Offline = true,
				Language = "Updated Language"
			};

			// Act
			var result = await _controller.UpdateSpecialist(specialistId, updateRequest);

			// Assert
			result.Should().NotBeNull();
			var noContentResult = result as NoContentResult;
			noContentResult.Should().NotBeNull();

			// Verify changes were saved
			var updatedSpecialist = await _context.Specialists.FindAsync(specialistId);
			updatedSpecialist.Should().NotBeNull();
			updatedSpecialist!.Education.Should().Be("Updated Education");
			updatedSpecialist.Experience.Should().Be("Updated Experience");
			updatedSpecialist.Specialization.Should().Be("Updated Specialization");
			updatedSpecialist.Price.Should().Be(950);
			updatedSpecialist.Online.Should().BeFalse();
			updatedSpecialist.Offline.Should().BeTrue();
			updatedSpecialist.Language.Should().Be("Updated Language");
		}

		[Fact]
		public async Task UpdateSpecialist_WithInvalidId_ShouldReturnNotFound()
		{
			// Arrange
			var invalidId = Guid.NewGuid();
			var updateRequest = new UpdateSpecialistRequest
			{
				Education = "Test Education",
				Experience = "Test Experience",
				Specialization = "Test Specialization",
				Price = 800,
				Online = true,
				Offline = true,
				Language = "English"
			};

			// Act
			var result = await _controller.UpdateSpecialist(invalidId, updateRequest);

			// Assert
			result.Should().NotBeNull();
			var notFoundResult = result as NotFoundResult;
			notFoundResult.Should().NotBeNull();
		}

		[Fact]
		public async Task DeleteSpecialist_WithValidId_ShouldDeleteSpecialistAndUser()
		{
			// Arrange
			var specialistToDelete = _context.Specialists.First(s => !s.IsActive); // Use inactive specialist
			var userToDelete = await _context.Users.FindAsync(specialistToDelete.UserId);

			// Act
			var result = await _controller.DeleteSpecialist(specialistToDelete.Id);

			// Assert
			result.Should().NotBeNull();
			var noContentResult = result as NoContentResult;
			noContentResult.Should().NotBeNull();

			// Verify specialist was deleted
			var deletedSpecialist = await _context.Specialists.FindAsync(specialistToDelete.Id);
			deletedSpecialist.Should().BeNull();

			// Verify user was deleted
			var deletedUser = await _context.Users.FindAsync(userToDelete!.Id);
			deletedUser.Should().BeNull();
		}

		[Fact]
		public async Task DeleteSpecialist_WithActiveAppointments_ShouldReturnBadRequest()
		{
			// Arrange - Create appointment for specialist
			var specialist = _context.Specialists.First(s => s.IsActive);
			var client = _context.Clients.First();

			var appointment = new Appointment
			{
				Id = Guid.NewGuid(),
				ClientId = client.Id,
				SpecialistId = specialist.Id,
				AppointmentDate = DateTime.UtcNow.AddDays(7),
				IsOnline = true,
				Status = "Scheduled", // Active appointment
				Notes = "Test appointment",
				CreatedAt = DateTime.UtcNow
			};

			_context.Appointments.Add(appointment);
			await _context.SaveChangesAsync();

			// Act
			var result = await _controller.DeleteSpecialist(specialist.Id);

			// Assert
			result.Should().NotBeNull();
			var badRequestResult = result as BadRequestObjectResult;
			badRequestResult.Should().NotBeNull();
			badRequestResult!.Value.Should().Be("Cannot delete specialist with active appointments");
		}

		[Fact]
		public async Task DeleteSpecialist_WithInvalidId_ShouldReturnNotFound()
		{
			// Arrange
			var invalidId = Guid.NewGuid();

			// Act
			var result = await _controller.DeleteSpecialist(invalidId);

			// Assert
			result.Should().NotBeNull();
			var notFoundResult = result as NotFoundResult;
			notFoundResult.Should().NotBeNull();
		}

		[Fact]
		public async Task ToggleSpecialistStatus_ShouldChangeActiveStatus()
		{
			// Arrange
			var specialist = _context.Specialists.First();
			var originalStatus = specialist.IsActive;

			// Act
			var result = await _controller.ToggleSpecialistStatus(specialist.Id);

			// Assert
			result.Should().NotBeNull();
			var okResult = result as OkObjectResult;
			okResult.Should().NotBeNull();

			// Verify status was toggled
			var updatedSpecialist = await _context.Specialists.FindAsync(specialist.Id);
			updatedSpecialist.Should().NotBeNull();
			updatedSpecialist!.IsActive.Should().Be(!originalStatus);

			// Verify response contains new status
			var response = okResult!.Value;
			response.Should().NotBeNull();
			var responseDict = response!.GetType().GetProperties()
				.ToDictionary(prop => prop.Name, prop => prop.GetValue(response));
			responseDict.Should().ContainKey("IsActive");
			responseDict["IsActive"].Should().Be(!originalStatus);
		}

		[Fact]
		public async Task ToggleSpecialistStatus_WithInvalidId_ShouldReturnNotFound()
		{
			// Arrange
			var invalidId = Guid.NewGuid();

			// Act
			var result = await _controller.ToggleSpecialistStatus(invalidId);

			// Assert
			result.Should().NotBeNull();
			var notFoundResult = result as NotFoundResult;
			notFoundResult.Should().NotBeNull();
		}

		[Fact]
		public async Task GetAllClients_ShouldReturnAllClients()
		{
			// Act
			var result = await _controller.GetAllClients();

			// Assert
			result.Should().NotBeNull();
			var okResult = result.Result as OkObjectResult;
			okResult.Should().NotBeNull();

			var clients = okResult!.Value as List<ClientDto>;
			clients.Should().NotBeNull();
			clients!.Should().HaveCount(1);
			clients.First().Email.Should().Be("client@test.com");
		}

		[Fact]
		public async Task GetAllAppointments_ShouldReturnAllAppointments()
		{
			// Arrange - Add test appointment
			var clientId = _context.Clients.First().Id;
			var specialistId = _context.Specialists.First().Id;

			var appointment = new Appointment
			{
				Id = Guid.NewGuid(),
				ClientId = clientId,
				SpecialistId = specialistId,
				AppointmentDate = DateTime.UtcNow.AddDays(7),
				IsOnline = true,
				Status = "Scheduled",
				Notes = "Admin test appointment",
				CreatedAt = DateTime.UtcNow
			};

			_context.Appointments.Add(appointment);
			await _context.SaveChangesAsync();

			// Act
			var result = await _controller.GetAllAppointments();

			// Assert
			result.Should().NotBeNull();
			var okResult = result.Result as OkObjectResult;
			okResult.Should().NotBeNull();

			var appointments = okResult!.Value as List<AppointmentDto>;
			appointments.Should().NotBeNull();
			appointments!.Should().HaveCount(1);
			appointments.First().Status.Should().Be("Scheduled");
			appointments.First().Notes.Should().Be("Admin test appointment");
		}

		[Fact]
		public async Task GetStatistics_ShouldReturnCorrectStatistics()
		{
			// Act
			var result = await _controller.GetStatistics();

			// Assert
			result.Should().NotBeNull();
			var okResult = result.Result as OkObjectResult;
			okResult.Should().NotBeNull();

			var stats = okResult!.Value;
			stats.Should().NotBeNull();

			// Verify statistics contain expected data
			var statsDict = stats!.GetType().GetProperties()
				.ToDictionary(prop => prop.Name, prop => prop.GetValue(stats));

			statsDict.Should().ContainKey("TotalUsers");
			statsDict.Should().ContainKey("TotalSpecialists");
			statsDict.Should().ContainKey("ActiveSpecialists");
			statsDict.Should().ContainKey("TotalClients");
			statsDict.Should().ContainKey("TotalAppointments");
			statsDict.Should().ContainKey("GeneratedAt");

			// Verify counts match our test data
			statsDict["TotalUsers"].Should().Be(3);
			statsDict["TotalSpecialists"].Should().Be(2);
			statsDict["ActiveSpecialists"].Should().Be(1);
			statsDict["TotalClients"].Should().Be(1);
		}

		[Fact]
		public async Task GetStatistics_ShouldIncludeAppointmentCounts()
		{
			// Arrange - Add appointments with different statuses
			var clientId = _context.Clients.First().Id;
			var specialistId = _context.Specialists.First().Id;

			var appointments = new List<Appointment>
			{
				new Appointment
				{
					Id = Guid.NewGuid(),
					ClientId = clientId,
					SpecialistId = specialistId,
					AppointmentDate = DateTime.UtcNow.AddDays(7),
					IsOnline = true,
					Status = "Scheduled",
					Notes = "Scheduled appointment",
					CreatedAt = DateTime.UtcNow
				},
				new Appointment
				{
					Id = Guid.NewGuid(),
					ClientId = clientId,
					SpecialistId = specialistId,
					AppointmentDate = DateTime.UtcNow.AddDays(-7),
					IsOnline = false,
					Status = "Completed",
					Notes = "Completed appointment",
					CreatedAt = DateTime.UtcNow
				}
			};

			_context.Appointments.AddRange(appointments);
			await _context.SaveChangesAsync();

			// Act
			var result = await _controller.GetStatistics();

			// Assert
			var okResult = result.Result as OkObjectResult;
			var stats = okResult!.Value;
			var statsDict = stats!.GetType().GetProperties()
				.ToDictionary(prop => prop.Name, prop => prop.GetValue(stats));

			statsDict["TotalAppointments"].Should().Be(2);
			statsDict["CompletedAppointments"].Should().Be(1);
			statsDict["ScheduledAppointments"].Should().Be(1);
		}

		public void Dispose()
		{
			_context.Dispose();
		}
	}
}