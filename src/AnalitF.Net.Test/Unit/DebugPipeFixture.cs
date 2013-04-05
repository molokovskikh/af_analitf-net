using System;
using System.IO;
using System.IO.Pipes;
using System.Reactive.Linq;
using AnalitF.Net.Client.Models;
using NUnit.Framework;

namespace AnalitF.Net.Test.Unit
{
	[TestFixture]
	public class DebugPipeFixture
	{
		[Test]
		public void Read_debug_pipe()
		{
			var server = new NamedPipeServerStream("test");
			var cliet = new NamedPipeClientStream("test");
			cliet.Connect();
			server.WaitForConnection();

			var observable = DebugPipe.Observe(cliet);
			var w = new StreamWriter(server);
			w.AutoFlush = true;
			w.WriteLine("test");
			var result = observable.First();
			Assert.AreEqual("test", result);
		}
	}
}