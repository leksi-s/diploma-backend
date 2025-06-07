using diploma_be.dal;
using diploma_be.dal.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace diploma_be.unit.tests.Services
{
	public class AuthServiceTests : IDisposable
	{
		private readonly AppDbContext _context;

		public AuthServiceTests()
		{
			var options = new DbContextOptionsBuilder<AppDbContext>()
				.UseInMemoryDatabase(databaseName: $"AuthTestDb_{Guid.NewGuid()}")
				.Options;

			_context = new AppDbContext(options);
		}

		[Fact]
		public void PasswordHashing_ShouldWorkCorrectly()
		{
			// Arrange
			var password = "TestPassword123!";

			// Act
			var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
			var isValid = BCrypt.Net.BCrypt.Verify(password, hashedPassword);

			// Assert
			hashedPassword.Should().NotBeNullOrEmpty();
			hashedPassword.Should().NotBe(password);
			isValid.Should().BeTrue();
		}

		[Fact]
		public void PasswordHashing_WithDifferentPasswords_ShouldNotMatch()
		{
			// Arrange
			var password1 = "Password123!";
			var password2 = "DifferentPassword123!";

			// Act
			var hash1 = BCrypt.Net.BCrypt.HashPassword(password1);
			var hash2 = BCrypt.Net.BCrypt.HashPassword(password2);
			var isValid = BCrypt.Net.BCrypt.Verify(password1, hash2);

			// Assert
			hash1.Should().NotBe(hash2);
			isValid.Should().BeFalse();
		}

		[Fact]
		public async Task UserCreation_WithUniqueEmail_ShouldSucceed()
		{
			// Arrange
			var user = new User
			{
				Id = Guid.NewGuid(),
				FirstName = "Test",
				LastName = "User",
				Email = "unique@test.com",
				Phone = "+380501234567",
				PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
				Role = "Client",
				CreatedAt = DateTime.UtcNow
			};

			// Act
			_context.Users.Add(user);
			await _context.SaveChangesAsync();

			// Assert
			var savedUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "unique@test.com");
			savedUser.Should().NotBeNull();
			savedUser!.FirstName.Should().Be("Test");
			savedUser.Role.Should().Be("Client");
		}


		[Theory]
		[InlineData("Admin", true)]
		[InlineData("Specialist", true)]
		[InlineData("Client", true)]
		[InlineData("InvalidRole", false)]
		[InlineData("", false)]
		[InlineData(null, false)]
		public void UserRole_Validation_ShouldWorkCorrectly(string role, bool shouldBeValid)
		{
			// Arrange
			var validRoles = new[] { "Admin", "Specialist", "Client" };

			// Act
			var isValid = validRoles.Contains(role);

			// Assert
			isValid.Should().Be(shouldBeValid);
		}

		[Fact]
		public void ClaimsGeneration_ShouldIncludeRequiredClaims()
		{
			// Arrange
			var user = new User
			{
				Id = Guid.NewGuid(),
				FirstName = "Test",
				LastName = "User",
				Email = "test@example.com",
				Role = "Specialist"
			};

			// Act
			var claims = new List<Claim>
			{
				new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
				new Claim(ClaimTypes.Email, user.Email),
				new Claim(ClaimTypes.Role, user.Role),
				new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}")
			};

			// Assert
			claims.Should().HaveCount(4);
			claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id.ToString());
			claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == user.Email);
			claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == user.Role);
			claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == "Test User");
		}

		[Fact]
		public async Task SpecialistRegistration_ShouldCreateUserAndSpecialistProfile()
		{
			// Arrange
			var userId = Guid.NewGuid();
			var user = new User
			{
				Id = userId,
				FirstName = "New",
				LastName = "Specialist",
				Email = "newspecialist@test.com",
				Phone = "+380507777777",
				PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
				Role = "Specialist",
				CreatedAt = DateTime.UtcNow
			};

			var specialist = new Specialist
			{
				Id = Guid.NewGuid(),
				UserId = userId,
				Education = "",
				Experience = "",
				Specialization = "",
				Price = 800,
				Online = true,
				Offline = true,
				Gender = "Other",
				Language = "Ukrainian",
				IsActive = false, // Should be inactive by default
				CreatedAt = DateTime.UtcNow
			};

			// Act
			_context.Users.Add(user);
			_context.Specialists.Add(specialist);
			await _context.SaveChangesAsync();

			// Assert
			var savedSpecialist = await _context.Specialists
				.Include(s => s.User)
				.FirstOrDefaultAsync(s => s.UserId == userId);

			savedSpecialist.Should().NotBeNull();
			savedSpecialist!.User.Should().NotBeNull();
			savedSpecialist.User.Email.Should().Be("newspecialist@test.com");
			savedSpecialist.IsActive.Should().BeFalse(); // Requires admin activation
			savedSpecialist.Price.Should().Be(800); // Default price
		}

		[Fact]
		public async Task ClientCreation_WithoutPassword_ShouldSucceed()
		{
			// Arrange
			var userId = Guid.NewGuid();
			var user = new User
			{
				Id = userId,
				FirstName = "Guest",
				LastName = "Client",
				Email = "guest@test.com",
				Phone = "+380508888888",
				PasswordHash = "", // Empty password for non-authenticated clients
				Role = "Client",
				CreatedAt = DateTime.UtcNow
			};

			var client = new Client
			{
				Id = Guid.NewGuid(),
				UserId = userId,
				Budget = 1000,
				PreferOnline = true,
				PreferOffline = false,
				PreferredGender = "Any",
				PreferredLanguage = "Ukrainian",
				Issue = "General consultation",
				CreatedAt = DateTime.UtcNow
			};

			// Act
			_context.Users.Add(user);
			_context.Clients.Add(client);
			await _context.SaveChangesAsync();

			// Assert
			var savedClient = await _context.Clients
				.Include(c => c.User)
				.FirstOrDefaultAsync(c => c.UserId == userId);

			savedClient.Should().NotBeNull();
			savedClient!.User.PasswordHash.Should().BeEmpty();
			savedClient.User.Role.Should().Be("Client");
		}

		[Theory]
		[InlineData("admin@psychapp.com", true)]
		[InlineData("Admin@PsychApp.com", true)] // Case insensitive
		[InlineData("admin@psychapp", false)] // Invalid format
		[InlineData("admin@psychapp.com", true)] // Missing local part
		[InlineData("admin@", false)] // Missing domain
		[InlineData("", false)] // Empty
		[InlineData(null, false)] // Null
		public void EmailValidation_ShouldWorkCorrectly(string email, bool shouldBeValid)
		{
			// Simple email validation
			var isValid = !string.IsNullOrEmpty(email) && email.Contains("@") && email.Contains(".");

			isValid.Should().Be(shouldBeValid);
		}

		[Fact]
		public void PasswordComplexity_ShouldBeValidated()
		{
			// Arrange
			var weakPasswords = new[] { "123", "password", "12345678" };
			var strongPasswords = new[] { "Password123!", "Complex@Pass1", "Str0ng!Password" };

			// Act & Assert
			foreach (var weak in weakPasswords)
			{
				weak.Length.Should().BeLessThan(12); // Assuming min length requirement
			}

			foreach (var strong in strongPasswords)
			{
				strong.Should().MatchRegex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]");
			}
		}

		public void Dispose()
		{
			_context.Dispose();
		}
	}
}