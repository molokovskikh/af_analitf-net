using System;
using System.IO;
using System.Linq;
using NHibernate;
using NHibernate.Linq;
using Test.Support;
using Test.Support.Documents;

namespace AnalitF.Net.Client.Test.Fixtures
{
	public class CreateCertificate : ServerFixture
	{
		public TestWaybillLine Line;

		public override void Execute(ISession session)
		{
			var user = User(session);
			Line = session.Query<TestWaybillLine>().FirstOrDefault(l => l.Waybill.Client == user.Client && l.CatalogProduct != null && l.SerialNumber != null);
			if (Line == null)
				throw new Exception("Нет ни одной подходящей накладной для создания сертификата");
			var source = session.Query<TestCertificateSource>().FirstOrDefault(s => s.Suppliers.Contains(Line.Waybill.Supplier));
			if (source == null) {
				source = new TestCertificateSource(Line.Waybill.Supplier);
				session.Save(source);
			}

			Line.Certificate = null;
			var existsCert = session.Query<TestCertificate>()
				.FirstOrDefault(c => c.CatalogProduct == Line.CatalogProduct.CatalogProduct && c.SerialNumber == Line.SerialNumber);
			if (existsCert != null) {
				session.Delete(existsCert);
				session.Flush();
			}

			var cert = new TestCertificate(Line.CatalogProduct.CatalogProduct, Line.SerialNumber);
			var file = new TestCertificateFile(source) {
				Extension = ".gif"
			};
			cert.NewFile(file);
			session.Save(cert);
			Line.Certificate = cert;
			File.Copy(Path.Combine(TestHelpers.DataMother.GetRoot(), "assets", "certificate.gif"),
				Path.Combine(Config.CertificatesPath, file.Id + ".gif"));
			if (Verbose)
				Console.WriteLine("Для строки {0} накладной {1} создан сертификат", Line.Product, Line.Waybill.Log.Id);
		}
	}
}