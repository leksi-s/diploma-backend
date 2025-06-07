namespace diploma.bll.Models
{
	public class LoginRequest
	{
		public string Email { get; set; } = string.Empty;
		public string Password { get; set; } = string.Empty;
	}

	public class LoginResponse
	{
		public string Token { get; set; } = string.Empty;
		public string Role { get; set; } = string.Empty;
		public string Name { get; set; } = string.Empty;
		public Guid UserId { get; set; }
	}

	public class RegisterRequest
	{
		public string FirstName { get; set; } = string.Empty;
		public string LastName { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
		public string Phone { get; set; } = string.Empty;
		public string Password { get; set; } = string.Empty;
		public string Role { get; set; } = string.Empty; // "Client" or "Specialist"
	}

	public class ChangePasswordRequest
	{
		public string CurrentPassword { get; set; } = string.Empty;
		public string NewPassword { get; set; } = string.Empty;
	}
}