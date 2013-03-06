using System;
using System.Linq;
using Common.Tools.Calendar;
using NHibernate;
using NHibernate.Linq;

namespace AnalitF.Net.Client.Models
{
	public class Stat
	{
		public Stat()
		{
		}

		public Stat(Address address)
		{
			if (address == null)
				return;

			var orders = address.Orders.Where(o => !o.Frozen).ToArray();
			OrdersCount = orders.Length;
			ReadyForSendOrdersCount = orders.Count(o => o.Send);
			OrderLinesCount = orders.SelectMany(o => o.Lines).Count();
			Sum = orders.Sum(o => o.Sum);
		}

		public Stat(Stat newStat, Stat currentStat)
		{
			OrdersCount = newStat.OrdersCount;
			OrderLinesCount = newStat.OrderLinesCount;
			Sum = newStat.Sum;
			WeeklySum = currentStat.WeeklySum;
			MonthlySum = currentStat.MonthlySum;
		}

		public int ReadyForSendOrdersCount { get; set; }
		public int OrdersCount { get; set; }
		public int OrderLinesCount { get; set; }
		public decimal Sum { get; set; }
		public decimal WeeklySum { get; set; }
		public decimal MonthlySum { get; set; }

		public override string ToString()
		{
			return string.Format("OrdersCount: {0}, OrderLinesCount: {1}, Sum: {2}, WeeklySum: {3}, MonthlySum: {4}",
				OrdersCount,
				OrderLinesCount,
				Sum,
				WeeklySum,
				MonthlySum);
		}

		public static Stat Update(ISession session, Address value)
		{
			if (value == null) {
				return new Stat {
					OrdersCount = 0,
					OrderLinesCount = 0,
					Sum = 0,
					WeeklySum = 0,
					MonthlySum = 0,
				};
			}

			var stat = new Stat();
			stat.OrdersCount = session.Query<Order>().Count(o => o.Address == value && !o.Frozen);
			stat.ReadyForSendOrdersCount = session.Query<Order>().Count(o => o.Address == value && !o.Frozen && o.Send);
			stat.OrderLinesCount = session.Query<OrderLine>().Count(o => o.Order.Address == value && !o.Order.Frozen);
			stat.Sum = session.Query<Order>().Where(o => o.Address == value && !o.Frozen).Sum(o => (decimal?)o.Sum)
				.GetValueOrDefault();
			stat.WeeklySum = session.Query<SentOrder>()
				.Where(o => o.Address == value && o.SentOn >= DateTime.Today.FirstDayOfWeek())
				.Sum(o => (decimal?)o.Sum)
				.GetValueOrDefault();
			stat.MonthlySum = session.Query<SentOrder>()
				.Where(o => o.Address == value && o.SentOn >= DateTime.Today.FirstDayOfMonth())
				.Sum(o => (decimal?)o.Sum)
				.GetValueOrDefault();
			return stat;
		}
	}
}