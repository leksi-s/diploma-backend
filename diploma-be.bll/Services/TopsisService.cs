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
			var client = await _context.Clients
				.Include(c => c.User)
				.FirstOrDefaultAsync(c => c.UserId == clientUserId);

			if (client == null)
				throw new Exception("Клієнта не знайдено");

			var request = new TopsisRequest
			{
				Budget = client.Budget,
				PreferOnline = client.PreferOnline,
				PreferOffline = client.PreferOffline,
				PreferredGender = client.PreferredGender,
				PreferredLanguage = client.PreferredLanguage,
				Issues = client.GetIssuesList()
			};

			return await CalculateTopsisAsync(request);
		}

		public async Task<List<SpecialistDto>> CalculateTopsisAsync(TopsisRequest request)
		{
			var specialists = await _context.Specialists
				.Include(s => s.User)
				.Where(s => s.IsActive)
				.ToListAsync();

			if (!specialists.Any())
				return new List<SpecialistDto>();

			var matrix = CreateDecisionMatrix(specialists, request);
			var scores = CalculateTopsisScores(matrix, request);

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
					Specializations = specialist.GetSpecializationsList(),
					Price = specialist.Price,
					Online = specialist.Online,
					Offline = specialist.Offline,
					Gender = specialist.Gender,
					Language = specialist.Language,
					IsActive = specialist.IsActive,
					TopsisScore = scores[i]
				});
			}

			return result.OrderByDescending(s => s.TopsisScore).ToList();
		}

		private double[,] CreateDecisionMatrix(List<Specialist> specialists, TopsisRequest request)
		{
			int specialistCount = specialists.Count;
			int criteriaCount = 5;

			var matrix = new double[specialistCount, criteriaCount];

			for (int i = 0; i < specialistCount; i++)
			{
				var specialist = specialists[i];

				matrix[i, 0] = CalculatePriceScore(specialist.Price, request.Budget);
				matrix[i, 1] = CalculateSpecializationsScore(specialist.GetSpecializationsList(), request.Issues);
				matrix[i, 2] = CalculateLanguageScore(specialist.Language, request.PreferredLanguage);
				matrix[i, 3] = CalculateGenderScore(specialist.Gender, request.PreferredGender);
				matrix[i, 4] = CalculateFormatScore(specialist, request);
			}

			return matrix;
		}

		private double CalculatePriceScore(decimal specialistPrice, decimal clientBudget)
		{
			if (specialistPrice > clientBudget)
				return 0.1;
			return 1.0 - (double)(specialistPrice / clientBudget) * 0.3;
		}
		private double CalculateSpecializationsScore(List<string> specialistSpecs, List<string> clientIssues)
		{
			if (!clientIssues.Any() || !specialistSpecs.Any())
				return 0.5;

			double totalScore = 0;
			int matchCount = 0;

			foreach (var issue in clientIssues)
			{
				double bestMatchForIssue = 0;

				foreach (var spec in specialistSpecs)
				{
					double matchScore = 0;

					if (spec.ToLower().Contains(issue.ToLower()) ||
						issue.ToLower().Contains(spec.ToLower()))
					{
						matchScore = 1.0;
					}
					else
					{
						var similarTerms = GetSimilarTerms(issue.ToLower());
						if (similarTerms.Any(term => spec.ToLower().Contains(term)))
						{
							matchScore = 0.7;
						}
					}

					bestMatchForIssue = Math.Max(bestMatchForIssue, matchScore);
				}

				totalScore += bestMatchForIssue;
				if (bestMatchForIssue > 0.5) matchCount++;
			}

			double avgScore = totalScore / clientIssues.Count;
			double coverageBonus = (double)matchCount / clientIssues.Count * 0.2;

			return Math.Min(avgScore + coverageBonus, 1.0);
		}

		private List<string> GetSimilarTerms(string issue)
		{
			var similarTermsMap = new Dictionary<string, List<string>>
			{
				{"тривожність", new List<string> {"стрес", "паніка", "хвилювання", "страх", "панічні атаки"}},
				{"депресія", new List<string> {"настрій", "сум", "біполярний", "апатія"}},
				{"стосунки", new List<string> {"сім'я", "пара", "шлюб", "розлучення", "конфлікти"}},
				{"травма", new List<string> {"птср", "насильство", "горе", "втрата"}},
				{"залежності", new List<string> {"алкоголь", "наркотики", "ігроманія"}},
				{"самооцінка", new List<string> {"впевненість", "прийняття себе", "комплекси"}},
				{"стрес", new List<string> {"вигорання", "перевантаження", "тиск"}}
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
				return 1.0;

			return specialistLang.ToLower() == clientLang.ToLower() ? 1.0 : 0.2;
		}

		private double CalculateGenderScore(string specialistGender, string clientPreferredGender)
		{
			if (string.IsNullOrEmpty(clientPreferredGender) ||
				clientPreferredGender.ToLower() == "будь-яка" ||
				clientPreferredGender.ToLower() == "any")
				return 1.0;

			return specialistGender.ToLower() == clientPreferredGender.ToLower() ? 1.0 : 0.3;
		}

		private double CalculateFormatScore(Specialist specialist, TopsisRequest request)
		{
			double score = 0;

			if (request.PreferOnline && specialist.Online)
				score += 0.5;

			if (request.PreferOffline && specialist.Offline)
				score += 0.5;

			if (specialist.Online && specialist.Offline)
				score += 0.2;

			return Math.Min(score, 1.0);
		}

		private List<double> CalculateTopsisScores(double[,] matrix, TopsisRequest request)
		{
			int alternatives = matrix.GetLength(0);
			int criteria = matrix.GetLength(1);

			double[] weights = { 0.25, 0.35, 0.15, 0.10, 0.15 };

			var normalizedMatrix = NormalizeMatrix(matrix);

			var weightedMatrix = new double[alternatives, criteria];
			for (int i = 0; i < alternatives; i++)
			{
				for (int j = 0; j < criteria; j++)
				{
					weightedMatrix[i, j] = normalizedMatrix[i, j] * weights[j];
				}
			}

			var idealSolution = new double[criteria];
			var negativeIdealSolution = new double[criteria];

			for (int j = 0; j < criteria; j++)
			{
				var columnValues = new List<double>();
				for (int i = 0; i < alternatives; i++)
				{
					columnValues.Add(weightedMatrix[i, j]);
				}

				idealSolution[j] = columnValues.Max();
				negativeIdealSolution[j] = columnValues.Min();
			}

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
				double sumOfSquares = 0;
				for (int i = 0; i < rows; i++)
				{
					sumOfSquares += Math.Pow(matrix[i, j], 2);
				}
				double norm = Math.Sqrt(sumOfSquares);

				for (int i = 0; i < rows; i++)
				{
					normalized[i, j] = norm > 0 ? matrix[i, j] / norm : 0;
				}
			}

			return normalized;
		}
	}
}