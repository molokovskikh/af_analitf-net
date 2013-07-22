namespace AnalitF.Net.Service.Config
{
	public class Config
	{
		public string Environment { get; set; }
		//для тестирования
		public string RootPath { get; set; }

		public string ExportPath { get; set; }
		public string ResultPath { get; set; }
		public string UpdatePath { get; set; }
		public string AdsPath { get; set; }
		public string DocsPath { get; set; }

		public uint MaxProducerCostPriceId { get; set; }
		public uint MaxProducerCostCostId { get; set; }
	}
}