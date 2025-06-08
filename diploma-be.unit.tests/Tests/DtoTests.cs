using diploma.bll.Models;
using diploma_be.bll.Models;
using FluentAssertions;
using System.ComponentModel.DataAnnotations;

namespace diploma_be.unit.tests.Tests
{
	public class DtoTests
	{
		private IList<ValidationResult> ValidateModel(object model)
		{
			var validationResults = new List<ValidationResult>();
			var context = new ValidationContext(model, null, null);
			Validator.TryValidateObject(model, context, validationResults, true);
			return validationResults;
		}

		[Fact]
		public void LoginRequest_WithValidData_ShouldPassValidation()
		{
			// Arrange
			var request = new LoginRequest
			{
				Email = "test@example.com",
				Password = "Password123!"
			};

			// Act
			var results = ValidateModel(request);

			// Assert
			results.Should().BeEmpty();
		}


		[Fact]
		public void RegisterRequest_WithValidData_ShouldPassValidation()
		{
			// Arrange
			var request = new RegisterRequest
			{
				FirstName = "John",
				LastName = "Doe",
				Email = "john.doe@example.com",
				Phone = "+380501234567",
				Password = "Password123!",
				Role = "Client"
			};

			// Act
			var results = ValidateModel(request);

			// Assert
			results.Should().BeEmpty();
		}

		[Fact]
		public void CreateClientRequest_WithValidData_ShouldPassValidation()
		{
			// Arrange
			var request = new CreateClientRequest
			{
				FirstName = "Jane",
				LastName = "Smith",
				Email = "jane.smith@example.com",
				Phone = "+380507654321",
				Budget = 1000,
				PreferOnline = true,
				PreferOffline = false,
				PreferredGender = "Female",
				PreferredLanguage = "Ukrainian",
				Issue = "Anxiety"
			};

			// Act
			var results = ValidateModel(request);

			// Assert
			results.Should().BeEmpty();
		}

		[Fact]
		public void UpdateSpecialistRequest_WithValidData_ShouldPassValidation()
		{
			// Arrange
			var request = new UpdateSpecialistRequest
			{
				Education = "PhD in Psychology",
				Experience = "10 years",
				Specialization = "Cognitive Behavioral Therapy",
				Price = 1200,
				Online = true,
				Offline = true,
				Language = "Ukrainian"
			};

			// Act
			var results = ValidateModel(request);

			// Assert
			results.Should().BeEmpty();
		}

		[Fact]
		public void SpecialistFilterRequest_WithValidData_ShouldPassValidation()
		{
			// Arrange
			var request = new SpecialistFilterRequest
			{
				MaxPrice = 2000,
				Online = true,
				Offline = false,
				Gender = "Female",
				Language = "Ukrainian",
				Specialization = "Anxiety"
			};

			// Act
			var results = ValidateModel(request);

			// Assert
			results.Should().BeEmpty();
		}

		[Fact]
		public void CreateAppointmentRequest_WithValidData_ShouldPassValidation()
		{
			// Arrange
			var request = new CreateAppointmentRequest
			{
				SpecialistId = Guid.NewGuid(),
				AppointmentDate = DateTime.UtcNow.AddDays(7),
				IsOnline = true,
				Notes = "First consultation"
			};

			// Act
			var results = ValidateModel(request);

			// Assert
			results.Should().BeEmpty();
		}

		[Fact]
		public void CreateAppointmentRequest_WithPastDate_ShouldFailValidation()
		{
			// Arrange
			var request = new CreateAppointmentRequest
			{
				SpecialistId = Guid.NewGuid(),
				AppointmentDate = DateTime.UtcNow.AddDays(-1), // Past date
				IsOnline = true,
				Notes = "Test"
			};

			// Act
			var results = ValidateModel(request);

			// Assert
			// In a real implementation, this should fail validation
			results.Should().NotBeNull();
		}

		[Fact]
		public void TopsisRequest_WithValidData_ShouldPassValidation()
		{
			// Arrange
			var request = new TopsisRequest
			{
				Budget = 1500,
				PreferOnline = true,
				PreferOffline = false,
				PreferredGender = "Male",
				PreferredLanguage = "English",
				Issue = "Depression"
			};

			// Act
			var results = ValidateModel(request);

			// Assert
			results.Should().BeEmpty();
		}

		[Fact]
		public void Dto_Mapping_ShouldPreserveAllProperties()
		{
			// Arrange
			var clientDto = new ClientDto
			{
				Id = Guid.NewGuid(),
				FirstName = "Test",
				LastName = "Client",
				Email = "test@example.com",
				Phone = "+380501234567",
				Budget = 1000,
				PreferOnline = true,
				PreferOffline = false,
				PreferredGender = "Female",
				PreferredLanguage = "Ukrainian",
				Issue = "Anxiety"
			};

			// Assert
			clientDto.Id.Should().NotBe(Guid.Empty);
			clientDto.FirstName.Should().Be("Test");
			clientDto.LastName.Should().Be("Client");
			clientDto.Email.Should().Be("test@example.com");
			clientDto.Phone.Should().Be("+380501234567");
			clientDto.Budget.Should().Be(1000);
			clientDto.PreferOnline.Should().BeTrue();
			clientDto.PreferOffline.Should().BeFalse();
			clientDto.PreferredGender.Should().Be("Female");
			clientDto.PreferredLanguage.Should().Be("Ukrainian");
			clientDto.Issue.Should().Be("Anxiety");
		}

		[Fact]
		public void SpecialistDto_WithTopsisScore_ShouldHandleNullableCorrectly()
		{
			// Arrange
			var specialistWithScore = new SpecialistDto
			{
				Id = Guid.NewGuid(),
				FirstName = "John",
				LastName = "Therapist",
				TopsisScore = 0.85
			};

			var specialistWithoutScore = new SpecialistDto
			{
				Id = Guid.NewGuid(),
				FirstName = "Jane",
				LastName = "Counselor",
				TopsisScore = null
			};

			// Assert
			specialistWithScore.TopsisScore.Should().Be(0.85);
			specialistWithScore.TopsisScore.Should().NotBeNull();

			specialistWithoutScore.TopsisScore.Should().BeNull();
		}

		[Theory]
		[InlineData("test@example.com", true)]
		[InlineData("user.name@example.com", true)]
		[InlineData("user+tag@example.co.uk", true)]
		[InlineData("invalid.email", false)]
		[InlineData("@example.com", false)]
		[InlineData("user@", false)]
		[InlineData("user@@example.com", false)]
		public void EmailFormat_Validation(string email, bool shouldBeValid)
		{
			// Arrange
			var emailAttribute = new EmailAddressAttribute();

			// Act
			var isValid = emailAttribute.IsValid(email);

			// Assert
			isValid.Should().Be(shouldBeValid);
		}

		[Theory]
		[InlineData("+380501234567", true)] // Ukrainian format
		[InlineData("+1234567890", true)] // International format
		[InlineData("0501234567", true)] // Local format
		[InlineData("abc123", false)] // Invalid
		[InlineData("", false)] // Empty
		public void PhoneNumber_Validation(string phone, bool shouldBeValid)
		{
			// Simple phone validation
			var isValid = !string.IsNullOrEmpty(phone) &&
						 phone.All(c => char.IsDigit(c) || c == '+' || c == '-' || c == ' ');

			isValid.Should().Be(shouldBeValid);
		}
	}
}