#if !DEBUG
using System;
using System.IO;
using AnalitF.Net.Client.Config.Initializers;
using log4net.ObjectRenderer;
using log4net.Plugin;
using log4net.Repository;

[assembly: log4net.Config.Plugin(typeof(ProductionStackTraceLog4NetPlugin))]

namespace AnalitF.Net.Client.Config.Initializers
{
	public class ProductionStackTraceLog4NetPlugin : PluginSkeleton, IObjectRenderer
	{
			public ProductionStackTraceLog4NetPlugin()
					: base("ProductionStackTrace") {}

			public override void Attach(ILoggerRepository repository)
			{
					base.Attach(repository);
					repository.RendererMap.Put(typeof(Exception), this);
			}

			public void RenderObject(RendererMap rendererMap, object obj, TextWriter writer)
			{
				try {
					writer.Write(ProductionStackTrace.ExceptionReporting.GetExceptionReport((Exception) obj));
				}
				catch (Exception) {
					writer.Write(obj);
				}
			}
	}
}
#endif