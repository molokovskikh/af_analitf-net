﻿using System;
using System.IO;

namespace AnalitF.Net.Service.Config
{
	public class Config
	{
		public Config()
		{
			//если настройка не задана отдаем файлы любого размера
			MaxReclameFileSize = long.MaxValue;
		}

		public string Environment { get; set; }
		//для тестирования
		public string RootPath { get; set; }

		public string LocalExportPath { get; set; }
		public string RemoteExportPath { get; set; }
		public string ResultPath { get; set; }
		public string UpdatePath { get; set; }
		public string AdsPath { get; set; }
		public string DocsPath { get; set; }
		public string CachePath { get; set; }
		public string AttachmentsPath { get; set; }
		public string PromotionsPath { get; set; }
		public string CertificatesPath { get; set; }
		public string PerUserUpdatePath { get; set; }

		public uint MaxProducerCostPriceId { get; set; }
		public uint MaxProducerCostCostId { get; set; }

		public string InjectedFault { get; set; }
		public string SupportMail { get; set; }
		public string OfficeMail { get; set; }
		public string BillingMail { get; set; }
		public TimeSpan ResultTimeout { get; set; }
		public bool DebugExport { get; set; }
		public long MaxReclameFileSize { get; set; }

		public string RtmUpdatePath
		{
			get { return Path.Combine(UpdatePath, "rtm"); }
		}

		public TimeSpan ExportTimeout { get; set; }
	}
}