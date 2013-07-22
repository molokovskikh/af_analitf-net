using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;

namespace AnalitF.Net.Service.Helpers
{
	public class RequestHelper
	{
		public static Version GetVersion(HttpRequestMessage request)
		{
			var headers = request.Headers;
			var version = new Version();
			IEnumerable<string> header;
			if (headers.TryGetValues("Version", out header)) {
				Version.TryParse(header.FirstOrDefault(), out version);
			}
			return version;
		}
	}
}