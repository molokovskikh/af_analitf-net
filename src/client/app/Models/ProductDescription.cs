using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace AnalitF.Net.Client.Models
{
	public interface IDocModel
	{
		string DisplayName { get; }
		FlowDocument ToFlowDocument();
		void Init(Config.Config config);
	}

	public class ProductDescription : IDocModel
	{
		public virtual uint Id { get; set; }
		public virtual string Name { get; set; }
		public virtual string EnglishName { get; set; }

		[Display(Name = "Состав:", Order = 0)]
		public virtual string Composition { get; set; }

		[Display(Name = "Фармакологическое действие:", Order = 1)]
		public virtual string PharmacologicalAction { get; set; }

		[Display(Name = "Показания к применению:", Order = 2)]
		public virtual string IndicationsForUse { get; set; }

		[Display(Name = "Способ применения и дозы:", Order = 3)]
		public virtual string Dosing { get; set; }

		[Display(Name = "Предостережения и противопоказания:", Order = 4)]
		public virtual string Warnings { get; set; }

		[Display(Name = "Побочные действия:", Order = 5)]
		public virtual string SideEffect { get; set; }

		[Display(Name = "Взаимодействие:", Order = 6)]
		public virtual string Interaction { get; set; }

		[Display(Name = "Форма выпуска:", Order = 7)]
		public virtual string ProductForm { get; set; }

		[Display(Name = "Дополнительно:", Order = 8)]
		public virtual string Description { get; set; }

		[Display(Name = "Условия хранения:", Order = 8)]
		public virtual string Storage { get; set; }

		[Display(Name = "Срок годности:", Order = 8)]
		public virtual string Expiration { get; set; }

		public virtual bool Hidden { get; set; }

		public virtual string FullName
		{
			get
			{
				if (!String.IsNullOrEmpty(EnglishName))
					return $"{Name} ({EnglishName})";
				return Name;
			}
		}

		public virtual bool Narcotic { get; set; }

		public virtual bool Toxic { get; set; }

		public virtual bool Combined { get; set; }

		public virtual bool Other { get; set; }

		public virtual bool IsPKU => Narcotic || Toxic || Combined || Other;

		public virtual string PKU
		{
			get
			{
				if (Narcotic)
					return "ПКУ:Наркотические и психотропные";
				if (Toxic)
					return "ПКУ:Сильнодействующие. и ядовитые";
				if (Combined)
					return "ПКУ:Комбинированные";
				if (Other)
					return "ПКУ:Иные лек.средства";
				return null;
			}
		}

		public virtual string DisplayName => "Описание " + FullName;

		public virtual FlowDocument ToFlowDocument()
		{
			var document = new FlowDocument();
			var properties = new List<Tuple<int, string, string>>();
			foreach (var property in GetType().GetProperties()) {
				var attribute = property.GetCustomAttributes(typeof(DisplayAttribute), true)
					.OfType<DisplayAttribute>()
					.FirstOrDefault();

				if (attribute == null || String.IsNullOrEmpty(attribute.Name))
					continue;

				var value = property.GetValue(this, null) as string;

				if (String.IsNullOrEmpty(value))
					continue;

				value = value.Trim();
				properties.Add(Tuple.Create(attribute.Order, attribute.Name, value));
			}

			document.Blocks.Add(new Paragraph(new Run(FullName)) {
				FontWeight = FontWeights.Bold,
				FontSize = 20,
				TextAlignment = TextAlignment.Center
			});
			if (IsPKU) {
				document.Blocks.Add(new Paragraph(new Run(PKU) { Foreground = Brushes.Red }));
			}

			foreach (var property in properties.OrderBy(p => p.Item1)) {
				document.Blocks.Add(new Paragraph(new Run(property.Item2) { FontWeight = FontWeights.Bold }));
				document.Blocks.Add(new Paragraph(new Run(property.Item3)));
			}

			return document;
		}

		public virtual void Init(Config.Config config)
		{
		}
	}
}