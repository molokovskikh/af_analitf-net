using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using AnalitF.Net.Client.Models;
using Common.Tools;
using NHibernate;
using NHibernate.Linq;
using NPOI.SS.Formula.Functions;
using Test.Support;
using Address = AnalitF.Net.Client.Models.Address;

namespace AnalitF.Net.Client.Test.TestHelpers
{
	public class DataMother
	{
		private ISession session;

		public DataMother(ISession session)
		{
			this.session = session;
		}

		public Waybill CreateWaybill(Address address, Settings settings)
		{
			var waybill = new Waybill {
				Address = address,
				WriteTime = DateTime.Now,
				DocumentDate = DateTime.Now,
				Supplier = session.Query<Supplier>().First()
			};
			waybill.Lines = Enumerable.Range(0, 10).Select(i => new WaybillLine(waybill)).ToList();
			var line = waybill.Lines[0];
			line.Quantity = 10;
			line.Nds = 10;
			line.ProducerCost = 15.13m;
			line.SupplierCostWithoutNds = 18.25m;
			line.SupplierCost = 20.8m;
			waybill.Calculate(settings);
			session.Save(waybill);
			session.Flush();

			return waybill;
		}

		public static TestWaybill CreateWaybill(ISession session, TestUser user)
		{
			return global::Test.Data.DataMother.CreateWaybill(session, user);
		}

		public static void CopyBin(string src, string dst)
		{
			var regex = new Regex(@"(\.dll|\.exe|\.config|\.pdb)$", RegexOptions.IgnoreCase);
			Directory.GetFiles(src).Where(f => regex.IsMatch(f))
				.Each(f => File.Copy(f, Path.Combine(dst, Path.GetFileName(f)), true));
		}

		public static string ProjectBin(string name)
		{
			return InternalProjectBin(name);
		}

		private static string InternalProjectBin(string name, [CallerFilePath]string self = "")
		{
			return Path.GetFullPath(Path.Combine(self, "..", "..", "..", name, "bin", "debug"));
		}
	}
}