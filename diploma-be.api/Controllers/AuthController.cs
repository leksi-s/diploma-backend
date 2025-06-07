using diploma.bll.Models;
using diploma_be.dal;
using diploma_be.dal.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace diploma.api.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class AuthController : ControllerBase
	{
		private readonly AppDbContext _context;
		private readonly IConfiguration _configuration;

		public AuthController(AppDbContext context, IConfiguration configuration)
		{
			_context = context;
			_configuration = configuration;
		}

		[HttpPost("login")]
		public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
		{
			var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

			if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
			{
				return Unauthorized("Invalid credentials");
			}

			var token = GenerateJwtToken(user);

			return Ok(new LoginResponse
			{
				Token = token,
				Role = user.Role,
				Name = $"{user.FirstName} {user.LastName}",
				UserId = user.Id
			});
		}

		[HttpPost("register")]
		public async Task<ActionResult<LoginResponse>> Register([FromBody] RegisterRequest request)
		{
			if (await _context.Users.AnyAsync(u => u.Email == request.Email))
			{
				return BadRequest("Email already exists");
			}

			var user = new User
			{
				FirstName = request.FirstName,
				LastName = request.LastName,
				Email = request.Email,
				Phone = request.Phone,
				PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
				Role = request.Role
			};

			_context.Users.Add(user);
			await _context.SaveChangesAsync();

			// Create profile based on role
			if (request.Role == "Client")
			{
				var client = new Client
				{
					UserId = user.Id,
					Budget = 1000,
					PreferOnline = true,
					PreferOffline = true,
					PreferredGender = "Any",
					PreferredLanguage = "Ukrainian",
					Issue = ""
				};
				_context.Clients.Add(client);
				await _context.SaveChangesAsync();
			}
			else if (request.Role == "Specialist")
			{
				var specialist = new Specialist
				{
					UserId = user.Id,
					Education = "",
					Experience = "",
					Specialization = "",
					Price = 800,
					Online = true,
					Offline = true,
					Gender = "Other",
					Language = "Ukrainian",
					IsActive = false // Admin must activate
				};
				_context.Specialists.Add(specialist);
				await _context.SaveChangesAsync();
			}

			var token = GenerateJwtToken(user);

			return Ok(new LoginResponse
			{
				Token = token,
				Role = user.Role,
				Name = $"{user.FirstName} {user.LastName}",
				UserId = user.Id
			});
		}

		private string GenerateJwtToken(User user)
		{
			var key = "YourSuperSecretKeyForJWTWhichShouldBeLongEnough123456789";
			var tokenHandler = new JwtSecurityTokenHandler();
			var keyBytes = Encoding.ASCII.GetBytes(key);

			var tokenDescriptor = new SecurityTokenDescriptor
			{
				Subject = new ClaimsIdentity(new[]
				{
					new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
					new Claim(ClaimTypes.Email, user.Email),
					new Claim(ClaimTypes.Role, user.Role),
					new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}")
				}),
				Expires = DateTime.UtcNow.AddHours(2),
				SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256Signature),
				Issuer = "PsychApp",
				Audience = "PsychApp"
			};

			var token = tokenHandler.CreateToken(tokenDescriptor);
			return tokenHandler.WriteToken(token);
		}

		[HttpPost("logout")]
		public IActionResult Logout()
		{
			// В JWT logout виконується на клієнті (видалення токена)
			// Але можемо логувати це для статистики
			var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			var userName = User.FindFirst(ClaimTypes.Name)?.Value;

			Console.WriteLine($"User {userName} (ID: {userId}) logged out at {DateTime.UtcNow}");

			return Ok(new
			{
				message = "Logged out successfully",
				timestamp = DateTime.UtcNow
			});
		}

		[HttpPost("change-password")]
		[Authorize]
		public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
		{
			var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
			var user = await _context.Users.FindAsync(userId);

			if (user == null)
				return NotFound("User not found");

			if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
				return BadRequest("Current password is incorrect");

			user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
			await _context.SaveChangesAsync();

			return Ok(new { message = "Password changed successfully" });
		}
	}
}