#if DEBUG
using System;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reactive.Linq;
using System.Text;

namespace AnalitF.Net.Client.Models
{
	public class DebugPipe
	{
		public DebugPipe(string[] args)
		{
			var token = "--debug-pipe=";
			var name = args.FirstOrDefault(a => a.StartsWith(token)) ?? "";
			name = name.Replace(token, "");

			if (string.IsNullOrEmpty(name))
				return;

			var pipe = new NamedPipeClientStream(name);
			pipe.Connect();
			Observe(pipe)
				.Subscribe(c => Dispatcher(c));
		}

		private void Dispatcher(string command)
		{
			if (command == "Collect") {
				GC.Collect();
				GC.WaitForPendingFinalizers();
				GC.Collect();
			}
		}

		public static IObservable<string> Observe(NamedPipeClientStream pipe)
		{
			var observable = Observable.FromAsyncPattern<byte[], int, int, int>(pipe.BeginRead, pipe.EndRead);
			var buffer = new byte[4 * 1024];
			var result = new StringBuilder();
			return observable(buffer, 0, buffer.Length)
				.Select(i => ReadBuffer(buffer, i, result))
				.Where(r => r != null);
		}

		public static string ReadBuffer(byte[] buffer, int readed, StringBuilder builder)
		{
			for(var i = 0; i < readed; i++) {
				if (buffer[i] == '\n' || buffer[i] == '\r') {
					var result = builder.ToString();
					builder.Clear();
					return result;
				}
				builder.Append((char)buffer[i]);
			}
			return null;
		}
	}
}
#endif