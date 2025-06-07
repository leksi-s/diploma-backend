namespace diploma_be.dal.Entities
{
	public class Appointment
	{
		public Guid Id { get; set; } = Guid.NewGuid();

		public Guid ClientId { get; set; }
		public Client Client { get; set; } = null!;

		public Guid SpecialistId { get; set; }
		public Specialist Specialist { get; set; } = null!;

		public DateTime AppointmentDate { get; set; }
		public bool IsOnline { get; set; }
		public string Status { get; set; } = "Scheduled"; // "Scheduled", "Completed", "Cancelled"
		public string Notes { get; set; } = string.Empty;

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	}
}