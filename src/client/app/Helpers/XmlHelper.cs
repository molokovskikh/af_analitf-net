using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml.XPath;
using Diadoc.Api;
using Diadoc.Api.Proto.Events;

namespace AnalitF.Net.Client.Helpers
{
	public class XmlDocHelper
	{
		public XmlDocHelper(byte[] xmldocument)
		{
			var str = Encoding.GetEncoding(1251).GetString(xmldocument);
			var streader = new StringReader(str);
			XmlDocument = new XPathDocument(streader);
			XNavigator = XmlDocument.CreateNavigator();
		}

		public XmlDocHelper(string xmldocument)
		{
			var streader = new StringReader(xmldocument);
			XmlDocument = new XPathDocument(streader);
			XNavigator = XmlDocument.CreateNavigator();
		}

		public string GetValue(string xpath)
		{
			var node = XNavigator.Select(xpath);
			node.MoveNext();
			return node.Current.Value;
		}

		public string GetDiadokFIO(string xpath)
		{
			var fnval = GetValue($"{xpath}@Имя");
			var snval = GetValue($"{xpath}@Фамилия");
			var pnval = GetValue($"{xpath}@Отчество");
			var sn = snval;
			var fn = fnval.Length > 0 ? fnval[0].ToString() : "";
			var pn = pnval.Length > 0 ? pnval[0].ToString() : "";

			var fio = $"{sn} {fn}.{pn}.";
			return fio;
		}

		protected XPathDocument XmlDocument;
		protected XPathNavigator XNavigator;
	}

	public class DiadocXmlHelper
	{
		XmlDocHelper xml;
		protected DiadocXmlHelper()
		{
		}

		public DiadocXmlHelper(Entity entity)
		{
			var str = Encoding.GetEncoding(1251).GetString(entity.Content.Data);
			str = str.Replace("xmlns=\"http://www.roseu.org/images/stories/roaming/amendment-request-v1.xsd\"","");
			xml = new XmlDocHelper(str);
		}

		public string GetValue(string xpath)
		{
			return xml.GetValue(xpath);
		}

		public string GetDiadokFIO(string xpath)
		{
			var fnval = xml.GetValue($"{xpath}@Имя");
			var snval = xml.GetValue($"{xpath}@Фамилия");
			var pnval = xml.GetValue($"{xpath}@Отчество");
			var sn = snval;
			var fn = fnval.Length > 0 ? fnval[0].ToString() : "";
			var pn = pnval.Length > 0 ? pnval[0].ToString() : "";

			var fio = $"{sn} {fn}.{pn}.";
			return fio;
		}

		public string GetDiadokTORG12Name(string spliter = "\n")
		{
			var num = xml.GetValue("Файл/Документ/СвТНО/ТН/@НомТН");
			var date = xml.GetValue("Файл/Документ/СвТНО/ТН/@ДатаТН");
			var summ = xml.GetValue("Файл/Документ/СвТНО/ТН/Таблица/ВсегоНакл/@СумУчНДСВс");
			var nds = xml.GetValue("Файл/Документ/СвТНО/ТН/Таблица/ВсегоНакл/@СумНДСВс");

			return $"Накладная №{num} от {date}{spliter}{summ} Руб. НДС: {nds} Руб.";
		}

		public string GetDiadokInvoiceName(string spliter = "\n")
		{
			var num = xml.GetValue("Файл/Документ/СвСчФакт/@НомерСчФ");
			var date = xml.GetValue("Файл/Документ/СвСчФакт/@ДатаСчФ");
			var summ = xml.GetValue("Файл/Документ/ТаблСчФакт/ВсегоОпл/@СтТовУчНалВсего");
			var nds = xml.GetValue("Файл/Документ/ТаблСчФакт/ВсегоОпл/СумНалВсего/СумНДС");

			return $"Счет-фактура №{num} от {date}{spliter}{summ} НДС: {nds} ";
		}
	}
}