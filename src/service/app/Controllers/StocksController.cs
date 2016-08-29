using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using Common.Models;
using Common.Tools;
using Ionic.Crc;
using Ionic.Zip;
using MySql.Data.MySqlClient;
using NHibernate;

namespace AnalitF.Net.Service.Controllers
{
	public class StocksController : ApiController
	{
		public ISession Session { get; set; }
		public User CurrentUser { get; set; }

		public HttpResponseMessage Post(HttpRequestMessage request)
		{
			var input = request.Content.ReadAsStreamAsync().Result;
			using (var zip = ZipFile.Read(input)) {
				foreach (var item in zip) {
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
					if (item.FileName == "checks")
						cmd = new MySqlCommand($"insert into Inventory.Checks (UserId, {columns}) values (?userId, {parametersSql})");
					else if (item.FileName == "check-lines")
						cmd = new MySqlCommand($"insert into Inventory.CheckLines ({columns}) values ({parametersSql})");
					else
						throw new Exception($"Неизвестный тип данных {item.FileName}");
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
			return new HttpResponseMessage(HttpStatusCode.OK);
		}
	}
}