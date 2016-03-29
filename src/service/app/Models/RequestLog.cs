using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Common.Models;
using Common.Tools;
using Common.Tools.Calendar;
using log4net;
using Newtonsoft.Json;
using NHibernate;

namespace AnalitF.Net.Service.Models
{
	public enum ErrorType
	{
		None,
		AccessDenied
	}

	public class RequestLog
	{
		//используется для тестирования что бы избежать асинхронного поведения
		public static TaskScheduler Scheduler;
		private static ILog log = LogManager.GetLogger(typeof(RequestLog));

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
			Version = GetVersion(request);
			LocalHost = Environment.MachineName;
			UpdateType = updateType;
			LastSync = lastSync;
			IEnumerable<string> values;
			if (request.Headers.TryGetValues("X-Forwarded-For", out values)) {
				RemoteHost = values.Implode();
			}
			if (request.Headers.TryGetValues("Client-Token", out values)) {
				ClientToken = values.Implode();
			}
			if (request.Headers.TryGetValues("OS-Version", out values)) {
				OSVersion = values.Implode();
			}
			if (String.IsNullOrEmpty(RemoteHost)) {
				if (request.Properties.ContainsKey("MS_HttpContext"))
					RemoteHost = ((HttpContextWrapper)request.Properties["MS_HttpContext"]).Request.UserHostAddress;
			}
			if (request.Headers.TryGetValues("Request-Token", out values)) {
				RequestToken = values.Implode();
			}
			if (request.Headers.TryGetValues("Branch", out values)) {
				Branch = values.Implode();
			}
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

		public virtual ErrorType ErrorType { get; set; }

		public virtual DateTime? LastSync { get; set; }

		public virtual long? Size { get; set; }

		public virtual string ClientToken { get; set; }

		public virtual string OSVersion { get; set; }

		public virtual string RequestToken { get; set; }

		//тип релиза обычный или перенос с analitf
		public virtual string Branch { get; set; }

		//дополнительные файлы которые следует передать с обновлением
		public virtual string MultipartContent { get; set; }

		public virtual void Faulted(Exception e)
		{
			ErrorDescription = null;
			if (e is ExporterException) {
				var export = (ExporterException)e;
				ErrorDescription = export.Message;
				ErrorType = export.ErrorType;
			}
			IsFaulted = true;
			Error = e.ToString();
			Completed();
		}

		public virtual void Completed()
		{
			IsCompleted = true;
			ExecuteInSeconds = (int)(DateTime.Now - CreatedOn).TotalSeconds;
			CompletedOn = DateTime.Now;
		}

		public virtual bool GetIsStale(TimeSpan lifetime) => DateTime.Now > CreatedOn.Add(lifetime);

		public virtual string OutputFile(Config.Config config)
		{
			return Path.Combine(config.ResultPath, Id.ToString());
		}

		public virtual HttpContent GetResult(Config.Config config)
		{
			if (IsFaulted)
				throw new Exception("Обработка запроса завершилась ошибкой");
			HttpContent content = null;
			if (!String.IsNullOrEmpty(MultipartContent)) {
				var multipart = new MultipartContent();
				multipart.Add(new StreamContent(File.OpenRead(OutputFile(config))));
				foreach (var file in JsonConvert.DeserializeObject<string[]>(MultipartContent))
					multipart.Add(new StreamContent(File.OpenRead(FileHelper.MakeRooted(file))));
				content = multipart;
			}
			content = content ?? new StreamContent(File.OpenRead(OutputFile(config)));
			if (UpdateType.Match("OrdersController"))
				content.Headers.Add("Content-Type", "application/json");
			return content;
		}

		public virtual HttpResponseMessage ToResult(Config.Config config)
		{
			if (!IsCompleted) {
				return new HttpResponseMessage(HttpStatusCode.Accepted) {
					Headers = { { "Request-Id", Id.ToString() } },
				};
			}
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
					return new HttpResponseMessage(HttpStatusCode.Accepted) {
						Headers = { { "Request-Id", Id.ToString() } },
					};
			}

			return new HttpResponseMessage(HttpStatusCode.OK) {
				Headers = { { "Request-Id", Id.ToString() } },
				Content = GetResult(config)
			};
		}

		public virtual void Confirm(Config.Config config, string message = null)
		{
			IsConfirmed = true;
			Error += message;
			if (!config.DebugExport)
				File.Delete(OutputFile(config));
		}

		public static Version GetVersion(HttpRequestMessage request)
		{
			var headers = request.Headers;
			var version = new Version();
			IEnumerable<string> header;
			if (headers.TryGetValues("Version", out header)) {
				Version.TryParse(header.FirstOrDefault(), out version);
			}
			return version;
		}

		public virtual Task StartJob(ISession session,
			Action<ISession, RequestLog> cmd)
		{
			var username = Thread.CurrentPrincipal.Identity.Name;
			session.Save(this);
			if (session.Transaction.IsActive)
				session.Transaction.Commit();
			var jobId = Id;

			var task = new Task(() => {
				try {
					ThreadContext.Properties["username"] = username;
					using (var logSession = session.SessionFactory.OpenSession()) {
						var job = logSession.Load<RequestLog>(jobId);
						try {
							Common.MySql.With.DeadlockWraper(() => {
								using(var cmdSession = session.SessionFactory.OpenSession())
								using(var cmdTransaction = cmdSession.BeginTransaction()) {
									cmd(cmdSession, job);
									if (cmdTransaction.IsActive)
										cmdTransaction.Commit();
								}
							}, sleepTime: 10.Second(), countRepeats: 5, maxDuration: TimeSpan.FromMinutes(10));
						}
						catch(Exception e) {
							//если это не ошибка кодирования нет смысла писать в рассылку
							if (e is ExporterException)
								log.Warn($"Произошла ошибка при обработке запроса {jobId}", e);
							else
								log.Error($"Произошла ошибка при обработке запроса {jobId}", e);
							job.Faulted(e);
						}
						finally {
							using (var logTrx = logSession.BeginTransaction()) {
								job.Completed();
								logSession.Save(job);
								logSession.Flush();
								logTrx.Commit();
							}
						}
					}
				}
				catch(Exception e) {
					log.Error($"Произошла ошибка при обработке запроса {jobId}", e);
				}
			});
			task.Start(Scheduler ?? TaskScheduler.Current);
			return task;
		}

		public static Task RunTask(ISession session, Action<ISession> action)
		{
			if (session.Transaction.IsActive)
				session.Transaction.Commit();
			var username = Thread.CurrentPrincipal.Identity.Name;
			var task = new Task(() => {
				ThreadContext.Properties["username"] = username;
				Common.MySql.With.DeadlockWraper(() => {
					using(var cmdSession = session.SessionFactory.OpenSession())
					using(var cmdTransaction = cmdSession.BeginTransaction()) {
						action(cmdSession);
						if (cmdTransaction.IsActive)
							cmdTransaction.Commit();
					}
				}, sleepTime: 10.Second(), countRepeats: 5, maxDuration: TimeSpan.FromMinutes(10));
			});
			task.ContinueWith(x => {
				if (x.IsFaulted)
					log.Error("Ошибка при выполнении фоновой задачи", x.Exception);
			});
			task.Start(Scheduler ?? TaskScheduler.Current);
			return task;
		}
	}
}