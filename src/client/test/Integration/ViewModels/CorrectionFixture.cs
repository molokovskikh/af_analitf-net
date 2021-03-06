﻿using System;
using System.IO;
using System.Linq;
using System.Text;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels.Orders;
using Common.NHibernate;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Integration.ViewModels
{
	[TestFixture]
	public class CorrectionFixture : ViewModelFixture
	{
		Correction correction;

		[SetUp]
		public void Setup()
		{
			session.DeleteEach<Order>();
			var order = MakeOrder();
			var orderLine = order.Lines[0];
			orderLine.Apply(new OrderLineResult {
				Result = LineResultStatus.CostChanged,
				ServerCost = orderLine.Cost + 1,
				ServerQuantity = orderLine.Count
			});
		}

		[Test]
		public void Correction_report()
		{
			correction = Open(new Correction());
			Assert.IsTrue(correction.IsUpdate);
			Assert.IsFalse(correction.IsOrderSend);
			var report = correction.Save().ToArray().OfType<SaveFileResult>().First();
			var text = File.ReadAllText(report.Dialog.FileName, Encoding.Default);
			Assert.That(text, Does.Contain(String.Format("адрес доставки {0}", address.Name)));
		}

		[Test]
		public void Disable_send_as_is()
		{
			correction = Open(new Correction(address.Id));

			Assert.AreEqual(1, correction.Lines.Count);
			Assert.IsTrue(correction.CanSend.Value);
			correction.Lines[0].Order.Send = false;
			Assert.IsFalse(correction.CanSend.Value);
		}
	}
}