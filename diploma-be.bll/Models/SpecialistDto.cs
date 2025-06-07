namespace diploma_be.bll.Models
{
	public class SpecialistDto
	{
		public Guid Id { get; set; }
		public string FirstName { get; set; } = string.Empty;
		public string LastName { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
		public string Phone { get; set; } = string.Empty;
		public string Education { get; set; } = string.Empty;
		public string Experience { get; set; } = string.Empty;
		public string Specialization { get; set; } = string.Empty;
		public decimal Price { get; set; }
		public bool Online { get; set; }
		public bool Offline { get; set; }
		public string Gender { get; set; } = string.Empty;
		public string Language { get; set; } = string.Empty;
		public bool IsActive { get; set; }
		public double? TopsisScore { get; set; }
		public int? TopsisRank { get; set; }
	}

	public class CreateSpecialistRequest
	{
		public string FirstName { get; set; } = string.Empty;
		public string LastName { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
		public string Phone { get; set; } = string.Empty;
		public string Password { get; set; } = string.Empty;
		public string Education { get; set; } = string.Empty;
		public string Experience { get; set; } = string.Empty;
		public string Specialization { get; set; } = string.Empty;
		public decimal Price { get; set; }
		public bool Online { get; set; }
		public bool Offline { get; set; }
		public string Gender { get; set; } = string.Empty;
		public string Language { get; set; } = string.Empty;
	}

	public class UpdateSpecialistRequest
	{
		public string Education { get; set; } = string.Empty;
		public string Experience { get; set; } = string.Empty;
		public string Specialization { get; set; } = string.Empty;
		public decimal Price { get; set; }
		public bool Online { get; set; }
		public bool Offline { get; set; }
		public string Language { get; set; } = string.Empty;
	}

	public class SpecialistFilterRequest
	{
		public decimal? MaxPrice { get; set; }
		public bool? Online { get; set; }
		public bool? Offline { get; set; }
		public string? Gender { get; set; }
		public string? Language { get; set; }
		public string? Specialization { get; set; }
	}
}
