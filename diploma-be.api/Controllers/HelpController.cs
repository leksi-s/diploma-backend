using Microsoft.AspNetCore.Mvc;

namespace diploma_be.api.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class HelpController : ControllerBase
	{
		[HttpGet("specializations")]
		public ActionResult<List<string>> GetAvailableSpecializations()
		{
			var specializations = new List<string>
			{
				"Anxiety",
				"Depression",
				"Relationships",
				"Family Therapy",
				"Trauma",
				"PTSD",
				"Addiction",
				"Eating Disorders",
				"Bipolar Disorder",
				"OCD",
				"ADHD",
				"Grief Counseling",
				"Stress Management",
				"Career Counseling",
				"Couples Therapy",
				"Child Psychology",
				"Adolescent Psychology"
			};

			return Ok(specializations);
		}

		[HttpGet("languages")]
		public ActionResult<List<string>> GetAvailableLanguages()
		{
			var languages = new List<string>
			{
				"Ukrainian",
				"English",
				"Russian",
				"Polish",
				"German",
				"French",
				"Spanish"
			};

			return Ok(languages);
		}

		[HttpGet("genders")]
		public ActionResult<List<string>> GetAvailableGenders()
		{
			var genders = new List<string>
			{
				"Male",
				"Female",
				"Any"
			};

			return Ok(genders);
		}

		[HttpGet("appointment-statuses")]
		public ActionResult<List<string>> GetAppointmentStatuses()
		{
			var statuses = new List<string>
			{
				"Scheduled",
				"Completed",
				"Cancelled",
				"NoShow"
			};

			return Ok(statuses);
		}

		[HttpGet("topsis-explanation")]
		public ActionResult<object> GetTopsisExplanation()
		{
			return Ok(new
			{
				Name = "TOPSIS (Technique for Order Preference by Similarity to Ideal Solution)",
				Description = "Метод багатокритеріального прийняття рішень для підбору оптимального спеціаліста",
				Criteria = new[]
				{
					new { Name = "Ціна", Weight = "25%", Description = "Відповідність ціни вашому бюджету" },
					new { Name = "Спеціалізація", Weight = "35%", Description = "Відповідність спеціалізації вашим проблемам" },
					new { Name = "Мова", Weight = "15%", Description = "Спільна мова спілкування" },
					new { Name = "Стать", Weight = "10%", Description = "Відповідність вашим уподобанням" },
					new { Name = "Формат", Weight = "15%", Description = "Онлайн/офлайн консультації" }
				},
				HowItWorks = new[]
				{
					"1. Аналізуємо ваші потреби та вподобання",
					"2. Оцінюємо кожного спеціаліста за всіма критеріями",
					"3. Розраховуємо відстань до ідеального варіанту",
					"4. Ранжуємо спеціалістів за релевантністю (0-100%)"
				},
				Tips = new[]
				{
					"Бал 80%+ означає відмінну відповідність",
					"Бал 60-80% означає хорошу відповідність",
					"Бал <60% означає часткову відповідність"
				}
			});
		}

		[HttpGet("about")]
		public ActionResult<object> GetAboutInfo()
		{
			return Ok(new
			{
				AppName = "Psychology Matching System",
				Version = "1.0.0",
				Description = "Система підбору психологів за індивідуальними потребами з використанням алгоритму TOPSIS",
				Features = new[]
				{
					"Реєстрація та автентифікація користувачів",
					"Профілі клієнтів та спеціалістів",
					"Інтелектуальний підбір за алгоритмом TOPSIS",
					"Система записів на консультації",
					"Адміністративна панель",
					"Фільтрація та пошук спеціалістів"
				},
				Developer = "Diploma Project 2024",
				Contact = "support@psychapp.com"
			});
		}
	}
}