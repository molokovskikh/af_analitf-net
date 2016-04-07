using System;
using System.ComponentModel;
using System.Net;
using System.Threading.Tasks;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using Diadoc.Api;
using Diadoc.Api.Cryptography;
using Diadoc.Api.Http;
using Diadoc.Api.Proto.Events;

namespace AnalitF.Net.Client.ViewModels.Diadok
{
	public class ActionPayload
	{
		public DiadocApi Api;
		public string BoxId;
		public string Token;

		public MessagePatchToPost Patch()
		{
			return new MessagePatchToPost {
				BoxId = BoxId,
				MessageId = Entity.DocumentInfo.MessageId
			};
		}

		public Entity Entity { get; set; }
	}

	public abstract class DiadokAction : BaseScreen
	{
		public bool Success;

		public DiadokAction(ActionPayload payload)
		{
			InitFields();
			Payload = payload;
			IsEnabled.Value = true;
		}

		public NotifyValue<bool> IsEnabled { get; set; }
		public ActionPayload Payload { get; set; }

		public async Task Async(Action<string> action)
		{
			try {
				IsEnabled.Value = false;
				await Util.Run(() => action(Payload.Token));
				Success = true;
				TryClose();
			} catch(Exception e) {
				Log.Error($"Не удалось обновить документ {Payload.Entity.EntityId}", e);
				Manager.Error(ErrorHelper.TranslateException(e)
					?? "Не удалось выполнить операцию, попробуйте повторить позднее.");
			} finally {
				IsEnabled.Value = true;
			}
		}

		protected bool TrySign(SignedContent content)
		{
			if (Settings.Value.DebugUseTestSign) {
				content.SignWithTestSignature = true;
				return true;
			}
			byte[] data;
			var result = TrySign(out data);
			content.Signature = data;
			return result;
		}

		protected bool TrySign(out byte[] data)
		{
			data = null;
			if (Settings.Value.DebugUseTestSign)
				return true;

			try {
				var cert = Settings.Value.GetCert(Settings.Value.DiadokCert);
				data = new WinApiCrypt().Sign(Payload.Entity.Content.Data, cert.RawData);
			}
			catch (Win32Exception e) {
				Log.Error($"Ошибка при подписании документа {Payload.Entity.EntityId}", e);
				Manager.Error(e.Message);
				return false;
			}
			catch (Exception e) {
				Log.Error($"Ошибка при подписании документа {Payload.Entity.EntityId}", e);
				Manager.Error(ErrorHelper.TranslateException(e) ?? "Не удалось подписать документ");
				return false;
			}
			return true;
		}
	}

	public class Sign : DiadokAction
	{
		public Sign(ActionPayload payload)
			: base(payload)
		{
		}

		public async Task Save()
		{
			byte[] data;
			if (!TrySign(out data))
				return;

			var patch = Payload.Patch();
			var sign = new DocumentSignature {
				ParentEntityId = Payload.Entity.EntityId,
				Signature = data
			};
			if (Settings.Value.DebugUseTestSign)
				sign.SignWithTestSignature = true;
			patch.AddSignature(sign);

			try {
				await Async(x => Payload.Api.PostMessagePatch(x, patch));
				Log.Info($"Документ {patch.MessageId} успешно подписан");
			} catch (HttpClientException e) {
				if (e.ResponseStatusCode == HttpStatusCode.Conflict) {
					Log.Warn($"Документ {patch.MessageId} был подписан ранее", e);
					Manager.Warning("Документ уже был подписан другим пользователем.");
				} else {
					throw;
				}
			}
		}
	}
}