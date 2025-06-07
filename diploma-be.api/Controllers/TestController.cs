using diploma_be.bll.Models;
using diploma_be.dal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace diploma.api.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class TestController : ControllerBase
	{
		private readonly AppDbContext _context;

		public TestController(AppDbContext context)
		{
			_context = context;
		}

		[HttpGet]
		public async Task<IActionResult> GetStatus()
		{
			var usersCount = await _context.Users.CountAsync();
			var specialistsCount = await _context.Specialists.CountAsync();
			var clientsCount = await _context.Clients.CountAsync();

			return Ok(new
			{
				Status = "API is working!",
				Database = "Connected",
				Users = usersCount,
				Specialists = specialistsCount,
				Clients = clientsCount,
				Time = DateTime.Now
			});
		}

		[HttpGet("specialists")]
		public async Task<ActionResult<List<SpecialistDto>>> GetSpecialists()
		{
			var specialists = await _context.Specialists
				.Include(s => s.User)
				.Where(s => s.IsActive)
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
	}
}