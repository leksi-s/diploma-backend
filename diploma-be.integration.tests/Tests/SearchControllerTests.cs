using diploma_be.api.Controllers;
using diploma_be.bll.Models;
using diploma_be.dal;
using diploma_be.dal.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace diploma_be.Tests.Controllers
{
	public class SearchControllerTests : IDisposable
	{
		private readonly AppDbContext _context;
		private readonly SearchController _controller;

		public SearchControllerTests()
		{
			var options = new DbContextOptionsBuilder<AppDbContext>()
				.UseInMemoryDatabase(databaseName: $"SearchTestDb_{Guid.NewGuid()}")
				.Options;

			_context = new AppDbContext(options);
			_controller = new SearchController(_context);

			SeedTestData();
		}

		private void SeedTestData()
		{
			var specialist1UserId = Guid.NewGuid();
			var specialist2UserId = Guid.NewGuid();
			var specialist3UserId = Guid.NewGuid();

			var users = new List<User>
			{
				new User
				{
					Id = specialist1UserId,
					FirstName = "Anna",
					LastName = "Psychology",
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
					Id = specialist3UserId,
					FirstName = "Maria",
					LastName = "Family",
					Email = "maria@test.com",
					Phone = "+123456791",
					PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
					Role = "Specialist",
					CreatedAt = DateTime.UtcNow
				}
			};

			var specialists = new List<Specialist>
			{
				new Specialist
				{
					Id = Guid.NewGuid(),
					UserId = specialist1UserId,
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
				},
				new Specialist
				{
					Id = Guid.NewGuid(),
					UserId = specialist2UserId,
					Education = "Master Psychology",
					Experience = "5 years in depression counseling",
					Specialization = "Depression",
					Price = 1200,
					Online = false,
					Offline = true,
					Gender = "Male",
					Language = "Ukrainian",
					IsActive = true,
					CreatedAt = DateTime.UtcNow
				},
				new Specialist
				{
					Id = Guid.NewGuid(),
					UserId = specialist3UserId,
					Education = "Master Family Therapy",
					Experience = "10 years in family counseling",
					Specialization = "Relationships",
					Price = 600,
					Online = true,
					Offline = false,
					Gender = "Female",
					Language = "Ukrainian",
					IsActive = false, // Inactive specialist
                    CreatedAt = DateTime.UtcNow
				}
			};

			_context.Users.AddRange(users);
			_context.Specialists.AddRange(specialists);
			_context.SaveChanges();
		}

		[Fact]
		public async Task SearchSpecialists_WithMaxPrice_ShouldReturnSpecialistsWithinBudget()
		{
			// Act
			var result = await _controller.SearchSpecialists(null, 900, null, null, null, null, null);

			// Assert
			result.Should().NotBeNull();
			var okResult = result.Result as OkObjectResult;
			okResult.Should().NotBeNull();

			var specialists = okResult!.Value as List<SpecialistDto>;
			specialists.Should().NotBeNull();
			specialists!.Should().HaveCount(1); // Only Anna (800) fits budget of 900
			specialists.All(s => s.Price <= 900).Should().BeTrue();
		}

		[Fact]
		public async Task SearchSpecialists_WithSpecialization_ShouldReturnMatchingSpecialists()
		{
			// Act
			var result = await _controller.SearchSpecialists(null, null, "Anxiety", null, null, null, null);

			// Assert
			result.Should().NotBeNull();
			var okResult = result.Result as OkObjectResult;
			okResult.Should().NotBeNull();

			var specialists = okResult!.Value as List<SpecialistDto>;
			specialists.Should().NotBeNull();
			specialists!.Should().HaveCount(1);
			specialists.First().Specialization.Should().Be("Anxiety");
		}

		[Fact]
		public async Task SearchSpecialists_WithLanguage_ShouldReturnMatchingSpecialists()
		{
			// Act
			var result = await _controller.SearchSpecialists(null, null, null, "Ukrainian", null, null, null);

			// Assert
			result.Should().NotBeNull();
			var okResult = result.Result as OkObjectResult;
			okResult.Should().NotBeNull();

			var specialists = okResult!.Value as List<SpecialistDto>;
			specialists.Should().NotBeNull();
			specialists!.Should().HaveCount(1); // Only John is active and speaks Ukrainian
			specialists.First().Language.Should().Contain("Ukrainian");
		}

		[Fact]
		public async Task SearchSpecialists_WithGender_ShouldReturnMatchingSpecialists()
		{
			// Act
			var result = await _controller.SearchSpecialists(null, null, null, null, "Female", null, null);

			// Assert
			result.Should().NotBeNull();
			var okResult = result.Result as OkObjectResult;
			okResult.Should().NotBeNull();

			var specialists = okResult!.Value as List<SpecialistDto>;
			specialists.Should().NotBeNull();
			specialists!.Should().HaveCount(1); // Only Anna is active and female
			specialists.First().Gender.Should().Be("Female");
		}

		[Fact]
		public async Task SearchSpecialists_WithOnlineFilter_ShouldReturnOnlineSpecialists()
		{
			// Act
			var result = await _controller.SearchSpecialists(null, null, null, null, null, true, null);

			// Assert
			result.Should().NotBeNull();
			var okResult = result.Result as OkObjectResult;
			okResult.Should().NotBeNull();

			var specialists = okResult!.Value as List<SpecialistDto>;
			specialists.Should().NotBeNull();
			specialists!.Should().HaveCount(1); // Only Anna is active and online
			specialists.All(s => s.Online).Should().BeTrue();
		}

		[Fact]
		public async Task SearchSpecialists_WithOfflineFilter_ShouldReturnOfflineSpecialists()
		{
			// Act
			var result = await _controller.SearchSpecialists(null, null, null, null, null, null, true);

			// Assert
			result.Should().NotBeNull();
			var okResult = result.Result as OkObjectResult;
			okResult.Should().NotBeNull();

			var specialists = okResult!.Value as List<SpecialistDto>;
			specialists.Should().NotBeNull();
			specialists!.Should().HaveCount(2); // Anna and John offer offline
			specialists.All(s => s.Offline).Should().BeTrue();
		}

		[Fact]
		public async Task SearchSpecialists_WithMultipleFilters_ShouldReturnMatchingSpecialists()
		{
			// Act
			var result = await _controller.SearchSpecialists(null, 1000, "Anxiety", "English", "Female", true, true);

			// Assert
			result.Should().NotBeNull();
			var okResult = result.Result as OkObjectResult;
			okResult.Should().NotBeNull();

			var specialists = okResult!.Value as List<SpecialistDto>;
			specialists.Should().NotBeNull();
			specialists!.Should().HaveCount(1); // Only Anna matches all criteria

			var specialist = specialists.First();
			specialist.Price.Should().BeLessOrEqualTo(1000);
			specialist.Specialization.Should().Contain("Anxiety");
			specialist.Gender.Should().Be("Female");
			specialist.Online.Should().BeTrue();
			specialist.Offline.Should().BeTrue();
		}

		[Fact]
		public async Task SearchSpecialists_WithNoMatches_ShouldReturnEmptyList()
		{
			// Act
			var result = await _controller.SearchSpecialists("NonExistentSpecialization", null, null, null, null, null, null);

			// Assert
			result.Should().NotBeNull();
			var okResult = result.Result as OkObjectResult;
			okResult.Should().NotBeNull();

			var specialists = okResult!.Value as List<SpecialistDto>;
			specialists.Should().NotBeNull();
			specialists!.Should().BeEmpty();
		}

		[Fact]
		public async Task GetPopularSpecialists_ShouldReturnSpecialistsByAppointmentCount()
		{
			// Arrange - Add some appointments for testing
			var clientUserId = Guid.NewGuid();
			var clientUser = new User
			{
				Id = clientUserId,
				FirstName = "Test",
				LastName = "Client",
				Email = "client@test.com",
				Phone = "+123456792",
				PasswordHash = "",
				Role = "Client",
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

			_context.Users.Add(clientUser);
			_context.Clients.Add(client);
			await _context.SaveChangesAsync();

			// Act
			var result = await _controller.GetPopularSpecialists();

			// Assert
			result.Should().NotBeNull();
			var okResult = result.Result as OkObjectResult;
			okResult.Should().NotBeNull();

			var specialists = okResult!.Value as List<SpecialistDto>;
			specialists.Should().NotBeNull();
			specialists!.Should().HaveCount(2); // Only active specialists
		}

		[Fact]
		public async Task GetSearchStatistics_ShouldReturnStatistics()
		{
			// Act
			var result = await _controller.GetSearchStatistics();

			// Assert
			result.Should().NotBeNull();
			var okResult = result.Result as OkObjectResult;
			okResult.Should().NotBeNull();

			var statistics = okResult!.Value;
			statistics.Should().NotBeNull();

			// Verify statistics structure
			var statsDict = statistics!.GetType().GetProperties()
				.ToDictionary(prop => prop.Name, prop => prop.GetValue(statistics));

			statsDict.Should().ContainKey("TotalActiveSpecialists");
			statsDict.Should().ContainKey("AveragePrice");
			statsDict.Should().ContainKey("PriceRange");
		}

		public void Dispose()
		{
			_context.Dispose();
		}
	}
}