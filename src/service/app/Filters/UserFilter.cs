using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using Common.Models;
using Common.Web.Service.Filters;
using log4net;
using NHibernate;
using NHibernate.Criterion;

namespace AnalitF.Net.Service.Filters
{
	public class UserFilter : ActionFilterAttribute
	{
		private ILog log = LogManager.GetLogger(typeof(Common.Web.Service.Filters.UserFilter));

		public UserFilter()
		{
			Permission = ConfigurationManager.AppSettings["UserFilter.PermissionShortcut"];
		}

		public string Permission { get; set; }

		public override void OnActionExecuting(HttpActionContext actionContext)
		{
			var controller = actionContext.ControllerContext.Controller;
			var property = controller.GetType().GetProperty("CurrentUser");
			if (property == null)
				return;

			var type = property.PropertyType;
			var session = (ISession)((dynamic)controller).Session;
			var login = Thread.CurrentPrincipal.Identity.Name;
#if DEBUG
			if (!String.IsNullOrEmpty(ConfigurationManager.AppSettings["DebugUser"])) {
				login = ConfigurationManager.AppSettings["DebugUser"];
			}
			if (String.IsNullOrEmpty(login)) {
				IEnumerable<string> values;
				if (actionContext.Request.Headers.TryGetValues("Debug-UserName", out values))
					login = values.First();
			}
#endif
			if (login.Contains("\\"))
				login = new Regex(@"\\(?<login>.+)").Match(login).Groups["login"].Value;
			var users = session.CreateCriteria(type)
				.Add(Expression.Eq("Login", login))
				.List();
			if (users.Count == 0)
				throw new HttpResponseException(HttpStatusCode.Forbidden);

			property.SetValue(controller, users[0], null);
			var user = users[0] as User;
			if (user == null)
				return;

			CheckUser(user);
		}

		public void CheckUser(User user)
		{
			if (!user.Enabled) {
				var error = $"Пользователь {user.Id} отключен";
				log.Warn(error);
				throw new AccessException(error, user, new HttpResponseMessage(HttpStatusCode.Forbidden) {
					Content = new StringContent("Пожалуйста, обратитесь в бухгалтерию АналитФармация." +
						"\r\nВ связи с неоплатой услуг доступ закрыт.")
				});
			}

			if (user.Client != null && !user.Client.Enabled) {
				var error = $"Клиент {user.Client.Id} отключен";
				log.Warn(error);
				throw new AccessException(error, user, new HttpResponseMessage(HttpStatusCode.Forbidden) {
					Content = new StringContent("Пожалуйста, обратитесь в бухгалтерию АналитФармация." +
						"\r\nВ связи с неоплатой услуг доступ закрыт.")
				});
			}

			if (String.IsNullOrEmpty(Permission))
				return;

			if (user.Permissions.All(p => p.Shortcut != Permission)) {
				var error = $"У пользователя {user.Id} нет права доступа {Permission}";
				log.Warn(error);
				throw new AccessException(error, user, HttpStatusCode.Forbidden);
			}
		}
	}
}