using System;

namespace AnalitF.Net.Client.Test.Fixtures
{
	public static class DiadokFixtureData
	{
		public static string Torg12Xml = @"<?xml version=""1.0"" encoding=""windows-1251""?>
<Файл ИдФайл=""DP_OTORG12_xxxx_2BM-9656351023-965601000-201607250243044951320_20160804_31204a73-4df3-4b67-ab49-ac9142e19104"" ВерсПрог=""Diadoc 1.0"" ВерсФорм=""5.01"">
  <СвУчДокОбор ИдОтпр=""2BM-9656351023-965601000-201607250243044951320"" ИдПок=""xxxx"">
    <СвОЭДОтпрСФ НаимОрг=""ЗАО &quot;ПФ &quot;СКБ Контур&quot;"" ИННЮЛ=""6663003127"" ИдЭДОСФ=""2BM"" />
  </СвУчДокОбор>
  <Документ КНД=""1175004"" ДатаДок=""04.08.2016"" ВремДок=""16.11.08"">
    <СвТНО НаимПервДок=""Товарная накладная"" ОКУДПервДок=""0330212"" НомФорм=""ТОРГ-12"">
      <ГрузОт>
        <ГрузОтпр ОКПО=""28108888"">
          <ИдСв>
            <СвЮЛ НаимОрг=""Тестовая организация №5635102"" ИННЮЛ=""9656351023"" КПП=""965601000"" />
          </ИдСв>
          <Адрес>
            <АдрРФ Индекс=""690001"" КодРегион=""25"" Район=""Нагорный"" Город=""Владивосток"" НаселПункт=""Туево"" Улица=""Люксенбург"" Дом=""1"" Корпус=""2"" Кварт=""3"" />
          </Адрес>
          <Контакт Тлф=""Михаил Михайлович"" />
          <БанкРекв НомерСчета=""12192533456080"">
            <СвБанк НаимБанк=""ООО Прадомир"" />
          </БанкРекв>
        </ГрузОтпр>
        <СтруктПодр>Главное управление ОПКР</СтруктПодр>
      </ГрузОт>
      <ГрузПолуч ОКПО=""4545455557"">
        <ИдСв>
          <СвЮЛ НаимОрг=""Тестовая организация №9875492"" ИННЮЛ=""9698754923"" КПП=""969801000"" />
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
          <СвЮЛ НаимОрг=""Тестовая организация №5635102"" ИННЮЛ=""9656351023"" КПП=""965601000"" />
        </ИдСв>
        <Адрес>
          <АдрРФ Индекс=""385200"" КодРегион=""01"" Район=""Красногвардейский"" Город=""Адыгейск"" НаселПункт=""Орехово"" Улица=""Кразнознаменой девизии имени моей бабушки"" Дом=""1"" Корпус=""2"" Кварт=""3"" />
        </Адрес>
        <Контакт Тлф=""Иван Иванович"" />
        <БанкРекв НомерСчета=""40810000000001234567"">
          <СвБанк />
        </БанкРекв>
      </Поставщик>
      <Плательщик ОКПО=""0130130130"">
        <ИдСв>
          <СвЮЛ НаимОрг=""Тестовая организация №9875492"" ИННЮЛ=""9698754923"" КПП=""969801000"" />
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
      <ЮЛ ИННЮЛ=""9656351023"" Должн=""Сотрудник"">
        <ФИО Фамилия=""Нипелевич"" Имя=""Иван"" Отчество=""Романович"" />
      </ЮЛ>
    </Подписант>
  </Документ>
</Файл>";

