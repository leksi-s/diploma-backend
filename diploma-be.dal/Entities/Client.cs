using System.ComponentModel.DataAnnotations.Schema;

namespace diploma_be.dal.Entities
{
	public class Client
	{
		public Guid Id { get; set; } = Guid.NewGuid();

		public Guid UserId { get; set; }
		public User User { get; set; } = null!;

		[Column(TypeName = "decimal(18,2)")]
		public decimal Budget { get; set; }

		public bool PreferOnline { get; set; }
		public bool PreferOffline { get; set; }
		public string PreferredGender { get; set; } = string.Empty; // "Male", "Female", "Any"
		public string PreferredLanguage { get; set; } = string.Empty;
		public string Issue { get; set; } = string.Empty; // Основна проблема

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	}
}
