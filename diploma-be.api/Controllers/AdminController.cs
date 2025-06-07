using diploma_be.bll.Models;
using diploma_be.dal;
using diploma_be.dal.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace diploma_be.api.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	[Authorize(Roles = "Admin")]
	public class AdminController : ControllerBase
	{
		private readonly AppDbContext _context;

		public AdminController(AppDbContext context)
		{
			_context = context;
		}

		[HttpGet("Specialists")]
		public async Task<ActionResult<List<SpecialistDto>>> GetAllSpecialists()
		{
			var specialists = await _context.Specialists
				.Include(s => s.User)
				.Select(s => new SpecialistDto
				{
					Id = s.Id,
					FirstName = s.User.FirstName,
					LastName = s.User.LastName,
					Email = s.User.Email,
					Phone = s.User.Phone,
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

		[HttpGet("Specialists/{id}")]
		public async Task<ActionResult<SpecialistDto>> GetSpecialist(Guid id)
		{
			var specialist = await _context.Specialists
				.Include(s => s.User)
				.FirstOrDefaultAsync(s => s.Id == id);

			if (specialist == null)
				return NotFound();

			return Ok(new SpecialistDto
			{
				Id = specialist.Id,
				FirstName = specialist.User.FirstName,
				LastName = specialist.User.LastName,
				Email = specialist.User.Email,
				Phone = specialist.User.Phone,
				Education = specialist.Education,
				Experience = specialist.Experience,
				Specialization = specialist.Specialization,
				Price = specialist.Price,
				Online = specialist.Online,
				Offline = specialist.Offline,
				Gender = specialist.Gender,
				Language = specialist.Language,
				IsActive = specialist.IsActive
			});
		}

		[HttpPost("specialists")]
		public async Task<ActionResult<SpecialistDto>> CreateSpecialist([FromBody] CreateSpecialistRequest request)
		{
			if (await _context.Users.AnyAsync(u => u.Email == request.Email))
				return BadRequest("Email already exists");

			var user = new User
			{
				FirstName = request.FirstName,
				LastName = request.LastName,
				Email = request.Email,
				Phone = request.Phone,
				PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
				Role = "Specialist"
			};

			_context.Users.Add(user);
			await _context.SaveChangesAsync();

			var specialist = new Specialist
			{
				UserId = user.Id,
				Education = request.Education,
				Experience = request.Experience,
				Specialization = request.Specialization,
				Price = request.Price,
				Online = request.Online,
				Offline = request.Offline,
				Gender = request.Gender,
				Language = request.Language,
				IsActive = true
			};

			_context.Specialists.Add(specialist);
			await _context.SaveChangesAsync();

			return Ok(new SpecialistDto
			{
				Id = specialist.Id,
				FirstName = user.FirstName,
				LastName = user.LastName,
				Email = user.Email,
				Phone = user.Phone,
				Education = specialist.Education,
				Experience = specialist.Experience,
				Specialization = specialist.Specialization,
				Price = specialist.Price,
				Online = specialist.Online,
				Offline = specialist.Offline,
				Gender = specialist.Gender,
				Language = specialist.Language,
				IsActive = specialist.IsActive
			});
		}

		[HttpPut("specialists/{id}")]
		public async Task<IActionResult> UpdateSpecialist(Guid id, [FromBody] UpdateSpecialistRequest request)
		{
			var specialist = await _context.Specialists
				.Include(s => s.User)
				.FirstOrDefaultAsync(s => s.Id == id);

			if (specialist == null)
				return NotFound();

			specialist.Education = request.Education;
			specialist.Experience = request.Experience;
			specialist.Specialization = request.Specialization;
			specialist.Price = request.Price;
			specialist.Online = request.Online;
			specialist.Offline = request.Offline;
			specialist.Language = request.Language;

			await _context.SaveChangesAsync();
			return NoContent();
		}

		[HttpDelete("specialists/{id}")]
		public async Task<IActionResult> DeleteSpecialist(Guid id)
		{
			var specialist = await _context.Specialists
				.Include(s => s.User)
				.FirstOrDefaultAsync(s => s.Id == id);

			if (specialist == null)
				return NotFound();

			var hasActiveAppointments = await _context.Appointments
				.AnyAsync(a => a.SpecialistId == id && a.Status == "Scheduled");

			if (hasActiveAppointments)
				return BadRequest("Cannot delete specialist with active appointments");

			_context.Specialists.Remove(specialist);
			_context.Users.Remove(specialist.User);
			await _context.SaveChangesAsync();

			return NoContent();
		}

		[HttpPut("specialists/{id}/toggle-status")]
		public async Task<IActionResult> ToggleSpecialistStatus(Guid id)
		{
			var specialist = await _context.Specialists.FindAsync(id);

			if (specialist == null)
				return NotFound();

			specialist.IsActive = !specialist.IsActive;
			await _context.SaveChangesAsync();

			return Ok(new { IsActive = specialist.IsActive });
		}

		[HttpGet("clients")]
		public async Task<ActionResult<List<ClientDto>>> GetAllClients()
		{
			var clients = await _context.Clients
				.Include(c => c.User)
				.Select(c => new ClientDto
				{
					Id = c.Id,
					FirstName = c.User.FirstName,
					LastName = c.User.LastName,
					Email = c.User.Email,
					Phone = c.User.Phone,
					Budget = c.Budget,
					PreferOnline = c.PreferOnline,
					PreferOffline = c.PreferOffline,
					PreferredGender = c.PreferredGender,
					PreferredLanguage = c.PreferredLanguage,
					Issue = c.Issue
				})
				.ToListAsync();

			return Ok(clients);
		}

		[HttpGet("appointments")]
		public async Task<ActionResult<List<AppointmentDto>>> GetAllAppointments()
		{
			var appointments = await _context.Appointments
				.Include(a => a.Client).ThenInclude(c => c.User)
				.Include(a => a.Specialist).ThenInclude(s => s.User)
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

		[HttpGet("statistics")]
		public async Task<ActionResult<object>> GetStatistics()
		{
			var totalUsers = await _context.Users.CountAsync();
			var totalSpecialists = await _context.Specialists.CountAsync();
			var activeSpecialists = await _context.Specialists.CountAsync(s => s.IsActive);
			var totalClients = await _context.Clients.CountAsync();
			var totalAppointments = await _context.Appointments.CountAsync();
			var completedAppointments = await _context.Appointments.CountAsync(a => a.Status == "Completed");
			var scheduledAppointments = await _context.Appointments.CountAsync(a => a.Status == "Scheduled");

			return Ok(new
			{
				TotalUsers = totalUsers,
				TotalSpecialists = totalSpecialists,
				ActiveSpecialists = activeSpecialists,
				TotalClients = totalClients,
				TotalAppointments = totalAppointments,
				CompletedAppointments = completedAppointments,
				ScheduledAppointments = scheduledAppointments,
				GeneratedAt = DateTime.UtcNow
			});
		}
	}
}