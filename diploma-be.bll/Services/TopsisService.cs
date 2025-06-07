using diploma_be.bll.Models;
using diploma_be.dal;
using diploma_be.dal.Entities;
using Microsoft.EntityFrameworkCore;

namespace diploma_be.bll.Services
{
	public interface ITopsisService
	{
		Task<List<SpecialistDto>> GetRankedSpecialistsAsync(Guid clientUserId);
		Task<List<SpecialistDto>> CalculateTopsisAsync(TopsisRequest request);
	}

	public class TopsisService : ITopsisService
	{
		private readonly AppDbContext _context;

		public TopsisService(AppDbContext context)
		{
			_context = context;
		}

		public async Task<List<SpecialistDto>> GetRankedSpecialistsAsync(Guid clientUserId)
		{
			// Отримуємо клієнта
			var client = await _context.Clients
				.Include(c => c.User)
				.FirstOrDefaultAsync(c => c.UserId == clientUserId);

			if (client == null)
				throw new Exception("Client not found");

			// Створюємо запит на основі профілю клієнта
			var request = new TopsisRequest
			{
				Budget = client.Budget,
				PreferOnline = client.PreferOnline,
				PreferOffline = client.PreferOffline,
				PreferredGender = client.PreferredGender,
				PreferredLanguage = client.PreferredLanguage,
				Issue = client.Issue
			};

			return await CalculateTopsisAsync(request);
		}

		public async Task<List<SpecialistDto>> CalculateTopsisAsync(TopsisRequest request)
		{
			// Отримуємо всіх активних спеціалістів
			var specialists = await _context.Specialists
				.Include(s => s.User)
				.Where(s => s.IsActive)
				.ToListAsync();

			if (!specialists.Any())
				return new List<SpecialistDto>();

			// Створюємо матриця критеріїв
			var matrix = CreateDecisionMatrix(specialists, request);

			// Розраховуємо TOPSIS
			var scores = CalculateTopsisScores(matrix, request);

			// Створюємо результат з рейтингами
			var result = new List<SpecialistDto>();
			for (int i = 0; i < specialists.Count; i++)
			{
				var specialist = specialists[i];
				result.Add(new SpecialistDto
				{
					Id = specialist.Id,
					FirstName = specialist.User.FirstName,
					LastName = specialist.User.LastName,
					Email = specialist.User.Email,
					Phone = specialist.User.Phone,
					Education = specialist.Education,
					Experience = specialist.Experience,
					Specialization = specialist.Specialization,
					Price = specialist.Price,
					Online = specialist.Online,
					Offline = specialist.Offline,
					Gender = specialist.Gender,
					Language = specialist.Language,
					IsActive = specialist.IsActive,
					TopsisScore = scores[i]
				});
			}

			// Сортуємо за рейтингом (від найкращого до найгіршого)
			return result.OrderByDescending(s => s.TopsisScore).ToList();
		}

		private double[,] CreateDecisionMatrix(List<Specialist> specialists, TopsisRequest request)
		{
			int specialistCount = specialists.Count;
			int criteriaCount = 5; // Ціна, Спеціалізація, Мова, Стать, Онлайн/Офлайн

			var matrix = new double[specialistCount, criteriaCount];

			for (int i = 0; i < specialistCount; i++)
			{
				var specialist = specialists[i];

				// Критерій 1: Ціна (менше = краще, тому інвертуємо)
				matrix[i, 0] = CalculatePriceScore(specialist.Price, request.Budget);

				// Критерій 2: Спеціалізація (співпадіння = краще)
				matrix[i, 1] = CalculateSpecializationScore(specialist.Specialization, request.Issue);

				// Критерій 3: Мова (співпадіння = краще)
				matrix[i, 2] = CalculateLanguageScore(specialist.Language, request.PreferredLanguage);

				// Критерій 4: Стать (співпадіння = краще)
				matrix[i, 3] = CalculateGenderScore(specialist.Gender, request.PreferredGender);

				// Критерій 5: Онлайн/Офлайн (співпадіння потреб = краще)
				matrix[i, 4] = CalculateFormatScore(specialist, request);
			}

			return matrix;
		}

		private double CalculatePriceScore(decimal specialistPrice, decimal clientBudget)
		{
			if (specialistPrice > clientBudget)
				return 0.1; // Дуже низький бал, якщо перевищує бюджет

			// Чим ближче до бюджету, тим краще, але не перевищує
			return 1.0 - (double)(specialistPrice / clientBudget) * 0.3;
		}

		private double CalculateSpecializationScore(string specialistSpec, string clientIssue)
		{
			if (string.IsNullOrEmpty(clientIssue) || string.IsNullOrEmpty(specialistSpec))
				return 0.5; // Нейтральний бал

			// Перевіряємо точне співпадіння або часткове
			if (specialistSpec.ToLower().Contains(clientIssue.ToLower()) ||
				clientIssue.ToLower().Contains(specialistSpec.ToLower()))
				return 1.0;

			// Перевіряємо схожі терміни
			var similarTerms = GetSimilarTerms(clientIssue.ToLower());
			if (similarTerms.Any(term => specialistSpec.ToLower().Contains(term)))
				return 0.7;

			return 0.3; // Мінімальний бал за відсутності співпадінь
		}

