﻿using System.Threading.Tasks;
using AnalitF.Net.Client.Helpers;
using Diadoc.Api;
using Diadoc.Api.Proto.Events;
using Diadoc.Api.Http;
using System;

namespace AnalitF.Net.Client.ViewModels.Diadok
{
	public class Resolution : DiadokAction
	{
		private ResolutionType type;

		public Resolution(ActionPayload payload, ResolutionType type)
			: base(payload)
		{
			this.type = type;
			if (type == ResolutionType.Approve) {
				Header.Value = "Согласование документа";
				Clarification.Value = "Результат согласования и комментарий будут видны только сотрудникам вашей организации.";
				ActionName.Value = "Согласовать";
			} else {
				Header.Value = "Отказ в согласовании документа";
				ActionName.Value = "Отказать";
			}
		}

		public NotifyValue<string> ActionName { get; set; }
		public NotifyValue<string> Header { get; set; }
		public NotifyValue<string> Clarification { get; set; }
		public NotifyValue<string> Comment { get; set; }

		public void Save()
		{
			try
			{
				BeginAction();
				LastPatchStamp = Payload.Message.LastPatchTimestamp;
				var patch = Payload.Patch();
				patch.AddResolution(new ResolutionAttachment {
					InitialDocumentId = Payload.Entity.EntityId,
					Comment = Comment.Value,
					ResolutionType = type
				});
				Payload.Api.PostMessagePatch(Payload.Token, patch);
				EndAction();
			}
			catch(Exception exception)
			{
				EndAction(false);
				if(exception is HttpClientException)
				{
					var e = exception as HttpClientException;
					Log.Warn($"Ошибка:", e);
					Manager.Error(e.AdditionalMessage);
				}
				else
					throw;
			}
			TryClose();
		}
	}
}