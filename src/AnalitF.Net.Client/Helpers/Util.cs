using System.Collections.Generic;

namespace AnalitF.Net.Client.Helpers
{
	public static class Util
	{
		public static bool IsDigitValue(object o)
		{
			return o is int || o is uint || o is decimal || o is double || o is float;
		}
	}
}