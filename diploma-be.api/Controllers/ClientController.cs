using diploma_be.bll.Models;
using diploma_be.bll.Services;
using diploma_be.dal;
using diploma_be.dal.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace diploma.api.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	[Authorize(Roles = "Client")]
	public class ClientController : ControllerBase
	{
		private readonly AppDbContext _context;
		private readonly ITopsisService _topsisService;

		public ClientController(AppDbContext context, ITopsisService topsisService)
		{
			_context = context;
			_topsisService = topsisService;
		}

		[HttpGet("profile")]
		public async Task<ActionResult<ClientDto>> GetProfile()
		{
			var userId = GetCurrentUserId();
			var client = await _context.Clients
				.Include(c => c.User)
				.FirstOrDefaultAsync(c => c.UserId == userId);

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

		[HttpPut("profile")]
		public async Task<IActionResult> UpdateProfile([FromBody] UpdateClientRequest request)
		{
			var userId = GetCurrentUserId();
			var client = await _context.Clients.FirstOrDefaultAsync(c => c.UserId == userId);

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

		[HttpGet("topsis/recommendations")]
		public async Task<ActionResult<List<SpecialistDto>>> GetTopsisRecommendations()
		{
			var userId = GetCurrentUserId();
			var recommendations = await _topsisService.GetRankedSpecialistsAsync(userId);
			return Ok(recommendations);
		}

		[HttpPost("topsis/calculate")]
		public async Task<ActionResult<List<SpecialistDto>>> CalculateTopsis([FromBody] TopsisRequest request)
		{
			var recommendations = await _topsisService.CalculateTopsisAsync(request);
			return Ok(recommendations);
		}

		[HttpPost("appointments")]
		public async Task<ActionResult<AppointmentDto>> CreateAppointment([FromBody] CreateAppointmentRequest request)
		{
			var userId = GetCurrentUserId();
			var client = await _context.Clients.FirstOrDefaultAsync(c => c.UserId == userId);

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

		[HttpGet("appointments")]
		public async Task<ActionResult<List<AppointmentDto>>> GetMyAppointments()
		{
			var userId = GetCurrentUserId();
			var client = await _context.Clients.FirstOrDefaultAsync(c => c.UserId == userId);

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

			return Ok(appointments);
		}

		private Guid GetCurrentUserId()
		{
			var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			return Guid.Parse(userIdClaim!);
		}
	}
}