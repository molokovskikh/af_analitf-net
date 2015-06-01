﻿using System;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels.Dialogs;
using NUnit.Framework;

namespace AnalitF.Net.Test.Unit.ViewModels
{
	[TestFixture]
	public class TextDocFixture
	{
		[Test]
		public void Convert_text()
		{
			var text = @"Предложения по данным позициям из заказа отсутствуют
адрес доставки тестовый адрес доставки 27131
    прайс-лист Тестовый поставщик минимальный заказ 17948
        АМИНАЗИН р-р д/ин. (амп.) 2.5% - 1 мл N10 - CANPOL 385882399: имеется различие в цене препарата (старая цена: 5 155,25р.; новая цена: 3 803,59р.)
        АЛКЕРАН лиоф. пор. д/ин. (амп.) 50 мг N1 - CANPOL 272172924: имеется различие в цене препарата (старая цена: 1 963,27р.; новая цена: 7 846,26р.)
        АМИНАЗИН р-р д/ин. (амп.) 2.5% - 1 мл N10 1362470529 - CANPOL 2032824958: имеется различие в цене препарата (старая цена: 5 215,45р.; новая цена: 8 891,76р.)
        АЛКЕРАН лиоф. пор. д/ин. (амп.) 50 мг N1 795399701 - CANPOL 1413518865: имеется различие в цене препарата (старая цена: 5 211,38р.; новая цена: 6 772,29р.)
        АЛКА-ЗЕЛЬТЦЕР табл. шип. N10 - CANPOL 812196717: имеется различие в цене препарата (старая цена: 3 806,94р.; новая цена: 1 724,41р.)
        АЛКЕРАН табл. 2 мг N25 - CANPOL 814314867: имеется различие в цене препарата (старая цена: 4 155,58р.; новая цена: 5 851,08р.)
        АЛЛЕРГЕНЫ ПЫЛЬЦЕВЫЕ в комплектах - CANPOL 905024831: имеется различие в цене препарата (старая цена: 456,32р.; новая цена: 7 283,92р.)";
			var model = new TextDoc("тест", text);
			var doc = model.ToFlowDocument();
			Assert.That(WpfTestHelper.FlowDocumentToText(doc), Is.StringContaining("АЛЛЕРГЕНЫ ПЫЛЬЦЕВЫЕ"));
		}
	}
}