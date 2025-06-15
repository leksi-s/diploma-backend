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
				"Тривожність",
				"Депресія",
				"Стосунки",
				"Сімейна терапія",
				"Травма",
				"ПТСР",
				"Залежності",
				"Розлади харчової поведінки",
				"Біполярний розлад",
				"ОКР (обсесивно-компульсивний розлад)",
				"СДУГ (синдром дефіциту уваги)",
				"Горе та втрата",
				"Управління стресом",
				"Кар'єрне консультування",
				"Терапія для пар",
				"Дитяча психологія",
				"Підліткова психологія",
				"Панічні атаки",
				"Фобії",
				"Розлади сну",
				"Емоційне вигорання",
				"Самооцінка",
				"Конфлікти на роботі",
				"Життєві кризи",
				"Психосоматика"
			};

			return Ok(specializations);
		}

		[HttpGet("languages")]
		public ActionResult<List<string>> GetAvailableLanguages()
		{
			var languages = new List<string>
			{
				"Українська",
				"Англійська",
				"Російська",
				"Польська",
				"Німецька",
				"Французька",
				"Іспанська"
			};

			return Ok(languages);
		}

		[HttpGet("genders")]
		public ActionResult<List<string>> GetAvailableGenders()
		{
			var genders = new List<string>
			{
				"Чоловік",
				"Жінка",
				"Будь-яка"
			};

			return Ok(genders);
		}

		[HttpGet("appointment-statuses")]
		public ActionResult<List<string>> GetAppointmentStatuses()
		{
			var statuses = new List<string>
			{
				"Заплановано",
				"Завершено",
				"Скасовано",
				"Не з'явився"
			};

			return Ok(statuses);
		}

		[HttpGet("topsis-explanation")]
		public ActionResult<object> GetTopsisExplanation()
		{
			return Ok(new
			{
				Name = "TOPSIS (Техніка впорядкування переваг за подібністю до ідеального рішення)",
				Description = "Інтелектуальний алгоритм для підбору найкращого психолога на основі ваших індивідуальних потреб",
				Criteria = new[]
				{
					new { Name = "Ціна", Weight = "25%", Description = "Відповідність ціни вашому бюджету" },
					new { Name = "Спеціалізації", Weight = "35%", Description = "Відповідність спеціалізацій психолога вашим проблемам" },
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
					"Оцінка 80%+ означає відмінну відповідність",
					"Оцінка 60-80% означає хорошу відповідність",
					"Оцінка <60% означає часткову відповідність"
				}
			});
		}

		[HttpGet("about")]
		public ActionResult<object> GetAboutInfo()
		{
			return Ok(new
			{
				AppName = "Система підбору психологів",
				Version = "1.0.0",
				Description = "Платформа для пошуку психологів за індивідуальними потребами з використанням алгоритму TOPSIS",
				Features = new[]
				{
					"Реєстрація та автентифікація користувачів",
					"Детальні профілі клієнтів та спеціалістів",
					"Інтелектуальний підбір за алгоритмом TOPSIS",
					"Система записів на консультації",
					"Адміністративна панель",
					"Фільтрація та пошук спеціалістів",
					"Підтримка множинного вибору проблем"
				},
				Developer = "Дипломний проект 2024",
				Contact = "support@psychapp.ua"
			});
		}
	}
}