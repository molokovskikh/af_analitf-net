using System;
using System.Reactive.Linq;
using System.Threading;
using NUnit.Framework;

namespace AnalitF.Net.Test.Unit
{
	[TestFixture]
	public class ObservableFixture
	{
		[Test]
		public void Test()
		{
			//Observable.Timer(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(60))
			Observable.Interval(TimeSpan.FromSeconds(3))
				.Do(c => {
					Console.WriteLine("origin = " + DateTime.Now);
				})
				.Take(4)
				//.Throttle(TimeSpan.FromSeconds(2))
				.Timeout(TimeSpan.FromSeconds(2))
				.Subscribe(l => Console.WriteLine(DateTime.Now));

			Thread.Sleep(50000);
		}
	}
}