using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Integration.ViewModels
{
	[TestFixture]
	public class RejectsFixture : ViewModelFixture<RejectsViewModel>
	{
		[Test]
		public void Mark()
		{
			ForceInit();

			model.CurrentReject.Value = model.Rejects.Value.First();
			model.CurrentReject.Value.Marked = false;
			model.Mark();

			Assert.IsTrue(session.Load<Reject>(model.CurrentReject.Value.Id).Marked);
		}

		[Test]
		public void Remove_all_marks()
		{
			ForceInit();

			var reject = model.Rejects.Value.First();
			model.CurrentReject.Value = reject;
			model.CurrentReject.Value.Marked = false;
			model.Mark();
			model.CurrentReject.Value = null;
			model.ClearMarks();

			Assert.IsFalse(reject.Marked);
			Assert.IsFalse(session.Load<Reject>(reject.Id).Marked);
		}

		[Test]
		public void Print()
		{
			ForceInit();

			Assert.IsTrue(model.CanPrint);
			var doc = model.Print().Paginator;
			Assert.That(doc, Is.Not.Null);
		}
	}
}