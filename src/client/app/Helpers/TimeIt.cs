﻿using System;
using System.Diagnostics;

namespace AnalitF.Net.Client.Helpers
{
	public class TimeIt : IDisposable
	{
		private string text;
		private Stopwatch watch = new Stopwatch();

		public TimeIt(string text, params object[] args)
		{
			this.text = String.Format(text, args);
			watch.Start();
		}

		public void Dispose()
		{
			watch.Stop();
			Console.WriteLine("{0} in {1}ms", text, watch.ElapsedMilliseconds);
		}
	}
}