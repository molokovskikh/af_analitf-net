using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using AnalitF.Net.Client.Models.Inventory;
using AnalitF.Net.Client.ViewModels;

namespace AnalitF.Net.Client.Models.Print
{
	public class CheckDocument : BaseDocument
	{
		public Check Check;
		public IList<CheckLine> Lines;

		public CheckDocument(Check check, IList<CheckLine> lines)
		{
			Check = check;
			Lines = lines;
		}

		public CheckDocument(Check check)
		{
			Check = check;
			Lines = check.Lines.ToArray();
		}

		protected override void BuildDoc()
		{
			var headers = new[] {
				new PrintColumn("Test", 0),
			};
			var table = BuildTableHeader(headers);
			doc.Blocks.Add(table);
		}
	}
}
