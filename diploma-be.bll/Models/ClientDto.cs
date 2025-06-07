namespace diploma_be.bll.Models
{
	public class ClientDto
	{
		public Guid Id { get; set; }
		public string FirstName { get; set; } = string.Empty;
		public string LastName { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
		public string Phone { get; set; } = string.Empty;
		public decimal Budget { get; set; }
		public bool PreferOnline { get; set; }
		public bool PreferOffline { get; set; }
		public string PreferredGender { get; set; } = string.Empty;
		public string PreferredLanguage { get; set; } = string.Empty;
		public string Issue { get; set; } = string.Empty;
	}

	public class UpdateClientRequest
	{
		public decimal Budget { get; set; }
		public bool PreferOnline { get; set; }
		public bool PreferOffline { get; set; }
		public string PreferredGender { get; set; } = string.Empty;
		public string PreferredLanguage { get; set; } = string.Empty;
		public string Issue { get; set; } = string.Empty;
	}

	public class TopsisRequest
	{
		public decimal Budget { get; set; }
		public bool PreferOnline { get; set; }
		public bool PreferOffline { get; set; }
		public string PreferredGender { get; set; } = string.Empty;
		public string PreferredLanguage { get; set; } = string.Empty;
		public string Issue { get; set; } = string.Empty;
	}

	// Додаткові моделі для покращеного TOPSIS
	public class DetailedTopsisRequest
	{
		public decimal Budget { get; set; }
		public bool PreferOnline { get; set; }
		public bool PreferOffline { get; set; }
		public string PreferredGender { get; set; } = string.Empty;
		public string PreferredLanguage { get; set; } = string.Empty;
		public string Issue { get; set; } = string.Empty;

		// Вагові коефіцієнти (опціонально)
		public double PriceWeight { get; set; } = 0.25;
		public double SpecializationWeight { get; set; } = 0.35;
		public double LanguageWeight { get; set; } = 0.15;
		public double GenderWeight { get; set; } = 0.10;
		public double FormatWeight { get; set; } = 0.15;

		// Додаткові критерії
		public int UrgencyLevel { get; set; } = 3; // 1-5 (1=не терміново, 5=дуже терміново)
		public bool HasPreviousTherapyExperience { get; set; }
		public string AdditionalNotes { get; set; } = string.Empty;
	}

	public class TopsisResultDto
	{
		public List<SpecialistDto> RankedSpecialists { get; set; } = new();
		public TopsisAnalysisDto Analysis { get; set; } = new();
		public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
	}

	public class TopsisAnalysisDto
	{
		public int TotalSpecialistsEvaluated { get; set; }
		public decimal AveragePrice { get; set; }
		public decimal MinPrice { get; set; }
		public decimal MaxPrice { get; set; }
		public List<string> AvailableSpecializations { get; set; } = new();
		public List<string> MatchingCriteria { get; set; } = new();
		public List<string> Recommendations { get; set; } = new();
		public Dictionary<string, double> UsedWeights { get; set; } = new();
	}
}
