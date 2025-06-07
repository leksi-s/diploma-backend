using diploma_be.api.Controllers;
using diploma_be.bll.Models;
using diploma_be.dal;
using diploma_be.dal.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace diploma_be.Tests.Controllers
{
	public class SpecialistControllerTests : IDisposable
	{
		private readonly AppDbContext _context;
		private readonly SpecialistController _controller;
		private readonly Guid _testSpecialistUserId;
		private readonly Guid _testSpecialistId;

		public SpecialistControllerTests()
		{
			var options = new DbContextOptionsBuilder<AppDbContext>()
				.UseInMemoryDatabase(databaseName: $"SpecialistTestDb_{Guid.NewGuid()}")
				.Options;

			_context = new AppDbContext(options);
			_controller = new SpecialistController(_context);

			_testSpecialistUserId = Guid.NewGuid();
			_testSpecialistId = Guid.NewGuid();

			SeedTestData();
			SetupControllerContext();
		}

		private void SeedTestData()
		{
			var clientUserId = Guid.NewGuid();

			var users = new List<User>
			{
				new User
				{
					Id = _testSpecialistUserId,
					FirstName = "Test",
					LastName = "Specialist",
					Email = "specialist@test.com",
					Phone = "+123456789",
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
					Phone = "+123456790",
					PasswordHash = "",
					Role = "Client",
					CreatedAt = DateTime.UtcNow
				}
			};

			var specialist = new Specialist
			{
				Id = _testSpecialistId,
				UserId = _testSpecialistUserId,
				Education = "PhD Clinical Psychology",
				Experience = "8 years in anxiety treatment",
				Specialization = "Anxiety",
				Price = 800,
				Online = true,
				Offline = true,
				Gender = "Female",
				Language = "English",
				IsActive = true,
				CreatedAt = DateTime.UtcNow
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

			// Add test appointment
			var appointment = new Appointment
			{
				Id = Guid.NewGuid(),
				ClientId = client.Id,
				SpecialistId = _testSpecialistId,
				AppointmentDate = DateTime.UtcNow.AddDays(7),
				IsOnline = true,
				Status = "Scheduled",
				Notes = "Test appointment",
				CreatedAt = DateTime.UtcNow
			};

			_context.Users.AddRange(users);
			_context.Specialists.Add(specialist);
			_context.Clients.Add(client);
			_context.Appointments.Add(appointment);
			_context.SaveChanges();
		}

		private void SetupControllerContext()
		{
			var claims = new List<Claim>
			{
				new Claim(ClaimTypes.NameIdentifier, _testSpecialistUserId.ToString()),
				new Claim(ClaimTypes.Email, "specialist@test.com"),
				new Claim(ClaimTypes.Role, "Specialist"),
				new Claim(ClaimTypes.Name, "Test Specialist")
			};

			var identity = new ClaimsIdentity(claims, "test");
			var principal = new ClaimsPrincipal(identity);

			_controller.ControllerContext = new ControllerContext
			{
				HttpContext = new DefaultHttpContext
				{
					User = principal
				}
			};
		}

		[Fact]
		public async Task GetProfile_ShouldReturnSpecialistProfile()
		{
			// Act
			var result = await _controller.GetProfile();

			// Assert
			result.Should().NotBeNull();
			var okResult = result.Result as OkObjectResult;
			okResult.Should().NotBeNull();

			var specialistDto = okResult!.Value as SpecialistDto;
			specialistDto.Should().NotBeNull();
			specialistDto!.Email.Should().Be("specialist@test.com");
			specialistDto.FirstName.Should().Be("Test");
			specialistDto.LastName.Should().Be("Specialist");
			specialistDto.Specialization.Should().Be("Anxiety");
			specialistDto.Price.Should().Be(800);
		}

		[Fact]
		public async Task GetProfile_WithNonExistentSpecialist_ShouldReturnNotFound()
		{
			// Arrange - Setup context with non-existent user
			var nonExistentUserId = Guid.NewGuid();
			var claims = new List<Claim>
			{
				new Claim(ClaimTypes.NameIdentifier, nonExistentUserId.ToString())
			};

			var identity = new ClaimsIdentity(claims, "test");
			var principal = new ClaimsPrincipal(identity);

			_controller.ControllerContext = new ControllerContext
			{
				HttpContext = new DefaultHttpContext { User = principal }
			};

			// Act
			var result = await _controller.GetProfile();

			// Assert
			result.Should().NotBeNull();
			var notFoundResult = result.Result as NotFoundObjectResult;
			notFoundResult.Should().NotBeNull();
			notFoundResult!.Value.Should().Be("Specialist profile not found");
		}

		[Fact]
		public async Task UpdateProfile_WithValidData_ShouldUpdateSpecialistProfile()
		{
			// Arrange
			var updateRequest = new UpdateSpecialistRequest
			{
				Education = "Updated PhD in Clinical Psychology",
				Experience = "Updated 10 years experience",
				Specialization = "Updated Anxiety and Depression",
				Price = 950,
				Online = false,
				Offline = true,
				Language = "Ukrainian"
			};

			// Act
			var result = await _controller.UpdateProfile(updateRequest);

			// Assert
			result.Should().NotBeNull();
			var noContentResult = result as NoContentResult;
			noContentResult.Should().NotBeNull();

			// Verify changes were saved to database
			var updatedSpecialist = await _context.Specialists.FindAsync(_testSpecialistId);
			updatedSpecialist.Should().NotBeNull();
			updatedSpecialist!.Education.Should().Be("Updated PhD in Clinical Psychology");
			updatedSpecialist.Experience.Should().Be("Updated 10 years experience");
			updatedSpecialist.Specialization.Should().Be("Updated Anxiety and Depression");
			updatedSpecialist.Price.Should().Be(950);
			updatedSpecialist.Online.Should().BeFalse();
			updatedSpecialist.Offline.Should().BeTrue();
			updatedSpecialist.Language.Should().Be("Ukrainian");
		}

		[Fact]
		public async Task UpdateProfile_WithNonExistentSpecialist_ShouldReturnNotFound()
		{
			// Arrange - Setup context with non-existent user
			var nonExistentUserId = Guid.NewGuid();
			var claims = new List<Claim>
			{
				new Claim(ClaimTypes.NameIdentifier, nonExistentUserId.ToString())
			};

			var identity = new ClaimsIdentity(claims, "test");
			var principal = new ClaimsPrincipal(identity);

			_controller.ControllerContext = new ControllerContext
			{
				HttpContext = new DefaultHttpContext { User = principal }
			};

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
			var result = await _controller.UpdateProfile(updateRequest);

			// Assert
			result.Should().NotBeNull();
			var notFoundResult = result as NotFoundObjectResult;
			notFoundResult.Should().NotBeNull();
		}

		[Fact]
		public async Task GetMyAppointments_ShouldReturnSpecialistAppointments()
		{
			// Act
			var result = await _controller.GetMyAppointments();

			// Assert
			result.Should().NotBeNull();
			var okResult = result.Result as OkObjectResult;
			okResult.Should().NotBeNull();

			var appointments = okResult!.Value as List<AppointmentDto>;
			appointments.Should().NotBeNull();
			appointments!.Should().HaveCount(1);

			var appointment = appointments.First();
			appointment.SpecialistName.Should().Be("Test Specialist");
			appointment.Status.Should().Be("Scheduled");
			appointment.IsOnline.Should().BeTrue();
		}

		[Fact]
		public async Task GetMyAppointments_WithNonExistentSpecialist_ShouldReturnNotFound()
		{
			// Arrange - Setup context with non-existent user
			var nonExistentUserId = Guid.NewGuid();
			var claims = new List<Claim>
			{
				new Claim(ClaimTypes.NameIdentifier, nonExistentUserId.ToString())
			};

			var identity = new ClaimsIdentity(claims, "test");
			var principal = new ClaimsPrincipal(identity);

			_controller.ControllerContext = new ControllerContext
			{
				HttpContext = new DefaultHttpContext { User = principal }
			};

			// Act
			var result = await _controller.GetMyAppointments();

			// Assert
			result.Should().NotBeNull();
			var notFoundResult = result.Result as NotFoundObjectResult;
			notFoundResult.Should().NotBeNull();
		}

		[Fact]
		public async Task UpdateAppointmentStatus_WithValidData_ShouldUpdateStatus()
		{
			// Arrange
			var appointmentId = _context.Appointments.First().Id;
			var newStatus = "Completed";

			// Act
			var result = await _controller.UpdateAppointmentStatus(appointmentId, newStatus);

			// Assert
			result.Should().NotBeNull();
			var noContentResult = result as NoContentResult;
			noContentResult.Should().NotBeNull();

			// Verify status was updated in database
			var updatedAppointment = await _context.Appointments.FindAsync(appointmentId);
			updatedAppointment.Should().NotBeNull();
			updatedAppointment!.Status.Should().Be("Completed");
		}

		[Fact]
		public async Task UpdateAppointmentStatus_WithInvalidStatus_ShouldReturnBadRequest()
		{
			// Arrange
			var appointmentId = _context.Appointments.First().Id;
			var invalidStatus = "InvalidStatus";

			// Act
			var result = await _controller.UpdateAppointmentStatus(appointmentId, invalidStatus);

			// Assert
			result.Should().NotBeNull();
			var badRequestResult = result as BadRequestObjectResult;
			badRequestResult.Should().NotBeNull();
			badRequestResult!.Value.Should().Be("Invalid status");
		}

		[Fact]
		public async Task UpdateAppointmentStatus_WithNonExistentAppointment_ShouldReturnNotFound()
		{
			// Arrange
			var nonExistentAppointmentId = Guid.NewGuid();
			var newStatus = "Completed";

			// Act
			var result = await _controller.UpdateAppointmentStatus(nonExistentAppointmentId, newStatus);

			// Assert
			result.Should().NotBeNull();
			var notFoundResult = result as NotFoundObjectResult;
			notFoundResult.Should().NotBeNull();
		}

		[Fact]
		public async Task UpdateAppointmentStatus_WithAppointmentNotBelongingToSpecialist_ShouldReturnNotFound()
		{
			// Arrange - Create appointment for different specialist
			var anotherSpecialistUserId = Guid.NewGuid();
			var anotherSpecialistUser = new User
			{
				Id = anotherSpecialistUserId,
				FirstName = "Another",
				LastName = "Specialist",
				Email = "another@test.com",
				Phone = "+123456799",
				PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
				Role = "Specialist",
				CreatedAt = DateTime.UtcNow
			};

			var anotherSpecialist = new Specialist
			{
				Id = Guid.NewGuid(),
				UserId = anotherSpecialistUserId,
				Education = "Another Education",
				Experience = "Another Experience",
				Specialization = "Another Specialization",
				Price = 700,
				Online = true,
				Offline = false,
				Gender = "Male",
				Language = "Ukrainian",
				IsActive = true,
				CreatedAt = DateTime.UtcNow
			};

			var anotherAppointment = new Appointment
			{
				Id = Guid.NewGuid(),
				ClientId = _context.Clients.First().Id,
				SpecialistId = anotherSpecialist.Id,
				AppointmentDate = DateTime.UtcNow.AddDays(5),
				IsOnline = false,
				Status = "Scheduled",
				Notes = "Another appointment",
				CreatedAt = DateTime.UtcNow
			};

			_context.Users.Add(anotherSpecialistUser);
			_context.Specialists.Add(anotherSpecialist);
			_context.Appointments.Add(anotherAppointment);
			await _context.SaveChangesAsync();

			// Act - Try to update appointment that doesn't belong to current specialist
			var result = await _controller.UpdateAppointmentStatus(anotherAppointment.Id, "Completed");

			// Assert
			result.Should().NotBeNull();
			var notFoundResult = result as NotFoundObjectResult;
			notFoundResult.Should().NotBeNull();
		}

		[Theory]
		[InlineData("Scheduled")]
		[InlineData("Completed")]
		[InlineData("Cancelled")]
		[InlineData("NoShow")]
		public async Task UpdateAppointmentStatus_WithValidStatuses_ShouldSucceed(string status)
		{
			// Arrange
			var appointmentId = _context.Appointments.First().Id;

			// Act
			var result = await _controller.UpdateAppointmentStatus(appointmentId, status);

			// Assert
			result.Should().NotBeNull();
			var noContentResult = result as NoContentResult;
			noContentResult.Should().NotBeNull();

			// Verify status was updated
			var updatedAppointment = await _context.Appointments.FindAsync(appointmentId);
			updatedAppointment!.Status.Should().Be(status);
		}

		public void Dispose()
		{
			_context.Dispose();
		}
	}
}