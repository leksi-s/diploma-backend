namespace diploma_be.bll.Models
{
	public class AppointmentDto
	{
		public Guid Id { get; set; }
		public string ClientName { get; set; } = string.Empty;
		public string SpecialistName { get; set; } = string.Empty;
		public DateTime AppointmentDate { get; set; }
		public bool IsOnline { get; set; }
		public string Status { get; set; } = string.Empty;
		public string Notes { get; set; } = string.Empty;
		public DateTime CreatedAt { get; set; }
	}

	public class CreateAppointmentRequest
	{
		public Guid SpecialistId { get; set; }
		public DateTime AppointmentDate { get; set; }
		public bool IsOnline { get; set; }
		public string Notes { get; set; } = string.Empty;
	}

	public class UpdateAppointmentStatusRequest
	{
		public string Status { get; set; } = string.Empty;
		public string Notes { get; set; } = string.Empty;
	}

	public class AppointmentFilterRequest
	{
		public DateTime? FromDate { get; set; }
		public DateTime? ToDate { get; set; }
		public string? Status { get; set; }
		public bool? IsOnline { get; set; }
	}

	public class CreateAppointmentRequestWithClient
	{
		public Guid ClientId { get; set; }
		public Guid SpecialistId { get; set; }
		public DateTime AppointmentDate { get; set; }
		public bool IsOnline { get; set; }
		public string Notes { get; set; } = string.Empty;
	}
}