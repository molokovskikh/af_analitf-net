using System;
using System.IO;
using Common.Models;

namespace AnalitF.Net.Models
{
	public class RequestLog
	{
		public RequestLog()
		{
		}

		public RequestLog(User user)
		{
			User = user;
			CreatedOn = DateTime.Now;
		}

		public virtual uint Id { get; set; }

		public virtual User User { get; set; }

		public virtual DateTime CreatedOn { get; set; }

		public virtual bool IsCompleted { get; set; }

		public virtual bool IsFaulted { get; set; }

		public virtual string Error { get; set; }

		public virtual bool IsStale
		{
			get { return DateTime.Now > CreatedOn.AddMinutes(30); }
		}

		public virtual string OutputFile
		{
			get { return Id.ToString(); }
		}

		public virtual Stream GetResult(string path)
		{
			if (IsFaulted)
				throw new Exception("Обработка запроса завершилась ошибкой");
			return File.OpenRead(Path.Combine(path, OutputFile));
		}
	}
}