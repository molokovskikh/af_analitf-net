using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using Diadoc.Api;
using Diadoc.Api.Cryptography;
using Diadoc.Api.Http;
using Diadoc.Api.Proto.Documents;
using Diadoc.Api.Proto.Events;
using Diadoc.Api.Proto.Invoicing;

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

		public MessagePatchToPost PatchTorg12(ReceiptAttachment receipattachment)
		{
			return new MessagePatchToPost {
				BoxId = BoxId,
				MessageId = Entity.DocumentInfo.MessageId,
				XmlTorg12BuyerTitles = { receipattachment }
			};
		}

		public MessagePatchToPost PatchInvoice(ReceiptAttachment receipattachment)
		{
			return new MessagePatchToPost {
				BoxId = BoxId,
				MessageId = Entity.DocumentInfo.MessageId,
				Receipts  = { receipattachment }
			};
		}

		public Entity Entity { get; set; }
		public Message Message { get; set; }
		public Document Document { get; set; }
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
				await TaskEx.Run(() => action(Payload.Token));
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
			byte[] sign;
			var result = TrySign(content.Content, out sign);
			if(result)
			{
				content.Signature = sign;
			}
			return result;
		}

		public bool TrySign(byte[] content, out byte[] sign)
		{
			sign = null;
			if (Settings.Value.DebugUseTestSign)
				return true;

			try {
				var cert = Settings.Value.GetCert(Settings.Value.DiadokCert);
				sign = new WinApiCrypt().Sign(content, cert.RawData);
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

			Signer sg = new Signer();



				Official off1 = new Official();// {"Misha", "Specialist", "Gennadevich", "Shunko" },
				off1.FirstName = "Misha";
				off1.JobTitle = "Specialist";
				off1.Patronymic = "Gennadevich";
				off1.Surname = "Shunko";

				Official off2 = new Official();// {"Misha", "Specialist", "Gennadevich", "Shunko" },
				off2.FirstName = "Misha1";
				off2.JobTitle = "Specialist1";
				off2.Patronymic = "Gennadevich1";
				off2.Surname = "Shunko1";

				Official off3 = new Official();// {"Misha", "Specialist", "Gennadevich", "Shunko" },
				off3.FirstName = "Misha2";
				off3.JobTitle = "Specialist2";
				off3.Patronymic = "Gennadevich2";
				off3.Surname = "Shunko2";

				Official off4 = new Official();// {"Misha", "Specialist", "Gennadevich", "Shunko" },
				off4.FirstName = "Misha3";
				off4.JobTitle = "Specialist3";
				off4.Patronymic = "Gennadevich3";
				off4.Surname = "Shunko3";



				Attorney addi = new Attorney();
				addi.Date = System.DateTime.Now.ToShortDateString();
				addi.IssuerAdditionalInfo = "step1";
				addi.IssuerOrganizationName = "step2";
				addi.Number = "3";
				addi.RecipientAdditionalInfo = "step4";
				addi.IssuerPerson = off1;
				addi.RecipientPerson = off2;

				sg.SignerCertificate = new byte[] {1,2,3};
				sg.SignerCertificateThumbprint = "asfkjaksfjaskfjaf";
				sg.SignerDetails = new SignerDetails();
				sg.SignerDetails.FirstName = "Misha";
				sg.SignerDetails.Inn = "9697899845";
				sg.SignerDetails.JobTitle = "Specialist";
				sg.SignerDetails.Patronymic = "Gennadevich";
				sg.SignerDetails.SoleProprietorRegistrationCertificate = "a ksbfajhbsf jahf as";
				sg.SignerDetails.Surname = "Shunko";

			if (Payload.Entity.AttachmentType == AttachmentType.XmlTorg12) {

				var inf =  new Torg12BuyerTitleInfo {
					AcceptedBy = off3,
					AdditionalInfo = "addinfo",
					Attorney = addi,
					ReceivedBy = off4,
					ShipmentReceiptDate = DateTime.Now.ToShortDateString(),
					Signer = sg
					};

				GeneratedFile torg12XmlForBuyer = Payload.Api.GenerateTorg12XmlForBuyer(
					Payload.Token,
					inf,
					Payload.BoxId,
					Payload.Entity.DocumentInfo.MessageId,
					Payload.Entity.DocumentInfo.EntityId);

				MessagePatchToPost patch = null;

				ReceiptAttachment receipt = new ReceiptAttachment {
					ParentEntityId = Payload.Entity.DocumentInfo.EntityId,
					SignedContent = new SignedContent
					{
						Content = torg12XmlForBuyer.Content,
						Signature = new byte[0],
						SignWithTestSignature = true
					}
				};
				patch = Payload.PatchTorg12(receipt);
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
			else
			{
				MessagePatchToPost patch = null;
				try {
					Entity invoice = Payload.Message.Entities.First(i => i.AttachmentTypeValue == Diadoc.Api.Com.AttachmentType.Invoice);
					GeneratedFile invoiceReceipt = Payload.Api.GenerateInvoiceDocumentReceiptXml(
						Payload.Token,
						Payload.BoxId,
						Payload.Entity.DocumentInfo.MessageId,
						invoice.EntityId,
						sg);
					ReceiptAttachment receipt = new ReceiptAttachment {
						ParentEntityId = invoice.EntityId,
						SignedContent = new SignedContent
						{
							Content = invoiceReceipt.Content,
							Signature = new byte[0],
							SignWithTestSignature = true
						}
					};
					patch = Payload.Patch();
					patch.AddReceipt(receipt);
					await Async(x => Payload.Api.PostMessagePatch(x, patch));
					Log.Info($"Документ {patch.MessageId} успешно подписан");
				}
				catch (HttpClientException e) {
					if (e.ResponseStatusCode == HttpStatusCode.Conflict) {
						Log.Warn($"Документ {patch.MessageId} был подписан ранее", e);
						Manager.Warning("Документ уже был подписан другим пользователем.");
					} else {
						throw;
					}
				}

				try {
					Entity invoiceConfirmation = Payload.Message.Entities.OrderBy(x => x.CreationTime).First(i => i.AttachmentTypeValue == Diadoc.Api.Com.AttachmentType.InvoiceConfirmation);
					GeneratedFile invoiceConfirmationReceipt = Payload.Api.GenerateInvoiceDocumentReceiptXml(
						Payload.Token,
						Payload.BoxId,
						Payload.Entity.DocumentInfo.MessageId,
						invoiceConfirmation.EntityId,
						sg);
					ReceiptAttachment receipt = new ReceiptAttachment {
						ParentEntityId = invoiceConfirmation.EntityId,
						SignedContent = new SignedContent
						{
							Content = invoiceConfirmationReceipt.Content,
							Signature = new byte[0],
							SignWithTestSignature = true
						}
					};
					patch = Payload.Patch();
					patch.AddReceipt(receipt);
					await Async(x => Payload.Api.PostMessagePatch(x, patch));
					Log.Info($"Документ {patch.MessageId} успешно подписан");
				}
				catch (HttpClientException e) {
					if (e.ResponseStatusCode == HttpStatusCode.Conflict) {
						Log.Warn($"Документ {patch.MessageId} был подписан ранее", e);
						Manager.Warning("Документ уже был подписан другим пользователем.");
					} else {
						throw;
					}
				}

				try {
					var message = Payload.Api.GetMessage(Payload.Token, Payload.BoxId, Payload.Entity.DocumentInfo.MessageId);
					Entity invoiceDateConfirmation = message.Entities.OrderBy(x => x.CreationTime).Last(i => i.AttachmentTypeValue == Diadoc.Api.Com.AttachmentType.InvoiceConfirmation);
					GeneratedFile invoiceConfirmationReceipt = Payload.Api.GenerateInvoiceDocumentReceiptXml(
						Payload.Token,
						Payload.BoxId,
						Payload.Entity.DocumentInfo.MessageId,
						invoiceDateConfirmation.EntityId,
						sg);
					ReceiptAttachment receipt = new ReceiptAttachment {
						ParentEntityId = invoiceDateConfirmation.EntityId,
						SignedContent = new SignedContent
						{
							Content = invoiceConfirmationReceipt.Content,
							Signature = new byte[0],
							SignWithTestSignature = true
						}
					};
					patch = Payload.Patch();
					patch.AddReceipt(receipt);
					await Async(x => Payload.Api.PostMessagePatch(x, patch));
					Log.Info($"Документ {patch.MessageId} успешно подписан");
				}
				catch (HttpClientException e) {
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
}