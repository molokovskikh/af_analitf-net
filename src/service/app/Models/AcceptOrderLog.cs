using Common.Models;

namespace AnalitF.Net.Service.Models
{
	public class AcceptedOrderLog
	{
		public AcceptedOrderLog()
		{
		}

		public AcceptedOrderLog(RequestLog request, Order order)
		{
			Request = request;
			Order = order;
		}

		public virtual uint Id { get; set; }
		public virtual RequestLog Request { get; set; }
		public virtual Order Order { get; set; }
	}
}