using System;
using Common.Models;

namespace AnalitF.Net.Service.Models
{
	public class PendingLimitLog
	{
		public PendingLimitLog()
		{
		}

		public PendingLimitLog(RequestLog request, OrderLimit limit, Tuple<decimal, decimal> values)
		{
			Request = request;
			Limit = limit;
			Value = values.Item1;
			ToDay = values.Item2;
		}

		public virtual uint Id { get; set; }
		public virtual RequestLog Request { get; set; }
		public virtual OrderLimit Limit { get; set; }
		public virtual decimal Value { get; set; }
		public virtual decimal ToDay { get; set; }
	}
}