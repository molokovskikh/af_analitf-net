using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models.Results;
using Diadoc.Api;
using Diadoc.Api.Cryptography;
using Diadoc.Api.Proto;
using Diadoc.Api.Proto.Events;
using Diadoc.Api.Http;

namespace AnalitF.Net.Client.ViewModels.Diadok
{
	public class RequestResolution : DiadokAction
	{
		private ResolutionRequestType type;

		public RequestResolution(ActionPayload payload, ResolutionRequestType type)
			: base(payload)
		{
			IsEnabled.Value = false;
			this.type = type;
			if (type == ResolutionRequestType.ApprovementRequest)
				Header.Value = "Передача на согласование";
			else
				Header.Value = "Передача на подпись";
		}

		public NotifyValue<string> Header { get; set; }
		public NotifyValue<Department> CurrentDepartment { get; set; }
		public NotifyValue<Department[]> Departments { get; set; }
		public NotifyValue<OrganizationUser> CurrentUser { get; set; }
		public NotifyValue<OrganizationUser[]> Users { get; set; }
		public NotifyValue<string> Comment { get; set;  }

		protected override void OnInitialize()
		{
			base.OnInitialize();
			LoadData();
		}

		public async void LoadData()
		{
			var orgs = await Async((x) => Payload.Api.GetMyOrganizations(x).Organizations);
			var departments = orgs.SelectMany(x => x.Departments).OrderBy(x => x.Name).ToList();
			departments.Insert(0, new Department {
				Name = "Головное подразделение"
			});
			Departments.Value = departments.ToArray();
			CurrentDepartment.Value = departments.FirstOrDefault();
			var me = await Async((x) => Payload.Api.GetMyUser(x));
			var users = await Async((s) => orgs.SelectMany(x => Payload.Api.GetOrganizationUsers(s, x.OrgId).Users)
				.OrderBy(x => x.Name).Where(x => x.Id != me.Id).ToList());
			users.Insert(0, new OrganizationUser {
				Name =  type == ResolutionRequestType.ApprovementRequest
					? "Любой с правом согласования"
					: "Любой с правом подписи"
			});
			Users.Value = users.ToArray();
			CurrentUser.Value = users.FirstOrDefault();
			IsEnabled.Value = true;
		}

		public async void Save()
		{
			try {
				BeginAction();
				var patch = Payload.Patch();
				var attachment = new ResolutionRequestAttachment {
					Comment = Comment.Value,
					Type = type,
					InitialDocumentId = Payload.Entity.EntityId,
				};
				if (CurrentUser.Value.Id != null)
					attachment.TargetUserId = CurrentUser.Value.Id;
				else
					attachment.TargetDepartmentId = CurrentDepartment.Value?.DepartmentId
						?? "00000000-0000-0000-0000-000000000000";
				patch.AddResolutionRequestAttachment(attachment);
				await Async(x => Payload.Api.PostMessagePatch(x, patch));
				EndAction();
			}
			catch(Exception exception) {
				if(exception is HttpClientException) {
					var e = exception as HttpClientException;
					Log.Warn($"Ошибка:", e);
					Manager.Error(e.AdditionalMessage);
				}
				EndAction(false);
			}
		}
	}
}