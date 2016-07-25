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
		public enum PemStringType
    {
        Certificate,
        RsaPrivateKey
    }

		public static IDictionary<string, string> ParseSubject(string certSubject)
		{
			//"СНИЛС=13784958116, ОГРН=1026605606620, STREET=\"ул. Радищева, 28\", G=Сертификат Тестовый, SN=Диадок, T=директор, OID.1.2.840.113549.1.9.2=6663003127-666301001-013784958116, CN=Тестовая организация, OU=головное, O=Тестовая организация, L=Екатеринбург, S=66 Свердловская область, C=RU, E=diadoc@skbkontur.ru, ИНН=006663003127";
			string part = "";
			bool inqq = false;
			var ret = new List<string>();
			for (int i = 0; i < certSubject.Length; i++)
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

		public static int DecodeIntegerSize(System.IO.BinaryReader rd)
		{
			byte byteValue;
			int count;
			byteValue = rd.ReadByte();
			if (byteValue != 0x02)
					return 0;
			byteValue = rd.ReadByte();
			if (byteValue == 0x81)
			{
					count = rd.ReadByte();
			}
			else if (byteValue == 0x82)
			{
					byte hi = rd.ReadByte();
					byte lo = rd.ReadByte();
					count = BitConverter.ToUInt16(new[] { lo, hi }, 0);
			}
			else
				count = byteValue;

			while (rd.ReadByte() == 0x00)
				count -= 1;

			rd.BaseStream.Seek(-1, System.IO.SeekOrigin.Current);
			return count;
		}

		public static byte[] GetBytesFromPEM(string pemString, PemStringType type)
		{
      string header; string footer;
			switch (type)
			{
					case PemStringType.Certificate:
							header = "-----BEGIN CERTIFICATE-----";
							footer = "-----END CERTIFICATE-----";
							break;
					case PemStringType.RsaPrivateKey:
							header = "-----BEGIN RSA PRIVATE KEY-----";
							footer = "-----END RSA PRIVATE KEY-----";
							break;
					default:
							return null;
			}
			int start = pemString.IndexOf(header) + header.Length;
			int end = pemString.IndexOf(footer, start) - start;
			return Convert.FromBase64String(pemString.Substring(start, end));
		}

		public static byte[] AlignBytes(byte[] inputBytes, int alignSize)
		{
			int inputBytesSize = inputBytes.Length;
			if ((alignSize != -1) && (inputBytesSize < alignSize))
			{
				byte[] buf = new byte[alignSize];
				for (int i = 0; i < inputBytesSize; ++i)
				{
					buf[i + (alignSize - inputBytesSize)] = inputBytes[i];
				}
				return buf;
			}
			else
			{
				return inputBytes;
			}
		}
	}
}