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
			try
			{
				Console.WriteLine($"🔐 Login attempt started");
				Console.WriteLine($"🔐 Email: {request?.Email ?? "NULL"}");
				Console.WriteLine($"🔐 Password length: {request?.Password?.Length ?? 0}");

				if (request == null)
				{
					Console.WriteLine("❌ Request is null");
					return BadRequest("Invalid request");
				}

				if (string.IsNullOrEmpty(request.Email))
				{
					Console.WriteLine("❌ Email is empty");
					return BadRequest("Email is required");
				}

				if (string.IsNullOrEmpty(request.Password))
				{
					Console.WriteLine("❌ Password is empty");
					return BadRequest("Password is required");
				}

				Console.WriteLine($"🔐 Looking for user with email: {request.Email}");

				var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

				if (user == null)
				{
					Console.WriteLine($"❌ User not found: {request.Email}");
					return Unauthorized("Invalid credentials");
				}

				Console.WriteLine($"🔐 User found: {user.Email}, Role: {user.Role}");
				Console.WriteLine($"🔐 Stored hash length: {user.PasswordHash?.Length ?? 0}");

				if (string.IsNullOrEmpty(user.PasswordHash))
				{
					Console.WriteLine("❌ User has no password hash");
					return Unauthorized("Invalid credentials");
				}

				Console.WriteLine($"🔐 Verifying password...");

				bool passwordValid;
				try
				{
					passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
					Console.WriteLine($"🔐 Password verification result: {passwordValid}");
				}
				catch (Exception ex)
				{
					Console.WriteLine($"❌ Password verification error: {ex.Message}");
					return Unauthorized("Invalid credentials");
				}

				if (!passwordValid)
				{
					Console.WriteLine($"❌ Password verification failed for: {request.Email}");
					return Unauthorized("Invalid credentials");
				}

				Console.WriteLine($"✅ Password verified for: {request.Email}");
				Console.WriteLine($"🔐 Generating token...");

				string token;
				try
				{
					token = GenerateJwtToken(user);
					Console.WriteLine($"✅ Token generated successfully");
				}
				catch (Exception ex)
				{
					Console.WriteLine($"❌ Token generation error: {ex.Message}");
					Console.WriteLine($"❌ Token generation stack trace: {ex.StackTrace}");
					return StatusCode(500, "Error generating authentication token");
				}

				Console.WriteLine($"✅ Login successful for: {request.Email}");

				return Ok(new LoginResponse
				{
					Token = token,
					Role = user.Role,
					Name = $"{user.FirstName} {user.LastName}",
					UserId = user.Id
				});
			}
			catch (Exception ex)
			{
				Console.WriteLine($"❌ Login error: {ex.Message}");
				Console.WriteLine($"❌ Login stack trace: {ex.StackTrace}");
				return StatusCode(500, $"Internal server error during login: {ex.Message}");
			}
		}

		[HttpPost("register")]
		public async Task<ActionResult<LoginResponse>> Register([FromBody] RegisterRequest request)
		{
			try
			{
				Console.WriteLine($"📝 Registration attempt for email: {request.Email}");

				// ТІЛЬКИ для Specialist та Admin - клієнти не реєструються тут
				if (request.Role != "Specialist" && request.Role != "Admin")
				{
					Console.WriteLine($"❌ Invalid role for registration: {request.Role}");
					return BadRequest("Registration only available for Specialists and Admins. Clients use direct creation via /api/client/create");
				}

				if (await _context.Users.AnyAsync(u => u.Email == request.Email))
				{
					Console.WriteLine($"❌ Email already exists: {request.Email}");
					return BadRequest("Email already exists");
				}

				var user = new User
				{
					FirstName = request.FirstName,
					LastName = request.LastName,
					Email = request.Email,
					Phone = request.Phone,
					PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
					Role = request.Role,
					CreatedAt = DateTime.UtcNow
				};

				_context.Users.Add(user);
				await _context.SaveChangesAsync();

				Console.WriteLine($"✅ User created: {user.Email} with role: {user.Role}");

				// Створюємо профіль тільки для Specialist
				if (request.Role == "Specialist")
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
						IsActive = false, // Admin must activate
						CreatedAt = DateTime.UtcNow
					};
					_context.Specialists.Add(specialist);
					await _context.SaveChangesAsync();
					Console.WriteLine($"✅ Specialist profile created for: {user.Email} (inactive - requires admin activation)");
				}

				var token = GenerateJwtToken(user);

				Console.WriteLine($"✅ Registration successful for: {request.Email}");

				return Ok(new LoginResponse
				{
					Token = token,
					Role = user.Role,
					Name = $"{user.FirstName} {user.LastName}",
					UserId = user.Id
				});
			}
			catch (Exception ex)
			{
				Console.WriteLine($"❌ Registration error: {ex.Message}");
				return StatusCode(500, "Internal server error during registration");
			}
		}

		private string GenerateJwtToken(User user)
		{
			try
			{
				// ТОЧНО ТАКИЙ САМИЙ КЛЮЧ як в Program.cs
				var key = "YourSuperSecretKeyForJWTWhichShouldBeLongEnough123456789";
				var tokenHandler = new JwtSecurityTokenHandler();
				var keyBytes = Encoding.UTF8.GetBytes(key);

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
				var tokenString = tokenHandler.WriteToken(token);

				Console.WriteLine($"🔑 Generated token for user: {user.Email} (Role: {user.Role})");
				Console.WriteLine($"🔑 Token preview: {tokenString.Substring(0, Math.Min(50, tokenString.Length))}...");

				return tokenString;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"❌ Error generating token: {ex.Message}");
				throw;
			}
		}

		[HttpPost("logout")]
		public IActionResult Logout()
		{
			// В JWT logout виконується на клієнті (видалення токена)
			// Але можемо логувати це для статистики
			var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			var userName = User.FindFirst(ClaimTypes.Name)?.Value;

			Console.WriteLine($"👋 User {userName} (ID: {userId}) logged out at {DateTime.UtcNow}");

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
			try
			{
				var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
				var user = await _context.Users.FindAsync(userId);

				if (user == null)
				{
					Console.WriteLine($"❌ User not found for ID: {userId}");
					return NotFound("User not found");
				}

				if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
				{
					Console.WriteLine($"❌ Current password incorrect for user: {user.Email}");
					return BadRequest("Current password is incorrect");
				}

				user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
				await _context.SaveChangesAsync();

				Console.WriteLine($"✅ Password changed successfully for user: {user.Email}");

				return Ok(new { message = "Password changed successfully" });
			}
			catch (Exception ex)
			{
				Console.WriteLine($"❌ Change password error: {ex.Message}");
				return StatusCode(500, "Internal server error during password change");
			}
		}

		[HttpGet("test-token")]
		[Authorize]
		public IActionResult TestToken()
		{
			try
			{
				var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
				var role = User.FindFirst(ClaimTypes.Role)?.Value;
				var email = User.FindFirst(ClaimTypes.Email)?.Value;
				var name = User.FindFirst(ClaimTypes.Name)?.Value;

				Console.WriteLine($"🔍 Token test - User: {email}, Role: {role}");

				return Ok(new
				{
					message = "Token is valid",
					userId = userId,
					role = role,
					email = email,
					name = name,
					isAuthenticated = User.Identity?.IsAuthenticated,
					claims = User.Claims.Select(c => new { type = c.Type, value = c.Value })
				});
			}
			catch (Exception ex)
			{
				Console.WriteLine($"❌ Token test error: {ex.Message}");
				return StatusCode(500, "Error testing token");
			}
		}

		// Додаємо інформаційний endpoint про доступні ролі
		[HttpGet("info")]
		public IActionResult GetAuthInfo()
		{
			return Ok(new
			{
				message = "Authentication API Information",
				availableRoles = new[]
				{
					new { role = "Admin", description = "Full system access, can manage specialists", registration = "Available" },
					new { role = "Specialist", description = "Can manage own profile and appointments", registration = "Available (requires admin activation)" },
					new { role = "Client", description = "Can browse specialists and book appointments", registration = "Not available - use /api/client/create instead" }
				},
				endpoints = new
				{
					login = "POST /api/auth/login - For Admins and Specialists",
					register = "POST /api/auth/register - For Admins and Specialists only",
					clientCreation = "POST /api/client/create - For Clients (no password required)",
					changePassword = "POST /api/auth/change-password - Change password (requires auth)",
					testToken = "GET /api/auth/test-token - Test JWT token validity"
				}
			});
		}
	}
}