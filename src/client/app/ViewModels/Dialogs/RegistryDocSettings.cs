using System;
using System.ComponentModel.DataAnnotations;
using AnalitF.Net.Client.Config.NHibernate;
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.Models.Results;
using Caliburn.Micro;

namespace AnalitF.Net.Client.ViewModels.Dialogs
{
	public class RegistryDocSettings : Screen, ICancelable
	{
		private RegistryDocumentSettings settings;

		public RegistryDocSettings(RegistryDocumentSettings settings)
		{
			DisplayName = "Настройка печати реестра";
			WasCancelled = true;
			this.settings = settings;
			RegistryId = settings.RegistryId;
			Date = settings.Date;
			CommitteeMember1 = settings.CommitteeMember1;
			CommitteeMember2 = settings.CommitteeMember2;
			CommitteeMember3 = settings.CommitteeMember3;
			Acceptor = settings.Acceptor;
			SignerType = settings.Type;
		}

		public bool WasCancelled { get; set; }

		public string RegistryId { get; set; }

		public DateTime Date { get; set; }

		public string CommitteeMember1 { get; set; }

		public string CommitteeMember2 { get; set; }

		public string CommitteeMember3 { get; set; }

		public string Acceptor { get; set; }

		public RegistryDocumentSettings.SignerType SignerType { get; set; }

		public void OK()
		{
			WasCancelled = false;
			settings.RegistryId = RegistryId;
			settings.Date = Date;
			settings.CommitteeMember1 = CommitteeMember1;
			settings.CommitteeMember2 = CommitteeMember2;
			settings.CommitteeMember3 = CommitteeMember3;
			settings.Acceptor = Acceptor;
			settings.Type = SignerType;
			TryClose();
		}
	}
}