		private List<string> GetSimilarTerms(string issue)
		{
			var similarTermsMap = new Dictionary<string, List<string>>
			{
				{"anxiety", new List<string> {"stress", "panic", "worry", "fear"}},
				{"depression", new List<string> {"mood", "sadness", "bipolar"}},
				{"relationships", new List<string> {"family", "couple", "marriage", "divorce"}},
				{"trauma", new List<string> {"ptsd", "abuse", "grief", "loss"}},
				{"addiction", new List<string> {"substance", "alcohol", "dependency"}}
			};

			foreach (var kvp in similarTermsMap)
			{
				if (issue.Contains(kvp.Key))
					return kvp.Value;
			}

			return new List<string>();
		}

		private double CalculateLanguageScore(string specialistLang, string clientLang)
		{
			if (string.IsNullOrEmpty(clientLang))
				return 1.0; // Якщо клієнт не вказав мову

			return specialistLang.ToLower() == clientLang.ToLower() ? 1.0 : 0.2;
		}

		private double CalculateGenderScore(string specialistGender, string clientPreferredGender)
		{
			if (string.IsNullOrEmpty(clientPreferredGender) || clientPreferredGender.ToLower() == "any")
				return 1.0; // Немає переваг

			return specialistGender.ToLower() == clientPreferredGender.ToLower() ? 1.0 : 0.3;
		}

		private double CalculateFormatScore(Specialist specialist, TopsisRequest request)
		{
			double score = 0;

			if (request.PreferOnline && specialist.Online)
				score += 0.5;

			if (request.PreferOffline && specialist.Offline)
				score += 0.5;

			// Якщо спеціаліст пропонує обидва формати, це бонус
			if (specialist.Online && specialist.Offline)
				score += 0.2;

			return Math.Min(score, 1.0);
		}

		private List<double> CalculateTopsisScores(double[,] matrix, TopsisRequest request)
		{
			int alternatives = matrix.GetLength(0); // кількість спеціалістів
			int criteria = matrix.GetLength(1); // кількість критеріїв

			// Вагові коефіцієнти (можна налаштовувати)
			double[] weights = { 0.25, 0.35, 0.15, 0.10, 0.15 }; // Ціна, Спеціалізація, Мова, Стать, Формат

			// Крок 1: Нормалізація матриці
			var normalizedMatrix = NormalizeMatrix(matrix);

			// Крок 2: Зважена нормалізована матриця
			var weightedMatrix = new double[alternatives, criteria];
			for (int i = 0; i < alternatives; i++)
			{
				for (int j = 0; j < criteria; j++)
				{
					weightedMatrix[i, j] = normalizedMatrix[i, j] * weights[j];
				}
			}

			// Крок 3: Ідеальні та негативні ідеальні рішення
			var idealSolution = new double[criteria];
			var negativeIdealSolution = new double[criteria];

			for (int j = 0; j < criteria; j++)
			{
				var columnValues = new List<double>();
				for (int i = 0; i < alternatives; i++)
				{
					columnValues.Add(weightedMatrix[i, j]);
				}

				// Всі критерії - максимізаційні (більше = краще)
				idealSolution[j] = columnValues.Max();
				negativeIdealSolution[j] = columnValues.Min();
			}

			// Крок 4: Відстані до ідеальних рішень
			var scores = new List<double>();

			for (int i = 0; i < alternatives; i++)
			{
				double distanceToIdeal = 0;
				double distanceToNegativeIdeal = 0;

				for (int j = 0; j < criteria; j++)
				{
					distanceToIdeal += Math.Pow(weightedMatrix[i, j] - idealSolution[j], 2);
					distanceToNegativeIdeal += Math.Pow(weightedMatrix[i, j] - negativeIdealSolution[j], 2);
				}

				distanceToIdeal = Math.Sqrt(distanceToIdeal);
				distanceToNegativeIdeal = Math.Sqrt(distanceToNegativeIdeal);

				// Крок 5: Розрахунок TOPSIS коефіцієнта
				double topsisScore = distanceToNegativeIdeal / (distanceToIdeal + distanceToNegativeIdeal);
				scores.Add(topsisScore);
			}

			return scores;
		}

		private double[,] NormalizeMatrix(double[,] matrix)
		{
			int rows = matrix.GetLength(0);
			int cols = matrix.GetLength(1);
			var normalized = new double[rows, cols];

			for (int j = 0; j < cols; j++)
			{
				// Розраховуємо норму для кожного критерію
				double sumOfSquares = 0;
				for (int i = 0; i < rows; i++)
				{
					sumOfSquares += Math.Pow(matrix[i, j], 2);
				}
				double norm = Math.Sqrt(sumOfSquares);

				// Нормалізуємо значення
				for (int i = 0; i < rows; i++)
				{
					normalized[i, j] = norm > 0 ? matrix[i, j] / norm : 0;
				}
			}

			return normalized;
		}
	}
}