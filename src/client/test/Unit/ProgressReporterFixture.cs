using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using AnalitF.Net.Client.Models;
using Common.Tools;
using NUnit.Framework;

namespace AnalitF.Net.Client.Test.Unit
{
	[TestFixture]
	public class ProgressReporterFixture
	{
		private ProgressReporter reporter;
		private List<Progress> reports;

		[SetUp]
		public void Setup()
		{
			var subject = new BehaviorSubject<Progress>(new Progress());
			reporter = new ProgressReporter(subject);
			reports = new List<Progress>();
			subject.Subscribe(reports.Add);
		}

		[Test]
		public void Progress()
		{
			reporter.Stage("test");
			reporter.Weight(100);
			reporter.Progress(30);
			reporter.Progress(30);
			reporter.Progress(40);
			reporter.EndStage();
			Assert.That(reports.Count, Is.EqualTo(6), reports.Implode());
			Assert.That(reports[3].Current, Is.EqualTo(60));
			Assert.That(reports[3].Total, Is.EqualTo(0));
			Assert.That(reports[3].Stage, Is.EqualTo("test"));
		}

		[Test]
		public void Ignore_same_stage_begin()
		{
			reporter.Stage("test");
			reporter.Stage("test");
			Assert.That(reports.Count, Is.EqualTo(2));
		}

		[Test]
		public void Auto_report_end()
		{
			reporter.Stage("test");
			reporter.Weight(100);
			reporter.Progress(100);
			var last = reports.Last();
			Assert.That(last.Total, Is.EqualTo(100));
			Assert.That(last.Current, Is.EqualTo(100));
		}

		[Test]
		public void Auto_calculate_impact()
		{
			reporter.StageCount(2);
			reporter.Stage("test");
			reporter.Stage("test1");
			Assert.That(reports[2].Total, Is.EqualTo(50));
		}

		[Test]
		public void Skip_progress_report_if_weight_unknown()
		{
			reporter.Stage("test");
			reporter.Weight(0);
			reporter.Progress(1000);
			Assert.That(reports.Count, Is.EqualTo(2));
		}
	}
}