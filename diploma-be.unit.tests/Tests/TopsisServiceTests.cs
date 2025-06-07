using diploma_be.bll.Models;
using diploma_be.bll.Services;
using diploma_be.dal;
using diploma_be.dal.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace diploma_be.unit.tests.Services
{
	public class TopsisServiceTests : IDisposable
	{
		private readonly AppDbContext _context;
		private readonly TopsisService _service;

		public TopsisServiceTests()
		{
			var options = new DbContextOptionsBuilder<AppDbContext>()
				.UseInMemoryDatabase(databaseName: $"TopsisTestDb_{Guid.NewGuid()}")
				.Options;

			_context = new AppDbContext(options);
			_service = new TopsisService(_context);

			SeedTestData();
		}

		private void SeedTestData()
		{
			// Create test users
			var users = new List<User>
			{
				new User
				{
					Id = Guid.NewGuid(),
					FirstName = "Anna",
					LastName = "Kovalenko",
					Email = "anna@test.com",
					Phone = "+380501111111",
					PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
					Role = "Specialist"
				},
				new User
				{
					Id = Guid.NewGuid(),
					FirstName = "Petro",
					LastName = "Ivanov",
					Email = "petro@test.com",
					Phone = "+380502222222",
					PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
					Role = "Specialist"
				},
				new User
				{
					Id = Guid.NewGuid(),
					FirstName = "Maria",
					LastName = "Shevchenko",
					Email = "maria@test.com",
					Phone = "+380503333333",
					PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
					Role = "Specialist"
				},
				new User
				{
					Id = Guid.NewGuid(),
					FirstName = "Test",
					LastName = "Client",
					Email = "client@test.com",
					Phone = "+380504444444",
					PasswordHash = "",
					Role = "Client"
				}
			};

			_context.Users.AddRange(users);
			_context.SaveChanges();

			// Create specialists with different characteristics
			var specialists = new List<Specialist>
			{
				new Specialist
				{
					Id = Guid.NewGuid(),
					UserId = users[0].Id,
					Education = "PhD Psychology",
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
					UserId = users[1].Id,
					Education = "Master Family Therapy",
					Experience = "12 years",
					Specialization = "Relationships",
					Price = 1200,
					Online = false,
					Offline = true,
					Gender = "Male",
					Language = "Ukrainian",
					IsActive = true
				},
				new Specialist
				{
					Id = Guid.NewGuid(),
					UserId = users[2].Id,
					Education = "PhD Clinical Psychology",
					Experience = "5 years",
					Specialization = "Depression",
					Price = 1000,
					Online = true,
					Offline = false,
					Gender = "Female",
					Language = "English",
					IsActive = true
				}
			};

			_context.Specialists.AddRange(specialists);

			// Create test client
			var client = new Client
			{
				Id = Guid.NewGuid(),
				UserId = users[3].Id,
				Budget = 1000,
				PreferOnline = true,
				PreferOffline = false,
				PreferredGender = "Female",
				PreferredLanguage = "Ukrainian",
				Issue = "Anxiety"
			};

			_context.Clients.Add(client);
			_context.SaveChanges();
		}

		[Fact]
		public async Task CalculateTopsisAsync_WithPerfectMatch_ShouldReturnHighScore()
		{
			// Arrange
			var request = new TopsisRequest
			{
				Budget = 1000,
				PreferOnline = true,
				PreferOffline = true,
				PreferredGender = "Female",
				PreferredLanguage = "Ukrainian",
				Issue = "Anxiety"
			};

			// Act
			var result = await _service.CalculateTopsisAsync(request);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(3); // All active specialists

			// Anna should have the highest score (perfect match)
			var topSpecialist = result.First();
			topSpecialist.FirstName.Should().Be("Anna");
			topSpecialist.TopsisScore.Should().BeGreaterThan(0.7); // High score for good match
		}


		[Fact]
		public async Task CalculateTopsisAsync_WithLanguagePreference_ShouldPrioritizeMatchingLanguage()
		{
			// Arrange
			var request = new TopsisRequest
			{
				Budget = 1500,
				PreferOnline = true,
				PreferOffline = false,
				PreferredGender = "Female",
				PreferredLanguage = "English", // Only Maria speaks English
				Issue = "Depression"
			};

			// Act
			var result = await _service.CalculateTopsisAsync(request);

			// Assert
			result.Should().NotBeNull();

			// Maria should have high score due to language and specialization match
			var topSpecialist = result.First();
			topSpecialist.FirstName.Should().Be("Maria");
			topSpecialist.TopsisScore.Should().BeGreaterThan(0.7);
		}

		[Fact]
		public async Task CalculateTopsisAsync_WithOnlineOnlyPreference_ShouldFilterOfflineOnlySpecialists()
		{
			// Arrange
			var request = new TopsisRequest
			{
				Budget = 1500,
				PreferOnline = true,
				PreferOffline = false,
				PreferredGender = "Any",
				PreferredLanguage = "Ukrainian",
				Issue = "Stress"
			};

			// Act
			var result = await _service.CalculateTopsisAsync(request);

			// Assert
			result.Should().NotBeNull();

			// Verify online specialists score higher
			var onlineSpecialists = result.Where(s => s.FirstName == "Anna" || s.FirstName == "Maria").ToList();
			var offlineOnlySpecialist = result.FirstOrDefault(s => s.FirstName == "Petro");

			onlineSpecialists.Should().HaveCount(2);
			offlineOnlySpecialist.Should().NotBeNull();

			// Online specialists should have better scores
			foreach (var onlineSpec in onlineSpecialists)
			{
				onlineSpec.TopsisScore.Should().BeGreaterThan((double)offlineOnlySpecialist!.TopsisScore);
			}
		}

		[Fact]
		public async Task CalculateTopsisAsync_WithNoActiveSpecialists_ShouldReturnEmptyList()
		{
			// Arrange - Deactivate all specialists
			var specialists = await _context.Specialists.ToListAsync();
			foreach (var spec in specialists)
			{
				spec.IsActive = false;
			}
			await _context.SaveChangesAsync();

			var request = new TopsisRequest
			{
				Budget = 1000,
				PreferOnline = true,
				PreferOffline = true,
				PreferredGender = "Female",
				PreferredLanguage = "Ukrainian",
				Issue = "Anxiety"
			};

			// Act
			var result = await _service.CalculateTopsisAsync(request);

			// Assert
			result.Should().NotBeNull();
			result.Should().BeEmpty();
		}

		[Fact]
		public async Task GetRankedSpecialistsAsync_WithValidClientId_ShouldReturnRankedList()
		{
			// Arrange
			var client = await _context.Clients.Include(c => c.User).FirstAsync();

			// Act
			var result = await _service.GetRankedSpecialistsAsync(client.UserId);

			// Assert
			result.Should().NotBeNull();
			result.Should().HaveCount(3); // All active specialists

			// Results should be ordered by TOPSIS score (descending)
			for (int i = 0; i < result.Count - 1; i++)
			{
				result[i].TopsisScore.Should().BeGreaterOrEqualTo((double)result[i + 1].TopsisScore);
			}
		}

		[Fact]
		public async Task GetRankedSpecialistsAsync_WithNonExistentClient_ShouldThrowException()
		{
			// Arrange
			var nonExistentUserId = Guid.NewGuid();

			// Act & Assert
			await Assert.ThrowsAsync<Exception>(async () =>
				await _service.GetRankedSpecialistsAsync(nonExistentUserId));
		}

		[Theory]
		[InlineData("anxiety", "Anxiety", 1.0)]
		[InlineData("anxiety", "stress management", 0.7)] // Similar term
		[InlineData("depression", "Anxiety", 0.3)] // Different specialization
		[InlineData("", "Anxiety", 0.5)] // Empty issue
		public void CalculateSpecializationScore_ShouldReturnExpectedScores(
			string clientIssue, string specialistSpec, double expectedMinScore)
		{
			// Use reflection to test private method
			var method = typeof(TopsisService).GetMethod("CalculateSpecializationScore",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

			var score = (double)method!.Invoke(_service, new object[] { specialistSpec, clientIssue })!;

			score.Should().BeGreaterOrEqualTo(expectedMinScore);
		}

		[Fact]
		public async Task CalculateTopsisAsync_ShouldNormalizeMatrixCorrectly()
		{
			// Arrange
			var request = new TopsisRequest
			{
				Budget = 1000,
				PreferOnline = true,
				PreferOffline = true,
				PreferredGender = "Female",
				PreferredLanguage = "Ukrainian",
				Issue = "Anxiety"
			};

			// Act
			var result = await _service.CalculateTopsisAsync(request);

			// Assert
			result.Should().NotBeNull();

			// All TOPSIS scores should be between 0 and 1
			foreach (var specialist in result)
			{
				specialist.TopsisScore.Should().BeInRange(0.0, 1.0);
			}
		}

		[Fact]
		public async Task CalculateTopsisAsync_WithSimilarSpecialists_ShouldHaveSimilarScores()
		{
			// Arrange - Add two very similar specialists
			var user1 = new User
			{
				Id = Guid.NewGuid(),
				FirstName = "Twin1",
				LastName = "Specialist",
				Email = "twin1@test.com",
				Phone = "+380505555555",
				PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
				Role = "Specialist"
			};

			var user2 = new User
			{
				Id = Guid.NewGuid(),
				FirstName = "Twin2",
				LastName = "Specialist",
				Email = "twin2@test.com",
				Phone = "+380506666666",
				PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
				Role = "Specialist"
			};

			_context.Users.AddRange(user1, user2);

			var twin1 = new Specialist
			{
				Id = Guid.NewGuid(),
				UserId = user1.Id,
				Education = "Same Education",
				Experience = "Same Experience",
				Specialization = "Same Spec",
				Price = 850,
				Online = true,
				Offline = true,
				Gender = "Female",
				Language = "Ukrainian",
				IsActive = true
			};

			var twin2 = new Specialist
			{
				Id = Guid.NewGuid(),
				UserId = user2.Id,
				Education = "Same Education",
				Experience = "Same Experience",
				Specialization = "Same Spec",
				Price = 850,
				Online = true,
				Offline = true,
				Gender = "Female",
				Language = "Ukrainian",
				IsActive = true
			};

			_context.Specialists.AddRange(twin1, twin2);
			await _context.SaveChangesAsync();

			var request = new TopsisRequest
			{
				Budget = 1000,
				PreferOnline = true,
				PreferOffline = true,
				PreferredGender = "Female",
				PreferredLanguage = "Ukrainian",
				Issue = "Same Spec"
			};

			// Act
			var result = await _service.CalculateTopsisAsync(request);

			// Assert
			var twin1Result = result.FirstOrDefault(s => s.FirstName == "Twin1");
			var twin2Result = result.FirstOrDefault(s => s.FirstName == "Twin2");

			twin1Result.Should().NotBeNull();
			twin2Result.Should().NotBeNull();

			// Their scores should be very close (within 0.01)
			Math.Abs(twin1Result!.TopsisScore!.Value - twin2Result!.TopsisScore!.Value).Should().BeLessThan(0.01);
		}

		public void Dispose()
		{
			_context.Dispose();
		}
	}
}