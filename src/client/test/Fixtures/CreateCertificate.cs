﻿using System;
using System.IO;
using System.Linq;
using AnalitF.Net.Client.Test.TestHelpers;
using Common.Tools;
using NHibernate;
using NHibernate.Linq;
using Test.Support;
using Test.Support.Documents;

namespace AnalitF.Net.Client.Test.Fixtures
{
	public class CreateCertificate : ServerFixture
	{
		public TestWaybillLine Line;
		public TestWaybill Waybill;

		public override void Execute(ISession session)
		{
			var user = User(session);
			IQueryable<TestWaybillLine> lines = session.Query<TestWaybillLine>().OrderByDescending(x => x.Id);
			if (Waybill != null)
				lines = session.Query<TestWaybillLine>().Where(l => l.Waybill.Id == Waybill.Id);
			Line = lines.FirstOrDefault(l => l.Waybill.Client == user.Client && l.CatalogProduct != null && l.SerialNumber != null);
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
				session.Query<TestWaybillLine>().Where(c => c.Certificate == existsCert).Each(l => l.Certificate = null);
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
			//если тесты запускать несколько раз с перезаливкой базы то id будет одинаковый
			File.Copy(Path.Combine(DbHelper.GetRoot(), "assets", "certificate.gif"),
				Path.Combine(Config.CertificatesPath, file.Id + ".gif"), true);
			if (Verbose)
				Console.WriteLine("Для строки {0} накладной {1} создан сертификат", Line.Product, Line.Waybill.Log.Id);
		}
	}
}