		public static string InvoiceXml = @"<?xml version=""1.0"" encoding=""windows-1251""?>
<Файл ИдФайл=""ON_SFAKT_xxxx_2BM-9656351023-965601000-201607250243044951320_20110601_90b68c7e-c98f-437a-88cd-5832661d7d7e"" ВерсФорм=""5.02"">
  <СвУчДокОбор ИдОтпр=""2BM-9656351023-965601000-201607250243044951320"" ИдПок=""xxxx"">
    <СвОЭДОтпр НаимОрг=""ЗАО &quot;ПФ &quot;СКБ Контур&quot;"" ИННЮЛ=""6663003127"" ИдЭДО=""2BM"" />
  </СвУчДокОбор>
  <Документ КНД=""1115101"">
    <СвСчФакт НомерСчФ=""17"" ДатаСчФ=""01.06.2011"" КодОКВ=""643"">
      <СвПрод>
        <ИдСв>
          <СвЮЛ НаимОрг=""Тестовая организация №5635102"" ИННЮЛ=""9656351023"" КПП=""965601000"" />
        </ИдСв>
        <Адрес>
          <АдрРФ Индекс=""222111"" КодРегион=""75"" Район=""Дульдургинский"" Город=""Лапиков"" НаселПункт=""Хвоя"" Улица=""Шагала"" Дом=""1"" Корпус=""2"" Кварт=""3"" />
        </Адрес>
      </СвПрод>
      <ГрузОт>
        <ГрузОтпр>
          <НаимГОП>
            <НаимОрг>Тестовая организация №5635102</НаимОрг>
          </НаимГОП>
          <Адрес>
            <АдрРФ Индекс=""222111"" КодРегион=""76"" Район=""Бабушкинский"" Город=""Сказка"" НаселПункт=""Усачев"" Улица=""Милионеров"" Дом=""1"" Корпус=""2"" Кварт=""3"" />
          </Адрес>
        </ГрузОтпр>
      </ГрузОт>
      <ГрузПолуч>
        <НаимГОП>
          <НаимОрг>Тестовая организация №9875492</НаимОрг>
        </НаимГОП>
        <Адрес>
          <АдрРФ Индекс=""222111"" КодРегион=""22"" Район=""Центральный"" Город=""Москва"" НаселПункт=""Михайлово"" Улица=""Щепика"" Дом=""1"" Корпус=""2"" Кварт=""3"" />
        </Адрес>
      </ГрузПолуч>
      <СвПРД НомерПРД=""1"" ДатаПРД=""01.02.2011"" />
      <СвПокуп>
        <ИдСв>
          <СвЮЛ НаимОрг=""Тестовая организация №9875492"" ИННЮЛ=""9698754923"" КПП=""969801000"" />
        </ИдСв>
        <Адрес>
          <АдрРФ Индекс=""222111"" КодРегион=""22"" Район=""Целинный"" Город=""Бабушки"" НаселПункт=""София"" Улица=""Круговая"" Дом=""1"" Корпус=""2"" Кварт=""3"" />
        </Адрес>
      </СвПокуп>
      <ИнфПол>
        <ТекстИнф Идентиф=""документ"" Значен=""заявление"" />
      </ИнфПол>
    </СвСчФакт>
    <ТаблСчФакт>
      <СведТов НалСт=""18%"" НомСтр=""1"" НаимТов=""Тальк"" ОКЕИ_Тов=""796"" КолТов=""2"" ЦенаТов=""1"" СтТовБезНДС=""2.00"" СтТовУчНал=""2.36"">
        <Акциз>
          <СумАкциз>1.00</СумАкциз>
        </Акциз>
        <СумНал>
          <СумНДС>0.36</СумНДС>
        </СумНал>
        <СвТД КодПроисх=""112"" НомерТД=""1"" />
      </СведТов>
      <СведТов НалСт=""18%"" НомСтр=""2"" НаимТов=""Батон"" КолТов=""4"" ЦенаТов=""1"" СтТовБезНДС=""56.00"" СтТовУчНал=""66.08"">
        <Акциз>
          <СумАкциз>1.00</СумАкциз>
        </Акциз>
        <СумНал>
          <СумНДС>10.08</СумНДС>
        </СумНал>
        <СвТД КодПроисх=""112"" НомерТД=""1"" />
      </СведТов>
      <СведТов НалСт=""18%"" НомСтр=""3"" НаимТов=""Икра"" КолТов=""18"" ЦенаТов=""1"" СтТовБезНДС=""21.00"" СтТовУчНал=""24.78"">
        <Акциз>
          <СумАкциз>1.00</СумАкциз>
        </Акциз>
        <СумНал>
          <СумНДС>3.78</СумНДС>
        </СумНал>
        <СвТД КодПроисх=""112"" НомерТД=""1"" />
      </СведТов>
      <СведТов НалСт=""18%"" НомСтр=""4"" НаимТов=""Диски"" ОКЕИ_Тов=""112"" СтТовБезНДС=""0.00"" СтТовУчНал=""0.00"">
        <Акциз>
          <БезАкциз>без акциза</БезАкциз>
        </Акциз>
        <СумНал>
          <СумНДС>0.00</СумНДС>
        </СумНал>
      </СведТов>
      <ВсегоОпл СтТовБезНДСВсего=""79.00"" СтТовУчНалВсего=""93.22"">
        <СумНалВсего>
          <СумНДС>14.22</СумНДС>
        </СумНалВсего>
      </ВсегоОпл>
    </ТаблСчФакт>
    <Подписант>
      <ЮЛ ИННЮЛ=""9656351023"" Должн=""Сотрудник"">
        <ФИО Фамилия=""Нипелевич"" Имя=""Иван"" Отчество=""Романович"" />
      </ЮЛ>
    </Подписант>
  </Документ>
</Файл>
";

