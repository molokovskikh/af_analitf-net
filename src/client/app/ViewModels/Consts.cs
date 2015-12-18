using System;
using Common.Tools.Calendar;

namespace AnalitF.Net.Client.ViewModels
{
	public class Consts
	{
		public const string AllRegionLabel = "Все регионы";
		public const string AllProducerLabel = "Все производители";
		public static TimeSpan SearchTimeout = 5.Second();
		public static TimeSpan LoadOrderHistoryTimeout = TimeSpan.FromMilliseconds(700);
		public static TimeSpan RefreshOrderStatTimeout = TimeSpan.FromMilliseconds(500);
		public static TimeSpan WarningTimeout = 5.Second();
		public static TimeSpan FilterUpdateTimeout = TimeSpan.FromMilliseconds(500);
		//задержка перед загрузкой связанных данных
		public static TimeSpan ScrollLoadTimeout = TimeSpan.FromMilliseconds(100);
		//задержка перед загрузкой данных при вводе текста в поле
		public static TimeSpan TextInputLoadTimeout = TimeSpan.FromMilliseconds(300);
	}
}