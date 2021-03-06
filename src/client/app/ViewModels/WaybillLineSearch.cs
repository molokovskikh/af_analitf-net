﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using AnalitF.Net.Client.Helpers;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Models.Results;
using AnalitF.Net.Client.ViewModels.Parts;
using Caliburn.Micro;
using Common.Tools;
using NHibernate;
using NHibernate.Linq;
using ReactiveUI;

namespace AnalitF.Net.Client.ViewModels
{
	public class WaybillLineSearch : BaseScreen
	{
		private DateTime begin;
		private DateTime end;
		private List<WaybillLine> dirty = new List<WaybillLine>();

		public WaybillLineSearch(DateTime begin, DateTime end)
		{
			DisplayName = "Поиск товара в накладных";
			this.begin = begin;
			this.end = end;

			Lines = new NotifyValue<List<WaybillLine>>(new List<WaybillLine>());
			SearchBehavior = new SearchBehavior(this);
			CurrentLine = new NotifyValue<WaybillLine>();
			CanEnterLine = new NotifyValue<bool>();

			CurrentLine.Select(x => x != null)
				.Subscribe(CanEnterLine);
		}

		public SearchBehavior SearchBehavior { get; set; }
		public NotifyValue<WaybillLine> CurrentLine { get; set; }
		public NotifyValue<bool> CanEnterLine { get; set; }
		public NotifyValue<List<WaybillLine>> Lines { get; set; }

		protected override void OnInitialize()
		{
			base.OnInitialize();

			Lines = SearchBehavior.ActiveSearchTerm
				.Select(v =>{
					//перед поиском нам нужно сохранить изменения
					SaveDirty();
					return RxQuery(s => {
						var query = s.Query<WaybillLine>()
							.Where(l => l.Waybill.WriteTime >= begin && l.Waybill.WriteTime < end);

						var term = SearchBehavior.ActiveSearchTerm.Value;
						if (!String.IsNullOrEmpty(term))
							query = query.Where(m => m.SerialNumber.Contains(term) || m.Product.Contains(term));

						var lines = query
							.OrderBy(l => l.Product)
							.Fetch(l => l.Waybill)
							.ThenFetch(w => w.Supplier)
							.FetchMany(l => l.CertificateFiles)
							.ToList();
						return lines;
					});
			})
			.Switch()
			.ToValue(lines => {
					//тк мы загружаем данные из stateless сессии то отслеживать изменения в них
					//нужно руками, изменения возникнут из-за загрузки сертификатов
					foreach (var line in Lines.Value ?? Enumerable.Empty<WaybillLine>())
						line.PropertyChanged -= Track;

					foreach (var line in lines)
						line.PropertyChanged += Track;

					var pendings = Shell.PendingDownloads.OfType<WaybillLine>().ToLookup(l => l.Id);
					if (pendings.Count > 0) {
						for(var i = 0; i < lines.Count; i ++) {
							var pending = pendings[lines[i].Id].FirstOrDefault();
							if (pending != null) {
								lines[i] = pending;
							}
						}
					}
				return lines;
			});
		}

		protected override void OnDeactivate(bool close)
		{
			SaveDirty();
			base.OnDeactivate(close);
		}

		private void SaveDirty()
		{
			if (dirty.Count > 0) {
				var items = dirty.ToArray();
				dirty.Clear();
				foreach (var item in items)
					Session.Update(item);
			}
		}

		private void Track(object sender, PropertyChangedEventArgs e)
		{
			if (Session == null || !Session.IsOpen)
				return;
			var props = new[] { "IsError", "IsDownloaded", "IsCertificateNotFound" };
			if (!props.Contains(e.PropertyName))
				return;

			if (!dirty.Contains((WaybillLine)sender))
				dirty.Add((WaybillLine)sender);
		}

		public override IEnumerable<IResult> Download(Loadable loadable)
		{
			var supplier = ((WaybillLine)loadable).Waybill.SafeSupplier;
			if (supplier == null || !supplier.HaveCertificates) {
				yield return new MessageResult("Данный поставщик не предоставляет сертификаты в АналитФармация." +
					"\r\nОбратитесь к поставщику.",
					MessageResult.MessageType.Warning);
				yield break;
			}
			base.Download(loadable);
		}


		public void EnterLine()
		{
			if (CurrentLine.Value == null)
				return;
			Shell.Navigate(new WaybillDetails(CurrentLine.Value.Waybill.Id));
		}
	}
}