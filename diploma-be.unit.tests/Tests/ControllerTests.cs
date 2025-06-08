using diploma_be.api.Controllers;
using diploma_be.bll.Models;
using diploma_be.dal;
using diploma_be.dal.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace diploma_be.unit.tests.Tests
{
	public class ControllerUnitTests
	{
		private AppDbContext CreateInMemoryContext()
		{
			var options = new DbContextOptionsBuilder<AppDbContext>()
				.UseInMemoryDatabase(databaseName: $"ControllerTestDb_{Guid.NewGuid()}")
				.Options;
			return new AppDbContext(options);
		}

		private ControllerContext CreateControllerContext(Guid userId, string role)
		{
			var claims = new List<Claim>
			{
				new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
				new Claim(ClaimTypes.Role, role)
			};

			var identity = new ClaimsIdentity(claims, "TestAuth");
			var principal = new ClaimsPrincipal(identity);

			return new ControllerContext
			{
				HttpContext = new DefaultHttpContext { User = principal }
			};
		}

		[Fact]
		public async Task SpecialistController_GetProfile_WithAuthorizedUser_ShouldReturnProfile()
		{
			// Arrange
			using var context = CreateInMemoryContext();
			var controller = new SpecialistController(context);

			var userId = Guid.NewGuid();
			var specialistId = Guid.NewGuid();

			var user = new User
			{
				Id = userId,
				FirstName = "Test",
				LastName = "Specialist",
				Email = "specialist@test.com",
				Phone = "+380501234567",
				PasswordHash = "hash",
				Role = "Specialist"
			};

			var specialist = new Specialist
			{
				Id = specialistId,
				UserId = userId,
				Education = "PhD Psychology",
				Experience = "10 years",
				Specialization = "Anxiety",
				Price = 1000,
				Online = true,
				Offline = true,
				Gender = "Female",
				Language = "Ukrainian",
				IsActive = true
			};

			context.Users.Add(user);
			context.Specialists.Add(specialist);
			await context.SaveChangesAsync();

			controller.ControllerContext = CreateControllerContext(userId, "Specialist");

			// Act
			var result = await controller.GetProfile();

			// Assert
			result.Should().NotBeNull();
			var okResult = result.Result as OkObjectResult;
			okResult.Should().NotBeNull();

			var dto = okResult!.Value as SpecialistDto;
			dto.Should().NotBeNull();
			dto!.FirstName.Should().Be("Test");
			dto.Education.Should().Be("PhD Psychology");
			dto.IsActive.Should().BeTrue();
		}

		[Fact]
		public async Task SpecialistController_UpdateProfile_WithValidData_ShouldUpdateSuccessfully()
		{
			// Arrange
			using var context = CreateInMemoryContext();
			var controller = new SpecialistController(context);

			var userId = Guid.NewGuid();
			var specialistId = Guid.NewGuid();

			var user = new User
			{
				Id = userId,
				FirstName = "Original",
				LastName = "Name",
				Email = "original@test.com",
				Phone = "+380501234567",
				PasswordHash = "hash",
				Role = "Specialist"
			};

			var specialist = new Specialist
			{
				Id = specialistId,
				UserId = userId,
				Education = "Master",
				Experience = "5 years",
				Specialization = "Depression",
				Price = 800,
				Online = true,
				Offline = false,
				Gender = "Male",
				Language = "Ukrainian",
				IsActive = true
			};

			context.Users.Add(user);
			context.Specialists.Add(specialist);
			await context.SaveChangesAsync();

			controller.ControllerContext = CreateControllerContext(userId, "Specialist");

			var updateRequest = new UpdateSpecialistRequest
			{
				Education = "PhD Clinical Psychology",
				Experience = "7 years",
				Specialization = "Anxiety and Depression",
				Price = 1200,
				Online = true,
				Offline = true,
				Language = "English"
			};

			// Act
			var result = await controller.UpdateProfile(updateRequest);

			// Assert
			result.Should().BeOfType<NoContentResult>();

			// Verify changes in database
			var updatedSpecialist = await context.Specialists.FindAsync(specialistId);
			updatedSpecialist!.Education.Should().Be("PhD Clinical Psychology");
			updatedSpecialist.Price.Should().Be(1200);
			updatedSpecialist.Language.Should().Be("English");
			updatedSpecialist.Offline.Should().BeTrue();
		}
	}
}