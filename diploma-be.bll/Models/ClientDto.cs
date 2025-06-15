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
		public List<string> Issues { get; set; } = new();
	}

	public class UpdateClientRequest
	{
		public decimal Budget { get; set; }
		public bool PreferOnline { get; set; }
		public bool PreferOffline { get; set; }
		public string PreferredGender { get; set; } = string.Empty;
		public string PreferredLanguage { get; set; } = string.Empty;
		public List<string> Issues { get; set; } = new();
	}

	public class TopsisRequest
	{
		public decimal Budget { get; set; }
		public bool PreferOnline { get; set; }
		public bool PreferOffline { get; set; }
		public string PreferredGender { get; set; } = string.Empty;
		public string PreferredLanguage { get; set; } = string.Empty;
		public List<string> Issues { get; set; } = new();
	}

	public class CreateClientRequest
	{
		public string FirstName { get; set; } = string.Empty;
		public string LastName { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
		public string Phone { get; set; } = string.Empty;
		public decimal Budget { get; set; }
		public bool PreferOnline { get; set; }
		public bool PreferOffline { get; set; }
		public string PreferredGender { get; set; } = string.Empty;
		public string PreferredLanguage { get; set; } = string.Empty;
		public List<string> Issues { get; set; } = new();
	}
}