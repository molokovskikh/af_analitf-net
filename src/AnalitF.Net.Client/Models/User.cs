using System;
using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Models.Print;
using AnalitF.Net.Client.ViewModels;
using Common.Tools;
using NHibernate.Mapping;

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
		};

		public static Dictionary<string, string> ShortcutExportMap = new Dictionary<string, string> {
			{ typeof(OrderDetailsViewModel).Name + "." + typeof(SentOrder), "ESOO" },
			{ typeof(OrderDetailsViewModel).Name + "." + typeof(Order), "ECOO" },
			{ typeof(CatalogNameViewModel).Name + "." + "CatalogNames", "FPCN" },
			{ typeof(CatalogNameViewModel).Name + "." + "Catalogs", "FPCF" },
			{ typeof(CatalogSearchViewModel).Name + "." + "Items", "FPCNF" },
			{ typeof(SearchOfferViewModel) + ".Offers", "FPL" },
			{ typeof(PriceViewModel).Name + "." + "Prices", "PLSL" },
			{ typeof(PriceOfferViewModel).Name + "." + "Offers", "PLSOS" },
			{ typeof(OrderLinesViewModel).Name + "." + "Lines", "COC" },
			{ typeof(OrderLinesViewModel).Name + "." + "SentLines", "COS" },
			{ typeof(OrdersViewModel).Name + "." + "Orders", "COA" },
			{ typeof(OrdersViewModel).Name + "." + "SentOrders", "SOA" },
			{ typeof(JunkOfferViewModel).Name + "." + "Offers", "EPP" },
			{ typeof(RejectsViewModel).Name + "." + "Rejects", "BP" },
			{ typeof(CatalogOfferViewModel).Name + "." + "Offers", "FPCPL" },
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
	}

	public class User
	{
		public User()
		{
			Permissions = new List<Permission>();
		}

		public virtual uint Id { get; set; }

		public virtual string FullName { get; set; }

		public virtual bool IsPriceEditDisabled { get; set; }

		public virtual bool IsPreprocessOrders { get; set; }

		public virtual bool ShowSupplierCost { get; set; }

		public virtual IList<Permission> Permissions { get; set; }

		public virtual bool CanPrint<T>()
		{
			return CanPrint(typeof(T));
		}

		public virtual bool CanPrint<T, T1>()
		{
			return CanPrint(typeof(T), typeof(T1));
		}

		public virtual bool CanPrint<T>(Type context)
		{
			return CanPrint(typeof(T), context);
		}

		public virtual bool CanPrint(params Type[] types)
		{
			var map = Permission.ShortcutPrintMap;
			var key = types.Implode(t => t.Name, ".");
			return Check(map, key);
		}

		public virtual bool CanExport(object model, string key)
		{
			return CanExport(model.GetType().Name + "." + key);
		}

		public virtual bool CanExport(string key)
		{
			return Check(Permission.ShortcutExportMap, key);
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