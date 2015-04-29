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
using log4net;
using NHibernate;

namespace AnalitF.Net.Service.Models
{
	public class RequestLog
	{
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
			if (String.IsNullOrEmpty(RemoteHost)) {
				if (request.Properties.ContainsKey("MS_HttpContext"))
					RemoteHost = ((HttpContextWrapper)request.Properties["MS_HttpContext"]).Request.UserHostAddress;
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

		public virtual DateTime? LastSync { get; set; }

		public virtual long? Size { get; set; }

		public virtual string ClientToken { get; set; }

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
			if (!IsCompleted) {
				return new HttpResponseMessage(HttpStatusCode.Accepted) {
					Content = new ObjectContent<object>(new { RequestId = Id }, new JsonMediaTypeFormatter())
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
						Content = new ObjectContent<object>(new { RequestId = Id }, new JsonMediaTypeFormatter())
					};
			}

			var streamContent = new StreamContent(GetResult(config));
			if (UpdateType.Match("OrdersController"))
				streamContent.Headers.Add("Content-Type", "application/json");
			return new HttpResponseMessage(HttpStatusCode.OK) {
				Content = streamContent
			};
		}

		public virtual void Confirm(Config.Config config)
		{
			IsConfirmed = true;
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
			Config.Config config,
			ISessionFactory sessionFactory,
			Action<ISession, Config.Config, RequestLog> cmd)
		{
			var principal = Thread.CurrentPrincipal;
			session.Save(this);
			session.Transaction.Commit();
			var jobId = Id;

			var task = new Task(() => {
				try {
					Thread.CurrentPrincipal = principal;
					using (var logSession = sessionFactory.OpenSession())
					using (var logTransaction = logSession.BeginTransaction()) {
						var job = logSession.Load<RequestLog>(jobId);
						try {
							using(var cmdSession = sessionFactory.OpenSession())
							using(var cmdTransaction = cmdSession.BeginTransaction()) {
								cmd(cmdSession, config, job);
								cmdTransaction.Commit();
							}
						}
						catch(ExporterException e) {
							log.Warn("Ошибка при обработке автозаказа", e);
							job.ErrorDescription = e.Message;
							job.Faulted(e);
						}
						catch(Exception e) {
							log.Error(String.Format("Произошла ошибка при обработке запроса {0}", jobId), e);
							job.Faulted(e);
						}
						finally {
							job.Completed();
							logSession.Save(job);
							logSession.Flush();
							logTransaction.Commit();
						}
					}
				}
				catch(Exception e) {
					log.Error(String.Format("Произошла ошибка при обработке запроса {0}", jobId), e);
				}
			});
			task.Start();
			return task;
		}
	}
}