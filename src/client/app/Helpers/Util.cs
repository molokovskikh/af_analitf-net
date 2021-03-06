﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using AnalitF.Net.Client.Controls;
using AnalitF.Net.Client.Models;
using Common.Tools;
using log4net;
using log4net.Appender;
using log4net.Repository.Hierarchy;
using NHibernate.Linq;
using NHibernate.Util;

namespace AnalitF.Net.Client.Helpers
{
	public static class Util
	{
		private static ILog log = LogManager.GetLogger(typeof(Util));

		private enum State
		{
			Key,
			Value
		}

		public static void SafeWait(this Task task)
		{
#if DEBUG
			task.Wait();
#else
			try {
				task.Wait();
			} catch(Exception e) {
				log.Error("Выполнение задачи завершилось ошибкой", e);
			}
#endif
		}

		public static void LogResult(this Task task)
		{
			task.ContinueWith(x => {
				if (x.IsFaulted) {
					log.Error("Выполнение задачи завершилось ошибкой", x.Exception);
				}
			});
		}

		[Conditional("DEBUG")]
		public static void Assert(bool condition, string error)
		{
			if (!condition)
				throw new Exception(error);
		}

		public static bool IsNumeric(object o)
		{
			return o is short || o is ushort
				|| o is int || o is uint
				|| o is long || o is ulong
				|| o is byte
				|| o is decimal
				|| o is double
				|| o is float;
		}

		public static bool IsDateTime(Type type)
		{
			type = Nullable.GetUnderlyingType(type) ?? type;
			return type == typeof(DateTime);
		}

		public static bool IsNumeric(this Type type)
		{
			type = Nullable.GetUnderlyingType(type) ?? type;
			return type == typeof(short) || type == typeof(ushort)
				|| type == typeof(int) || type == typeof(uint)
				|| type == typeof(long) || type == typeof(ulong)
				|| type == typeof(byte)
				|| type == typeof(decimal)
				|| type == typeof(double)
				|| type == typeof(float);
		}

		public static PropertyInfo GetProperty(Type type, string path)
		{
			if (type == null)
				return null;
			var parts = path.Split('.');
			PropertyInfo property = null;
			foreach (var part in parts) {
				property = type.GetProperty(part);
				if (property == null)
					return null;
				type = property.PropertyType;
			}
			return property;
		}

		public static object GetValue(object item, string path)
		{
			var parts = path.Split('.');

			var value = item;
			foreach (var part in parts) {
				if (value == null)
					return null;
				var type = value.GetType();
				var property = type.GetProperty(part);
				if (property != null) {
					value = property.GetValue(value, null);
				}
				else {
					var field = type.GetField(part);
					if (field == null)
						return null;
					value = field.GetValue(value);
				}
			}
			return value;
		}

		public static void SetValue(object item, string path, object value)
		{
			var parts = path.Split('.');

			var current = item;
			PropertyInfo property = null;
			for(var i = 0; i < parts.Length; i++) {
				if (current == null)
					return;
				var type = current.GetType();
				property = type.GetProperty(parts[i]);
				if (property != null) {
					if (i < parts.Length - 1)
						current = property.GetValue(current, null);
					else if (property.CanWrite)
						property.SetValue(current, value, null);
				}
				else {
					var field = type.GetField(parts[i]);
					if (field == null)
						return;
					if (i < parts.Length - 1)
						current = field.GetValue(current);
					else
						field.SetValue(current, value);
				}
			}
		}

		public static string HumanizeSize(long size, string zero = "")
		{
			if (size == 0)
				return "-";
			if (size < 1024)
				return size + " Б";
			if (size < 1048576)
				return (size / 1024f).ToString("#.##") + " КБ";
			if (size < 1073741824)
				return (size / 1048576f).ToString("#.##") + " МБ";
			return (size / 1073741824f).ToString("#.##") + " ГБ";
		}

		public static T Cache<T, TKey>(this SimpleMRUCache cache, TKey key, Func<TKey, T> @select)
		{
			var cached = (T)cache[key];
			if (!Equals(cached, default(T))) {
				return cached;
			}
			cached = @select(key);
			cache.Put(key, cached);
			return cached;
		}

		public static IQueryable<T> Filter<T>(IQueryable<T> query,
			Expression<Func<T, PriceComposedId>> selectFunc,
			IEnumerable<Selectable<Price>> prices)
		{
			var selected = prices.Where(p => p.IsSelected).Select(p => p.Item.Id).ToArray();
			if (selected.Count() != prices.Count()) {
				var param = Expression.Parameter(typeof(T), "o");
				var field = (MemberExpression)selectFunc.Body;
				Expression result = null;
				foreach (var id in selected) {
					if (result == null)
						result = Expression.Equal(field, Expression.Constant(id));
					else
						result = Expression.OrElse(result, Expression.Equal(field, Expression.Constant(id)));
				}

				result = result ?? Expression.Constant(false);
				query = query.Where(Expression.Lambda<Func<T, bool>>(result, new[] { param }));
			}
			return query;
		}

