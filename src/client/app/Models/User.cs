using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq.Observαble;
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.ViewModels;
using AnalitF.Net.Client.ViewModels.Offers;
using AnalitF.Net.Client.ViewModels.Orders;
using Common.Tools;
using NHibernate.Mapping;
using NPOI.SS.Formula.Functions;

namespace AnalitF.Net.Client.Models
{
	public class Permission
	{
		public static Dictionary<string, string > ShortcutPrintMap = new Dictionary<string, string> {
			{ typeof(CatalogOfferDocument).Name, "PCPL" },
			{ typeof(PriceOfferDocument).Name, "PPLS" },
			{ typeof(RejectsDocument).Name, "PBP" },
			{ typeof(OrderDocument).Name + "." + typeof(Order).Name, "PCO" },
			{ typeof(OrderDocument).Name + "." + typeof(SentOrder).Name, "PSO" },
			{ typeof(OrderLinesDocument).Name + "." + typeof(OrderLine).Name, "PCCO" },
			{ typeof(OrderLinesDocument).Name + "." + typeof(SentOrderLine).Name, "PSCO" },
			{ typeof(Batch).Name + "." + typeof(Offer).Name, "PCPL" },
			{ typeof(Batch).Name + "." + typeof(BatchLine).Name, "PCCO" },
			{ typeof(Awaited).Name + "." + typeof(Offer).Name, "PCPL" },
		};

		public static Dictionary<string, string> ShortcutExportMap = new Dictionary<string, string> {
			{ typeof(OrderDetailsViewModel).Name + "." + typeof(SentOrder).Name, "ESOO" },
			{ typeof(OrderDetailsViewModel).Name + "." + typeof(Order).Name, "ECOO" },
			{ typeof(CatalogNameViewModel).Name + "." + "CatalogNames", "FPCN" },
			{ typeof(CatalogNameViewModel).Name + "." + "Catalogs", "FPCF" },
			{ typeof(CatalogSearchViewModel).Name + "." + "Items", "FPCNF" },
			{ typeof(SearchOfferViewModel).Name + ".Offers", "FPL" },
			{ typeof(PriceViewModel).Name + "." + "Prices", "PLSL" },
			{ typeof(PriceOfferViewModel).Name + "." + "Offers", "PLSOS" },
			{ typeof(OrderLinesViewModel).Name + "." + "Lines", "COC" },
			{ typeof(OrderLinesViewModel).Name + "." + "SentLines", "COS" },
			{ typeof(OrdersViewModel).Name + "." + "Orders", "COA" },
			{ typeof(OrdersViewModel).Name + "." + "SentOrders", "SOA" },
			{ typeof(JunkOfferViewModel).Name + "." + "Offers", "EPP" },
			{ typeof(RejectsViewModel).Name + "." + "Rejects", "BP" },
			{ typeof(CatalogOfferViewModel).Name + "." + "Offers", "FPCPL" },
			{ typeof(Batch).Name + "." + "Offers", "FPCPL" },
			{ typeof(Batch).Name + "." + "ReportLines", "COC" },
			{ typeof(Awaited).Name + "." + "Offers", "FPCPL" },
			{ typeof(Awaited).Name + "." + "Items", "FPCNF" },
			{ typeof(MinCosts).Name + "." + "Offers", "FPCPL" },
			{ typeof(MnnViewModel).Name + "." + "Mnns", "FPCNF" },
		};

		public Permission()
		{
		}

		public Permission(string name)
		{
			Name = name;
		}

		public virtual uint Id { get; set; }
		public virtual string Name { get; set; }

		public override string ToString()
		{
			return Name;
		}
	}

	public class User
	{
		public User()
		{
			Permissions = new List<Permission>();
		}

		public virtual uint Id { get; set; }

		public virtual string Name { get; set; }

		public virtual string FullName { get; set; }

		public virtual bool IsPriceEditDisabled { get; set; }

		public virtual bool IsPreprocessOrders { get; set; }

		public virtual bool ShowSupplierCost { get; set; }

		public virtual bool IsDelayOfPaymentEnabled { get; set; }

		public virtual bool SaveOrders { get; set; }

		public virtual string SupportPhone { get; set; }

		public virtual string SupportHours { get; set; }

		public virtual DateTime? LastSync { get; set; }

		public virtual bool HaveLimits { get; set; }

		public virtual bool CalculateOnProducerCost { get; set; }

		public virtual IList<Permission> Permissions { get; set; }

		public virtual bool CanPrint<T>()
		{
			return HasPermission(Permission.ShortcutPrintMap, typeof(T));
		}

		public virtual bool CanPrint<T, T1>()
		{
			return HasPermission(Permission.ShortcutPrintMap, typeof(T), typeof(T1));
		}

		public virtual bool CanPrint<T>(Type context)
		{
			return HasPermission(Permission.ShortcutPrintMap, typeof(T), context);
		}

		public virtual bool CanExport(object model, string key)
		{
			return CanExport(model.GetType().Name + "." + key);
		}

		public virtual bool CanExport(string key)
		{
			return Check(Permission.ShortcutExportMap, key);
		}

		public virtual bool HasPermission(Dictionary<string, string> map, params Type[] types)
		{
			var key = types.Implode(t => t.Name, ".");
			return Check(map, key);
		}

		private bool Check(Dictionary<string, string> map, string key)
		{
			var shortcut = map.GetValueOrDefault(key);
			if (shortcut == null)
				return true;
			return Permissions.Any(p => p.Name.Match(shortcut));
		}
	}
}