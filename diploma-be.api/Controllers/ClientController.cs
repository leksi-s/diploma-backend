using diploma_be.bll.Models;
using diploma_be.bll.Services;
using diploma_be.dal;
using diploma_be.dal.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace diploma.api.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	// ПРИБРАЛИ [Authorize(Roles = "Client")]
	public class ClientController : ControllerBase
	{
		private readonly AppDbContext _context;
		private readonly ITopsisService _topsisService;

		public ClientController(AppDbContext context, ITopsisService topsisService)
		{
			_context = context;
			_topsisService = topsisService;
		}

		// Тепер методи працюють без авторизації, але потребують clientId як параметр

		[HttpGet("profile/{clientId}")]
		public async Task<ActionResult<ClientDto>> GetProfile(Guid clientId)
		{
			var client = await _context.Clients
				.Include(c => c.User)
				.FirstOrDefaultAsync(c => c.Id == clientId);

			if (client == null)
				return NotFound("Client profile not found");

			return Ok(new ClientDto
			{
				Id = client.Id,
				FirstName = client.User.FirstName,
				LastName = client.User.LastName,
				Email = client.User.Email,
				Phone = client.User.Phone,
				Budget = client.Budget,
				PreferOnline = client.PreferOnline,
				PreferOffline = client.PreferOffline,
				PreferredGender = client.PreferredGender,
				PreferredLanguage = client.PreferredLanguage,
				Issue = client.Issue
			});
		}

		[HttpPut("profile/{clientId}")]
		public async Task<IActionResult> UpdateProfile(Guid clientId, [FromBody] UpdateClientRequest request)
		{
			var client = await _context.Clients.FirstOrDefaultAsync(c => c.Id == clientId);

			if (client == null)
				return NotFound("Client profile not found");

			client.Budget = request.Budget;
			client.PreferOnline = request.PreferOnline;
			client.PreferOffline = request.PreferOffline;
			client.PreferredGender = request.PreferredGender;
			client.PreferredLanguage = request.PreferredLanguage;
			client.Issue = request.Issue;

			await _context.SaveChangesAsync();
			return NoContent();
		}

		[HttpGet("specialists")]
		public async Task<ActionResult<List<SpecialistDto>>> GetAllSpecialists()
		{
			var specialists = await _context.Specialists
				.Include(s => s.User)
				.Where(s => s.IsActive)
				.Select(s => new SpecialistDto
				{
					Id = s.Id,
					FirstName = s.User.FirstName,
					LastName = s.User.LastName,
					Education = s.Education,
					Experience = s.Experience,
					Specialization = s.Specialization,
					Price = s.Price,
					Online = s.Online,
					Offline = s.Offline,
					Gender = s.Gender,
					Language = s.Language,
					IsActive = s.IsActive
				})
				.ToListAsync();

			return Ok(specialists);
		}

		[HttpPost("specialists/filter")]
		public async Task<ActionResult<List<SpecialistDto>>> FilterSpecialists([FromBody] SpecialistFilterRequest request)
		{
			var query = _context.Specialists
				.Include(s => s.User)
				.Where(s => s.IsActive);

			if (request.MaxPrice.HasValue)
				query = query.Where(s => s.Price <= request.MaxPrice.Value);

			if (request.Online.HasValue)
				query = query.Where(s => s.Online == request.Online.Value);

			if (request.Offline.HasValue)
				query = query.Where(s => s.Offline == request.Offline.Value);

			if (!string.IsNullOrEmpty(request.Gender))
				query = query.Where(s => s.Gender.ToLower() == request.Gender.ToLower());

			if (!string.IsNullOrEmpty(request.Language))
				query = query.Where(s => s.Language.ToLower().Contains(request.Language.ToLower()));

			if (!string.IsNullOrEmpty(request.Specialization))
				query = query.Where(s => s.Specialization.ToLower().Contains(request.Specialization.ToLower()));

			var specialists = await query
				.Select(s => new SpecialistDto
				{
					Id = s.Id,
					FirstName = s.User.FirstName,
					LastName = s.User.LastName,
					Education = s.Education,
					Experience = s.Experience,
					Specialization = s.Specialization,
					Price = s.Price,
					Online = s.Online,
					Offline = s.Offline,
					Gender = s.Gender,
					Language = s.Language,
					IsActive = s.IsActive
				})
				.ToListAsync();

			return Ok(specialists);
		}

		// TOPSIS тепер приймає clientId як параметр
		[HttpGet("topsis/recommendations/{clientId}")]
		public async Task<ActionResult<List<SpecialistDto>>> GetTopsisRecommendations(Guid clientId)
		{
			// Знаходимо клієнта по ID
			var client = await _context.Clients.FirstOrDefaultAsync(c => c.Id == clientId);
			if (client == null)
				return NotFound("Client not found");

			// Використовуємо UserId клієнта для TOPSIS
			var recommendations = await _topsisService.GetRankedSpecialistsAsync(client.UserId);
			return Ok(recommendations);
		}

		[HttpPost("topsis/calculate")]
		public async Task<ActionResult<List<SpecialistDto>>> CalculateTopsis([FromBody] TopsisRequest request)
		{
			var recommendations = await _topsisService.CalculateTopsisAsync(request);
			return Ok(recommendations);
		}

		// Appointments тепер потребують clientId
		[HttpPost("appointments")]
		public async Task<ActionResult<AppointmentDto>> CreateAppointment([FromBody] CreateAppointmentRequestWithClient request)
		{
			var client = await _context.Clients
				.Include(c => c.User)
				.FirstOrDefaultAsync(c => c.Id == request.ClientId);

			if (client == null)
				return NotFound("Client not found");

			var specialist = await _context.Specialists
				.Include(s => s.User)
				.FirstOrDefaultAsync(s => s.Id == request.SpecialistId);

			if (specialist == null)
				return NotFound("Specialist not found");

			var appointment = new Appointment
			{
				ClientId = client.Id,
				SpecialistId = request.SpecialistId,
				AppointmentDate = request.AppointmentDate,
				IsOnline = request.IsOnline,
				Notes = request.Notes,
				Status = "Scheduled"
			};

			_context.Appointments.Add(appointment);
			await _context.SaveChangesAsync();

			return Ok(new AppointmentDto
			{
				Id = appointment.Id,
				ClientName = $"{client.User.FirstName} {client.User.LastName}",
				SpecialistName = $"{specialist.User.FirstName} {specialist.User.LastName}",
				AppointmentDate = appointment.AppointmentDate,
				IsOnline = appointment.IsOnline,
				Status = appointment.Status,
				Notes = appointment.Notes
			});
		}

		[HttpGet("appointments/{clientId}")]
		public async Task<ActionResult<List<AppointmentDto>>> GetClientAppointments(Guid clientId)
		{
			var client = await _context.Clients.FirstOrDefaultAsync(c => c.Id == clientId);

			if (client == null)
				return NotFound("Client not found");

			var appointments = await _context.Appointments
				.Include(a => a.Client).ThenInclude(c => c.User)
				.Include(a => a.Specialist).ThenInclude(s => s.User)
				.Where(a => a.ClientId == client.Id)
				.Select(a => new AppointmentDto
				{
					Id = a.Id,
					ClientName = $"{a.Client.User.FirstName} {a.Client.User.LastName}",
					SpecialistName = $"{a.Specialist.User.FirstName} {a.Specialist.User.LastName}",
					AppointmentDate = a.AppointmentDate,
					IsOnline = a.IsOnline,
					Status = a.Status,
					Notes = a.Notes
				})
				.OrderBy(a => a.AppointmentDate)
				.ToListAsync();
			return appointments;
		}

		[HttpPost("create")]
		public async Task<ActionResult<ClientDto>> CreateClient([FromBody] CreateClientRequest request)
		{
			try
			{
				if (await _context.Users.AnyAsync(u => u.Email == request.Email))
					return BadRequest("Email already exists");

				var user = new User
				{
					FirstName = request.FirstName,
					LastName = request.LastName,
					Email = request.Email,
					Phone = request.Phone,
					PasswordHash = "",
					Role = "Client"
				};

				_context.Users.Add(user);
				await _context.SaveChangesAsync();

				var client = new Client
				{
					UserId = user.Id,
					Budget = request.Budget,
					PreferOnline = request.PreferOnline,
					PreferOffline = request.PreferOffline,
					PreferredGender = request.PreferredGender,
					PreferredLanguage = request.PreferredLanguage,
					Issue = request.Issue
				};

				_context.Clients.Add(client);
				await _context.SaveChangesAsync();

				return Ok(new ClientDto
				{
					Id = client.Id,
					FirstName = user.FirstName,
					LastName = user.LastName,
					Email = user.Email,
					Phone = user.Phone,
					Budget = client.Budget,
					PreferOnline = client.PreferOnline,
					PreferOffline = client.PreferOffline,
					PreferredGender = client.PreferredGender,
					PreferredLanguage = client.PreferredLanguage,
					Issue = client.Issue
				});
			}
			catch (Exception ex)
			{
				return BadRequest($"Error creating client: {ex.Message}");
			}
		}
	}
}