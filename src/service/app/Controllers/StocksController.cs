using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using AnalitF.Net.Service.Models;
using AnalitF.Net.Service.Models.Inventory;
using Common.Models;
using Common.Tools;
using Ionic.Crc;
using Ionic.Zip;
using log4net;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using NHibernate;
using NHibernate.Linq;

namespace AnalitF.Net.Service.Controllers
{
	public class StocksController : ApiController
	{
		private ILog log = LogManager.GetLogger(typeof(StocksController));

		public Config.Config Config;
		public ISession Session { get; set; }
		public User CurrentUser { get; set; }

		public HttpResponseMessage Post(HttpRequestMessage request)
		{
			var input = request.Content.ReadAsStreamAsync().Result;
			var serverTimestamp = DateTime.Now;
			var lastSync = DateTime.MinValue;
			using (var zip = ZipFile.Read(input)) {
				foreach (var item in zip) {
					if (item.FileName == "stock-actions") {
						var reader = new JsonTextReader(new StreamReader(item.OpenReader()));
						var serializer = new JsonSerializer();
						var actions = serializer.Deserialize<StockActionAttrs[]>(reader);
						foreach (var action in actions) {
							try {
								HandleStockAction(action);
							} catch (Exception ex) {
								log.Error(ex);
							}
						}
						continue;
					} else if (item.FileName == "server-timestamp") {
						var stream = item.OpenReader();
						var buffer = new byte[stream.Length];
						stream.Read(buffer, 0, buffer.Length);
						lastSync = DateTime.Parse(Encoding.UTF8.GetString(buffer));
						continue;
					}

					var table = new DataTable();
					table.ReadXml(item.OpenReader());
					MySqlCommand cmd;
					var columnMap = new Dictionary<string ,string>(StringComparer.InvariantCultureIgnoreCase) {
						{ "Id", "ClientPrimaryKey" }
					};
					foreach (DataColumn dataColumn in table.Columns) {
						if (!columnMap.ContainsKey(dataColumn.ColumnName))
							columnMap.Add(dataColumn.ColumnName, dataColumn.ColumnName);
					}
					var parametersSql = columnMap.Values.Implode(x => "?" + x);
					var columns = columnMap.Values.Implode();
					if (item.FileName == "checks") {
						cmd = new MySqlCommand($"insert into Inventory.Checks (UserId, {columns}) values (?userId, {parametersSql})");
					} else if (item.FileName == "check-lines") {
						cmd = new MySqlCommand($"insert into Inventory.CheckLines ({columns}) values ({parametersSql})");
					} else {
						throw new Exception($"Неизвестный тип данных {item.FileName}");
					}

					cmd.Connection = (MySqlConnection)Session.Connection;
					cmd.Prepare();

					cmd.Parameters.AddWithValue("userId", CurrentUser.Id);
					foreach (var column in columnMap.Values) {
						cmd.Parameters.Add(new MySqlParameter {
							ParameterName = column
						});
					}

					foreach (var row in table.AsEnumerable()) {
						foreach (DataColumn column in table.Columns) {
							cmd.Parameters[columnMap[column.ColumnName]].Value = row[column];
						}
						cmd.ExecuteNonQuery();
					}
				}
			}

			Stock.CreateInTransitStocks(Session, CurrentUser);
			var memory = new MemoryStream();
			//todo: обдумать возможность ограничения передачи клиентского идентификатора исходя из версии
			//из-за возможных проблем при пересоздании базы
			Session.Flush();
			using (var exporter = new Exporter(Session, Config, new RequestLog(CurrentUser, request, GetType().Name))) {
				var sql = @"
select if(s.CreatedByUserId = ?userId, s.ClientPrimaryKey, null) as Id,
	s.Id as ServerId,
	s.Version as ServerVersion,
	s.AddressId,
	s.ProductId,
	s.CatalogId,
	s.Product,
	s.ProducerId,
	s.Producer,
	s.Country,
	s.Period,
	s.Exp,
	s.SerialNumber,
	s.Certificates,
	s.Unit,
	s.ExciseTax,
	s.BillOfEntryNumber,
	s.VitallyImportant,
	s.ProducerCost,
	s.RegistryCost,
	s.SupplierPriceMarkup,
	s.SupplierCostWithoutNds,
	s.SupplierCost,
	s.Nds,
	s.NdsAmount,
	s.Barcode,
	s.Status,
	s.Quantity,
	s.SupplyQuantity,
	s.RetailCost,
	s.RetailMarkup,
	dh.DownloadId as WaybillId
from Inventory.Stocks s
	join Customers.Addresses a on a.Id = s.AddressId
		join Customers.UserAddresses ua on ua.Addressid = a.Id
			join Customers.Users u on u.Id = ua.UserId
	left join Documents.DocumentBodies db on db.Id = s.WaybillLineId
		left join Documents.DocumentHeaders dh on dh.Id = db.DocumentId
where a.Enabled = 1
	and u.Id = ?userId
	and s.Timestamp > ?lastSync";
				exporter.Export(exporter.Result, sql, "stocks", false, new { userId = CurrentUser.Id, lastSync });
				exporter.Result.Add(new UpdateData("server-timestamp") {
					Content = serverTimestamp.ToString("O")
				});
				exporter.Compress(memory);
				memory.Position = 0;
			}

			return new HttpResponseMessage(HttpStatusCode.OK) {
				Content = new StreamContent(memory)
			};
		}

		private void HandleStockAction(StockActionAttrs action)
		{
			Stock source;
			if (action.SourceStockId != null) {
				source = Session.Load<Stock>(action.SourceStockId);
				if (source.Version != action.SourceStockVersion)
					throw new Exception($"Конкурентная операция над строкой {action.SourceStockId}");
			} else {
				source = Session.Query<Stock>().OrderByDescending(x => x.Id)
					.FirstOrDefault(x => x.CreatedByUser == CurrentUser && x.ClientPrimaryKey == action.ClientStockId);
				if (source == null)
					throw new Exception($"Не удалось найти запись {action.ClientStockId}");
			}
			if (action.ActionType == ActionType.Sale)
				source.Quantity -= action.Quantity;
			else if (action.ActionType == ActionType.Stock) {
				source.Quantity -= action.Quantity;
				var target = new Stock(CurrentUser, source, action.Quantity, action.ClientStockId,
						action.RetailCost.Value,
						action.RetailMarkup.Value);
				Session.Save(target);
			} else {
				throw new Exception($"Неизвестная операция {action.ActionType} над строкой {action.SourceStockId}");
			}
		}
	}
}