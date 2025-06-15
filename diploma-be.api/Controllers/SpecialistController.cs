using diploma_be.bll.Models;
using diploma_be.dal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Claims;

namespace diploma_be.api.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	[Authorize(Roles = "Specialist")]
	public class SpecialistController : ControllerBase
	{
		private readonly AppDbContext _context;

		public SpecialistController(AppDbContext context)
		{
			_context = context;
		}

		[HttpGet("profile")]
		public async Task<ActionResult<SpecialistDto>> GetProfile()
		{
			var userId = GetCurrentUserId();
			var specialist = await _context.Specialists
				.Include(s => s.User)
				.FirstOrDefaultAsync(s => s.UserId == userId);

			if (specialist == null)
				return NotFound("Профіль спеціаліста не знайдено");

			return Ok(new SpecialistDto
			{
				Id = specialist.Id,
				FirstName = specialist.User.FirstName,
				LastName = specialist.User.LastName,
				Email = specialist.User.Email,
				Phone = specialist.User.Phone,
				Education = specialist.Education,
				Experience = specialist.Experience,
				Specializations = specialist.GetSpecializationsList(),
				Price = specialist.Price,
				Online = specialist.Online,
				Offline = specialist.Offline,
				Gender = specialist.Gender,
				Language = specialist.Language,
				IsActive = specialist.IsActive
			});
		}

		[HttpPut("profile")]
		public async Task<IActionResult> UpdateProfile([FromBody] UpdateSpecialistRequest request)
		{
			var userId = GetCurrentUserId();
			var specialist = await _context.Specialists.FirstOrDefaultAsync(s => s.UserId == userId);

			if (specialist == null)
				return NotFound("Профіль спеціаліста не знайдено");

			specialist.Education = request.Education;
			specialist.Experience = request.Experience;
			specialist.SetSpecializationsList(request.Specializations);
			specialist.Price = request.Price;
			specialist.Online = request.Online;
			specialist.Offline = request.Offline;
			specialist.Language = request.Language;

			await _context.SaveChangesAsync();
			return NoContent();
		}

		[HttpGet("appointments")]
		public async Task<ActionResult<List<AppointmentDto>>> GetMyAppointments()
		{
			var userId = GetCurrentUserId();
			var specialist = await _context.Specialists.FirstOrDefaultAsync(s => s.UserId == userId);

			if (specialist == null)
				return NotFound("Спеціаліста не знайдено");

			var appointments = await _context.Appointments
				.Include(a => a.Client).ThenInclude(c => c.User)
				.Include(a => a.Specialist).ThenInclude(s => s.User)
				.Where(a => a.SpecialistId == specialist.Id)
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

		[HttpPut("appointments/{appointmentId}/status")]
		public async Task<IActionResult> UpdateAppointmentStatus(Guid appointmentId, [FromBody] string status)
		{
			var userId = GetCurrentUserId();
			var specialist = await _context.Specialists.FirstOrDefaultAsync(s => s.UserId == userId);

			if (specialist == null)
				return NotFound("Спеціаліста не знайдено");

			var appointment = await _context.Appointments
				.FirstOrDefaultAsync(a => a.Id == appointmentId && a.SpecialistId == specialist.Id);

			if (appointment == null)
				return NotFound("Запис не знайдено");

			var validStatuses = new[] { "Заплановано", "Завершено", "Скасовано", "Не з'явився" };
			if (!validStatuses.Contains(status))
				return BadRequest("Невірний статус");

			appointment.Status = status;
			await _context.SaveChangesAsync();

			return NoContent();
		}

		private Guid GetCurrentUserId()
		{
			var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			return Guid.Parse(userIdClaim!);
		}
	}
}