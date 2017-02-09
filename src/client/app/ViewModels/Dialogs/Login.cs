using AnalitF.Net.Client.Config;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Commands;
using AnalitF.Net.Client.Models.Results;
using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Handlers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AnalitF.Net.Client.ViewModels.Dialogs
{
	public class Login : BaseScreen, ICancelable
	{
		public Login()
		{
			InitFields();
			WasCancelled = true;
		}

		public string UserName { get; set; }
		public string Password { get; set; }

		public bool WasCancelled { get; private set; }

		public void OK()
		{
			if (!ValidateLoginInfo())
				return;

			var checkResult = CheckCredentials();
			switch (checkResult)
			{
				case CheckResult.UnAuthorized:
					Manager.Error("Неверные логин и/или пароль");
					break;
				case CheckResult.Correct:
					Save();
					WasCancelled = false;
					TryClose();
					break;
				case CheckResult.NoHosts:
					Manager.Error("Не обнаружены серверы для аутентификации");
					break;
				case CheckResult.RequestError:
					Manager.Error("Ошибка при запросе к серверу");
					break;
			}
		}

		private void Save()
		{
			Settings.Value.UserName = UserName;
			Settings.Value.Password = Password;
			Session.CreateSQLQuery($"update settings set UserName = '{UserName}', Password = '{Password}' where Id = {Settings.Value.Id}")
				.ExecuteUpdate();
		}

		private bool ValidateLoginInfo()
		{
			if (string.IsNullOrWhiteSpace(UserName)) {
				Manager.Error("Не указано имя пользователя");
				return false;;
			}
			if (string.IsNullOrWhiteSpace(Password)) {
				Manager.Error("Пароль не может быть пустым");
				return false;
			}
			return true;
		}

		private CheckResult CheckCredentials()
		{
			var result = CheckResult.NoHosts;
			string[] urls = null;
			if (!string.IsNullOrWhiteSpace(Shell.Config.AltUri))
				urls = Shell.Config.AltUri.Split(',').ToArray();
			else if (Shell.Config.BaseUrl != null)
				urls = new string[1] {Shell.Config.BaseUrl.ToString()};

			if (urls != null)
				try {
					HttpClientHandler handler = new HttpClientHandler();
					handler.Credentials = new NetworkCredential(UserName, Password);
					using (var client = new HttpClient(handler)) {
						ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, errors) => true;
						client.Timeout = TimeSpan.FromMilliseconds(10 * 1000);
						result = CheckResult.RequestError;
						foreach (var url in urls) {
							var res = Check(client, url);
							if (res is HttpStatusCode)
							{
								if ((HttpStatusCode) res == HttpStatusCode.OK) {
									result = CheckResult.Correct;
									break;
								}
								if ((HttpStatusCode) res == HttpStatusCode.Unauthorized) {
									result = CheckResult.UnAuthorized;
									break;
								}
							}
						}
					}
				}
				catch {
					result = CheckResult.RequestError;
				}

			return result;
		}

		private object Check(HttpClient client, string url)
		{
			try {
				var response = client.GetAsync(url + "Status").Result;
				return response.StatusCode;
			}
			catch (Exception e) {
				return e;
			}
		}

		enum CheckResult
		{
			Correct,
			NoHosts,
			RequestError,
			UnAuthorized
		}
	}
}
