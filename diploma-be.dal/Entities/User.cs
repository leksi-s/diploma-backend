using System.ComponentModel.DataAnnotations;

namespace diploma_be.dal.Entities
{
	public class User
	{
		public Guid Id { get; set; } = Guid.NewGuid();

		[Required]
		[MaxLength(100)]
		public string FirstName { get; set; } = string.Empty;

		[Required]
		[MaxLength(100)]
		public string LastName { get; set; } = string.Empty;

		[Required]
		[EmailAddress]
		public string Email { get; set; } = string.Empty;

		public string Phone { get; set; } = string.Empty;

		[Required]
		public string PasswordHash { get; set; } = string.Empty;

		[Required]
		public string Role { get; set; } = string.Empty; // "Client", "Specialist", "Admin"

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	}
}