		public static string CertBin = @"MIIJLTCCCNygAwIBAgIKQKrzQgAAAAQEujAIBgYqhQMCAgMwggFbMRgwFgYFKoUD
ZAESDTAwMDAwMDAwMDAwMDAxGjAYBggqhQMDgQMBARIMMDAwMDAwMDAwMDAwMSQw
IgYDVQQJDBvQo9C70YzRj9C90L7QstGB0LrQsNGPIDEz0LAxHjAcBgkqhkiG9w0B
CQEWD2NhQHNrYmtvbnR1ci5ydTELMAkGA1UEBhMCUlUxMzAxBgNVBAgMKjY2INCh
0LLQtdGA0LTQu9C+0LLRgdC60LDRjyDQvtCx0LvQsNGB0YLRjDEhMB8GA1UEBwwY
0JXQutCw0YLQtdGA0LjQvdCx0YPRgNCzMSgwJgYDVQQKDB/Ql9CQ0J4g0J/QpCDQ
odCa0JEg0JrQvtC90YLRg9GAMTAwLgYDVQQLDCfQo9C00L7RgdGC0L7QstC10YDR
j9GO0YnQuNC5INGG0LXQvdGC0YAxHDAaBgNVBAMTE1VDIFRlc3QgKFF1YWxpZmll
ZCkwHhcNMTYwNzE5MDgwODAwWhcNMTYxMDE5MDgxODAwWjCCAecxGjAYBggqhQMD
gQMBARIMMDA5NjU2Mjc5OTYyMSYwJAYJKoZIhvcNAQkBFhdtaWhhaWwuc2h1bmtv
QHlhbmRleC5ydTELMAkGA1UEBhMCUlUxPDA6BgNVBAgMMzAxINCg0LXRgdC/0YPQ
sdC70LjQutCwINCQ0LTRi9Cz0LXRjyAo0JDQtNGL0LPQtdGPKTEKMAgGA1UEBwwB
ZzE7MDkGA1UECgwy0KLQtdGB0YLQvtCy0LDRjyDQvtGA0LPQsNC90LjQt9Cw0YbQ
uNGPIOKEljU2Mjc5OTYxOzA5BgNVBAMMMtCi0LXRgdGC0L7QstCw0Y8g0L7RgNCz
0LDQvdC40LfQsNGG0LjRjyDihJY1NjI3OTk2MTAwLgYJKoZIhvcNAQkCDCE5NjU2
Mjc5OTYyLTk2NTYwMTAwMC0wMDAwMDAwMDAwMDAxGzAZBgNVBAwMEtC00L7Qu9C2
0L3QvtGB0YLRjDEVMBMGA1UEBAwM0KjRg9C90YzQutC+MSwwKgYDVQQqDCPQnNC4
0YXQsNC40Lsg0JPQtdC90L3QsNC00YzQtdCy0LjRhzEKMAgGA1UECQwBZTEYMBYG
BSqFA2QBEg0wMDAwMDAwMDAwMDAwMRYwFAYFKoUDZAMSCzAwMDAwMDAwMDAwMGMw
HAYGKoUDAgITMBIGByqFAwICJAAGByqFAwICHgEDQwAEQAYo3RRtM5lWLnwVuhXI
bDQzWeDC3+Fw6RzIoAGyLfUWmJnFBhVel0WsvG5kUHzwx0mbjfFJ/sB3YOHsPImn
XrujggTuMIIE6jAOBgNVHQ8BAf8EBAMCBPAwEwYDVR0gBAwwCjAIBgYqhQNkcQEw
SwYDVR0lBEQwQgYIKwYBBQUHAwIGByqFAwICIgYGCCsGAQUFBwMEBgcqhQMDBwgB
BggqhQMDBwEBAQYGKoUDAwcBBggqhQMDBwABAzAiBgNVHREEGzAZgRdtaWhhaWwu
c2h1bmtvQHlhbmRleC5ydTAdBgNVHQ4EFgQU/D3Zs2NDX2KLTWPiFAl6u5qFfpYw
ggGcBgNVHSMEggGTMIIBj4AUwpQH565Pv7nAWWiEiNAsfvAtQHOhggFjpIIBXzCC
AVsxGDAWBgUqhQNkARINMDAwMDAwMDAwMDAwMDEaMBgGCCqFAwOBAwEBEgwwMDAw
MDAwMDAwMDAxJDAiBgNVBAkMG9Cj0LvRjNGP0L3QvtCy0YHQutCw0Y8gMTPQsDEe
MBwGCSqGSIb3DQEJARYPY2FAc2tia29udHVyLnJ1MQswCQYDVQQGEwJSVTEzMDEG
A1UECAwqNjYg0KHQstC10YDQtNC70L7QstGB0LrQsNGPINC+0LHQu9Cw0YHRgtGM
MSEwHwYDVQQHDBjQldC60LDRgtC10YDQuNC90LHRg9GA0LMxKDAmBgNVBAoMH9CX
0JDQniDQn9CkINCh0JrQkSDQmtC+0L3RgtGD0YAxMDAuBgNVBAsMJ9Cj0LTQvtGB
0YLQvtCy0LXRgNGP0Y7RidC40Lkg0YbQtdC90YLRgDEcMBoGA1UEAxMTVUMgVGVz
dCAoUXVhbGlmaWVkKYIQVJ4AlkGFxItA0uUhz9irCTByBgNVHR8EazBpMDKgMKAu
hixodHRwOi8vY2RwLnNrYmtvbnR1ci5ydS9jZHAvdWMtdGVzdC02M2Z6LmNybDAz
oDGgL4YtaHR0cDovL2NkcDIuc2tia29udHVyLnJ1L2NkcC91Yy10ZXN0LTYzZnou
Y3JsMIGXBggrBgEFBQcBAQSBijCBhzBBBggrBgEFBQcwAoY1aHR0cDovL2NkcC5z
a2Jrb250dXIucnUvY2VydGlmaWNhdGVzL3VjLXRlc3QtNjNmei5jcnQwQgYIKwYB
BQUHMAKGNmh0dHA6Ly9jZHAyLnNrYmtvbnR1ci5ydS9jZXJ0aWZpY2F0ZXMvdWMt
dGVzdC02M2Z6LmNydDArBgNVHRAEJDAigA8yMDE2MDcxOTA4MDgwMFqBDzIwMTYx
MDE5MDgwODAwWjAjBgUqhQNkbwQaDBgi0JrRgNC40L/RgtC+0J/RgNC+IENTUCIw
ggExBgUqhQNkcASCASYwggEiDCsi0JrRgNC40L/RgtC+0J/RgNC+IENTUCIgKNCy
0LXRgNGB0LjRjyAzLjYpDFMi0KPQtNC+0YHRgtC+0LLQtdGA0Y/RjtGJ0LjQuSDR
htC10L3RgtGAICLQmtGA0LjQv9GC0L7Qn9GA0L4g0KPQpiIg0LLQtdGA0YHQuNC4
IDEuNQxOQ9C10YDRgtC40YTQuNC60LDRgiDRgdC+0L7RgtCy0LXRgtGB0YLQstC4
0Y8g4oSWINCh0KQvMTIxLTE4NTkg0L7RgiAxNy4wNi4yMDEyDE5D0LXRgNGC0LjR
hNC40LrQsNGCINGB0L7QvtGC0LLQtdGC0YHRgtCy0LjRjyDihJYg0KHQpC8xMjgt
MTgyMiDQvtGCIDAxLjA2LjIwMTIwCAYGKoUDAgIDA0EAMtqGzsNC1LXxBQxs5WeI
5w1gxUWNiKD2J6rbJkIvA1CCP41ExNZ6CFGHdZvp4rf+hDy7r98fURnQCSx4tkiG
gQ==";

	}
}