using System;
using System.Text;
using System.Threading.Tasks;
using AnalitF.Net.Client.Helpers;
using Diadoc.Api.Proto.Events;

namespace AnalitF.Net.Client.ViewModels.Diadok
{
	public class Reject : DiadokAction
	{
		public Reject(ActionPayload payload)
			: base(payload)
		{
		}

		public NotifyValue<string> Comment { get; set; }

		public async Task Save()
		{
			var patch = Payload.Patch();
			var content = new SignedContent();
			//комментарий должен быть всегда
			if (!String.IsNullOrEmpty(Comment.Value))
				content.Content = Encoding.UTF8.GetBytes(Comment.Value);
			else
				content.Content = Encoding.UTF8.GetBytes(" ");

			if (!TrySign(content))
				return;
			patch.RequestedSignatureRejections.Add(new RequestedSignatureRejection {
				ParentEntityId = Payload.Entity.EntityId,
				SignedContent = content
			});
			await Async(x => Payload.Api.PostMessagePatch(x, patch));
		}
	}
}