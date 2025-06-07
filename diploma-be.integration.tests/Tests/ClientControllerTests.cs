using diploma.api.Controllers;
using diploma_be.bll.Models;
using diploma_be.bll.Services;
using diploma_be.dal;
using diploma_be.dal.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace diploma_be.integration.tests.Tests
{
	public class ClientControllerTests : IDisposable
	{
		private readonly AppDbContext _context;
		private readonly ClientController _controller;
		private readonly Mock<ITopsisService> _topsisServiceMock;
		private readonly Guid _testClientId;
		private readonly Guid _testSpecialistId;

		public ClientControllerTests()
		{
			var options = new DbContextOptionsBuilder<AppDbContext>()
				.UseInMemoryDatabase(databaseName: $"ClientTestDb_{Guid.NewGuid()}")
				.Options;

			_context = new AppDbContext(options);
			_topsisServiceMock = new Mock<ITopsisService>();
			_controller = new ClientController(_context, _topsisServiceMock.Object);

			// Initialize test IDs
			_testClientId = Guid.NewGuid();
			_testSpecialistId = Guid.NewGuid();

			SeedTestData();
		}

		private void SeedTestData()
		{
			var clientUserId = Guid.NewGuid();
			var specialistUserId = Guid.NewGuid();

			var users = new List<User>
			{
				new User
				{
					Id = clientUserId,
					FirstName = "Test",
					LastName = "Client",
					Email = "testclient@test.com",
					Phone = "+380501234567",
					PasswordHash = "", // Empty for non-authenticated clients
                    Role = "Client",
					CreatedAt = DateTime.UtcNow
				},
				new User
				{
					Id = specialistUserId,
					FirstName = "Test",
					LastName = "Specialist",
					Email = "specialist@test.com",
					Phone = "+380507654321",
					PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
					Role = "Specialist",
					CreatedAt = DateTime.UtcNow
				}
			};

			var client = new Client
			{
				Id = _testClientId,
				UserId = clientUserId,
				Budget = 1000,
				PreferOnline = true,
				PreferOffline = false,
				PreferredGender = "Female",
				PreferredLanguage = "Ukrainian",
				Issue = "Anxiety",
				CreatedAt = DateTime.UtcNow
			};

			var specialist = new Specialist
			{
				Id = _testSpecialistId,
				UserId = specialistUserId,
				Education = "PhD Psychology",
				Experience = "8 years",
				Specialization = "Anxiety",
				Price = 800,
				Online = true,
				Offline = true,
				Gender = "Female",
				Language = "Ukrainian",
				IsActive = true,
				CreatedAt = DateTime.UtcNow
			};

			_context.Users.AddRange(users);
			_context.Clients.Add(client);
			_context.Specialists.Add(specialist);
			_context.SaveChanges();
		}

		[Fact]
		public async Task GetProfile_WithValidClientId_ShouldReturnClientProfile()
		{
			// Act
			var result = await _controller.GetProfile(_testClientId);

			// Assert
			result.Should().NotBeNull();
			var okResult = result.Result as OkObjectResult;
			okResult.Should().NotBeNull();

			var clientDto = okResult!.Value as ClientDto;
			clientDto.Should().NotBeNull();
			clientDto!.FirstName.Should().Be("Test");
			clientDto.LastName.Should().Be("Client");
			clientDto.Email.Should().Be("testclient@test.com");
			clientDto.Budget.Should().Be(1000);
			clientDto.PreferOnline.Should().BeTrue();
			clientDto.PreferOffline.Should().BeFalse();
		}

		[Fact]
		public async Task GetProfile_WithInvalidClientId_ShouldReturnNotFound()
		{
			// Arrange
			var invalidClientId = Guid.NewGuid();

			// Act
			var result = await _controller.GetProfile(invalidClientId);

			// Assert
			result.Should().NotBeNull();
			var notFoundResult = result.Result as NotFoundObjectResult;
			notFoundResult.Should().NotBeNull();
			notFoundResult!.Value.Should().Be("Client profile not found");
		}

		[Fact]
		public async Task UpdateProfile_WithValidData_ShouldUpdateClient()
		{
			// Arrange
			var updateRequest = new UpdateClientRequest
			{
				Budget = 1500,
				PreferOnline = false,
				PreferOffline = true,
				PreferredGender = "Male",
				PreferredLanguage = "English",
				Issue = "Depression"
			};

			// Act
			var result = await _controller.UpdateProfile(_testClientId, updateRequest);

			// Assert
			result.Should().NotBeNull();
			var noContentResult = result as NoContentResult;
			noContentResult.Should().NotBeNull();

			// Verify changes in database
			var updatedClient = await _context.Clients.FindAsync(_testClientId);
			updatedClient.Should().NotBeNull();
			updatedClient!.Budget.Should().Be(1500);
			updatedClient.PreferOnline.Should().BeFalse();
			updatedClient.PreferOffline.Should().BeTrue();
			updatedClient.PreferredGender.Should().Be("Male");
			updatedClient.PreferredLanguage.Should().Be("English");
			updatedClient.Issue.Should().Be("Depression");
		}

		[Fact]
		public async Task UpdateProfile_WithInvalidClientId_ShouldReturnNotFound()
		{
			// Arrange
			var invalidClientId = Guid.NewGuid();
			var updateRequest = new UpdateClientRequest
			{
				Budget = 1500,
				PreferOnline = true,
				PreferOffline = false,
				PreferredGender = "Female",
				PreferredLanguage = "Ukrainian",
				Issue = "Anxiety"
			};

			// Act
			var result = await _controller.UpdateProfile(invalidClientId, updateRequest);

			// Assert
			result.Should().NotBeNull();
			var notFoundResult = result as NotFoundObjectResult;
			notFoundResult.Should().NotBeNull();
		}

		[Fact]
		public async Task GetAllSpecialists_ShouldReturnActiveSpecialistsOnly()
		{
			// Arrange - Add inactive specialist
			var inactiveUser = new User
			{
				Id = Guid.NewGuid(),
				FirstName = "Inactive",
				LastName = "Specialist",
				Email = "inactive@test.com",
				Phone = "+380509999999",
				PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
				Role = "Specialist",
				CreatedAt = DateTime.UtcNow
			};

			var inactiveSpecialist = new Specialist
			{
				Id = Guid.NewGuid(),
				UserId = inactiveUser.Id,
				Education = "Master Psychology",
				Experience = "5 years",
				Specialization = "Trauma",
				Price = 900,
				Online = true,
				Offline = false,
				Gender = "Male",
				Language = "English",
				IsActive = false, // Inactive
				CreatedAt = DateTime.UtcNow
			};

			_context.Users.Add(inactiveUser);
			_context.Specialists.Add(inactiveSpecialist);
			await _context.SaveChangesAsync();

			// Act
			var result = await _controller.GetAllSpecialists();

			// Assert
			result.Should().NotBeNull();
			var okResult = result.Result as OkObjectResult;
			okResult.Should().NotBeNull();

			var specialists = okResult!.Value as List<SpecialistDto>;
			specialists.Should().NotBeNull();
			specialists!.Should().HaveCount(1); // Only active specialist
			specialists.Should().NotContain(s => s.FirstName == "Inactive");
		}

		[Fact]
		public async Task FilterSpecialists_WithValidFilters_ShouldReturnFilteredResults()
		{
			// Arrange - Add more specialists for filtering
			var user2 = new User
			{
				Id = Guid.NewGuid(),
				FirstName = "Another",
				LastName = "Specialist",
				Email = "another@test.com",
				Phone = "+380508888888",
				PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
				Role = "Specialist",
				CreatedAt = DateTime.UtcNow
			};

			var specialist2 = new Specialist
			{
				Id = Guid.NewGuid(),
				UserId = user2.Id,
				Education = "Master Family Therapy",
				Experience = "10 years",
				Specialization = "Relationships",
				Price = 1200,
				Online = false,
				Offline = true,
				Gender = "Male",
				Language = "English",
				IsActive = true,
				CreatedAt = DateTime.UtcNow
			};

			_context.Users.Add(user2);
			_context.Specialists.Add(specialist2);
			await _context.SaveChangesAsync();

			var filterRequest = new SpecialistFilterRequest
			{
				MaxPrice = 1000,
				Online = true,
				Gender = "Female",
				Language = "Ukrainian"
			};

			// Act
			var result = await _controller.FilterSpecialists(filterRequest);

			// Assert
			result.Should().NotBeNull();
			var okResult = result.Result as OkObjectResult;
			okResult.Should().NotBeNull();

			var specialists = okResult!.Value as List<SpecialistDto>;
			specialists.Should().NotBeNull();
			specialists!.Should().HaveCount(1);
			specialists.First().Price.Should().BeLessOrEqualTo(1000);
			specialists.First().Online.Should().BeTrue();
			specialists.First().Gender.Should().Be("Female");
		}

		[Fact]
		public async Task GetTopsisRecommendations_WithValidClientId_ShouldReturnRankedSpecialists()
		{
			// Arrange
			var expectedSpecialists = new List<SpecialistDto>
			{
				new SpecialistDto
				{
					Id = _testSpecialistId,
					FirstName = "Test",
					LastName = "Specialist",
					Specialization = "Anxiety",
					Price = 800,
					TopsisScore = 0.95
				}
			};

			_topsisServiceMock
				.Setup(x => x.GetRankedSpecialistsAsync(It.IsAny<Guid>()))
				.ReturnsAsync(expectedSpecialists);

			// Act
			var result = await _controller.GetTopsisRecommendations(_testClientId);

			// Assert
			result.Should().NotBeNull();
			var okResult = result.Result as OkObjectResult;
			okResult.Should().NotBeNull();

			var specialists = okResult!.Value as List<SpecialistDto>;
			specialists.Should().NotBeNull();
			specialists!.Should().HaveCount(1);
			specialists.First().TopsisScore.Should().Be(0.95);
		}

		[Fact]
		public async Task GetTopsisRecommendations_WithInvalidClientId_ShouldReturnNotFound()
		{
			// Arrange
			var invalidClientId = Guid.NewGuid();

			// Act
			var result = await _controller.GetTopsisRecommendations(invalidClientId);

			// Assert
			result.Should().NotBeNull();
			var notFoundResult = result.Result as NotFoundObjectResult;
			notFoundResult.Should().NotBeNull();
			notFoundResult!.Value.Should().Be("Client not found");
		}

		[Fact]
		public async Task CalculateTopsis_WithValidRequest_ShouldReturnRankedSpecialists()
		{
			// Arrange
			var topsisRequest = new TopsisRequest
			{
				Budget = 1000,
				PreferOnline = true,
				PreferOffline = false,
				PreferredGender = "Female",
				PreferredLanguage = "Ukrainian",
				Issue = "Anxiety"
			};

			var expectedSpecialists = new List<SpecialistDto>
			{
				new SpecialistDto
				{
					Id = _testSpecialistId,
					FirstName = "Test",
					LastName = "Specialist",
					TopsisScore = 0.85
				}
			};

			_topsisServiceMock
				.Setup(x => x.CalculateTopsisAsync(It.IsAny<TopsisRequest>()))
				.ReturnsAsync(expectedSpecialists);

			// Act
			var result = await _controller.CalculateTopsis(topsisRequest);

			// Assert
			result.Should().NotBeNull();
			var okResult = result.Result as OkObjectResult;
			okResult.Should().NotBeNull();

			var specialists = okResult!.Value as List<SpecialistDto>;
			specialists.Should().NotBeNull();
			specialists!.Should().HaveCount(1);
		}

		[Fact]
		public async Task CreateAppointment_WithValidData_ShouldCreateAppointment()
		{
			// Arrange
			var createRequest = new CreateAppointmentRequestWithClient
			{
				ClientId = _testClientId,
				SpecialistId = _testSpecialistId,
				AppointmentDate = DateTime.UtcNow.AddDays(7),
				IsOnline = true,
				Notes = "Test appointment"
			};

			// Act
			var result = await _controller.CreateAppointment(createRequest);

			// Assert
			result.Should().NotBeNull();
			var okResult = result.Result as OkObjectResult;
			okResult.Should().NotBeNull();

			var appointmentDto = okResult!.Value as AppointmentDto;
			appointmentDto.Should().NotBeNull();
			appointmentDto!.ClientName.Should().Be("Test Client");
			appointmentDto.SpecialistName.Should().Be("Test Specialist");
			appointmentDto.Status.Should().Be("Scheduled");

			// Verify in database
			var createdAppointment = await _context.Appointments.FirstOrDefaultAsync();
			createdAppointment.Should().NotBeNull();
			createdAppointment!.ClientId.Should().Be(_testClientId);
			createdAppointment.SpecialistId.Should().Be(_testSpecialistId);
		}

		[Fact]
		public async Task CreateAppointment_WithInvalidClientId_ShouldReturnNotFound()
		{
			// Arrange
			var createRequest = new CreateAppointmentRequestWithClient
			{
				ClientId = Guid.NewGuid(), // Invalid client ID
				SpecialistId = _testSpecialistId,
				AppointmentDate = DateTime.UtcNow.AddDays(7),
				IsOnline = true,
				Notes = "Test appointment"
			};

			// Act
			var result = await _controller.CreateAppointment(createRequest);

			// Assert
			result.Should().NotBeNull();
			var notFoundResult = result.Result as NotFoundObjectResult;
			notFoundResult.Should().NotBeNull();
			notFoundResult!.Value.Should().Be("Client not found");
		}

		[Fact]
		public async Task CreateAppointment_WithInvalidSpecialistId_ShouldReturnNotFound()
		{
			// Arrange
			var createRequest = new CreateAppointmentRequestWithClient
			{
				ClientId = _testClientId,
				SpecialistId = Guid.NewGuid(), // Invalid specialist ID
				AppointmentDate = DateTime.UtcNow.AddDays(7),
				IsOnline = true,
				Notes = "Test appointment"
			};

			// Act
			var result = await _controller.CreateAppointment(createRequest);

			// Assert
			result.Should().NotBeNull();
			var notFoundResult = result.Result as NotFoundObjectResult;
			notFoundResult.Should().NotBeNull();
			notFoundResult!.Value.Should().Be("Specialist not found");
		}

		[Fact]
		public async Task GetClientAppointments_WithInvalidClientId_ShouldReturnNotFound()
		{
			// Arrange
			var invalidClientId = Guid.NewGuid();

			// Act
			var result = await _controller.GetClientAppointments(invalidClientId);

			// Assert
			result.Should().NotBeNull();
			var notFoundResult = result.Result as NotFoundObjectResult;
			notFoundResult.Should().NotBeNull();
			notFoundResult!.Value.Should().Be("Client not found");
		}

		[Fact]
		public async Task CreateClient_WithValidData_ShouldCreateNewClient()
		{
			// Arrange
			var createRequest = new CreateClientRequest
			{
				FirstName = "New",
				LastName = "Client",
				Email = "newclient@test.com",
				Phone = "+380666666666",
				Budget = 1200,
				PreferOnline = true,
				PreferOffline = true,
				PreferredGender = "Any",
				PreferredLanguage = "English",
				Issue = "Stress"
			};

			// Act
			var result = await _controller.CreateClient(createRequest);

			// Assert
			result.Should().NotBeNull();
			var okResult = result.Result as OkObjectResult;
			okResult.Should().NotBeNull();

			var clientDto = okResult!.Value as ClientDto;
			clientDto.Should().NotBeNull();
			clientDto!.Email.Should().Be("newclient@test.com");
			clientDto.FirstName.Should().Be("New");
			clientDto.LastName.Should().Be("Client");
			clientDto.Budget.Should().Be(1200);

			// Verify in database
			var createdUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "newclient@test.com");
			createdUser.Should().NotBeNull();
			createdUser!.Role.Should().Be("Client");
			createdUser.PasswordHash.Should().Be(""); // Empty password for non-authenticated clients

			var createdClient = await _context.Clients.FirstOrDefaultAsync(c => c.UserId == createdUser.Id);
			createdClient.Should().NotBeNull();
		}

		[Fact]
		public async Task CreateClient_WithExistingEmail_ShouldReturnBadRequest()
		{
			// Arrange
			var createRequest = new CreateClientRequest
			{
				FirstName = "Duplicate",
				LastName = "Client",
				Email = "testclient@test.com", // Email already exists
				Phone = "+380777777777",
				Budget = 900,
				PreferOnline = true,
				PreferOffline = false,
				PreferredGender = "Female",
				PreferredLanguage = "Ukrainian",
				Issue = "Anxiety"
			};

			// Act
			var result = await _controller.CreateClient(createRequest);

			// Assert
			result.Should().NotBeNull();
			var badRequestResult = result.Result as BadRequestObjectResult;
			badRequestResult.Should().NotBeNull();
			badRequestResult!.Value.Should().Be("Email already exists");
		}

		public void Dispose()
		{
			_context.Dispose();
		}
	}
}