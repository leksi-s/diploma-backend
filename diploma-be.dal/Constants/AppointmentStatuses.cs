namespace diploma_be.dal.Constants
{
	public static class AppointmentStatuses
	{
		public const string Scheduled = "Заплановано";
		public const string Completed = "Завершено";
		public const string Cancelled = "Скасовано";
		public const string NoShow = "Не з'явився";

		public static readonly string[] AllStatuses = new[]
		{
			Scheduled,
			Completed,
			Cancelled,
			NoShow
		};

		public static bool IsValidStatus(string status)
		{
			return AllStatuses.Contains(status);
		}
	}

	public static class GenderOptions
	{
		public const string Male = "Чоловік";
		public const string Female = "Жінка";
		public const string Any = "Будь-яка";

		public static readonly string[] AllOptions = new[]
		{
			Male,
			Female,
			Any
		};
	}
}