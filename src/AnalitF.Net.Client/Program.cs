using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using NDesk.Options;

namespace AnalitF.Net.Client
{
	public class Program
	{
		public const uint ATTACH_PARENT_PROCESS = 0x0ffffffff;

		[DllImport("kernel32", SetLastError = true)]
		public static extern bool AttachConsole(uint dwProcessId);

		[STAThread]
		public static void Main(string[] args)
		{
			var app = new App();
			var help = false;
			var version  = false;
			var options = new OptionSet {
				{"help", "Показать справку", v => help = v != null},
				{"version", "Показать информацию о версии", v => version = v != null},
				{"quiet", "Не выводить предупреждения при запуске", v => app.Quiet = v != null},
			};
			options.Parse(args);

			//по умолчанию у gui приложения нет консоли, но если кто то запустил из консоли
			//то нужно с ним говорить
			AttachConsole(ATTACH_PARENT_PROCESS);

			if (help) {
				options.WriteOptionDescriptions(Console.Out);
				return;
			}

			if (version) {
				var assembly = typeof(Program).Assembly;
				Console.WriteLine(assembly.GetName().Version.ToString());
				var hash = assembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false)
					.OfType<AssemblyCopyrightAttribute>()
					.Select(a => a.Copyright)
					.FirstOrDefault();
				Console.WriteLine(hash);
				return;
			}

			app.InitializeComponent();
			app.Run();
		}
	}
}