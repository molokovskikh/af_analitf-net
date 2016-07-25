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
			Start();

			Env.Settings = session.Query<Settings>().First();

			Env.Settings.DiadokSignerJobTitle = "Signer";
			Env.Settings.DiadokUsername = "c963832@mvrht.com";
			Env.Settings.DiadokPassword = "222852";
			Env.Settings.DiadokCert = "Тестовая организация №5627996 - UC Test (Qualified)";
			Env.Settings.DebugUseTestSign = true;

			session.Save(Env.Settings);
			session.Flush();



			Click("ShowExtDocs");
			ddkIndex = shell.ActiveItem as Index;


			Wait();
			dispatcher.Invoke(() => ddkIndex.DeleteAll());
			Wait();

			diadokDatas = Fixture<CreateDiadokInbox>();
		}

		void Wait()
		{
			if(ddkIndex != null)
			{
				(ddkIndex.Scheduler as TestScheduler).AdvanceByMs(5000);
				dispatcher.Invoke(() => scheduler.AdvanceByMs(1000));
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

			int outboxCount = diadokDatas.GetMessages().Item1.Count;
			int inboxCount = ddkIndex.Items.Value.Count;

			Assert.AreEqual(inboxCount, outboxCount);
		}

		[Test]
		public void Sign_Inbox()
		{
			Thread.Sleep(TimeSpan.FromSeconds(30));
			dispatcher.Invoke(() => ddkIndex.Reload());

			Wait();

			for(int i = 0; i < 1; i++)
			{
				dispatcher.Invoke(() => {
					ddkIndex.CurrentItem.Value = ddkIndex.Items.Value.Skip(i).First();
				});
				Wait();
				AsyncClick("Sign");
				Wait();
				Click("Save");
			}

			Wait();
			dispatcher.Invoke(() => ddkIndex.Reload());
			Wait();

			bool signok = ddkIndex.Items.Value.Skip(0).First().Entity.DocumentInfo.XmlTorg12Metadata.DocumentStatus == Diadoc.Api.Proto.Documents.BilateralDocument.BilateralDocumentStatus.InboundWithRecipientSignature;

			Assert.AreEqual(signok, true);
		}
	}
}