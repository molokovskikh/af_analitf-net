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
using NHibernate.Linq;
using System.Threading;
using Common.Tools.Calendar;

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
		public bool Success;

		public DiadokAction(ActionPayload payload)
		{
			Success = false;
			InitFields();
			Payload = payload;
			switch(payload.Entity.AttachmentType) {
				case AttachmentType.XmlTorg12:
					DocumentName = new DiadocXmlHelper(payload.Entity).GetDiadokTORG12Name(" , ");
					break;
				case AttachmentType.Invoice:
					DocumentName = new DiadocXmlHelper(payload.Entity).GetDiadokInvoiceName(" , ");
					break;
				default:
					DocumentName = payload.Entity.FileName;
					break;
			}

			IsEnabled.Value = true;

			if(Settings.Value.DebugUseTestSign) {
				SignerFirstName = "Иван";
				SignerSureName = "Иванович";
				SignerPatronimic = "Иванов";
				SignerINN = Settings.Value.DebugDiadokSignerINN;
			}
			else {
				Cert = Settings.Value.GetCert(Settings.Value.DiadokCert);
				var certFields = X509Helper.ParseSubject(Cert.Subject);
				try {
					var namefp = certFields["G"].Split(' ');
					SignerFirstName = namefp[0];
					SignerSureName = certFields["SN"];
					SignerPatronimic = namefp[1];
					if(!String.IsNullOrEmpty(Settings.Value.DebugDiadokSignerINN))
						SignerINN = Settings.Value.DebugDiadokSignerINN;
					else {
						if(certFields.Keys.Contains("OID.1.2.643.3.131.1.1"))
							SignerINN = certFields["OID.1.2.643.3.131.1.1"];
						if(certFields.Keys.Contains("ИНН"))
							SignerINN = certFields["ИНН"];
						if(String.IsNullOrEmpty(SignerINN))
							throw new Exception("Не найдено поле ИНН(OID.1.2.643.3.131.1.1)");
					}
				}
				catch(Exception exept) {
					Log.Error("Ошибка разбора сертификата, G,SN,OID.1.2.643.3.131.1.1", exept);
				}
			}
		}

		public Task<TResult> Async<TResult>(Func<string, TResult> action)
		{
			return Task<TResult>.Factory.StartNew(() => {
				try {
					TaskEx.Delay(5.Second());
					throw new Exception();
					LastPatchStamp = Payload.Message.LastPatchTimestamp;
					return action(Payload.Token);
				}
				catch(Exception exception) {
					if(exception is TimeoutException) {
						Log.Warn($"Превышено время ожидания ответа при обработке документа {Payload.Message.MessageId}", exception);
						Manager.Warning("Превышено время ожидания ответа, повторите операцию позже.");
					}
					else if (exception is HttpClientException) {
						var e = exception as HttpClientException;
						if (e.ResponseStatusCode == HttpStatusCode.Conflict) {
							Log.Warn($"Документ {Payload.Message.MessageId} был подписан ранее", e);
							Manager.Warning("Документ уже был подписан другим пользователем.");
						} else {
							Log.Warn($"Ошибка при подписи документа {Payload.Message.MessageId}", e);
							Manager.Warning($"Ошибка при обработке документа:\n{e.AdditionalMessage}");
						}
					}
					Log.Warn($"Ошибка при подписи документа {Payload.Message.MessageId}", exception);
					throw;
				}
			}, CloseCancellation.Token, TaskCreationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
		}

		public Task Async(Action<string> action)
		{
			return Task.Factory.StartNew(() => {
				try {
					TaskEx.Delay(5.Second());
					throw new Exception();
					LastPatchStamp = Payload.Message.LastPatchTimestamp;
					action(Payload.Token);
				}
				catch(Exception exception) {
					if(exception is TimeoutException) {
						Log.Warn($"Превышено время ожидания ответа при обработке документа {Payload.Message.MessageId}", exception);
						Manager.Warning("Превышено время ожидания ответа, повторите операцию позже.");
					}
					else if (exception is HttpClientException) {
						var e = exception as HttpClientException;
						if (e.ResponseStatusCode == HttpStatusCode.Conflict) {
							Log.Warn($"Документ {Payload.Message.MessageId} был подписан ранее", e);
							Manager.Warning("Документ уже был подписан другим пользователем.");
						} else {
							Log.Warn($"Ошибка при подписи документа {Payload.Message.MessageId}", e);
							Manager.Warning($"Ошибка при обработке документа:\n{e.AdditionalMessage}");
						}
					}
					Log.Warn($"Ошибка при подписи документа {Payload.Message.MessageId}", exception);
				}
			}, CloseCancellation.Token, TaskCreationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
		}

		public string SignerFirstName { get; set;}
		public string SignerSureName { get; set;}
		public string SignerPatronimic { get; set;}
		public string SignerINN { get;set;}

		public X509Certificate2 Cert { get; protected set;}
		public NotifyValue<bool> IsEnabled { get; set; }
		public ActionPayload Payload { get; set; }
		public string DocumentName { get; set; }
		public DateTime LastPatchStamp { get; set;}

		public bool ReqRevocationSign
		{
			get
			{
				return Payload.Entity.DocumentInfo.RevocationStatus == RevocationStatus.RequestsMyRevocation;
			}
		}

		public Signer GetSigner()
		{
			Signer ret = new Signer();
			ret.SignerDetails = new SignerDetails();
			if(Settings.Value.DebugUseTestSign) {
				ret.SignerCertificate = new byte[1];
				ret.SignerCertificateThumbprint = "0987654321ABCDE";
				ret.SignerDetails.FirstName = "Иван";
				ret.SignerDetails.Surname = "Иванович";
				ret.SignerDetails.Patronymic = "Иванов";
				ret.SignerDetails.JobTitle = "Специалист";
				ret.SignerDetails.Inn = Settings.Value.DebugDiadokSignerINN;
			}
			else {
				ret.SignerCertificate = Cert.RawData;
				ret.SignerCertificateThumbprint = Cert.Thumbprint;
				ret.SignerDetails.FirstName = SignerFirstName;
				ret.SignerDetails.Surname = SignerSureName;
				ret.SignerDetails.Patronymic = SignerPatronimic;
				ret.SignerDetails.JobTitle = Settings.Value.DiadokSignerJobTitle;
				ret.SignerDetails.Inn = SignerINN;
			}

			return ret;
		}

		protected void BeginAction()
		{
			IsEnabled.Value = false;
		}

		protected async void EndAction(bool waitupdate = true)
		{
			if(waitupdate) {
				await Async((x) => {
					Message msg = null;
					int breaker = 0;
					do {
						msg = Payload.Api.GetMessage(x, Payload.BoxId, Payload.Entity.DocumentInfo.MessageId, Payload.Entity.EntityId);
						if(LastPatchStamp != msg.LastPatchTimestamp)
							break;
						else
							TaskEx.Delay(1000).Wait();
						breaker++;
					} while(breaker<5 && LastPatchStamp != DateTime.MinValue);
					Result = msg;
				});
			}
			IsEnabled.Value = true;
			Success = Result != null;
			TryClose();
		}

		public Message Result { get; set;}

		public bool TrySign(SignedContent content)
		{
			if (Settings.Value.DebugUseTestSign) {
				content.SignWithTestSignature = true;
				return true;
			}
			byte[] sign;
			var result = TrySign(content.Content, out sign);
			if(result)
				content.Signature = sign;

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
			// Редактор XAML передает в value массив, а CLR передает модель
			try {
				BindingGroup bindingGroup = (BindingGroup)value;
				Sign model = bindingGroup.Items[0] as Sign;

				string error = "Заполнены не все обязательные поля:";
				string fields = "";

				if(bindingGroup.Name == "AcceptedValidation") {
					if(model.Detailed && (!String.IsNullOrEmpty(model.AcptFirstName) ||
					!String.IsNullOrEmpty(model.AcptSurename) ||
					!String.IsNullOrEmpty(model.AcptPatronimic) ||
					!String.IsNullOrEmpty(model.AcptJobTitle))) {
						if(String.IsNullOrEmpty(model.AcptSurename))
							fields += "\nФамилия";
						if(String.IsNullOrEmpty(model.AcptFirstName))
							fields += "\nИмя";
					}
				}
				if(bindingGroup.Name == "AttorneyValidation") {
					if (model.Detailed && model.ByAttorney)
					{
						if(String.IsNullOrEmpty(model.AtrNum))
							fields += "\nНомер";
						if(!model.AtrDate.HasValue)
							fields += "\nДата";

						if(!String.IsNullOrEmpty(model.AtrFirstName) ||
							!String.IsNullOrEmpty(model.AtrSurename) ||
							!String.IsNullOrEmpty(model.AtrPatronymic))
						{
							if(String.IsNullOrEmpty(model.AtrFirstName))
								fields += "\nИмя";
							if(String.IsNullOrEmpty(model.AtrSurename))
								fields += "\nФамилия";
							if(String.IsNullOrEmpty(model.AtrPatronymic))
								fields += "\nОтчество";
						}
					}
				}

				if(String.IsNullOrEmpty(fields))
					return ValidationResult.ValidResult;

				return new ValidationResult(false, error + fields);
			}
			catch (Exception) {
				return new ValidationResult(false, "Заполнены не все обязательные поля.");
			}
		}
	}

	public class Sign : DiadokAction
	{
		public Sign(ActionPayload payload)
			: base(payload)
		{
			if(ReqRevocationSign)
				OperationName.Value = "Аннулирование документа";
			else
				OperationName.Value = "Подписание Документа";
			Torg12TitleVisible = Payload.Entity.AttachmentType == AttachmentType.XmlTorg12 && !ReqRevocationSign;
			RcvFIO.Value = $"{SignerSureName} {SignerFirstName} {SignerPatronimic}";
			RcvJobTitle.Value = Settings.Value.DiadokSignerJobTitle;
			RcvDate.Value = DateTime.Now;
			LikeReciever.Subscribe(x => {
				if(x) {
					AcptFirstName.Value = SignerFirstName;
					AcptSurename.Value = SignerSureName;
					AcptPatronimic.Value = SignerPatronimic;
					AcptJobTitle.Value = RcvJobTitle.Value;
				}
			});
			ByAttorney.Subscribe(x => {
				if(!x) {
					AtrNum.Value = "";
					AtrOrganization.Value = "";
					AtrSurename.Value = "";
					AtrFirstName.Value = "";
					AtrPatronymic.Value = "";
					AtrAddInfo.Value = "";
				}
			});
			CurrentAutoSave = new NotifyValue<SignTorg12Autosave>();
			CurrentAutoSave.Subscribe(x =>
			{
				if(x != null) {
					RcvJobTitle.Value = x.SignerJobTitle;
					if(x.LikeReciever) {
						LikeReciever.Value = true;
					}
					else {
						AcptFirstName.Value = x.AcptFirstName;
						AcptSurename.Value = x.AcptSurename;
						AcptPatronimic.Value = x.AcptPatronimic;
						AcptJobTitle.Value = x.AcptJobTitle;
					}
					if(x.ByAttorney) {
						ByAttorney.Value = true;
						AtrNum.Value = x.AtrNum;
						AtrDate.Value = x.AtrDate;
						AtrOrganization.Value = x.AtrOrganization;
						AtrJobTitle.Value = x.AtrJobTitle;
						AtrSurename.Value = x.AtrSurename;
						AtrFirstName.Value = x.AtrFirstName;
						AtrPatronymic.Value = x.AtrPatronymic;
						AtrAddInfo.Value = x.AtrAddInfo;
					}
					else {
						ByAttorney.Value = false;
						AtrNum.Value = "";
						AtrDate.Value = null;
						AtrOrganization.Value = "";
						AtrJobTitle.Value = "";
						AtrSurename.Value ="";
						AtrFirstName.Value = "";
						AtrPatronymic.Value = "";
						AtrAddInfo.Value = "";
					}
					Comment.Value = x.Comment;
				}
			});
			Comment.Subscribe(x => {
				if(!String.IsNullOrEmpty(x))
					CommentVisibility.Value = true;
				else
					CommentVisibility.Value = false;
			});
		}

		protected override void OnInitialize()
		{
			base.OnInitialize();
			SavedData.Value = Session.Query<SignTorg12Autosave>().OrderByDescending(o => o.CreationDate).ToList();
		}

		public NotifyValue<string> OperationName { get; set;}
		public bool Torg12TitleVisible { get;set;}
		public NotifyValue<bool> Detailed { get;set;}

		public NotifyValue<string> RcvFIO { get; set;}
		public NotifyValue<string> RcvJobTitle { get; set;}
		public NotifyValue<DateTime> RcvDate { get; set;}

		public NotifyValue<SignTorg12Autosave> CurrentAutoSave { get;set;}
		public NotifyValue<List<SignTorg12Autosave>> SavedData { get; set;}
		public NotifyValue<bool> LikeReciever { get; set;}
		public NotifyValue<string> AcptSurename { get; set;}
		public NotifyValue<string> AcptFirstName { get; set;}
		public NotifyValue<string> AcptPatronimic { get; set;}
		public NotifyValue<string> AcptJobTitle { get; set;}

		public NotifyValue<bool> ByAttorney { get; set;}
		public NotifyValue<string> AtrNum { get; set;}
		public NotifyValue<DateTime?> AtrDate { get; set;}
		public NotifyValue<string> AtrOrganization { get; set;}
		public NotifyValue<string> AtrJobTitle { get; set;}
		public NotifyValue<string> AtrSurename { get; set;}
		public NotifyValue<string> AtrFirstName { get; set;}
		public NotifyValue<string> AtrPatronymic { get; set;}
		public NotifyValue<string> AtrAddInfo { get; set;}

		public NotifyValue<bool> SaveData { get; set;}
		const int autosave_max = 10;
		public NotifyValue<bool> CommentVisibility { get; set;}
		public NotifyValue<string> Comment { get; set;}

		Official GetRevicedOfficial()
		{// тоже что и SignerDetails
			Official ret = new Official();
			ret.FirstName = SignerFirstName;
			ret.Surname = SignerSureName;
			ret.Patronymic = SignerPatronimic;
			ret.JobTitle = RcvJobTitle;
			return ret;
		}

		Official GetAcceptetOfficial()
		{
			Official ret = null;
			if(Detailed.Value && !String.IsNullOrEmpty(AcptFirstName) && !String.IsNullOrEmpty(AcptSurename)) {
				ret = new Official();
				ret.FirstName = AcptFirstName;
				ret.Surname = AcptSurename;
				ret.Patronymic = AcptPatronimic;
				ret.JobTitle = AcptJobTitle;
				return ret;
			}
			return ret;
		}

		Attorney GetAttorney()
		{
			if(Detailed.Value && !String.IsNullOrEmpty(AtrNum) && AtrDate.HasValue) {
				Attorney ret = new Attorney();
				ret.Number = AtrNum;
				ret.Date = AtrDate.Value.Value.ToString("dd.MM.yyyy");
				ret.IssuerOrganizationName = AtrOrganization;

				if(!String.IsNullOrEmpty(AtrFirstName.Value) &&
					!String.IsNullOrEmpty(AtrSurename.Value) &&
					!String.IsNullOrEmpty(AtrPatronymic.Value)) {
					ret.IssuerPerson = new Official();
					ret.IssuerPerson.FirstName = AtrFirstName;
					ret.IssuerPerson.Surname = AtrSurename;
					ret.IssuerPerson.Patronymic = AtrPatronymic;
					ret.IssuerPerson.JobTitle = AtrJobTitle;
					ret.IssuerAdditionalInfo = AtrAddInfo;
				}
				return ret;
			}
			return null;
		}

		Entity GetDateConfirmationStep7(Message msg)
		{
			Entity invoice = msg.Entities.FirstOrDefault(i => i.AttachmentTypeValue == Diadoc.Api.Com.AttachmentType.Invoice);
			Entity invoiceReciept = msg.Entities.FirstOrDefault(i => i.ParentEntityId == invoice?.EntityId &&
			i.AttachmentTypeValue == Diadoc.Api.Com.AttachmentType.InvoiceReceipt);
			Entity invoiceRecieptConfirmation = msg.Entities.FirstOrDefault(i => i.ParentEntityId == invoiceReciept?.EntityId &&
			i.AttachmentTypeValue == Diadoc.Api.Com.AttachmentType.InvoiceConfirmation);
			return invoiceRecieptConfirmation;
		}

		public async void Save()
		{
			BeginAction();

			MessagePatchToPost patch = null;
			Signer signer = GetSigner();
			try {
				if(ReqRevocationSign) {
					Entity revocReq = Payload.Message.Entities.FirstOrDefault(x => x.AttachmentTypeValue == Diadoc.Api.Com.AttachmentType.RevocationRequest);
					patch = Payload.Patch();
					DocumentSignature acceptSignature = new DocumentSignature();
					acceptSignature.IsApprovementSignature = true;
					acceptSignature.ParentEntityId = revocReq.EntityId;
					byte[] sign = null;
					if(!TrySign(revocReq.Content.Data, out sign))
						throw new Exception();
					if (Settings.Value.DebugUseTestSign) {
						acceptSignature.SignWithTestSignature = true;
					}
					acceptSignature.Signature = sign;
					patch.AddSignature(acceptSignature);
					await Async(x => Payload.Api.PostMessagePatch(x, patch));
				}
				else {
					if(Payload.Entity.AttachmentType == AttachmentType.Nonformalized) {
						patch = Payload.Patch();
						byte[] sign = null;
						if(TrySign(Payload.Entity.Content.Data, out sign) == false)
							throw new Exception();
						var signature = new DocumentSignature {
							ParentEntityId = Payload.Entity.EntityId,
							Signature = sign
						};
						if (Settings.Value.DebugUseTestSign)
							signature.SignWithTestSignature = true;
						patch.AddSignature(signature);
						await Async(x => Payload.Api.PostMessagePatch(x, patch));
					}
					else if (Payload.Entity.AttachmentType == AttachmentType.XmlTorg12) {
						if(SaveData.Value) {
							SignTorg12Autosave autosave = new SignTorg12Autosave();
							autosave.SignerJobTitle = RcvJobTitle.Value;
							if(LikeReciever.Value) {
								 autosave.LikeReciever = true;
							}
							else {
								autosave.AcptFirstName = AcptFirstName.Value;
								autosave.AcptSurename = AcptSurename.Value;
								autosave.AcptPatronimic = AcptPatronimic.Value;
								autosave.AcptJobTitle = AcptJobTitle.Value;
							}
							if(ByAttorney.Value) {
								autosave.ByAttorney = true;
								autosave.AtrNum = AtrNum.Value;
								autosave.AtrDate = AtrDate.Value.Value;
								autosave.AtrOrganization = AtrOrganization.Value;
								autosave.AtrJobTitle = AtrJobTitle.Value;
								autosave.AtrSurename = AtrSurename.Value;
								autosave.AtrFirstName = AtrFirstName.Value;
								autosave.AtrPatronymic = AtrPatronymic.Value;
								autosave.AtrAddInfo = AtrAddInfo.Value;
							}
							autosave.Comment = Comment.Value;
							Session.Save(autosave);
							for(int i = autosave_max - 1; i < SavedData.Value.Count; i++) {
								Session.Delete(SavedData.Value[i]);
							}
						}
						Official recived = GetRevicedOfficial();
						Official accepted = GetAcceptetOfficial();
						Attorney attorney = GetAttorney();

						var inf =  new Torg12BuyerTitleInfo() {
							ReceivedBy = recived, //лицо, получившее груз signer
							AcceptedBy = accepted,//лицо, принявшее груз
							Attorney = attorney,
							AdditionalInfo = Comment,
							ShipmentReceiptDate = RcvDate.Value.ToString("dd.MM.yyyy"),
							Signer = signer
							};

						GeneratedFile torg12XmlForBuyer = await Async((x) => Payload.Api.GenerateTorg12XmlForBuyer(
							x,
							inf,
							Payload.BoxId,
							Payload.Entity.DocumentInfo.MessageId,
							Payload.Entity.DocumentInfo.EntityId));

						SignedContent signContent = new SignedContent();
						signContent.Content = torg12XmlForBuyer.Content;
						if(TrySign(signContent) == false)
							throw new Exception();

						ReceiptAttachment receipt = new ReceiptAttachment {
							ParentEntityId = Payload.Entity.DocumentInfo.EntityId,
							SignedContent = signContent
						};
						patch = Payload.PatchTorg12(receipt);
						await Async(x => Payload.Api.PostMessagePatch(x, patch));
						Log.Info($"Документ {patch.MessageId} успешно подписан");
					}
					else if (Payload.Entity.AttachmentType == AttachmentType.Invoice) {
						Entity invoice = Payload.Message.Entities.First(i => i.AttachmentTypeValue == Diadoc.Api.Com.AttachmentType.Invoice);

						GeneratedFile invoiceReceipt = await Async((x) => Payload.Api.GenerateInvoiceDocumentReceiptXml(
							x,
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
						GeneratedFile invoiceConfirmationReceipt = await Async((x) => Payload.Api.GenerateInvoiceDocumentReceiptXml(
							x,
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

						Entity invoiceDateConfirmation = await Async((x) => {
							Message msg = null;
							Entity dateConfirm = null;
							int breaker = 0;
							do {
								TaskEx.Delay(1000).Wait();
								msg = Payload.Api.GetMessage(Payload.Token,
									Payload.BoxId,
									Payload.Entity.DocumentInfo.MessageId,
									Payload.Entity.EntityId);
								dateConfirm = GetDateConfirmationStep7(msg);
								breaker++;
							}
							while (dateConfirm == null && breaker < 10);
							if(dateConfirm == null)
								throw new TimeoutException("Превышено время ожидания ответа, повторите операцию позже.");
							return dateConfirm;
						});

						GeneratedFile invoiceOperConfirmationReceipt = await Async((x) => Payload.Api.GenerateInvoiceDocumentReceiptXml(
							x,
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
					}
				}
				EndAction();
			}
			catch(Exception exception) {
				if(exception is TimeoutException) {
					Manager.Warning("Превышено время ожидания ответа, повторите операцию позже.");
				}
				else if (exception is HttpClientException) {
					var e = exception as HttpClientException;
					if (e.ResponseStatusCode == HttpStatusCode.Conflict) {
						Log.Warn($"Документ {patch.MessageId} был подписан ранее", e);
						Manager.Warning("Документ уже был подписан другим пользователем.");
					}
				}
				EndAction(false);
			}
		}
	}
}