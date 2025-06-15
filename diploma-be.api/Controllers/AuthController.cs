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
				Console.WriteLine($"Спроба входу розпочата");
				Console.WriteLine($"Email: {request?.Email ?? "NULL"}");
				Console.WriteLine($"Довжина паролю: {request?.Password?.Length ?? 0}");

				if (request == null)
				{
					Console.WriteLine("Запит є null");
					return BadRequest("Невірний запит");
				}

				if (string.IsNullOrEmpty(request.Email))
				{
					Console.WriteLine("Email порожній");
					return BadRequest("Email є обов'язковим");
				}

				if (string.IsNullOrEmpty(request.Password))
				{
					Console.WriteLine("Пароль порожній");
					return BadRequest("Пароль є обов'язковим");
				}

				Console.WriteLine($"Пошук користувача з email: {request.Email}");

				var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

				if (user == null)
				{
					Console.WriteLine($"Користувача не знайдено: {request.Email}");
					return Unauthorized("Невірні облікові дані");
				}

				Console.WriteLine($"Користувача знайдено: {user.Email}, Роль: {user.Role}");
				Console.WriteLine($"Довжина збереженого хешу: {user.PasswordHash?.Length ?? 0}");

				if (string.IsNullOrEmpty(user.PasswordHash))
				{
					Console.WriteLine("Користувач не має хешу паролю");
					return Unauthorized("Невірні облікові дані");
				}

				Console.WriteLine($"Перевірка паролю...");

				bool passwordValid;
				try
				{
					passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
					Console.WriteLine($"Результат перевірки паролю: {passwordValid}");
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Помилка перевірки паролю: {ex.Message}");
					return Unauthorized("Невірні облікові дані");
				}

				if (!passwordValid)
				{
					Console.WriteLine($"Перевірка паролю не вдалася для: {request.Email}");
					return Unauthorized("Невірні облікові дані");
				}

				Console.WriteLine($"Пароль підтверджено для: {request.Email}");
				Console.WriteLine($"Генерація токена...");

				string token;
				try
				{
					token = GenerateJwtToken(user);
					Console.WriteLine($"Токен успішно згенеровано");
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Помилка генерації токена: {ex.Message}");
					Console.WriteLine($"Stack trace: {ex.StackTrace}");
					return StatusCode(500, "Помилка генерації токена автентифікації");
				}

				Console.WriteLine($"Вхід успішний для: {request.Email}");

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
				Console.WriteLine($"Помилка входу: {ex.Message}");
				Console.WriteLine($"Stack trace: {ex.StackTrace}");
				return StatusCode(500, $"Внутрішня помилка сервера під час входу: {ex.Message}");
			}
		}

		[HttpPost("register")]
		public async Task<ActionResult<LoginResponse>> Register([FromBody] RegisterRequest request)
		{
			try
			{
				Console.WriteLine($"Спроба реєстрації для email: {request.Email}");

				if (request.Role != "Specialist" && request.Role != "Admin")
				{
					Console.WriteLine($"Невірна роль для реєстрації: {request.Role}");
					return BadRequest("Реєстрація доступна лише для спеціалістів та адміністраторів. Клієнти використовують пряме створення через /api/client/create");
				}

				if (await _context.Users.AnyAsync(u => u.Email == request.Email))
				{
					Console.WriteLine($"Email вже існує: {request.Email}");
					return BadRequest("Email вже існує");
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

				Console.WriteLine($"Користувача створено: {user.Email} з роллю: {user.Role}");

				if (request.Role == "Specialist")
				{
					var specialist = new Specialist
					{
						UserId = user.Id,
						Education = "",
						Experience = "",
						Specializations = "",
						Price = 800,
						Online = true,
						Offline = true,
						Gender = "Інше",
						Language = "Українська",
						IsActive = false,
						CreatedAt = DateTime.UtcNow
					};
					_context.Specialists.Add(specialist);
					await _context.SaveChangesAsync();
					Console.WriteLine($"Профіль спеціаліста створено для: {user.Email} (неактивний - потребує активації адміністратором)");
				}

				var token = GenerateJwtToken(user);

				Console.WriteLine($"Реєстрація успішна для: {request.Email}");

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
				Console.WriteLine($"Помилка реєстрації: {ex.Message}");
				return StatusCode(500, "Внутрішня помилка сервера під час реєстрації");
			}
		}

		private string GenerateJwtToken(User user)
		{
			try
			{
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

				Console.WriteLine($"Згенеровано токен для користувача: {user.Email} (Роль: {user.Role})");
				Console.WriteLine($"Попередній перегляд токена: {tokenString.Substring(0, Math.Min(50, tokenString.Length))}...");

				return tokenString;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Помилка генерації токена: {ex.Message}");
				throw;
			}
		}

		[HttpPost("logout")]
		public IActionResult Logout()
		{
			var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			var userName = User.FindFirst(ClaimTypes.Name)?.Value;

			Console.WriteLine($"Користувач {userName} (ID: {userId}) вийшов о {DateTime.UtcNow}");

			return Ok(new
			{
				message = "Вихід виконано успішно",
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
					Console.WriteLine($"Користувача не знайдено для ID: {userId}");
					return NotFound("Користувача не знайдено");
				}

				if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
				{
					Console.WriteLine($"Поточний пароль невірний для користувача: {user.Email}");
					return BadRequest("Поточний пароль невірний");
				}

				user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
				await _context.SaveChangesAsync();

				Console.WriteLine($"Пароль успішно змінено для користувача: {user.Email}");

				return Ok(new { message = "Пароль успішно змінено" });
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Помилка зміни паролю: {ex.Message}");
				return StatusCode(500, "Внутрішня помилка сервера під час зміни паролю");
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

				Console.WriteLine($"Тест токена - Користувач: {email}, Роль: {role}");

				return Ok(new
				{
					message = "Токен дійсний",
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
				Console.WriteLine($"Помилка тесту токена: {ex.Message}");
				return StatusCode(500, "Помилка тестування токена");
			}
		}

		[HttpGet("info")]
		public IActionResult GetAuthInfo()
		{
			return Ok(new
			{
				message = "Інформація API автентифікації",
				availableRoles = new[]
				{
					new { role = "Admin", description = "Повний доступ до системи, може керувати спеціалістами", registration = "Доступна" },
					new { role = "Specialist", description = "Може керувати власним профілем та записами", registration = "Доступна (потребує активації адміністратором)" },
					new { role = "Client", description = "Може переглядати спеціалістів та записуватися на консультації", registration = "Недоступна - використовуйте /api/client/create" }
				},
				endpoints = new
				{
					login = "POST /api/auth/login - Для адміністраторів та спеціалістів",
					register = "POST /api/auth/register - Тільки для адміністраторів та спеціалістів",
					clientCreation = "POST /api/client/create - Для клієнтів (пароль не потрібен)",
					changePassword = "POST /api/auth/change-password - Зміна паролю (потребує автентифікації)",
					testToken = "GET /api/auth/test-token - Перевірка дійсності JWT токена"
				}
			});
		}
	}
}