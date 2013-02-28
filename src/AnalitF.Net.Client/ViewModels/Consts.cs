using System;

namespace AnalitF.Net.Client.ViewModels
{
	public class Consts
	{
		public const string AllPricesLabel = "Все прайс-листы";
		public const string AllRegionLabel = "Все регионы";
		public const string AllProducerLabel = "Все производители";
		public static TimeSpan SearchTimeout = TimeSpan.FromMilliseconds(5000);
		public static TimeSpan LoadOrderHistoryTimeout = TimeSpan.FromMilliseconds(2000);
		public static TimeSpan RefreshOrderStatTimeout = TimeSpan.FromMilliseconds(500);
		public static readonly TimeSpan WarningTimeout = TimeSpan.FromSeconds(5);
	}
}