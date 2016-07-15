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
using System.Windows.Controls;
using System.Windows.Data;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Media;
using System.Globalization;

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

		public MessagePatchToPost Patch(params ReceiptAttachment[] receipattachments)
		{
			var msg = new MessagePatchToPost {
				BoxId = BoxId,
				MessageId = Entity.DocumentInfo.MessageId
			};
			msg.Receipts.AddRange(receipattachments);
			return msg;
		}

		public Entity Entity { get; set; }
		public Message Message { get; set; }
		public Document Document { get; set; }
	}

	public abstract class DiadokAction : BaseScreen
	{
		public NotifyValue<bool> isDone { get; set;}

		public DiadokAction(ActionPayload payload)
		{
			isDone = new NotifyValue<bool>(false);
			InitFields();
			Payload = payload;
			ShortFileName = $"{Payload.Entity.FileName.Substring(0, 25)}...{Payload.Entity.FileName.Substring(Payload.Entity.FileName.Length - 25)}";
			IsEnabled.Value = true;
			Cert = Settings.Value.GetCert(Settings.Value.DiadokCert);
		}

		public X509Certificate2 Cert { get; protected set;}
		public NotifyValue<bool> IsEnabled { get; set; }
		public ActionPayload Payload { get; set; }
		public string ShortFileName { get; set; }

		protected void BeginAction()
		{
			IsEnabled.Value = false;
		}

		protected void EndAction()
		{
			IsEnabled.Value = true;
			isDone.Value = true;
			TryClose();
		}

		public async Task Async(Action<string> action)
		{
			try {
				await TaskEx.Run(() => action(Payload.Token));
			} catch(Exception e) {
				Log.Error($"Не удалось обновить документ {Payload.Entity.EntityId}", e);
				Manager.Error(ErrorHelper.TranslateException(e)
					?? "Не удалось выполнить операцию, попробуйте повторить позднее.");
			}
		}

		public bool TrySign(SignedContent content)
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
				sign = new WinApiCrypt().Sign(content, Cert.RawData);
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

	public class FormValidation: ValidationRule
	{
		public override ValidationResult Validate(object value, CultureInfo cultureInfo)
		{
			// Редактор XAML почему то передает в value массив, а CLR передает модель
			try
			{
				BindingGroup bindingGroup = (BindingGroup)value;
				Sign model = bindingGroup.Items[0] as Sign;

				string error = "Заполнены не все обязательные поля:";

				if(!string.IsNullOrEmpty(model.ACPTFirstName) ||
					!string.IsNullOrEmpty(model.ACPTSurename) ||
					!string.IsNullOrEmpty(model.ACPTPatronimic) ||
					!string.IsNullOrEmpty(model.ACPTJobTitle)
					)
				{
					if(string.IsNullOrEmpty(model.ACPTSurename))
						error += "\nФамилия";
					if(string.IsNullOrEmpty(model.ACPTFirstName))
						error += "\nИмя";
					return new ValidationResult(false, error);
				}

				if (!string.IsNullOrEmpty(model.ATRNum) ||
						model.ATRDate != DateTime.MinValue ||
						!string.IsNullOrEmpty(model.ATROrganization) ||
						!string.IsNullOrEmpty(model.ATRFirstName) ||
						!string.IsNullOrEmpty(model.ATRSurename) ||
						!string.IsNullOrEmpty(model.ATRPatronymic) ||
						!string.IsNullOrEmpty(model.ATRAddInfo))
				{
					if(string.IsNullOrEmpty(model.ATRNum))
						error += "\nНомер";
					if(model.ATRDate == DateTime.MinValue)
						error += "\nДата";
					return new ValidationResult(false, error);
				}

			}
			catch (Exception ex)
			{
				return new ValidationResult(false, "Заполнены не все обязательные поля.");
			}

			return ValidationResult.ValidResult;
		}
	}

	public class Sign : DiadokAction
	{
		public Sign(ActionPayload payload)
			: base(payload)
		{
			Torg12TitleVisible = Payload.Entity.AttachmentType == AttachmentType.XmlTorg12;

			var certFields = Cert.Subject.Split(',').Select(s => s.Split('=')).ToDictionary(p => p[0].Trim(), p => p[1].Trim());

			SignerFirstName = certFields["CN"];
			SignerSureName = certFields["CN"];
			SignerPatronimic = certFields["CN"];
			SignerINN = "9656279962";//certFields["CN"];

			RCVFIO.Value = $"{SignerSureName} {SignerFirstName} {SignerPatronimic}";
			RCVJobTitle.Value = certFields["CN"];
			RCVDate.Value = DateTime.Now;

			LikeReciever.Subscribe(x => {
				if(x)
				{
					ACPTFirstName.Value = SignerFirstName;
					ACPTSurename.Value = SignerSureName;
					ACPTPatronimic.Value = SignerPatronimic;
					ACPTJobTitle.Value = RCVJobTitle.Value;
				}
			});

			ByAttorney.Subscribe(x => {
				if(!x)
				{
					ATRNum.Value = "";
					ATROrganization.Value = "";
					ATRSurename.Value = "";
					ATRFirstName.Value = "";
					ATRPatronymic.Value = "";
					ATRAddInfo.Value = "";
				}
			});

		}

		public bool Torg12TitleVisible { get;set;}

		string SignerFirstName;
		string SignerSureName;
		string SignerPatronimic;
		string SignerINN;

		public NotifyValue<string> RCVFIO { get; set;}
		public NotifyValue<string> RCVJobTitle { get; set;}
		public NotifyValue<DateTime> RCVDate { get; set;}

		public NotifyValue<bool> LikeReciever { get; set;}
		public NotifyValue<string> ACPTSurename { get; set;}
		public NotifyValue<string> ACPTFirstName { get; set;}
		public NotifyValue<string> ACPTPatronimic { get; set;}
		public NotifyValue<string> ACPTJobTitle { get; set;}

		public NotifyValue<bool> ByAttorney { get; set;}
		public NotifyValue<string> ATRNum { get; set;}
		public NotifyValue<DateTime> ATRDate { get; set;}
		public NotifyValue<string> ATROrganization { get; set;}
		public NotifyValue<string> ATRSurename { get; set;}
		public NotifyValue<string> ATRFirstName { get; set;}
		public NotifyValue<string> ATRPatronymic { get; set;}
		public NotifyValue<string> ATRAddInfo { get; set;}

		public NotifyValue<string> Comment { get; set;}

		Official GetRevicedOfficial()
		{// тоже что и SignerDetails
			Official ret = new Official();
			ret.FirstName = SignerFirstName;
			ret.Surname = SignerSureName;
			ret.Patronymic = SignerPatronimic;
			ret.JobTitle = RCVJobTitle;
			return ret;
		}

		Official GetAcceptetOfficial()
		{
			Official ret = null;
			if(!string.IsNullOrEmpty(ACPTFirstName) && !string.IsNullOrEmpty(ACPTSurename))
			{
				ret = new Official();
				ret.FirstName = ACPTFirstName;
				ret.Surname = ACPTSurename;
				ret.Patronymic = ACPTPatronimic;
				ret.JobTitle = ACPTJobTitle;
				return ret;
			}
			return ret;
		}

		Signer GetSigner()
		{
			Signer sg = new Signer();
			sg.SignerCertificate = Cert.RawData;
			sg.SignerCertificateThumbprint = Cert.Thumbprint;
			sg.SignerDetails = GetSignerDetails();
			return sg;
		}

		SignerDetails GetSignerDetails()
		{
			SignerDetails ret = new SignerDetails();
			ret.FirstName = SignerFirstName;
			ret.Surname = SignerSureName;
			ret.Patronymic = SignerPatronimic;
			ret.JobTitle = RCVJobTitle;
			ret.Inn = SignerINN;
			return ret;
		}

		Attorney GetAttorney()
		{
			if(!string.IsNullOrEmpty(ATRNum) && ATRDate.Value != DateTime.MinValue)
			{
				Attorney ret = new Attorney();
				ret.Number = ATRNum;
				ret.Date = ATRDate.Value.ToString("dd.MM.yyyy");
				ret.IssuerOrganizationName = ATROrganization;
				ret.IssuerPerson = new Official();
				ret.IssuerPerson.FirstName = ATRSurename;
				ret.IssuerPerson.Surname = ATRSurename;
				ret.IssuerPerson.Patronymic = ATRPatronymic;
				ret.IssuerPerson.JobTitle = ATRAddInfo;
				return ret;
			}
			return null;
		}

		Entity GetDateConfirmationStep7(Message msg)
		{
			Entity invoice = msg.Entities.FirstOrDefault(i => i.AttachmentTypeValue == Diadoc.Api.Com.AttachmentType.Invoice);
			Entity invoiceReciept = msg.Entities.FirstOrDefault(i => i.ParentEntityId == invoice?.EntityId && i.AttachmentTypeValue == Diadoc.Api.Com.AttachmentType.InvoiceReceipt);
			Entity invoiceRecieptConfirmation = msg.Entities.FirstOrDefault(i => i.ParentEntityId == invoiceReciept?.EntityId && i.AttachmentTypeValue == Diadoc.Api.Com.AttachmentType.InvoiceConfirmation);
			return invoiceRecieptConfirmation;
		}

		public async Task Save()
		{
			BeginAction();

			try
			{
				Signer signer = GetSigner();

				if (Payload.Entity.AttachmentType == AttachmentType.XmlTorg12)
				{
					Official recived = GetRevicedOfficial();
					Official accepted = GetAcceptetOfficial();
					Attorney attorney = GetAttorney();

					var inf =  new Torg12BuyerTitleInfo() {
						ReceivedBy = recived, //лицо, получившее груз signer
						AcceptedBy = accepted,//лицо, принявшее груз
						Attorney = attorney,
						AdditionalInfo = Comment,
						ShipmentReceiptDate = RCVDate.Value.ToString("dd.MM.yyyy"),
						Signer = signer
						};

					GeneratedFile torg12XmlForBuyer = await TaskEx.Run(() => Payload.Api.GenerateTorg12XmlForBuyer(
						Payload.Token,
						inf,
						Payload.BoxId,
						Payload.Entity.DocumentInfo.MessageId,
						Payload.Entity.DocumentInfo.EntityId));

					MessagePatchToPost patch = null;

					SignedContent signContent = new SignedContent();
					signContent.Content = torg12XmlForBuyer.Content;
					if(TrySign(signContent) == false)
						throw new Exception();

					ReceiptAttachment receipt = new ReceiptAttachment {
						ParentEntityId = Payload.Entity.DocumentInfo.EntityId,
						SignedContent = signContent
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
					try
					{
						Entity invoice = Payload.Message.Entities.First(i => i.AttachmentTypeValue == Diadoc.Api.Com.AttachmentType.Invoice);
						GeneratedFile invoiceReceipt = await TaskEx.Run(() => Payload.Api.GenerateInvoiceDocumentReceiptXml(
							Payload.Token,
							Payload.BoxId,
							Payload.Entity.DocumentInfo.MessageId,
							invoice.EntityId,
							signer));

						SignedContent signContentInvoiceReciept = new SignedContent();
						signContentInvoiceReciept.Content = invoiceReceipt.Content;
						if(TrySign(signContentInvoiceReciept) == false)
							throw new Exception();

						ReceiptAttachment receiptInvoice = new ReceiptAttachment {
							ParentEntityId = invoice.EntityId,
							SignedContent = signContentInvoiceReciept
						};

						Entity invoiceConfirmation = Payload.Message.Entities.OrderBy(x => x.CreationTime).First(i => i.AttachmentTypeValue == Diadoc.Api.Com.AttachmentType.InvoiceConfirmation);
						GeneratedFile invoiceConfirmationReceipt = await TaskEx.Run(() => Payload.Api.GenerateInvoiceDocumentReceiptXml(
							Payload.Token,
							Payload.BoxId,
							Payload.Entity.DocumentInfo.MessageId,
							invoiceConfirmation.EntityId,
							signer));

						SignedContent signContentInvoiceConfirmationReciept = new SignedContent();
						signContentInvoiceConfirmationReciept.Content = invoiceReceipt.Content;

						if (TrySign(signContentInvoiceConfirmationReciept) == false)
							throw new Exception();

						ReceiptAttachment invoiceConfirmationreceipt = new ReceiptAttachment {
							ParentEntityId = invoiceConfirmation.EntityId,
							SignedContent = signContentInvoiceConfirmationReciept
						};

						patch = Payload.Patch(receiptInvoice, invoiceConfirmationreceipt);

						await Async(x => Payload.Api.PostMessagePatch(x, patch));
						Log.Info($"Документ {patch.MessageId} receiptInvoice, invoiceConfirmationreceipt отправлены");

						Entity invoiceDateConfirmation = await TaskEx.Run(() =>
						{
							Message msg = null;
							Entity dateConfirm = null;
							int breaker = 0;
							do
							{
								System.Threading.Thread.Sleep(1000);
								msg = Payload.Api.GetMessage(Payload.Token, Payload.BoxId, Payload.Entity.DocumentInfo.MessageId);
								dateConfirm = GetDateConfirmationStep7(msg);
								breaker++;
							}
							while (dateConfirm ==null && breaker < 10);
							if(dateConfirm == null)
								throw new TimeoutException("Превышено время ожидания ответа, повторите операцию позже.");
							return dateConfirm;
						});

						GeneratedFile invoiceOperConfirmationReceipt = await TaskEx.Run(() => Payload.Api.GenerateInvoiceDocumentReceiptXml(
							Payload.Token,
							Payload.BoxId,
							Payload.Entity.DocumentInfo.MessageId,
							invoiceDateConfirmation.EntityId,
							signer));

						SignedContent signContentOperInvoiceConfrmReciept = new SignedContent();
						signContentOperInvoiceConfrmReciept.Content = invoiceOperConfirmationReceipt.Content;

						if (TrySign(signContentOperInvoiceConfrmReciept) == false)
							throw new Exception();

						ReceiptAttachment receipt = new ReceiptAttachment {
							ParentEntityId = invoiceDateConfirmation.EntityId,
							SignedContent = signContentOperInvoiceConfrmReciept
						};
						patch = Payload.Patch(receipt);
						await Async(x => Payload.Api.PostMessagePatch(x, patch));
						Log.Info($"Документ {patch.MessageId} invoiceDateConfirmation отправлен");

						await TaskEx.Run(() =>
						{
							Message msg = null;
							int breaker = 0;
							do
							{
								System.Threading.Thread.Sleep(500);
								msg = Payload.Api.GetMessage(Payload.Token, Payload.BoxId, Payload.Entity.DocumentInfo.MessageId);
								breaker++;
							} while(breaker<10 && msg.Entities.First().DocumentInfo.InvoiceMetadata.Status != Diadoc.Api.Com.InvoiceStatus.InboundFinished);
							if(breaker >= 10)
								throw new TimeoutException("Превышено время ожидания ответа, повторите операцию позже.");
							return msg;
						});
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
			}catch(Exception)
			{
			}
			finally
			{
				EndAction();
			}
		}
	}
}