﻿using System;
using System.Collections.Generic;
using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using NHibernate;
using NHibernate.Linq;

namespace AnalitF.Net.Client.Test.Fixtures
{
	public class LocalWaybill
	{
		public Waybill Waybill;

		public void Execute(ISession session)
		{
			var address = session.Query<Address>().First();
			var settings = session.Query<Settings>().First();

			Waybill = new Waybill(address, session.Query<Supplier>().First());
			Waybill.Lines = Enumerable.Range(0, 10).Select(i => new WaybillLine(Waybill)).ToList();
			Waybill.ProviderDocumentId = "н2";
			var line = Waybill.Lines[0];
			line.Quantity = 10;
			line.Nds = 10;
			line.ProducerCost = 15.13m;
			line.SupplierCostWithoutNds = 18.25m;
			line.SupplierCost = 20.8m;

			Waybill.Lines.Add(new WaybillLine(Waybill) {
				Product = "Доксазозин 4мг таб. Х30 (R)",
				Certificates = "РОСС RU.ФМ08.Д38737",
				Period = "01.05.2017",
				Producer = "Нью-Фарм Инк./Вектор-Медика ЗАО, РОССИЯ",
				ProducerCost = 213.18m,
				RegistryCost = 382.89m,
				SupplierPriceMarkup = -5.746m,
				SupplierCostWithoutNds = 200.93m,
				SupplierCost = 221.03m,
				Quantity = 2,
				VitallyImportant = true,
				Nds = 10,
				SerialNumber = "21012",
				Amount = 442.05m,
				NdsAmount = 40.19m,
				BillOfEntryNumber = "10609010/101209/0004305/1",
				//для отчета по жизененно важным
				EAN13 = "4606915000379",
			});
			//плохая строка с ценой производителя 0
			Waybill.Lines.Add(new WaybillLine(Waybill) {
				Product = "Доксазозин 4мг таб. Х30 (R)",
				Certificates = "РОСС RU.ФМ08.Д38737",
				Period = "01.05.2017",
				Producer = "Нью-Фарм Инк./Вектор-Медика ЗАО, РОССИЯ",
				ProducerCost = 0,
				RegistryCost = 382.89m,
				SupplierPriceMarkup = -5.746m,
				SupplierCostWithoutNds = 200.93m,
				SupplierCost = 221.03m,
				Quantity = 2,
				VitallyImportant = true,
				Nds = 10,
				SerialNumber = "21012",
				Amount = 442.05m,
				NdsAmount = 40.19m,
				BillOfEntryNumber = "10609010/101209/0004305/1",
				//для отчета по жизененно важным
				EAN13 = "4606915000379",
			});
			//плохая строка с SupplierCostWithoutNds 0 это раньше вызывало ошибку в VitallyImportantReport
			Waybill.Lines.Add(new WaybillLine(Waybill) {
				Product = "Доксазозин 2мг таб. Х30 (R)",
				Certificates = "РОСС RU.ФМ08.Д38737",
				Period = "01.05.2017",
				Producer = "Нью-Фарм Инк./Вектор-Медика ЗАО, РОССИЯ",
				ProducerCost = 120m,
				RegistryCost = 382.89m,
				SupplierPriceMarkup = -5.746m,
				SupplierCostWithoutNds = 0,
				SupplierCost = 128,
				Quantity = 2,
				Nds = 10,
				SerialNumber = "21012",
				Amount = 442.05m,
				NdsAmount = 40.19m,
				BillOfEntryNumber = "10609010/101209/0004305/1",
				//для отчета ежемесячного
				EAN13 = "4606915000355",
			});
			Waybill.Calculate(settings, new List<uint>());
			session.Save(Waybill);
			session.Flush();
		}
	}
}