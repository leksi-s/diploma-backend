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
		public string PreferredGender { get; set; } = string.Empty;
		public string PreferredLanguage { get; set; } = string.Empty;
		public string Issues { get; set; } = string.Empty;

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		public List<string> GetIssuesList()
		{
			return string.IsNullOrEmpty(Issues)
				? new List<string>()
				: Issues.Split(',').Select(s => s.Trim()).ToList();
		}


		public void SetIssuesList(List<string> issues)
		{
			Issues = string.Join(",", issues);
		}
	}
}