using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels.Diadok;
using NHibernate.Linq;
using NUnit.Framework;
using ReactiveUI.Testing;
using AnalitF.Net.Client.Test.Fixtures;
using Caliburn.Micro;
using System.Windows.Controls;
using System.Reactive.Concurrency;
using Microsoft.Reactive.Testing;
using System.Threading.Tasks;
using System.Threading;
using System;
using Common.Tools.Helpers;
using System.Windows;
using System.Security.Cryptography.X509Certificates;
using BilateralDocumentStatus = Diadoc.Api.Proto.Documents.BilateralDocument.BilateralDocumentStatus;
using InvoiceStatus = Diadoc.Api.Com.InvoiceStatus;

namespace AnalitF.Net.Client.Test.Integration.ViewModels
{
	[TestFixture]
	public class DiadokFixture : DispatcherFixture
	{
		CreateDiadokInbox diadokDatas;
		Index ddkIndex;

		[SetUp]
		public new void Setup()
		{
			base.Setup();
			StartWait();

			Env.Settings = session.Query<Settings>().First();

			Env.Settings.DiadokSignerJobTitle = "Signer";
			Env.Settings.DiadokUsername = ddk.ie_login;
			Env.Settings.DiadokPassword = ddk.ie_passwd;
			Env.Settings.DiadokCert = "Тестовая организация №5627996 - UC Test (Qualified)";
			Env.Settings.DebugUseTestSign = true;

			session.Save(Env.Settings);
			session.Flush();


			Click("ShowExtDocs");

			ddkIndex = shell.ActiveItem as Index;

			dispatcher.Invoke(() => (ddkIndex.Scheduler as TestScheduler).Start());
			scheduler.Start();

			Wait();
			dispatcher.Invoke(() => ddkIndex.DeleteAll());
			Wait();

			diadokDatas = Fixture<CreateDiadokInbox>();
		}

		void Wait()
		{
			if(ddkIndex != null)
			{
				dispatcher.Invoke(() => (ddkIndex.Scheduler as TestScheduler).AdvanceByMs(5000));
				scheduler.AdvanceByMs(5000);
				dispatcher.WaitIdle();
				WaitIdle();
			}
		}

		[Test]
		public void Load_Inbox()
		{
			Thread.Sleep(TimeSpan.FromSeconds(30));
			dispatcher.Invoke(() => ddkIndex.Reload());
			Wait();

			int outboxCount = -34523521;
			int inboxCount = -235643532;

			inboxCount = diadokDatas.GetMessages().Item1.Count;
			dispatcher.Invoke(() =>
			{
				outboxCount = ddkIndex.Items.Value.Count;
				Assert.AreEqual(inboxCount, outboxCount);
			});
		}

		[Test]
		public void Sign_Inbox()
		{
			Thread.Sleep(TimeSpan.FromSeconds(30));
			dispatcher.Invoke(() => ddkIndex.Reload());

			Wait();

			string torg12id = "";
			string invoiceid = "";
			//Подписываем ТОРГ12
			dispatcher.Invoke(() => {
				ddkIndex.CurrentItem.Value = ddkIndex.Items.Value.First(f => f.Entity.DocumentInfo.Type == Diadoc.Api.Com.DocumentType.XmlTorg12);
				torg12id = ddkIndex.CurrentItem.Value.Entity.EntityId;
			});

			Wait();
			AsyncClick("Sign");
			Wait();
			WaitWindow("АналитФАРМАЦИЯ");
			Wait();
			Click("Save");

			Wait();

			//подписываем Инвойс
			dispatcher.Invoke(() => {
				ddkIndex.CurrentItem.Value = ddkIndex.Items.Value.First(f => f.Entity.DocumentInfo.Type == Diadoc.Api.Com.DocumentType.Invoice);
				invoiceid = ddkIndex.CurrentItem.Value.Entity.EntityId;
			});
			Wait();
			AsyncClick("Sign");
			Wait();
			WaitWindow("АналитФАРМАЦИЯ");
			Wait();
			Click("Save");

			Wait();
			Thread.Sleep(TimeSpan.FromSeconds(3));
			dispatcher.Invoke(() => ddkIndex.Reload());
			Wait();

			dispatcher.Invoke(() => {
				var signtorg12ok = ddkIndex.Items.Value.First(e => e.Entity.EntityId == torg12id).Entity.DocumentInfo.XmlTorg12Metadata.DocumentStatus == BilateralDocumentStatus.InboundWithRecipientSignature;
				Assert.IsTrue(signtorg12ok);
			});

			dispatcher.Invoke(() => {
				var signinvoice = ddkIndex.Items.Value.First(e => e.Entity.EntityId == invoiceid).Entity.DocumentInfo.InvoiceMetadata.Status == InvoiceStatus.InboundFinished;
				Assert.IsTrue(signinvoice);
			});

			Wait();

		}

		//Подпись
		//Отказ
		//Аннулирование
		//А - подпись
		//А - отказ
		//Открыть
		//Сохранить
		//Удалить
		//Распечатать
		//Согласование
		// На согласование
		// На подпись
		// Согласовать
		// Отказать в согласовании
	}
}