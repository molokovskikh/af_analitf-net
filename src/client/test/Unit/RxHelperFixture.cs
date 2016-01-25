using System;
using System.Collections.Generic;
using System.Reactive.Threading.Tasks;
using System.Threading;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Test.TestHelpers;
using Caliburn.Micro;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Unit
{
	[TestFixture]
	public class RxHelperFixture : BaseUnitFixture
	{
		public class CancelResult : IResult
		{
			public void Execute(ActionExecutionContext context)
			{
				Completed(this, new ResultCompletionEventArgs { WasCancelled = true });
			}

			public event EventHandler<ResultCompletionEventArgs> Completed;
		}

		public class FailResult : IResult
		{
			public void Execute(ActionExecutionContext context)
			{
				throw new NotImplementedException();
			}

			public event EventHandler<ResultCompletionEventArgs> Completed;
		}

		[Test]
		public void To_observable()
		{
			var results = new List<IResult>();
			Exception e = null;
			RxHelper.ToObservable(FakeResult()).Subscribe(r => {
				results.Add(r);
				r.Execute(new ActionExecutionContext());
			}, x => e = x);
			Assert.IsNull(e, e?.ToString());
			Assert.AreEqual(1, results.Count);
		}

		public IEnumerable<IResult> FakeResult()
		{
			yield return new CancelResult();
			yield return new FailResult();
		}
	}
}