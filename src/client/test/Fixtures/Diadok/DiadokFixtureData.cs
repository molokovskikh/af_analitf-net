using System.Linq;
using AnalitF.Net.Client.Models;
using AnalitF.Net.Client.Test.TestHelpers;
using AnalitF.Net.Client.ViewModels.Diadok;
using NHibernate.Linq;
using NUnit.Framework;
using ReactiveUI.Testing;
using AnalitF.Net.Client.Test.Fixtures;
using System.Security.Cryptography.X509Certificates;
using System;
using Diadoc.Api.Proto.Invoicing;
using AnalitF.Net.Client.Helpers;

namespace AnalitF.Net.Client.Test.Fixtures
{
	public class CertEx
	{
		public X509Certificate2 Cert { get; set;}
		public SignerDetails SignerDetails { get; set;}
	}
	public static class DiadokFixtureData
	{

		public static CertEx Cert
		{
			get
			{
				X509Certificate2 cert = new X509Certificate2();
				cert.Import(Convert.FromBase64String(CertBin));
				SignerDetails sd = new SignerDetails();

				var certFields = X509Helper.ParseSubject(cert.Subject);

				var namefp = certFields["G"].Split(' ');
				sd.FirstName = namefp[0];
				sd.Surname = certFields["SN"];
				sd.Patronymic = namefp[1];
				if(certFields.Keys.Contains("OID.1.2.643.3.131.1.1"))
					sd.Inn = certFields["OID.1.2.643.3.131.1.1"];
				if(certFields.Keys.Contains("ИНН"))
					sd.Inn = certFields["ИНН"];
				if(string.IsNullOrEmpty(sd.Inn))
						throw new Exception("Не найдено поле ИНН(OID.1.2.643.3.131.1.1)");

				CertEx ret = new CertEx{ Cert = cert, SignerDetails = sd};

				return ret;
			}
		}

		public static string Sender_BoxId = ddk.ch_boxid;
		public static string Receiver_BoxId = ddk.ie_boxid;

