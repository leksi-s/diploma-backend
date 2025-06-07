using diploma_be.bll.Models;
using diploma_be.dal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace diploma_be.api.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class SearchController : ControllerBase
	{
		private readonly AppDbContext _context;

		public SearchController(AppDbContext context)
		{
			_context = context;
		}

		[HttpGet("specialists")]
		public async Task<ActionResult<List<SpecialistDto>>> SearchSpecialists(
			[FromQuery] string? query,
			[FromQuery] decimal? maxPrice,
			[FromQuery] string? specialization,
			[FromQuery] string? language,
			[FromQuery] string? gender,
			[FromQuery] bool? online,
			[FromQuery] bool? offline)
		{
			var specialists = _context.Specialists
				.Include(s => s.User)
				.Where(s => s.IsActive);

			if (!string.IsNullOrEmpty(query))
			{
				specialists = specialists.Where(s =>
					s.User.FirstName.ToLower().Contains(query.ToLower()) ||
					s.User.LastName.ToLower().Contains(query.ToLower()) ||
					s.Education.ToLower().Contains(query.ToLower()) ||
					s.Experience.ToLower().Contains(query.ToLower()) ||
					s.Specialization.ToLower().Contains(query.ToLower()));
			}

			if (maxPrice.HasValue)
				specialists = specialists.Where(s => s.Price <= maxPrice.Value);

			if (!string.IsNullOrEmpty(specialization))
				specialists = specialists.Where(s => s.Specialization.ToLower().Contains(specialization.ToLower()));

			if (!string.IsNullOrEmpty(language))
				specialists = specialists.Where(s => s.Language.ToLower().Contains(language.ToLower()));

			if (!string.IsNullOrEmpty(gender))
				specialists = specialists.Where(s => s.Gender.ToLower() == gender.ToLower());

			if (online.HasValue)
				specialists = specialists.Where(s => s.Online == online.Value);

			if (offline.HasValue)
				specialists = specialists.Where(s => s.Offline == offline.Value);

			var result = await specialists
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
				.OrderBy(s => s.Price)
				.ToListAsync();

			return Ok(result);
		}

		[HttpGet("specialists/popular")]
		public async Task<ActionResult<List<SpecialistDto>>> GetPopularSpecialists()
		{
			var popularSpecialists = await _context.Specialists
				.Include(s => s.User)
				.Where(s => s.IsActive)
				.Select(s => new
				{
					Specialist = s,
					CompletedAppointments = _context.Appointments.Count(a => a.SpecialistId == s.Id && a.Status == "Completed")
				})
				.OrderByDescending(x => x.CompletedAppointments)
				.Take(10)
				.Select(x => new SpecialistDto
				{
					Id = x.Specialist.Id,
					FirstName = x.Specialist.User.FirstName,
					LastName = x.Specialist.User.LastName,
					Education = x.Specialist.Education,
					Experience = x.Specialist.Experience,
					Specialization = x.Specialist.Specialization,
					Price = x.Specialist.Price,
					Online = x.Specialist.Online,
					Offline = x.Specialist.Offline,
					Gender = x.Specialist.Gender,
					Language = x.Specialist.Language,
					IsActive = x.Specialist.IsActive
				})
				.ToListAsync();

			return Ok(popularSpecialists);
		}

		[HttpGet("statistics")]
		public async Task<ActionResult<object>> GetSearchStatistics()
		{
			var stats = new
			{
				TotalActiveSpecialists = await _context.Specialists.CountAsync(s => s.IsActive),
				AveragePrice = await _context.Specialists.Where(s => s.IsActive).AverageAsync(s => s.Price),
				PriceRange = new
				{
					Min = await _context.Specialists.Where(s => s.IsActive).MinAsync(s => s.Price),
					Max = await _context.Specialists.Where(s => s.IsActive).MaxAsync(s => s.Price)
				},
				SpecializationDistribution = await _context.Specialists
					.Where(s => s.IsActive)
					.GroupBy(s => s.Specialization)
					.Select(g => new { Specialization = g.Key, Count = g.Count() })
					.OrderByDescending(x => x.Count)
					.ToListAsync(),
				GenderDistribution = await _context.Specialists
					.Where(s => s.IsActive)
					.GroupBy(s => s.Gender)
					.Select(g => new { Gender = g.Key, Count = g.Count() })
					.ToListAsync(),
				FormatAvailability = new
				{
					OnlineOnly = await _context.Specialists.CountAsync(s => s.IsActive && s.Online && !s.Offline),
					OfflineOnly = await _context.Specialists.CountAsync(s => s.IsActive && !s.Online && s.Offline),
					Both = await _context.Specialists.CountAsync(s => s.IsActive && s.Online && s.Offline)
				}
			};

			return Ok(stats);
		}
	}
}