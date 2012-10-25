namespace AnalitF.Net.Client.Models
{
	public class ProductDescription
	{
		public virtual uint Id { get; set; }
		public virtual string Name { get; set; }
		public virtual string EnglishName { get; set; }
		public virtual string Description { get; set; }
		public virtual string Interaction { get; set; }
		public virtual string SideEffect { get; set; }
		public virtual string IndicationsForUse { get; set; }
		public virtual string Dosing { get; set; }
		public virtual string Warnings { get; set; }
		public virtual string ProductForm { get; set; }
		public virtual string PharmacologicalAction { get; set; }
		public virtual string Storage { get; set; }
		public virtual string Expiration { get; set; }
		public virtual string Composition { get; set; }
	}
}