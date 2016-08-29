using System;
using System.Threading.Tasks;
using AnalitF.Net.Client.Helpers;
using Diadoc.Api;
using Diadoc.Api.Http;

namespace AnalitF.Net.Client.ViewModels.Diadok
{
	public class Delete : DiadokAction
	{
		public Delete(ActionPayload payload)
			: base(payload)
		{
		}

		public async void Save()
		{
			try {
				BeginAction();
				await	Async(x => Payload.Api.Delete(x, Payload.BoxId, Payload.Entity.DocumentInfo.MessageId, Payload.Entity.EntityId));
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