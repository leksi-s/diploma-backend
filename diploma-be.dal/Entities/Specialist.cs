﻿using System.ComponentModel.DataAnnotations.Schema;

namespace diploma_be.dal.Entities
{
	public class Specialist
	{
		public Guid Id { get; set; } = Guid.NewGuid();

		public Guid UserId { get; set; }
		public User User { get; set; } = null!;

		public string Education { get; set; } = string.Empty;
		public string Experience { get; set; } = string.Empty;
		public string Specializations { get; set; } = string.Empty;

		[Column(TypeName = "decimal(18,2)")]
		public decimal Price { get; set; }

		public bool Online { get; set; }
		public bool Offline { get; set; }
		public string Gender { get; set; } = string.Empty;
		public string Language { get; set; } = string.Empty;
		public bool IsActive { get; set; } = true;

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
		public List<string> GetSpecializationsList()
		{
			return string.IsNullOrEmpty(Specializations)
				? new List<string>()
				: Specializations.Split(',').Select(s => s.Trim()).ToList();
		}
		public void SetSpecializationsList(List<string> specializations)
		{
			Specializations = string.Join(",", specializations);
		}
	}
}