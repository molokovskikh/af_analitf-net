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
using Dapper;
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
			var postProcessing = new List<string>();
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
					table.Constraints.Clear();
					if (table.Columns.Contains("ServerDocId"))
						table.Columns.Remove("ServerDocId");
					if (table.Columns.Contains("ServerId"))
						table.Columns.Remove("ServerId");
					if (table.Columns.Contains("Timestamp") && !item.FileName.EndsWith("Waybills"))
						table.Columns.Remove("Timestamp");

					MySqlCommand cmd;
					var columnMap = new Dictionary<string ,string>(StringComparer.InvariantCultureIgnoreCase) {
						{ "Id", "ClientPrimaryKey" }
					};
					if (item.FileName == "check-lines") {
						columnMap.Add("CheckId", "ClientDocId");
					} else if (item.FileName.EndsWith("Lines")) {
						var name = item.FileName.Replace("Lines", "");
						columnMap.Add($"{name}DocId", "ClientDocId");
					}

					foreach (DataColumn dataColumn in table.Columns) {
						if (!columnMap.ContainsKey(dataColumn.ColumnName))
							columnMap.Add(dataColumn.ColumnName, dataColumn.ColumnName);
					}
					var parametersSql = columnMap.Values.Implode(x => "?" + x);
					var columns = columnMap.Values.Implode();
					if (item.FileName == "checks") {
						cmd = new MySqlCommand($"insert into Inventory.Checks (UserId, {columns}) values (?userId, {parametersSql});");
					} else if (item.FileName == "check-lines") {
						cmd = new MySqlCommand($"insert into Inventory.CheckLines (UserId, {columns}) values (?userId, {parametersSql});");
						postProcessing.Add(@"
update Inventory.CheckLines l
join Inventory.Checks d on d.ClientPrimaryKey = l.ClientDocId and d.UserId = l.UserId
set l.CheckId = d.Id
where l.CheckId is null
	and d.UserId = ?userId");
					} else if (item.FileName.EndsWith("Docs")) {
						cmd = new MySqlCommand($"insert into Inventory.{item.FileName} (UserId, {columns}) values (?userId, {parametersSql});");
					} else if (item.FileName.EndsWith("Lines")) {
						var name = item.FileName.Replace("Lines", "");
						cmd = new MySqlCommand($"insert into Inventory.{item.FileName} (UserId, {columns}) values (?userId, {parametersSql});");
						postProcessing.Add($@"
update Inventory.{item.FileName} l
join Inventory.{name}Docs d on d.ClientPrimaryKey = l.ClientDocId and d.UserId = l.UserId
set l.{name}DocId = d.Id
where l.{name}DocId is null
	and d.UserId = ?userId");
					} else if (item.FileName.EndsWith("Waybills")) {
						cmd = new MySqlCommand("insert into Inventory.StockedWaybills (UserId, DownloadId, ClientTimestamp) values (?userId, ?ClientPrimaryKey, ?Timestamp);");
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
							var value = row[column];
							if (value is DateTime) {
								value = ((DateTime)value).ToLocalTime();
							}
							cmd.Parameters[columnMap[column.ColumnName]].Value = value;
						}
						try {
							cmd.ExecuteNonQuery();
						} catch(Exception e) {
							throw new Exception($"Не удалось выполнить запрос {cmd.CommandText}", e);
						}
					}
				}
			}

			Stock.CreateInTransitStocks(Session, CurrentUser);
			foreach (var sql in postProcessing) {
				Session.Connection.Execute(sql, new { userId = CurrentUser.Id });
			}
			var memory = new MemoryStream();
			//todo: обдумать возможность ограничения передачи клиентского идентификатора исходя из версии
			//из-за возможных проблем при пересоздании базы
			Session.Flush();
			using (var exporter = new Exporter(Session, Config, new RequestLog(CurrentUser, request, GetType().Name))) {
				exporter.Prefix = Guid.NewGuid().ToString();
				exporter.ExportStocks(lastSync);
				exporter.ExportStockActions(lastSync);
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
			} else {
				source = Session.Query<Stock>().OrderByDescending(x => x.Id)
					.FirstOrDefault(x => x.CreatedByUser == CurrentUser && x.ClientPrimaryKey == action.ClientStockId);
				if (source == null)
					throw new Exception($"Не удалось найти запись {action.ClientStockId}");
			}
			if (action.ActionType == ActionType.Sale)
				source.Quantity -= action.Quantity;
			else if (action.ActionType == ActionType.Stock) {
				source.ClientPrimaryKey = action.ClientStockId;
				source.CreatedByUser = CurrentUser;
				source.RetailCost = action.RetailCost;
				source.RetailMarkup = action.RetailMarkup;
				source.Status = StockStatus.Available;
				source.CreatedByUser = CurrentUser;
				Session.Save(source);
			} else {
				throw new Exception($"Неизвестная операция {action.ActionType} над строкой {action.SourceStockId}");
			}

			string sql = @"insert into Inventory.stockactions " +
		"(UserId, Timestamp, DisplayDoc, NumberDoc, FromIn, OutTo, ActionType, TypeChange, " +
		" ClientStockId, SourceStockId,SourceStockVersion, Quantity, RetailCost, RetailMarkup, DiscountSum)" +
		" values (?userId, ?Timestamp, ?DisplayDoc, ?NumberDoc, ?FromIn, ?OutTo, ?ActionType, ?TypeChange, " +
		" ?ClientStockId, ?SourceStockId, ?SourceStockVersion, ?Quantity, ?RetailCost, ?RetailMarkup, " +
		" ?DiscountSum);";

			MySqlCommand cmd = new MySqlCommand(sql);
			cmd.Parameters.AddWithValue("userId", CurrentUser.Id);
			cmd.Parameters.AddWithValue("Timestamp", action.Timestamp.ToLocalTime());
			cmd.Parameters.AddWithValue("DisplayDoc", action.DisplayDoc);
			cmd.Parameters.AddWithValue("NumberDoc", action.NumberDoc);
			cmd.Parameters.AddWithValue("FromIn", action.FromIn);
			cmd.Parameters.AddWithValue("OutTo", action.OutTo);
			cmd.Parameters.AddWithValue("ActionType", (int)action.ActionType);
			cmd.Parameters.AddWithValue("TypeChange", (int)action.TypeChange);
			cmd.Parameters.AddWithValue("ClientStockId", action.ClientStockId);
			cmd.Parameters.AddWithValue("SourceStockId", action.SourceStockId);
			cmd.Parameters.AddWithValue("SourceStockVersion", action.SourceStockVersion);
			cmd.Parameters.AddWithValue("Quantity", action.Quantity);
			cmd.Parameters.AddWithValue("RetailCost", action.RetailCost);
			cmd.Parameters.AddWithValue("RetailMarkup", action.RetailMarkup);
			cmd.Parameters.AddWithValue("DiscountSum", action.DiscountSum);
			cmd.Connection = (MySqlConnection)Session.Connection;
			cmd.Prepare();
			try
			{
				cmd.ExecuteNonQuery();
			}
			catch (Exception e)
			{
				throw new Exception($"Не удалось выполнить запрос {cmd.CommandText}", e);
			}
		}
	}
}