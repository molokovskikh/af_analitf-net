using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using AnalitF.Net.Client.ViewModels;
using Common.Tools;
using NHibernate;
using NHibernate.Linq;

namespace AnalitF.Net.Client.Models
{
	public enum DiffCalcMode
	{
		[Description("От предыдущего предложения")] PrevOffer,
		[Description("От минимальной цены")] MinCost,
		[Description("От минимальной цены в основных поставщиках")] MinBaseCost,
	}

	public class WaybillSettings
	{
		public WaybillSettings()
		{
		}

		public WaybillSettings(User user, Address address)
		{
			Name = user.FullName;
			Address = address.Name;
			BelongsToAddress = address;
		}

		public virtual uint Id { get; set; }

		public virtual string Name { get; set; }
		public virtual string Address { get; set; }
		public virtual string Director { get; set; }
		public virtual string Accountant { get; set; }
		public virtual Address BelongsToAddress { get; set; }

		public virtual string FullName
		{
			get { return String.Format("{0}, {1}", Name, Address); }
		}
	}

	public enum PriceTagType
	{
		[Description("Стандартный размер")] Normal,
		[Description("Малый размер")] Small,
		[Description("Малый размер с большой ценой")] BigCost,
		[Description("Малый размер с большой ценой №2")] BigCost2,
	}

	public class PriceTagSettings
	{
		public PriceTagSettings()
		{
			PrintProduct  = true;
			PrintCountry  = true;
			PrintProducer  = true;
			PrintPeriod  = true;
			PrintProviderDocumentId  = true;
			PrintSupplier  = true;
			PrintSerialNumber  = true;
			PrintDocumentDate  = true;
		}

		public virtual PriceTagType Type { get; set; }
		public virtual bool PrintEmpty { get; set; }
		public virtual bool HideNotPrinted { get; set; }

		public virtual bool PrintProduct { get; set; }
		public virtual bool PrintCountry { get; set; }
		public virtual bool PrintProducer { get; set; }
		public virtual bool PrintPeriod { get; set; }
		public virtual bool PrintProviderDocumentId { get; set; }
		public virtual bool PrintSupplier { get; set; }
		public virtual bool PrintSerialNumber { get; set; }
		public virtual bool PrintDocumentDate { get; set; }
	}

	public enum RackingMapSize
	{
		[Description("Стандартный размер")] Normal,
		[Description("Большой размер")] Big
	}

	public class RackingMapSettings
	{
		public RackingMapSettings()
		{
			PrintProduct = true;
			PrintProducer = true;
			PrintSerialNumber = true;
			PrintPeriod = true;
			PrintQuantity = true;
			PrintSupplier = true;
			PrintCertificates = true;
			PrintDocumentDate = true;
			PrintRetailCost = true;
		}

		public virtual RackingMapSize Size { get; set; }
		public virtual bool HideNotPrinted { get; set; }
		public virtual bool PrintProduct { get; set; }
		public virtual bool PrintProducer { get; set; }
		public virtual bool PrintSerialNumber { get; set; }
		public virtual bool PrintPeriod { get; set; }
		public virtual bool PrintQuantity { get; set; }
		public virtual bool PrintSupplier { get; set; }
		public virtual bool PrintCertificates { get; set; }
		public virtual bool PrintDocumentDate { get; set; }
		public virtual bool PrintRetailCost { get; set; }
	}

	public class Settings
	{
		public Settings()
		{
			OverCountWarningFactor = 5;
			OverCostWarningPercent = 5;
			RackingMap = new RackingMapSettings();
			PriceTag = new PriceTagSettings();
		}

		public virtual int Id { get; set; }

		public virtual bool CanViewOffersByCatalogName { get; set; }
		public virtual bool GroupByProduct { get; set; }
		public virtual bool ShowPriceName { get; set; }
		public virtual int BaseFromCategory { get; set; }

		public virtual decimal OverCountWarningFactor { get; set; }

		public virtual decimal OverCostWarningPercent { get; set; }

		public virtual decimal MaxOverCostOnRestoreOrder { get; set; }

		public virtual DiffCalcMode DiffCalcMode { get; set; }

		public virtual string UserName { get; set; }

		public virtual string Password { get; set; }

		public virtual DateTime? LastUpdate { get; set; }

		public virtual bool LookupMarkByProducerCost { get; set; }

		public virtual RackingMapSettings RackingMap { get; set; }

		public virtual PriceTagSettings PriceTag { get; set; }

		public virtual bool IsValid
		{
			get { return !String.IsNullOrEmpty(Password) && !String.IsNullOrEmpty(UserName); }
		}

		public virtual IEnumerable<string> DocumentDirs
		{
			get
			{
				var root = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
				root = Path.Combine(root, "АналитФАРМАЦИЯ");
				var dirs = new [] {"Документы", "Отказы", "Накладные"};
				return dirs.Select(d => Path.Combine(root, d));
			}
		}

		public virtual void ApplyChanges(ISession session)
		{
			session
				.CreateSQLQuery("update prices set BasePrice = Category > :baseCategory")
				.SetParameter("baseCategory", BaseFromCategory)
				.ExecuteUpdate();

			var prices = session.Query<Price>().ToList();

			UpdatePriceNames(prices);
		}

		public virtual void UpdatePriceNames(List<Price> prices)
		{
			if (ShowPriceName) {
				prices.Each(p => p.Name = String.Format("{0} {1}", p.SupplierName, p.PriceName));
			}
			else {
				var groups = prices.GroupBy(p => p.SupplierId).ToDictionary(g => g.Key, g => g.Count());
				prices.Each(p => {
					if (groups[p.SupplierId] > 1)
						p.Name = String.Format("{0} {1}", p.SupplierName, p.PriceName);
					else
						p.Name = p.SupplierName;
				});
			}
		}

		public virtual string CheckUpdateCondition()
		{
			if (LastUpdate == null)
				return "База данных программы не заполнена. Выполнить обновление?";

			if (LastUpdate < DateTime.Now.AddHours(-8))
				return "Вы работаете с устаревшим набором данных. Выполнить обновление?";

			return null;
		}
	}
}