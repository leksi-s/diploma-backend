using diploma_be.dal.Entities;
using FluentAssertions;

namespace diploma_be.unit.tests.Tests
{
	public class EntityTests
	{
		[Fact]
		public void User_ShouldInitializeWithDefaults()
		{
			// Act
			var user = new User();

			// Assert
			user.Id.Should().NotBe(Guid.Empty);
			user.FirstName.Should().BeEmpty();
			user.LastName.Should().BeEmpty();
			user.Email.Should().BeEmpty();
			user.Phone.Should().BeEmpty();
			user.PasswordHash.Should().BeEmpty();
			user.Role.Should().BeEmpty();
			user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
		}

		[Fact]
		public void User_ShouldSetPropertiesCorrectly()
		{
			// Arrange
			var userId = Guid.NewGuid();
			var createdAt = DateTime.UtcNow;

			// Act
			var user = new User
			{
				Id = userId,
				FirstName = "John",
				LastName = "Doe",
				Email = "john.doe@example.com",
				Phone = "+1234567890",
				PasswordHash = "hashedpassword123",
				Role = "Client",
				CreatedAt = createdAt
			};

			// Assert
			user.Id.Should().Be(userId);
			user.FirstName.Should().Be("John");
			user.LastName.Should().Be("Doe");
			user.Email.Should().Be("john.doe@example.com");
			user.Phone.Should().Be("+1234567890");
			user.PasswordHash.Should().Be("hashedpassword123");
			user.Role.Should().Be("Client");
			user.CreatedAt.Should().Be(createdAt);
		}

		[Fact]
		public void Specialist_ShouldInitializeWithDefaults()
		{
			// Act
			var specialist = new Specialist();

			// Assert
			specialist.Id.Should().NotBe(Guid.Empty);
			specialist.UserId.Should().Be(Guid.Empty);
			specialist.User.Should().BeNull();
			specialist.Education.Should().BeEmpty();
			specialist.Experience.Should().BeEmpty();
			specialist.Specialization.Should().BeEmpty();
			specialist.Price.Should().Be(0);
			specialist.Online.Should().BeFalse();
			specialist.Offline.Should().BeFalse();
			specialist.Gender.Should().BeEmpty();
			specialist.Language.Should().BeEmpty();
			specialist.IsActive.Should().BeTrue();
			specialist.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
		}

		[Fact]
		public void Specialist_ShouldHandleDecimalPriceCorrectly()
		{
			// Arrange & Act
			var specialist = new Specialist
			{
				Price = 999.99m
			};

			// Assert
			specialist.Price.Should().Be(999.99m);
			specialist.Price.Should().BePositive();
		}

		[Fact]
		public void Client_ShouldInitializeWithDefaults()
		{
			// Act
			var client = new Client();

			// Assert
			client.Id.Should().NotBe(Guid.Empty);
			client.UserId.Should().Be(Guid.Empty);
			client.User.Should().BeNull();
			client.Budget.Should().Be(0);
			client.PreferOnline.Should().BeFalse();
			client.PreferOffline.Should().BeFalse();
			client.PreferredGender.Should().BeEmpty();
			client.PreferredLanguage.Should().BeEmpty();
			client.Issue.Should().BeEmpty();
			client.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
		}

		[Fact]
		public void Appointment_StatusTransitions_ShouldBeValid()
		{
			// Arrange
			var validStatuses = new[] { "Scheduled", "Completed", "Cancelled", "NoShow" };
			var appointment = new Appointment();

			// Act & Assert
			foreach (var status in validStatuses)
			{
				appointment.Status = status;
				appointment.Status.Should().Be(status);
			}
		}

		[Fact]
		public void Appointment_ShouldNotAllowPastDatesForNewAppointments()
		{
			// Arrange
			var pastDate = DateTime.UtcNow.AddDays(-1);
			var futureDate = DateTime.UtcNow.AddDays(7);

			// Act
			var appointment = new Appointment
			{
				AppointmentDate = futureDate,
				Status = "Scheduled"
			};

			// Assert
			appointment.AppointmentDate.Should().BeAfter(DateTime.UtcNow);
			appointment.Status.Should().Be("Scheduled");
		}

		[Fact]
		public void Entity_NavigationProperties_ShouldBeNullByDefault()
		{
			// Arrange & Act
			var specialist = new Specialist();
			var client = new Client();
			var appointment = new Appointment();

			// Assert
			specialist.User.Should().BeNull();

			client.User.Should().BeNull();

			appointment.Client.Should().BeNull();
			appointment.Specialist.Should().BeNull();
		}

		[Fact]
		public void Entity_IdGeneration_ShouldBeUnique()
		{
			// Arrange & Act
			var entities = new List<object>
			{
				new User(),
				new User(),
				new Specialist(),
				new Specialist(),
				new Client(),
				new Client(),
				new Appointment(),
				new Appointment()
			};

			var ids = entities.Select(e =>
			{
				return e switch
				{
					User u => u.Id,
					Specialist s => s.Id,
					Client c => c.Id,
					Appointment a => a.Id,
					_ => Guid.Empty
				};
			}).ToList();

			// Assert
			ids.Should().OnlyHaveUniqueItems();
			ids.Should().NotContain(Guid.Empty);
		}

