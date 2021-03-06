﻿using System;
using System.Collections.Generic;
using System.IO;
using Common.Models;

namespace AnalitF.Net.Service.Models
{
	public class AnalitfNetData
	{
		public AnalitfNetData()
		{
		}

		public AnalitfNetData(RequestLog log)
		{
			User = log.User;
			LastPendingUpdateAt = DateTime.Now;
			ClientTokenV2 = log.ClientToken;
			ClientVersion = log.Version;
		}

		public virtual uint Id { get; set; }
		public virtual User User { get; set; }
		public virtual DateTime LastUpdateAt { get; set; }
		public virtual DateTime? LastPendingUpdateAt { get; set; }
		//отдельное поле для синхронизации рекламы нужно тк реклама привязана к домашнему региону
		//при изменении мы должны передать рекламу заново
		public virtual DateTime? LastReclameUpdateAt { get; set; }
		public virtual DateTime? LastPendingReclameUpdateAt { get; set; }
		public virtual string BinUpdateChannel { get; set; }
		//вторая версия идентификатора клиентского приложения
		//первая версия использовала guid вторая передает хеш sha1 guid + путь к программе
		public virtual string ClientTokenV2 { get; set; }
		public virtual Version ClientVersion { get; set; }

		public virtual void Reset()
		{
			LastUpdateAt = DateTime.MinValue;
			LastReclameUpdateAt = null;
		}

		public virtual void Confirm()
		{
			LastUpdateAt = LastPendingUpdateAt.GetValueOrDefault();
			LastReclameUpdateAt = LastPendingReclameUpdateAt;
		}
	}
}