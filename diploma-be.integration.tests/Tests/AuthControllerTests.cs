using diploma.api.Controllers;
using diploma.bll.Models;
using diploma_be.dal;
using diploma_be.dal.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;

namespace diploma_be.Tests.Controllers
{
	public class AuthControllerTests : IDisposable
	{
		private readonly AppDbContext _context;
		private readonly AuthController _controller;
		private readonly Mock<IConfiguration> _configurationMock;

		public AuthControllerTests()
		{
			var options = new DbContextOptionsBuilder<AppDbContext>()
				.UseInMemoryDatabase(databaseName: $"AuthTestDb_{Guid.NewGuid()}")
				.Options;

			_context = new AppDbContext(options);
			_configurationMock = new Mock<IConfiguration>();
			_controller = new AuthController(_context, _configurationMock.Object);

			SeedTestData();
		}

		private void SeedTestData()
		{
			var adminUser = new User
			{
				Id = Guid.NewGuid(),
				FirstName = "Admin",
				LastName = "User",
				Email = "admin@test.com",
				Phone = "+123456789",
				PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
				Role = "Admin",
				CreatedAt = DateTime.UtcNow
			};

			var specialistUser = new User
			{
				Id = Guid.NewGuid(),
				FirstName = "Specialist",
				LastName = "User",
				Email = "specialist@test.com",
				Phone = "+123456790",
				PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
				Role = "Specialist",
				CreatedAt = DateTime.UtcNow
			};

			_context.Users.AddRange(adminUser, specialistUser);
			_context.SaveChanges();
		}

		[Fact]
		public async Task Login_WithValidAdminCredentials_ShouldReturnSuccessWithToken()
		{
			// Arrange
			var loginRequest = new LoginRequest
			{
				Email = "admin@test.com",
				Password = "admin123"
			};

			// Act
			var result = await _controller.Login(loginRequest);

			// Assert
			result.Should().NotBeNull();
			var okResult = result.Result as OkObjectResult;
			okResult.Should().NotBeNull();

			var loginResponse = okResult!.Value as LoginResponse;
			loginResponse.Should().NotBeNull();
			loginResponse!.Token.Should().NotBeEmpty();
			loginResponse.Role.Should().Be("Admin");
			loginResponse.Name.Should().Be("Admin User");
		}

		[Fact]
		public async Task Login_WithInvalidEmail_ShouldReturnUnauthorized()
		{
			// Arrange
			var loginRequest = new LoginRequest
			{
				Email = "nonexistent@test.com",
				Password = "password123"
			};

			// Act
			var result = await _controller.Login(loginRequest);

			// Assert
			result.Should().NotBeNull();
			var unauthorizedResult = result.Result as UnauthorizedObjectResult;
			unauthorizedResult.Should().NotBeNull();
			unauthorizedResult!.Value.Should().Be("Invalid credentials");
		}

		[Fact]
		public async Task Register_WithValidSpecialistData_ShouldCreateUser()
		{
			// Arrange
			var registerRequest = new RegisterRequest
			{
				FirstName = "New",
				LastName = "Specialist",
				Email = "newspecialist@test.com",
				Phone = "+123456799",
				Password = "newpassword123",
				Role = "Specialist"
			};

			// Act
			var result = await _controller.Register(registerRequest);

			// Assert
			result.Should().NotBeNull();
			var okResult = result.Result as OkObjectResult;
			okResult.Should().NotBeNull();

			var loginResponse = okResult!.Value as LoginResponse;
			loginResponse.Should().NotBeNull();
			loginResponse!.Role.Should().Be("Specialist");
		}

		[Fact]
		public async Task Register_WithClientRole_ShouldReturnBadRequest()
		{
			// Arrange
			var registerRequest = new RegisterRequest
			{
				FirstName = "Test",
				LastName = "Client",
				Email = "testclient@test.com",
				Phone = "+123456797",
				Password = "password123",
				Role = "Client"
			};

			// Act
			var result = await _controller.Register(registerRequest);

			// Assert
			result.Should().NotBeNull();
			var badRequestResult = result.Result as BadRequestObjectResult;
			badRequestResult.Should().NotBeNull();
		}

		public void Dispose()
		{
			_context.Dispose();
		}
	}
}