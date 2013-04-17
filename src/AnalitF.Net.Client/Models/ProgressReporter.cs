using System.Reactive.Subjects;
using AnalitF.Net.Client.ViewModels;

namespace AnalitF.Net.Client.Models
{
	public class ProgressReporter
	{
		private BehaviorSubject<Progress> behavior;
		private string stage;
		private int weight;
		private int total;
		private decimal current;
		private int stageImpact;
		private int defaultImpact = 100;
		private bool isStageCompleted = true;

		public ProgressReporter()
		{
			behavior = new BehaviorSubject<Progress>(new Progress());
		}

		public ProgressReporter(BehaviorSubject<Progress> behavior, int total = 0)
		{
			this.behavior = behavior;
		}

		public void Stage(string stage, int impact = 0)
		{
			if (this.stage == stage)
				return;

			if (!isStageCompleted)
				EndStage();

			this.stage = stage;
			isStageCompleted = false;
			stageImpact = impact == 0 ? defaultImpact : impact;
			weight = 100;
			current = 0;

			behavior.OnNext(new Progress(stage, 0, total));
		}

		public void Progress(int value)
		{
			if (weight <= 0)
				return;

			current += value * 100m / weight;
			behavior.OnNext(new Progress(stage, (int)current, total));
			if (current == 100)
				EndStage();
		}

		public void EndStage()
		{
			if (isStageCompleted)
				return;

			total += stageImpact;
			isStageCompleted = true;
			behavior.OnNext(new Progress(stage, 100, total));
		}

		public void Weight(int weight)
		{
			this.weight = weight;
		}

		public void StageCount(int i)
		{
			defaultImpact = 100 / i;
		}
	}

	public class Progress
	{
		public string Stage { get; set; }
		public int Current { get; set; }
		public int Total { get; set; }

		public Progress()
		{
		}

		public Progress(string stage, int current, int total)
		{
			Stage = stage;
			Current = current;
			Total = total;
		}

		public override string ToString()
		{
			return string.Format("Stage: {0}, Current: {1}, Weight: {2}", Stage, Current, Total);
		}
	}
}