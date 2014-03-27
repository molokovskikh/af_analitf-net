namespace AnalitF.Net.Service.Config
{
	public class Config
	{
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

		public uint MaxProducerCostPriceId { get; set; }
		public uint MaxProducerCostCostId { get; set; }

		public string InjectedFault { get; set; }
	}
}