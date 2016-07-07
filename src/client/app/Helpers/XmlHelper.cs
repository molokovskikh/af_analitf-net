using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml.XPath;

namespace AnalitF.Net.Client.Helpers
{
	public class XMLDocHelper
	{
		public XMLDocHelper(byte[] xmldocument)
		{
			var str = Encoding.Default.GetString(xmldocument);
			var streader = new StringReader(str);
			xmlDocument = new XPathDocument(streader);
			xNavigator = xmlDocument.CreateNavigator();
		}

		public XMLDocHelper(string xmldocument)
		{
			var streader = new StringReader(xmldocument);
			xmlDocument = new XPathDocument(streader);
			xNavigator = xmlDocument.CreateNavigator();
		}

		public string GetValue(string xpath)
		{
			var node = xNavigator.Select(xpath);
			node.MoveNext();
			return node.Current.Value;
		}

		protected XPathDocument xmlDocument;
		protected XPathNavigator xNavigator;
	}
}