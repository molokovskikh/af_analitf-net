using System;
using Common.Tools.Calendar;

namespace AnalitF.Net.Client.ViewModels
{
	public class Consts
	{
		public const string AllRegionLabel = "Все регионы";
		public const string AllProducerLabel = "Все производители";
		public static TimeSpan SearchTimeout = 5.Second();
		public static TimeSpan LoadOrderHistoryTimeout = 2.Second();
		public static TimeSpan RefreshOrderStatTimeout = TimeSpan.FromMilliseconds(500);
		public static TimeSpan WarningTimeout = 5.Second();
		public static TimeSpan FilterUpdateTimeout = TimeSpan.FromMilliseconds(500);
		public static TimeSpan ScrollLoadTimeout = TimeSpan.FromMilliseconds(100);
	}
}