		public static string Torg12Xml = @"<?xml version=""1.0"" encoding=""windows-1251""?>
<Файл ИдФайл=""DP_OTORG12_2BM-9656279962-965601000-201607080606080987674_2BM-9667029241-966701000-201607080608586286217_20160720_8a43b8a2-0c3a-4d78-b536-1d2403ccbb2b"" ВерсПрог=""Diadoc 1.0"" ВерсФорм=""5.01"">
  <СвУчДокОбор ИдОтпр=""2BM-9667029241-966701000-201607080608586286217"" ИдПок=""2BM-9656279962-965601000-201607080606080987674"">
    <СвОЭДОтпрСФ НаимОрг=""ЗАО &quot;ПФ &quot;СКБ Контур&quot;"" ИННЮЛ=""6663003127"" ИдЭДОСФ=""2BM"" />
  </СвУчДокОбор>
  <Документ КНД=""1175004"" ДатаДок=""20.07.2016"" ВремДок=""15.22.58"">
    <СвТНО НаимПервДок=""Товарная накладная"" ОКУДПервДок=""0330212"" НомФорм=""ТОРГ-12"">
      <ГрузОт>
        <ГрузОтпр ОКПО=""28108888"">
          <ИдСв>
            <СвЮЛ НаимОрг=""Тестовая организация №6702924"" ИННЮЛ=""9667029241"" КПП=""966701000"" />
          </ИдСв>
          <Адрес>
            <АдрРФ Индекс=""690001"" КодРегион=""25"" Район=""Нагорный"" Город=""Владивосток"" НаселПункт=""Туево"" Улица=""Люксенбург"" Дом=""1"" Корпус=""2"" Кварт=""3"" />
          </Адрес>
          <Контакт Тлф=""Михаил Михайлович"" />
          <БанкРекв НомерСчета=""12192533456080"">
            <СвБанк НаимБанк=""ООО Прадомир"" />
          </БанкРекв>
        </ГрузОтпр>
        <СтруктПодр>ОПКР</СтруктПодр>
      </ГрузОт>
      <ГрузПолуч ОКПО=""4545455557"">
        <ИдСв>
          <СвЮЛ НаимОрг=""Тестовая организация №5627996"" ИННЮЛ=""9656279962"" КПП=""965601000"" />
        </ИдСв>
        <Адрес>
          <АдрРФ Индекс=""690000"" КодРегион=""65"" Район=""Свердловский"" Город=""Южно-Сахалинск"" НаселПункт=""Лиственничное"" Улица=""Серова"" Дом=""1"" Корпус=""2"" Кварт=""3"" />
        </Адрес>
        <Контакт Тлф=""ИванИвичГрузерштерн"" />
        <БанкРекв НомерСчета=""30100000000000023467"">
          <СвБанк НаимБанк=""ПУ БАНКА РОССИИ N 83604"" БИК=""040021002"" />
        </БанкРекв>
      </ГрузПолуч>
      <Поставщик ОКПО=""4545458"">
        <ИдСв>
          <СвЮЛ НаимОрг=""Тестовая организация №6702924"" ИННЮЛ=""9667029241"" КПП=""966701000"" />
        </ИдСв>
        <Адрес>
          <АдрРФ Индекс=""385200"" КодРегион=""01"" Район=""Красногвардейский"" Город=""Адыгейск"" НаселПункт=""Орехово"" Улица=""Кразнознаменой девизии имени моей бабушки"" Дом=""1"" Корпус=""2"" Кварт=""3"" />
        </Адрес>
        <Контакт Тлф=""Иван Иванович"" />
        <БанкРекв НомерСчета=""40810000000001234567"">
          <СвБанк НаимБанк=""СберБанк"" />
        </БанкРекв>
      </Поставщик>
      <Плательщик ОКПО=""0130130130"">
        <ИдСв>
          <СвЮЛ НаимОрг=""Тестовая организация №5627996"" ИННЮЛ=""9656279962"" КПП=""965601000"" />
        </ИдСв>
        <Адрес>
          <АдрРФ Индекс=""222111"" КодРегион=""63"" Район=""Камышлинский"" Город=""Город"" НаселПункт=""Орехово"" Улица=""Белчанского"" Дом=""1"" Корпус=""2"" Кварт=""3"" />
        </Адрес>
        <Контакт Тлф=""Лесная 11"" />
        <БанкРекв НомерСчета=""705040222888111"">
          <СвБанк НаимБанк=""МТХАЙСОЛЮШЕН ГРУПП"" />
        </БанкРекв>
      </Плательщик>
      <Основание НаимОсн=""договор"" НомОсн=""1"" ДатаОсн=""29.02.2016"" />
      <ТранНакл НомТранНакл=""48"" ДатаТранНакл=""01.02.2011"" />
      <ВидОперации>доставка</ВидОперации>
      <ТН НомТН=""123"" ДатаТН=""06.08.2012"">
        <Таблица>
          <СвТов НомТов=""1"" НаимТов=""Марля"" КодТов=""1"" НаимЕдИзм=""(10 в ст.3) усл. м"" ОКЕИ_Тов=""048"" ВидУпак=""туба"" Место=""1"" КолМест=""1"" Брутто=""1"" Нетто=""1"" Цена=""1"" СумБезНДС=""1"" СтавкаНДС=""10"" СумНДС=""0.1"" СумУчНДС=""1"" />
          <ВсегоНакл КолМестВс=""1"" БруттоВс=""1"" НеттоВс=""1"" СумБезНДСВс=""1"" СумНДСВс=""0.1"" СумУчНДСВс=""1"" />
        </Таблица>
        <ТНОбщ КолНомЗап=""1"" КолНомЗапПр=""один"" ВсМест=""1"" ВсМестПр=""один"" Нетто=""1"" НеттоПр=""один"" Брутто=""1"" БруттоПр=""один"" />
      </ТН>
      <ОтпускГруз КолПрил=""1"" КолПрилПр=""один"" СумОтпуск=""1"" СумОтпускПр=""Один рубль 00 копеек"" ДатаОтпуск=""01.01.2016"">
        <ОтпускРазреш Должность=""специалист"">
          <ФИО Фамилия=""Султан"" Имя=""Михаил"" Отчество=""Геннадьевич"" />
        </ОтпускРазреш>
        <Бухгалтер>
          <ФИО Фамилия=""Погремушкин"" Имя=""Алексей"" Отчество=""Михайлович"" />
        </Бухгалтер>
        <ОтпускПроизв Должность=""специалист"">
          <ФИО Фамилия=""Фуко"" Имя=""Михаил"" Отчество=""Алексеевич"" />
        </ОтпускПроизв>
      </ОтпускГруз>
      <ИнфПол ТекстИнф=""хрупкий груз"" />
    </СвТНО>
    <Подписант>
      <ЮЛ ИННЮЛ=""9667029241"" Должн=""Сотрудник"">
        <ФИО Фамилия=""Шунько2"" Имя=""Миша2"" Отчество=""Геннадьевич2"" />
      </ЮЛ>
    </Подписант>
  </Документ>
</Файл>";

