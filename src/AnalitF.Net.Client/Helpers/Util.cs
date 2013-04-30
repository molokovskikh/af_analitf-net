using System.Collections.Generic;

namespace AnalitF.Net.Client.Helpers
{
	public static class Util
	{
		public static bool IsDigitValue(object o)
		{
			return o is int || o is uint || o is decimal || o is double || o is float;
		}

		public static T GetValueOrDefault<K, T>(this IDictionary<K,T> dict, K key)
		{
			var value = default(T);
			dict.TryGetValue(key, out value);
			return value;
		}
	}
}