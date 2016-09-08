using System.ComponentModel;
using System.Runtime.CompilerServices;
using AnalitF.Net.Client.Config.NHibernate;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;

namespace AnalitF.Net.Client.Helpers
{
	public static class X509Helper
	{
		public static IDictionary<string, string> ParseSubject(string certSubject)
		{
			//"СНИЛС=13784958116, ОГРН=1026605606620, STREET=\"ул. Радищева, 28\", G=Сертификат Тестовый, SN=Диадок, T=директор, OID.1.2.840.113549.1.9.2=6663003127-666301001-013784958116, CN=Тестовая организация, OU=головное, O=Тестовая организация, L=Екатеринбург, S=66 Свердловская область, C=RU, E=diadoc@skbkontur.ru, ИНН=006663003127";
			var part = "";
			var inqq = false;
			var ret = new List<string>();
			for (var i = 0; i < certSubject.Length; i++)
			{
				if(inqq)
				{
					if(certSubject[i] == '\"')
						inqq = false;
					else
						part += certSubject[i];
					continue;
				}
				if(certSubject[i] == ',')
				{
					ret.Add(part);
					part = "";
					i++;
					continue;
				}
				if(certSubject[i] == '\"')
				{
					if(inqq)
						inqq = false;
					else
						inqq = true;
					continue;
				}
				part += certSubject[i];
			}
			ret.Add(part);
			return ret.Select(s => s.Split('=')).ToDictionary(p => p[0].Trim(), p => p[1].Trim());
		}
	}
}