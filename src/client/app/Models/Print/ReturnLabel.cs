﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms.VisualStyles;
using System.Windows.Media;
using AnalitF.Net.Client.Models.Inventory;

namespace AnalitF.Net.Client.Models.Print
{
	public class ReturnLabel : BaseDocument
	{
		private ReturnToSupplier returnToSupplier;
		private WaybillSettings waybillSettings;
		public ReturnLabel(ReturnToSupplier _returnToSupplier)
		{
			returnToSupplier = _returnToSupplier;
			if (returnToSupplier.Lines.Count != 0)
				waybillSettings = returnToSupplier.Lines.First().Stock.WaybillSettings;
		}

		protected override void BuildDoc()
		{
			var center = Header("Приложение N 1 \n" +
				"к постановлению Правительства Российской Федерации \n" +
				"от 26 декабря 2011 г. N 1137");
			center.FontSize = 48;
			center.FontWeight = FontWeights.Bold;

			var body = new Grid()
				.Cell(0, 0, new Label
				{
					Content = new TextBlock
					{
						FontFamily = new FontFamily("Arial"),
						FontSize = 20,
						Text = "Отправитель",
					}
				})
				.Cell(0, 1, new Label
				{
					Content = new TextBlock
					{
						FontFamily = new FontFamily("Arial"),
						FontSize = 10,
						Text = returnToSupplier.SupplierName + "\n" + returnToSupplier.AddressName,
					}
				})
				.Cell(1, 0, new Label
				{
					Content = new TextBlock
					{
						FontFamily = new FontFamily("Arial"),
						FontSize = 20,
						Text = "Получатель"
					}
				})
				.Cell(1, 1, new Label
				{
					Content = new TextBlock
					{
						FontFamily = new FontFamily("Arial"),
						FontSize = 10,
						Text = waybillSettings==null ? "" : waybillSettings.FullName
					}
				})
				.Cell(2, 0, new Label
				{
					Content = new TextBlock
					{
						FontFamily = new FontFamily("Arial"),
						FontSize = 20,
						Text = "Комментарий",
					}
				})
				.Cell(2, 1, new Label
				{
					Content = new TextBlock
					{
						FontFamily = new FontFamily("Arial"),
						FontSize = 10,
						Text = returnToSupplier.Comment,
					}
				});
			doc.Blocks.Add(new BlockUIContainer(body));
		}
	}
}