		public static string InvoiceXml = @"<?xml version=""1.0"" encoding=""windows-1251""?>
	<Файл ИдФайл=""ON_SFAKT_2BM-9656279962-965601000-201607080606080987674_2BM-9667029241-966701000-201607080608586286217_20110101_02fcae4c-2d45-4a2f-aa51-612e49652e8e"" ВерсФорм=""5.02"">
  <СвУчДокОбор ИдОтпр=""2BM-9667029241-966701000-201607080608586286217"" ИдПок=""2BM-9656279962-965601000-201607080606080987674"">
    <СвОЭДОтпр НаимОрг=""ЗАО &quot;ПФ &quot;СКБ Контур&quot;"" ИННЮЛ=""6663003127"" ИдЭДО=""2BM"" />
  </СвУчДокОбор>
  <Документ КНД=""1115101"">
    <СвСчФакт НомерСчФ=""1"" ДатаСчФ=""01.01.2011"" КодОКВ=""643"">
      <СвПрод>
        <ИдСв>
          <СвЮЛ НаимОрг=""Тестовая организация №6702924"" ИННЮЛ=""9667029241"" КПП=""966701000"" />
        </ИдСв>
        <Адрес>
          <АдрРФ Индекс=""222111"" КодРегион=""63"" Район=""1"" Город=""1"" НаселПункт=""1"" Улица=""1"" Дом=""1"" Корпус=""1"" Кварт=""1"" />
        </Адрес>
      </СвПрод>
      <ГрузОт>
        <ГрузОтпр>
          <НаимГОП>
            <НаимОрг>Тестовая организация №6702924</НаимОрг>
          </НаимГОП>
          <Адрес>
            <АдрРФ Индекс=""222111"" КодРегион=""63"" Район=""1"" Город=""1"" НаселПункт=""1"" Улица=""1"" Дом=""1"" Корпус=""1"" Кварт=""1"" />
          </Адрес>
        </ГрузОтпр>
      </ГрузОт>
      <ГрузПолуч>
        <НаимГОП>
          <НаимОрг>ГРУЗОПОЛУЧАЕТЛЬ</НаимОрг>
        </НаимГОП>
        <Адрес>
          <АдрРФ Индекс=""222111"" КодРегион=""63"" Район=""1"" Город=""1"" НаселПункт=""1"" Улица=""1"" Дом=""1"" Корпус=""1"" Кварт=""1"" />
        </Адрес>
      </ГрузПолуч>
      <СвПРД НомерПРД=""1"" ДатаПРД=""01.01.2011"" />
      <СвПокуп>
        <ИдСв>
          <СвЮЛ НаимОрг=""Тестовая организация №5627996"" ИННЮЛ=""9656279962"" КПП=""965601000"" />
        </ИдСв>
        <Адрес>
          <АдрРФ Индекс=""222111"" КодРегион=""63"" Район=""1"" Город=""1"" НаселПункт=""1"" Улица=""1"" Дом=""1"" Корпус=""1"" Кварт=""1"" />
        </Адрес>
      </СвПокуп>
      <ИнфПол>
        <ТекстИнф Идентиф=""1"" Значен=""1"" />
      </ИнфПол>
    </СвСчФакт>
    <ТаблСчФакт>
      <СведТов НалСт=""10/110"" НомСтр=""1"" НаимТов=""1"" СтТовУчНал=""1.18"">
        <Акциз>
          <БезАкциз>без акциза</БезАкциз>
        </Акциз>
        <СумНал>
          <СумНДС>1.00</СумНДС>
        </СумНал>
        <СвТД КодПроисх=""112"" НомерТД=""1"" />
      </СведТов>
      <ВсегоОпл СтТовБезНДСВсего=""1.00"" СтТовУчНалВсего=""1.18"">
        <СумНалВсего>
          <СумНДС>1.00</СумНДС>
        </СумНалВсего>
      </ВсегоОпл>
    </ТаблСчФакт>
    <Подписант>
      <ЮЛ ИННЮЛ=""9667029241"" Должн=""Сотрудник"">
        <ФИО Фамилия=""Шунько2"" Имя=""Миша2"" Отчество=""Геннадьевич2"" />
      </ЮЛ>
    </Подписант>
  </Документ>
</Файл>
";

