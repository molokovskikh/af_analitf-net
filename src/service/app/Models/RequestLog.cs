using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Web;
using AnalitF.Net.Service.Helpers;
using Common.Models;
using Newtonsoft.Json;

namespace AnalitF.Net.Service.Models
{
	public class RequestLog
	{
		public RequestLog()
		{
		}

		public RequestLog(User user, Version version)
		{
			CreatedOn = DateTime.Now;
			User = user;
			Version = version;
			LocalHost = Environment.MachineName;
		}

		public RequestLog(User user, HttpRequestMessage request, string updateType, DateTime? lastSync = null)
		{
			User = user;
			CreatedOn = DateTime.Now;
			Version = RequestHelper.GetVersion(request);
			LocalHost = Environment.MachineName;
			UpdateType = updateType;
			LastSync = lastSync;
			if (request.Properties.ContainsKey("MS_HttpContext"))
				RemoteHost = ((HttpContextWrapper)request.Properties["MS_HttpContext"]).Request.UserHostAddress;
		}

		public virtual uint Id { get; set; }

		public virtual User User { get; set; }

		public virtual DateTime CreatedOn { get; set; }

		public virtual DateTime? CompletedOn { get; set; }

		public virtual int ExecuteInSeconds { get; set; }

		public virtual bool IsCompleted { get; set; }

		public virtual bool IsFaulted { get; set; }

		public virtual bool IsConfirmed { get; set; }

		public virtual string Error { get; set; }

		public virtual Version Version { get; set; }

		public virtual string UpdateType { get; set; }

		public virtual string RemoteHost { get; set; }

		public virtual string LocalHost { get; set; }

		//описание ошибки которое возвращается пользователю
		public virtual string ErrorDescription { get; set; }

		public virtual DateTime? LastSync { get; set; }

		public virtual void Faulted(Exception e)
		{
			IsFaulted = true;
			Error = e.ToString();
			CompletedOn = DateTime.Now;
		}

		public virtual void Completed()
		{
			IsCompleted = true;
			ExecuteInSeconds = (int)(DateTime.Now - CreatedOn).TotalSeconds;
			CompletedOn = DateTime.Now;
		}

		public virtual bool IsStale
		{
			get { return DateTime.Now > CreatedOn.AddMinutes(30); }
		}

		public virtual string OutputFile(Config.Config config)
		{
			return Path.Combine(config.ResultPath, Id.ToString());
		}

		public virtual Stream GetResult(Config.Config config)
		{
			if (IsFaulted)
				throw new Exception("Обработка запроса завершилась ошибкой");
			return File.OpenRead(OutputFile(config));
		}

		public virtual HttpResponseMessage ToResult(Config.Config config)
		{
			if (!IsCompleted)
				return new HttpResponseMessage(HttpStatusCode.Accepted);
			if (IsFaulted) {
				var message = new HttpResponseMessage(HttpStatusCode.InternalServerError);
				if (!String.IsNullOrEmpty(ErrorDescription)) {
					message.Content = new StringContent(ErrorDescription);
				}
#if DEBUG
				else {
					throw new Exception(Error);
				}
#endif
				return message;
			}

			//файл результата выкладывается на dfs репликация может занять до нескольких минут
			if (config.ResultTimeout > TimeSpan.Zero) {
				if (!File.Exists(OutputFile(config)) && DateTime.Now < (CompletedOn + config.ResultTimeout))
					return new HttpResponseMessage(HttpStatusCode.Accepted);
			}

			return new HttpResponseMessage(HttpStatusCode.OK) {
				Content = new StreamContent(GetResult(config))
			};
		}
	}
}