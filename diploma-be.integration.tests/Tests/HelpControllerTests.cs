using diploma_be.api.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

namespace diploma_be.Tests.Controllers
{
	public class HelpControllerTests
	{
		private readonly HelpController _controller;

		public HelpControllerTests()
		{
			_controller = new HelpController();
		}

		[Fact]
		public void GetAvailableSpecializations_ShouldReturnSpecializationsList()
		{
			// Act
			var result = _controller.GetAvailableSpecializations();

			// Assert
			result.Should().NotBeNull();
			var okResult = result.Result as OkObjectResult;
			okResult.Should().NotBeNull();

			var specializations = okResult!.Value as List<string>;
			specializations.Should().NotBeNull();
			specializations!.Should().NotBeEmpty();
			specializations.Should().Contain("Anxiety");
			specializations.Should().Contain("Depression");
			specializations.Should().Contain("Relationships");
		}

		[Fact]
		public void GetAvailableLanguages_ShouldReturnLanguagesList()
		{
			// Act
			var result = _controller.GetAvailableLanguages();

			// Assert
			result.Should().NotBeNull();
			var okResult = result.Result as OkObjectResult;
			okResult.Should().NotBeNull();

			var languages = okResult!.Value as List<string>;
			languages.Should().NotBeNull();
			languages!.Should().NotBeEmpty();
			languages.Should().Contain("Ukrainian");
			languages.Should().Contain("English");
			languages.Should().Contain("Russian");
		}

		[Fact]
		public void GetAvailableGenders_ShouldReturnGendersList()
		{
			// Act
			var result = _controller.GetAvailableGenders();

			// Assert
			result.Should().NotBeNull();
			var okResult = result.Result as OkObjectResult;
			okResult.Should().NotBeNull();

			var genders = okResult!.Value as List<string>;
			genders.Should().NotBeNull();
			genders!.Should().NotBeEmpty();
			genders.Should().Contain("Male");
			genders.Should().Contain("Female");
			genders.Should().Contain("Any");
		}

		[Fact]
		public void GetAppointmentStatuses_ShouldReturnStatusesList()
		{
			// Act
			var result = _controller.GetAppointmentStatuses();

			// Assert
			result.Should().NotBeNull();
			var okResult = result.Result as OkObjectResult;
			okResult.Should().NotBeNull();

			var statuses = okResult!.Value as List<string>;
			statuses.Should().NotBeNull();
			statuses!.Should().NotBeEmpty();
			statuses.Should().Contain("Scheduled");
			statuses.Should().Contain("Completed");
			statuses.Should().Contain("Cancelled");
			statuses.Should().Contain("NoShow");
		}

		[Fact]
		public void GetTopsisExplanation_ShouldReturnTopsisInformation()
		{
			// Act
			var result = _controller.GetTopsisExplanation();

			// Assert
			result.Should().NotBeNull();
			var okResult = result.Result as OkObjectResult;
			okResult.Should().NotBeNull();

			var explanation = okResult!.Value;
			explanation.Should().NotBeNull();

			// Verify the structure contains expected properties
			var explanationDict = explanation!.GetType().GetProperties()
				.ToDictionary(prop => prop.Name, prop => prop.GetValue(explanation));

			explanationDict.Should().ContainKey("Name");
			explanationDict.Should().ContainKey("Description");
			explanationDict.Should().ContainKey("Criteria");
			explanationDict.Should().ContainKey("HowItWorks");
			explanationDict.Should().ContainKey("Tips");

			// Verify content
			explanationDict["Name"].Should().NotBeNull();
			explanationDict["Name"].ToString().Should().Contain("TOPSIS");
		}

		[Fact]
		public void GetAboutInfo_ShouldReturnApplicationInformation()
		{
			// Act
			var result = _controller.GetAboutInfo();

			// Assert
			result.Should().NotBeNull();
			var okResult = result.Result as OkObjectResult;
			okResult.Should().NotBeNull();

			var aboutInfo = okResult!.Value;
			aboutInfo.Should().NotBeNull();

			// Verify the structure contains expected properties
			var aboutDict = aboutInfo!.GetType().GetProperties()
				.ToDictionary(prop => prop.Name, prop => prop.GetValue(aboutInfo));

			aboutDict.Should().ContainKey("Features");
			aboutDict.Should().ContainKey("Developer");
			aboutDict.Should().ContainKey("Contact");

			// Verify content
			aboutDict["AppName"].Should().NotBeNull();
			aboutDict["AppName"].ToString().Should().Contain("Psychology");
			aboutDict["Version"].Should().NotBeNull();
		}


		[Fact]
		public void GetAvailableSpecializations_ShouldContainExpectedSpecializations()
		{
			// Act
			var result = _controller.GetAvailableSpecializations();
			var okResult = result.Result as OkObjectResult;
			var specializations = okResult!.Value as List<string>;

			// Assert - Check for specific important specializations
			specializations.Should().Contain("Anxiety");
			specializations.Should().Contain("Depression");
			specializations.Should().Contain("PTSD");
			specializations.Should().Contain("Family Therapy");
			specializations.Should().Contain("Couples Therapy");
			specializations.Should().Contain("Addiction");
			specializations.Should().Contain("Child Psychology");

			// Should have reasonable number of specializations
			specializations!.Count.Should().BeGreaterThan(10);
		}

		[Fact]
		public void GetAvailableLanguages_ShouldContainCommonLanguages()
		{
			// Act
			var result = _controller.GetAvailableLanguages();
			var okResult = result.Result as OkObjectResult;
			var languages = okResult!.Value as List<string>;

			// Assert - Check for expected languages in Ukraine context
			languages.Should().Contain("Ukrainian");
			languages.Should().Contain("English");
			languages.Should().Contain("Russian");

			// Should have reasonable number of languages
			languages!.Count.Should().BeGreaterThan(3);
			languages.Count.Should().BeLessThan(20); // Not too many
		}
	}
}