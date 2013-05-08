using System.IO;
using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels;
using NUnit.Framework;
using Test.Support.log4net;

namespace AnalitF.Net.Test.Integration.ViewModes
{
	[TestFixture]
	public class RejectsFixture : BaseFixture
	{
		private RejectsViewModel model;

		[SetUp]
		public void Setup()
		{
			model = Init<RejectsViewModel>();
		}

		[Test]
		public void Mark()
		{
			model.CurrentReject.Value = model.Rejects.Value.First();
			model.CurrentReject.Value.Marked = false;
			model.Mark();

			Assert.IsTrue(session.Load<Reject>(model.CurrentReject.Value.Id).Marked);
		}

		[Test]
		public void Remove_all_marks()
		{
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
			Assert.IsTrue(model.CanPrint);
			var doc = model.Print().Paginator;
			Assert.That(doc, Is.Not.Null);
		}
	}
}