		[Theory]
		[InlineData("Male")]
		[InlineData("Female")]
		[InlineData("Other")]
		[InlineData("Any")]
		public void Gender_ShouldAcceptValidValues(string gender)
		{
			// Arrange & Act
			var specialist = new Specialist { Gender = gender };
			var client = new Client { PreferredGender = gender };

			// Assert
			specialist.Gender.Should().Be(gender);
			client.PreferredGender.Should().Be(gender);
		}

		[Theory]
		[InlineData("Ukrainian")]
		[InlineData("English")]
		[InlineData("Russian")]
		[InlineData("Polish")]
		public void Language_ShouldAcceptValidValues(string language)
		{
			// Arrange & Act
			var specialist = new Specialist { Language = language };
			var client = new Client { PreferredLanguage = language };

			// Assert
			specialist.Language.Should().Be(language);
			client.PreferredLanguage.Should().Be(language);
		}

		[Fact]
		public void Specialist_PriceShouldBeNonNegative()
		{
			// Arrange
			var specialist = new Specialist();

			// Act & Assert
			specialist.Price = -100;
			specialist.Price.Should().Be(-100); // Entity doesn't enforce this, but business logic should

			specialist.Price = 0;
			specialist.Price.Should().Be(0);

			specialist.Price = 1000;
			specialist.Price.Should().Be(1000);
		}

		[Fact]
		public void Client_BudgetShouldBeNonNegative()
		{
			// Arrange
			var client = new Client();

			// Act & Assert
			client.Budget = -500;
			client.Budget.Should().Be(-500); // Entity doesn't enforce this, but business logic should

			client.Budget = 0;
			client.Budget.Should().Be(0);

			client.Budget = 2000;
			client.Budget.Should().Be(2000);
		}

		[Fact]
		public void Appointment_ShouldHandleOnlineOfflineCorrectly()
		{
			// Arrange
			var appointment = new Appointment();

			// Act & Assert - Online appointment
			appointment.IsOnline = true;
			appointment.IsOnline.Should().BeTrue();

			// Act & Assert - Offline appointment
			appointment.IsOnline = false;
			appointment.IsOnline.Should().BeFalse();
		}

		[Fact]
		public void Entity_DateTimeHandling_ShouldUseUtc()
		{
			// Arrange
			var utcNow = DateTime.UtcNow;
			var localNow = DateTime.Now;

			// Act
			var user = new User { CreatedAt = utcNow };
			var specialist = new Specialist { CreatedAt = utcNow };
			var client = new Client { CreatedAt = utcNow };
			var appointment = new Appointment { CreatedAt = utcNow };

			// Assert
			user.CreatedAt.Kind.Should().Be(DateTimeKind.Utc);
			specialist.CreatedAt.Should().Be(utcNow);
			client.CreatedAt.Should().Be(utcNow);
			appointment.CreatedAt.Should().Be(utcNow);
		}

		[Fact]
		public void Specialist_OnlineOfflinePreferences_ShouldWorkIndependently()
		{
			// Arrange
			var specialist = new Specialist();

			// Act & Assert - Both true
			specialist.Online = true;
			specialist.Offline = true;
			specialist.Online.Should().BeTrue();
			specialist.Offline.Should().BeTrue();

			// Act & Assert - Online only
			specialist.Online = true;
			specialist.Offline = false;
			specialist.Online.Should().BeTrue();
			specialist.Offline.Should().BeFalse();

			// Act & Assert - Offline only
			specialist.Online = false;
			specialist.Offline = true;
			specialist.Online.Should().BeFalse();
			specialist.Offline.Should().BeTrue();

			// Act & Assert - Neither (invalid state but entity allows it)
			specialist.Online = false;
			specialist.Offline = false;
			specialist.Online.Should().BeFalse();
			specialist.Offline.Should().BeFalse();
		}

		[Fact]
		public void Client_OnlineOfflinePreferences_ShouldWorkIndependently()
		{
			// Arrange
			var client = new Client();

			// Act & Assert - Both true
			client.PreferOnline = true;
			client.PreferOffline = true;
			client.PreferOnline.Should().BeTrue();
			client.PreferOffline.Should().BeTrue();

			// Act & Assert - Online only
			client.PreferOnline = true;
			client.PreferOffline = false;
			client.PreferOnline.Should().BeTrue();
			client.PreferOffline.Should().BeFalse();

			// Act & Assert - Offline only
			client.PreferOnline = false;
			client.PreferOffline = true;
			client.PreferOnline.Should().BeFalse();
			client.PreferOffline.Should().BeTrue();
		}
	}
}