		public static IQueryable<T> ContainsAny<T>(IQueryable<T> query,
			Expression<Func<T, string>> select,
			IEnumerable<string> values)
		{
			if (!values.Any())
				return query;

			var field = (MemberExpression)@select.Body;
			Expression result = null;
			foreach (var value in values) {
				var exp = Expression.Call(field, typeof(String).GetMethod("Contains"), Expression.Constant(value));
				if (result == null)
					result = exp;
				else
					result = Expression.AndAlso(result, exp);
			}

			return query.Where(Expression.Lambda<Func<T, bool>>(result,
				new[] { Expression.Parameter(typeof(T), "o") }));
		}

		public static string DebugDump(object value, Type type, string name)
		{
			var p = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Instance);
			if (p != null) {
				var result = p.GetValue(value, null);
				return $"{name} = {result}";
			}
			var f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
			return $"{name} = {f.GetValue(value)}";
		}

		public static string FormatCost(this decimal? value)
		{
			return value != null ? value.Value.ToString("0.00") : "";
		}

		public static decimal? Round(decimal? value, Rounding rounding)
		{
			if (rounding != Rounding.None) {
				var @base = 10;
				var factor = 1;
				if (rounding == Rounding.To1_00) {
					@base = 1;
				} else if (rounding == Rounding.To0_50) {
					factor = 5;
				}
				var normalized = (int?) (value * @base);
				return (normalized - normalized % factor) / (decimal) @base;
			}
			return value;
		}

		public static Dictionary<string, string> ParseSubject(X509Certificate2 c)
		{
			List<Char> accum = new List<char>();
			var quoted = false;
			var state = State.Key;
			var result = new Dictionary<string, string>();
			string key = null;
			for (var i = 0; i < c.Subject.Length; i++) {
				var current = c.Subject[i];
				var next = i < c.Subject.Length - 1 ? c.Subject[i + 1] : '\u0000';
				if (state == State.Key) {
					if (current == '=') {
						state = State.Value;
						key = new String(accum.ToArray());
						accum.Clear();
						continue;
					}
					if (current == ' ') {
						continue;
					}
				}
				if (state == State.Value) {
					if (!quoted && current == ',') {
						state = State.Key;
						quoted = false;
						result[key] = new String(accum.ToArray());
						accum.Clear();
						continue;
					}
					if (current == '\"') {
						if (next != '\"') {
							quoted = !quoted;
						}
						else {
							accum.Add(current);
							i++;
						}
						continue;
					}
				}
				accum.Add(current);
			}
			return result;
		}

		public static IDisposable FlushLogs()
		{
			var appenders = ((Hierarchy)LogManager.GetRepository()).GetAppenders().OfType<FileAppender>().ToArray();
			var files = new Dictionary<object, string>();
			appenders.Each(x => {
				files.Add(x, x.File);
				x.Writer = null;
			});
			return new DisposibleAction(() => {
				appenders.Each(x => {
					x.File = files.GetValueOrDefault(x);
				});
			});
		}

		public static string HumanizeDaysAgo(int days)
		{
			if (days == 1)
				return "Товар был заказан вчера.";
			if (days > 1 && days < 5)
				return "Товар был заказан за последние " + days + " дня";
			if (days >= 5)
				return "Товар был заказан за последние " + days + " дней";
			return "";
		}

		/// <summary>
		/// Проверка на валидность 8/12/13/14-значного штрих-кода
		/// </summary>
		/// <param name="code"></param>
		/// <returns>True, если штрих-код валидный, иначе false</returns>
		public static bool IsValidBarCode(string code)
		{
			if (string.IsNullOrWhiteSpace(code)) return false;
			if (code.Length != 8 && code.Length != 12 && code.Length != 13 && code.Length != 14) return false;

			int sum = 0;
			for (int i = 0; i < code.Length - 1; i++){
				if (!char.IsNumber(code[i])) return false;

				var cchari = (int)char.GetNumericValue(code[i]);
				if ((code.Length + i) % 2 == 0)
					sum += cchari * 3;
				else
					sum += cchari;
			}

			//Последняя цифра - контрольное число
			char checkChar = code[code.Length - 1];
			if (!char.IsNumber(checkChar)) return false;

			int checkChari = (int)char.GetNumericValue(checkChar);
			return checkChari == (10 - (sum % 10)) % 10;
		}
	}
}