		public static string CertBin = @"MIIJBjCCCLWgAwIBAgIKNLfsEgAAAAPyFTAIBgYqhQMCAgMwggFbMRgwFgYFKoUD
ZAESDTAwMDAwMDAwMDAwMDAxGjAYBggqhQMDgQMBARIMMDAwMDAwMDAwMDAwMSQw
IgYDVQQJDBvQo9C70YzRj9C90L7QstGB0LrQsNGPIDEz0LAxHjAcBgkqhkiG9w0B
CQEWD2NhQHNrYmtvbnR1ci5ydTELMAkGA1UEBhMCUlUxMzAxBgNVBAgMKjY2INCh
0LLQtdGA0LTQu9C+0LLRgdC60LDRjyDQvtCx0LvQsNGB0YLRjDEhMB8GA1UEBwwY
0JXQutCw0YLQtdGA0LjQvdCx0YPRgNCzMSgwJgYDVQQKDB/Ql9CQ0J4g0J/QpCDQ
odCa0JEg0JrQvtC90YLRg9GAMTAwLgYDVQQLDCfQo9C00L7RgdGC0L7QstC10YDR
j9GO0YnQuNC5INGG0LXQvdGC0YAxHDAaBgNVBAMTE1VDIFRlc3QgKFF1YWxpZmll
ZCkwHhcNMTYwNzA1MDY1MDAwWhcNMTYxMDA1MDcwMDAwWjCCAbExGjAYBggqhQMD
gQMBARIMMDA5NjUzNzMxMzYyMSIwIAYJKoZIhvcNAQkBFhNyLmt2YXNvdkBhbmFs
aXQubmV0MQswCQYDVQQGEwJSVTEcMBoGA1UECAwTNzcg0LMuINCc0L7RgdC60LLQ
sDEVMBMGA1UEBwwM0JzQvtGB0LrQstCwMTswOQYDVQQKDDLQotC10YHRgtC+0LLQ
sNGPINC+0YDQs9Cw0L3QuNC30LDRhtC40Y8g4oSWOTM5MzYxNDE7MDkGA1UEAwwy
0KLQtdGB0YLQvtCy0LDRjyDQvtGA0LPQsNC90LjQt9Cw0YbQuNGPIOKEljkzOTM2
MTQxMDAuBgkqhkiG9w0BCQIMITk2NTM3MzEzNjItOTY1MzAxMDAwLTAwMDAwMDAw
MDAwMDEVMBMGA1UEBAwM0KjRg9C90YzQutC+MSwwKgYDVQQqDCPQnNC40YXQsNC4
0Lsg0JPQtdC90L3QsNC00YzQtdCy0LjRhzEKMAgGA1UECQwBMTEYMBYGBSqFA2QB
Eg0wMDAwMDAwMDAwMDAwMRYwFAYFKoUDZAMSCzAwMDAwMDAwMDAwMGMwHAYGKoUD
AgITMBIGByqFAwICJAAGByqFAwICHgEDQwAEQKrT1YzxJ0fBTZxyDIAo+uer7pVB
/CKFrK7qnuy6M98aE6XuwYdC2xYTCbwzzCF4gZ3GLRlkjzJHXhuKr00E2VCjggT9
MIIE+TAOBgNVHQ8BAf8EBAMCBPAwEwYDVR0gBAwwCjAIBgYqhQNkcQEwSwYDVR0l
BEQwQgYIKwYBBQUHAwIGByqFAwICIgYGCCsGAQUFBwMEBgcqhQMDBwgBBggqhQMD
BwEBAQYGKoUDAwcBBggqhQMDBwABAzAeBgNVHREEFzAVgRNyLmt2YXNvdkBhbmFs
aXQubmV0MB0GA1UdDgQWBBTIiaC1PV7KCYerduZxBXonO38lkTCCAZwGA1UdIwSC
AZMwggGPgBTClAfnrk+/ucBZaISI0Cx+8C1Ac6GCAWOkggFfMIIBWzEYMBYGBSqF
A2QBEg0wMDAwMDAwMDAwMDAwMRowGAYIKoUDA4EDAQESDDAwMDAwMDAwMDAwMDEk
MCIGA1UECQwb0KPQu9GM0Y/QvdC+0LLRgdC60LDRjyAxM9CwMR4wHAYJKoZIhvcN
AQkBFg9jYUBza2Jrb250dXIucnUxCzAJBgNVBAYTAlJVMTMwMQYDVQQIDCo2NiDQ
odCy0LXRgNC00LvQvtCy0YHQutCw0Y8g0L7QsdC70LDRgdGC0YwxITAfBgNVBAcM
GNCV0LrQsNGC0LXRgNC40L3QsdGD0YDQszEoMCYGA1UECgwf0JfQkNCeINCf0KQg
0KHQmtCRINCa0L7QvdGC0YPRgDEwMC4GA1UECwwn0KPQtNC+0YHRgtC+0LLQtdGA
0Y/RjtGJ0LjQuSDRhtC10L3RgtGAMRwwGgYDVQQDExNVQyBUZXN0IChRdWFsaWZp
ZWQpghBUngCWQYXEi0DS5SHP2KsJMHIGA1UdHwRrMGkwMqAwoC6GLGh0dHA6Ly9j
ZHAuc2tia29udHVyLnJ1L2NkcC91Yy10ZXN0LTYzZnouY3JsMDOgMaAvhi1odHRw
Oi8vY2RwMi5za2Jrb250dXIucnUvY2RwL3VjLXRlc3QtNjNmei5jcmwwgZcGCCsG
AQUFBwEBBIGKMIGHMEEGCCsGAQUFBzAChjVodHRwOi8vY2RwLnNrYmtvbnR1ci5y
dS9jZXJ0aWZpY2F0ZXMvdWMtdGVzdC02M2Z6LmNydDBCBggrBgEFBQcwAoY2aHR0
cDovL2NkcDIuc2tia29udHVyLnJ1L2NlcnRpZmljYXRlcy91Yy10ZXN0LTYzZnou
Y3J0MCsGA1UdEAQkMCKADzIwMTYwNzA1MDY1MDAwWoEPMjAxNjEwMDUwNjUwMDBa
MDYGBSqFA2RvBC0MKyLQmtGA0LjQv9GC0L7Qn9GA0L4gQ1NQIiAo0LLQtdGA0YHQ
uNGPIDMuNikwggExBgUqhQNkcASCASYwggEiDCsi0JrRgNC40L/RgtC+0J/RgNC+
IENTUCIgKNCy0LXRgNGB0LjRjyAzLjYpDFMi0KPQtNC+0YHRgtC+0LLQtdGA0Y/R
jtGJ0LjQuSDRhtC10L3RgtGAICLQmtGA0LjQv9GC0L7Qn9GA0L4g0KPQpiIg0LLQ
tdGA0YHQuNC4IDEuNQxOQ9C10YDRgtC40YTQuNC60LDRgiDRgdC+0L7RgtCy0LXR
gtGB0YLQstC40Y8g4oSWINCh0KQvMTIxLTE4NTkg0L7RgiAxNy4wNi4yMDEyDE5D
0LXRgNGC0LjRhNC40LrQsNGCINGB0L7QvtGC0LLQtdGC0YHRgtCy0LjRjyDihJYg
0KHQpC8xMjgtMTgyMiDQvtGCIDAxLjA2LjIwMTIwCAYGKoUDAgIDA0EA3B+ZcAbQ
y1+hexTfzSJBY2y5Kf3h40Fc2PEVlyK//6vUJqNEwDf017VuHd6DL250RiDCrvb6
O/nROj61ZgIthw==";

	}
}