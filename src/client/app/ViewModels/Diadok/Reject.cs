using System;
using System.Text;
using System.Threading.Tasks;
using AnalitF.Net.Client.Helpers;
using Diadoc.Api.Proto.Events;
using System.Linq;
using Diadoc.Api.Proto.Invoicing;
using Diadoc.Api.Http;

namespace AnalitF.Net.Client.ViewModels.Diadok
{
	public class Reject : DiadokAction
	{
		public Reject(ActionPayload payload)
			: base(payload)
		{
		}

		public NotifyValue<string> Comment { get; set; }

		public async void Save()
		{
			try {
				BeginAction();
				var patch = Payload.Patch();

				if(ReqRevocationSign) {
					Entity revocreq = Payload.Message.Entities.FirstOrDefault(x =>
					x.AttachmentTypeValue == Diadoc.Api.Com.AttachmentType.RevocationRequest);
					SignatureRejectionInfo signrejinfo = new SignatureRejectionInfo();
					signrejinfo.Signer = GetSigner();
					signrejinfo.ErrorMessage = Comment.Value;
					GeneratedFile revocRejectSign = await Async((x) => Payload.Api.GenerateSignatureRejectionXml(
						x,
						Payload.BoxId,
						Payload.Message.MessageId,
						revocreq.EntityId,
						signrejinfo));
					XmlSignatureRejectionAttachment signrejattch = new XmlSignatureRejectionAttachment();
					signrejattch.ParentEntityId = revocreq.EntityId;
					SignedContent signcontent = new SignedContent();
					signcontent.Content = revocRejectSign.Content;
					if (!TrySign(signcontent))
						throw new Exception("Ошибка подписи документа TrySign");
					signrejattch.SignedContent = signcontent;
					patch.AddXmlSignatureRejectionAttachment(signrejattch);
				}
				else {
					var content = new SignedContent();
					//комментарий должен быть всегда
					if (!String.IsNullOrEmpty(Comment.Value))
						content.Content = Encoding.UTF8.GetBytes(Comment.Value);
					else
						content.Content = Encoding.UTF8.GetBytes(" ");

					if (!TrySign(content))
						throw new Exception("Ошибка подписи документа TrySign");
					patch.RequestedSignatureRejections.Add(new RequestedSignatureRejection {
						ParentEntityId = Payload.Entity.EntityId,
						SignedContent = content
					});
				}
				await Async(x => Payload.Api.PostMessagePatch(x, patch));
				EndAction();
			}
			catch(Exception e) {
				var error = ErrorHelper.TranslateException(e)
						?? "Не удалось выполнить операцию, попробуйте повторить позднее.";
				Manager.Warning(error);
				Log.Error(error, e);
				EndAction(false);
			}
		}
	}
}