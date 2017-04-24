use customers;

DROP PROCEDURE Customers.BaseGetPrices;

CREATE DEFINER=`RootDBMS`@`127.0.0.1` PROCEDURE `BaseGetPrices`(IN `UserIdParam` INT UNSIGNED, IN `AddressIdParam` INT UNSIGNED)
	LANGUAGE SQL
	NOT DETERMINISTIC
	CONTAINS SQL
	SQL SECURITY DEFINER
	COMMENT ''
BEGIN

set @currentDay = usersettings.CurrentDayOfWeek();
drop temporary table IF EXISTS Customers.BasePrices;
create temporary table
Customers.BasePrices
(
 FirmCode int Unsigned,
 PriceCode int Unsigned,
 CostCode int Unsigned,
 PriceSynonymCode int Unsigned,
 RegionCode BigInt Unsigned,
 DelayOfPayment decimal(5,3),
 DisabledByClient bool,
 Upcost decimal(7,5),
 Actual bool,
 CostType bool,
 PriceDate DateTime,
 ShowPriceName bool,
 PriceName VarChar(50),
 PositionCount int Unsigned,
 MinReq mediumint,
 ControlMinReq int Unsigned,
 AllowOrder bool,
 ShortName varchar(50),
 FirmCategory tinyint unsigned,
 MainFirm bool,
 Storage bool,
 VitallyImportantDelay decimal(5,3),
 SupplementDelay decimal(5,3),
 OtherDelay decimal(5,3),
 index (PriceCode),
 index (RegionCode)
)engine = MEMORY;

INSERT
INTO    Customers.BasePrices
select distinct Pd.firmcode,
        i.PriceId,
        if(r.InvisibleOnFirm = 0, i.CostId, ifnull(prd.BaseCost, pc.CostCode)),
        ifnull(pd.ParentSynonym, pd.pricecode) PriceSynonymCode,
        i.RegionId,
        0 as DelayOfPayment,
        if(up.PriceId is null, 1, 0),
        round((1 + pd.UpCost / 100) * (1 + prd.UpCost / 100) * (1 + i.PriceMarkup / 100), 5),
        (to_seconds(now()) - to_seconds(pi.PriceDate)) < (f.maxold * 86400),
        pd.CostType,
        pi.PriceDate,
        r.ShowPriceName,
        pd.PriceName,
        pi.RowCount,
        if(ai.Id is not null, if (ai.MinReq > 0, ai.MinReq, prd.MinReq), prd.MinReq),
        if(ai.Id is not null, if (ai.ControlMinReq, 1, 0), 0),
        (r.OrderRegionMask & i.RegionId & u.OrderRegionMask) > 0,
        supplier.Name as ShortName,
        si.SupplierCategory,
        si.SupplierCategory >= r.BaseFirmCategory,
        Storage,
        dop.VitallyImportantDelay,
        dop.SupplementDelay,
        dop.OtherDelay
from customers.Users u
  join Customers.Addresses adr on adr.Id = AddressIdParam
  join Customers.Intersection i on i.ClientId = u.ClientId and i.LegalEntityId = Adr.LegalEntityId
  join Customers.AddressIntersection ai ON ai.IntersectionId = i.Id AND ai.addressid = adr.Id
  join Customers.Clients drugstore ON drugstore.Id = i.ClientId
  join usersettings.RetClientsSet r ON r.clientcode = drugstore.Id
  join usersettings.PricesData pd ON pd.pricecode = i.PriceId
    join usersettings.SupplierIntersection si on si.SupplierId = pd.FirmCode and i.ClientId = si.ClientId
    join usersettings.PriceIntersections pinter on pinter.SupplierIntersectionId = si.Id and pinter.PriceId = pd.PriceCode
    join usersettings.DelayOfPayments dop on dop.PriceIntersectionId = pinter.Id and dop.DayOfWeek = @currentDay
  JOIN usersettings.PricesCosts pc on pc.PriceCode = i.PriceId and exists(select * from userSettings.pricesregionaldata prdd where prdd.PriceCode = pd.PriceCode and prdd.BaseCost=pc.CostCode)
    join usersettings.PriceItems pi on pi.Id = pc.PriceItemId
    join farm.FormRules f on f.Id = pi.FormRuleId
    join Customers.Suppliers supplier ON supplier.Id = pd.firmcode
    join usersettings.PricesRegionalData prd ON prd.regioncode = i.RegionId AND prd.pricecode = pd.pricecode
    join usersettings.RegionalData rd ON rd.RegionCode = i.RegionId AND rd.FirmCode = pd.firmcode
  left join Customers.UserPrices up on up.PriceId = i.PriceId and up.UserId = ifnull(u.InheritPricesFrom, u.Id) and up.RegionId = i.RegionId
where   supplier.Disabled = 0
    AND (supplier.RegionMask & i.RegionId) > 0
    and (drugstore.maskregion & i.RegionId & u.WorkRegionMask) > 0
    and (r.WorkRegionMask & i.RegionId) > 0
    and pd.agencyenabled = 1
    and pd.enabled = 1
    and pd.pricetype <> 1
    and prd.enabled = 1
    and if(not r.ServiceClient, supplier.Id != 234, 1)
    AND i.AvailableForClient = 1
	AND i.AgencyEnabled = 1
    and u.Id = UserIdParam
group by PriceId, RegionId;

END;

DROP PROCEDURE Customers.GetActivePrices;

CREATE DEFINER=`RootDBMS`@`127.0.0.1` PROCEDURE `GetActivePrices`(IN `UserIdParam` INT UNSIGNED)
	LANGUAGE SQL
	NOT DETERMINISTIC
	CONTAINS SQL
	SQL SECURITY DEFINER
	COMMENT ''
BEGIN

Declare TabelExsists Bool DEFAULT false;
DECLARE CONTINUE HANDLER FOR 1146
begin
  Call Customers.GetPrices(UserIdParam);
end;

if not TabelExsists then
DROP TEMPORARY TABLE IF EXISTS Usersettings.ActivePrices;
create temporary table
Usersettings.ActivePrices
(
 FirmCode int Unsigned,
 PriceCode int Unsigned,
 CostCode int Unsigned,
 PriceSynonymCode int Unsigned,
 RegionCode BigInt Unsigned,
 Fresh bool,
 DelayOfPayment decimal(5,3),
 Upcost decimal(7,5),
 MaxSynonymCode Int Unsigned,
 MaxSynonymFirmCrCode Int Unsigned,
 CostType bool,
 PriceDate DateTime,
 ShowPriceName bool,
 PriceName VarChar(50),
 PositionCount int Unsigned,
 MinReq mediumint Unsigned,
 FirmCategory tinyint unsigned,
 MainFirm bool,
 VitallyImportantDelay decimal(5,3),
 SupplementDelay decimal(5,3),
 OtherDelay decimal(5,3),
 unique (PriceCode, RegionCode, CostCode),
 index  (CostCode, PriceCode),
 index  (PriceSynonymCode),
 index  (MaxSynonymCode),
 index  (PriceCode),
 index  (MaxSynonymFirmCrCode)
 )engine=MEMORY
 ;
set TabelExsists=true;
end if;

select null from Usersettings.Prices limit 0;

INSERT
INTO Usersettings.ActivePrices(
 FirmCode,
 PriceCode,
 CostCode,
 PriceSynonymCode,
 RegionCode,
 Fresh,
 DelayOfPayment,
 Upcost,
 MaxSynonymCode,
 MaxSynonymFirmCrCode,
 CostType,
 PriceDate,
 ShowPriceName,
 PriceName,
 PositionCount,
 MinReq,
 FirmCategory,
 MainFirm,
 VitallyImportantDelay,
 SupplementDelay,
 OtherDelay
)
SELECT P.FirmCode,
       P.PriceCode,
       P.CostCode,
       P.PriceSynonymCode,
       P.RegionCode,
       1,
       p.DelayOfPayment,
       P.Upcost,
       0,
       0,
       P.CostType,
       P.PriceDate,
       P.ShowPriceName,
       P.PriceName,
       P.PositionCount,
       P.MinReq,
       P.FirmCategory,
       P.MainFirm,
       P.VitallyImportantDelay,
       P.SupplementDelay,
       P.OtherDelay
FROM Usersettings.Prices P
WHERE p.DisabledByClient = 0
  and p.Actual = 1;

drop temporary table IF EXISTS Usersettings.Prices;

END;

DROP PROCEDURE Customers.GetActivePricesForAddress;

CREATE DEFINER=`RootDBMS`@`127.0.0.1` PROCEDURE `GetActivePricesForAddress`(IN `UserIdParam` INT UNSIGNED, IN `AddressIdParam` INT UNSIGNED)
	LANGUAGE SQL
	NOT DETERMINISTIC
	CONTAINS SQL
	SQL SECURITY DEFINER
	COMMENT ''
BEGIN

Declare TabelExsists Bool DEFAULT false;
DECLARE CONTINUE HANDLER FOR 1146
begin
  Call Customers.GetPricesForAddress(UserIdParam, AddressIdParam);
end;

if not TabelExsists then
DROP TEMPORARY TABLE IF EXISTS Usersettings.ActivePrices;
create temporary table
Usersettings.ActivePrices
(
 FirmCode int Unsigned,
 PriceCode int Unsigned,
 CostCode int Unsigned,
 PriceSynonymCode int Unsigned,
 RegionCode BigInt Unsigned,
 Fresh bool,
 DelayOfPayment decimal(5,3),
 Upcost decimal(7,5),
 MaxSynonymCode Int Unsigned,
 MaxSynonymFirmCrCode Int Unsigned,
 CostType bool,
 PriceDate DateTime,
 ShowPriceName bool,
 PriceName VarChar(50),
 PositionCount int Unsigned,
 MinReq mediumint Unsigned,
 FirmCategory tinyint unsigned,
 MainFirm bool,
 VitallyImportantDelay decimal(5,3),
 SupplementDelay decimal(5,3),
 OtherDelay decimal(5,3),
 unique (PriceCode, RegionCode, CostCode),
 index  (CostCode, PriceCode),
 index  (PriceSynonymCode),
 index  (MaxSynonymCode),
 index  (PriceCode),
 index  (MaxSynonymFirmCrCode)
 )engine=MEMORY
 ;
set TabelExsists=true;
end if;

select null from Usersettings.Prices limit 0;

INSERT
INTO Usersettings.ActivePrices(
 FirmCode,
 PriceCode,
 CostCode,
 PriceSynonymCode,
 RegionCode,
 Fresh,
 DelayOfPayment,
 Upcost,
 MaxSynonymCode,
 MaxSynonymFirmCrCode,
 CostType,
 PriceDate,
 ShowPriceName,
 PriceName,
 PositionCount,
 MinReq,
 FirmCategory,
 MainFirm,
 VitallyImportantDelay,
 SupplementDelay,
 OtherDelay
)
SELECT P.FirmCode,
       P.PriceCode,
       P.CostCode,
       P.PriceSynonymCode,
       P.RegionCode,
       1,
       p.DelayOfPayment,
       P.Upcost,
       0,
       0,
       P.CostType,
       P.PriceDate,
       P.ShowPriceName,
       P.PriceName,
       P.PositionCount,
       P.MinReq,
       P.FirmCategory,
       P.MainFirm,
       P.VitallyImportantDelay,
       P.SupplementDelay,
       P.OtherDelay
FROM Usersettings.Prices P
WHERE p.DisabledByClient = 0
  and p.Actual = 1;

drop temporary table IF EXISTS Usersettings.Prices;

END;

DROP PROCEDURE Customers.GetPrices;

CREATE DEFINER=`RootDBMS`@`127.0.0.1` PROCEDURE `GetPrices`(IN `UserIdParam` INT UNSIGNED)
	LANGUAGE SQL
	NOT DETERMINISTIC
	CONTAINS SQL
	SQL SECURITY DEFINER
	COMMENT ''
BEGIN

set @currentDay = usersettings.CurrentDayOfWeek();
drop temporary table IF EXISTS Usersettings.Prices;
create temporary table
Usersettings.Prices
(
 FirmCode int Unsigned,
 PriceCode int Unsigned,
 CostCode int Unsigned,
 PriceSynonymCode int Unsigned,
 RegionCode BigInt Unsigned,
 DelayOfPayment decimal(5,3),
 DisabledByClient bool,
 Upcost decimal(7,5),
 Actual bool,
 CostType bool,
 PriceDate DateTime,
 ShowPriceName bool,
 PriceName VarChar(50),
 PositionCount int Unsigned,
 MinReq mediumint Unsigned,
 ControlMinReq bool,
 AllowOrder bool,
 ShortName varchar(50),
 FirmCategory tinyint unsigned,
 MainFirm bool,
 Storage bool,
 VitallyImportantDelay decimal(5,3),
 SupplementDelay decimal(5,3),
 OtherDelay decimal(5,3),
 index (PriceCode),
 index (RegionCode)
)engine = MEMORY;

INSERT
INTO    Usersettings.Prices
SELECT distinct pd.firmcode,
        i.PriceId,
        if(r.InvisibleOnFirm = 0, i.CostId, ifnull(prd.BaseCost, pc.CostCode)),
        ifnull(pd.ParentSynonym, pd.pricecode) PriceSynonymCode,
        i.RegionId,
        0 as DelayOfPayment,
        if(up.PriceId is null, 1, 0),
        round((1 + pd.UpCost / 100) * (1 + prd.UpCost / 100) * (1 + i.PriceMarkup / 100), 5),
        (to_seconds(now()) - to_seconds(pi.PriceDate)) < (f.maxold * 86400),
        pd.CostType,
        pi.PriceDate,
        r.ShowPriceName,
        pd.PriceName,
        pi.RowCount,
        prd.MinReq,
        0,
        (r.OrderRegionMask & i.RegionId & u.OrderRegionMask) > 0,
        supplier.Name as ShortName,
        si.SupplierCategory,
        si.SupplierCategory >= r.BaseFirmCategory,
        Storage,
        dop.VitallyImportantDelay,
        dop.SupplementDelay,
        dop.OtherDelay
FROM Customers.Users u
  join Customers.Intersection i on i.ClientId = u.ClientId and i.AgencyEnabled = 1
  JOIN Customers.Clients drugstore ON drugstore.Id = i.ClientId
  JOIN usersettings.RetClientsSet r ON r.clientcode = drugstore.Id
  JOIN usersettings.PricesData pd ON pd.pricecode = i.PriceId
    join usersettings.SupplierIntersection si on si.SupplierId = pd.FirmCode and i.ClientId = si.ClientId
    join usersettings.PriceIntersections pinter on pinter.SupplierIntersectionId = si.Id and pinter.PriceId = pd.PriceCode
    join usersettings.DelayOfPayments dop on dop.PriceIntersectionId = pinter.Id and dop.DayOfWeek = @currentDay
  JOIN usersettings.PricesCosts pc on pc.PriceCode = i.PriceId and exists(select * from userSettings.pricesregionaldata prdd where prdd.PriceCode = pd.PriceCode and prdd.BaseCost=pc.CostCode)
    JOIN usersettings.PriceItems pi on pi.Id = pc.PriceItemId
    JOIN farm.FormRules f on f.Id = pi.FormRuleId
    JOIN Customers.Suppliers supplier ON supplier.Id = pd.firmcode
    JOIN usersettings.PricesRegionalData prd ON prd.regioncode = i.RegionId AND prd.pricecode = pd.pricecode
    JOIN usersettings.RegionalData rd ON rd.RegionCode = i.RegionId AND rd.FirmCode = pd.firmcode
  left join Customers.UserPrices up on up.PriceId = i.PriceId and up.UserId = ifnull(u.InheritPricesFrom, u.Id) and up.RegionId = i.RegionId
WHERE   supplier.Disabled = 0
    and (supplier.RegionMask & i.RegionId) > 0
    AND (drugstore.maskregion & i.RegionId & u.WorkRegionMask) > 0
    AND (r.WorkRegionMask & i.RegionId) > 0
    AND pd.agencyenabled = 1
    AND pd.enabled = 1
    AND pd.pricetype <> 1
    AND prd.enabled = 1
    AND if(not r.ServiceClient, supplier.Id != 234, 1)
    and i.AvailableForClient = 1
    AND u.Id = UserIdParam
group by PriceId, RegionId;

END;

DROP PROCEDURE Customers.GetPricesForAddress;

CREATE DEFINER=`RootDBMS`@`127.0.0.1` PROCEDURE `GetPricesForAddress`(IN `UserIdParam` INT UNSIGNED, IN `AddressIdParam` INT UNSIGNED)
	LANGUAGE SQL
	NOT DETERMINISTIC
	CONTAINS SQL
	SQL SECURITY DEFINER
	COMMENT ''
BEGIN

CALL Customers.BaseGetPrices(UserIdParam, AddressIdParam);

drop temporary table IF EXISTS Usersettings.Prices;
create temporary table
Usersettings.Prices
(
 FirmCode int Unsigned,
 PriceCode int Unsigned,
 CostCode int Unsigned,
 PriceSynonymCode int Unsigned,
 RegionCode BigInt Unsigned,
 DelayOfPayment decimal(5,3),
 DisabledByClient bool,
 Upcost decimal(7,5),
 Actual bool,
 CostType bool,
 PriceDate DateTime,
 ShowPriceName bool,
 PriceName VarChar(50),
 PositionCount int Unsigned,
 MinReq mediumint Unsigned,
 ControlMinReq bool,
 AllowOrder bool,
 ShortName varchar(50),
 FirmCategory tinyint unsigned,
 MainFirm bool,
 Storage bool,
 VitallyImportantDelay decimal(5,3),
 SupplementDelay decimal(5,3),
 OtherDelay decimal(5,3),
 index (PriceCode),
 index (RegionCode)
)engine = MEMORY;

INSERT
INTO    Usersettings.Prices(
  FirmCode,
  PriceCode,
  CostCode,
  PriceSynonymCode,
  RegionCode,
  DelayOfPayment,
  DisabledByClient,
  Upcost,
  Actual,
  CostType,
  PriceDate,
  ShowPriceName,
  PriceName,
  PositionCount,
  MinReq,
  ControlMinReq,
  AllowOrder,
  ShortName,
  FirmCategory,
  MainFirm,
  Storage,
  VitallyImportantDelay,
  SupplementDelay,
  OtherDelay
)
SELECT
  FirmCode,
  PriceCode,
  CostCode,
  PriceSynonymCode,
  RegionCode,
  DelayOfPayment,
  DisabledByClient,
  Upcost,
  Actual,
  CostType,
  PriceDate,
  ShowPriceName,
  PriceName,
  PositionCount,
  MinReq,
  if(ControlMinReq = 1, true, false),
  AllowOrder,
  ShortName,
  FirmCategory,
  MainFirm,
  Storage,
  VitallyImportantDelay,
  SupplementDelay,
  OtherDelay
FROM Customers.BasePrices;

DROP TEMPORARY TABLE IF EXISTS Customers.BasePrices;

END;

DROP PROCEDURE Customers.GetPricesForClient;

CREATE DEFINER=`RootDBMS`@`127.0.0.1` PROCEDURE `GetPricesForClient`(IN `ClientIdParam` INT UNSIGNED)
	LANGUAGE SQL
	NOT DETERMINISTIC
	CONTAINS SQL
	SQL SECURITY DEFINER
	COMMENT ''
BEGIN

set @currentDay = usersettings.CurrentDayOfWeek();
drop temporary table IF EXISTS Usersettings.PricesForClient;
create temporary table
Usersettings.PricesForClient
(
 FirmCode int Unsigned,
 PriceCode int Unsigned,
 CostCode int Unsigned,
 PriceSynonymCode int Unsigned,
 RegionCode BigInt Unsigned,
 DelayOfPayment decimal(5,3),
 DisabledByClient bool,
 Upcost decimal(7,5),
 Actual bool,
 CostType bool,
 PriceDate DateTime,
 ShowPriceName bool,
 PriceName VarChar(50),
 PositionCount int Unsigned,
 MinReq mediumint Unsigned,
 ControlMinReq bool,
 AllowOrder bool,
 ShortName varchar(50),
 FirmCategory tinyint unsigned,
 MainFirm bool,
 Storage bool,
 VitallyImportantDelay decimal(5,3),
 SupplementDelay decimal(5,3),
 OtherDelay decimal(5,3),
 index (PriceCode),
 index (RegionCode)
)engine = MEMORY;

INSERT
INTO    Usersettings.PricesForClient
SELECT distinct pd.firmcode,
        i.PriceId,
        if(r.InvisibleOnFirm = 0, i.CostId, ifnull(prd.BaseCost, pc.CostCode)),
        ifnull(pd.ParentSynonym, pd.pricecode) PriceSynonymCode,
        i.RegionId,
        0 as DelayOfPayment,
        0,
        round((1 + pd.UpCost / 100) * (1 + prd.UpCost / 100) * (1 + i.PriceMarkup / 100), 5),
        (to_seconds(now()) - to_seconds(pi.PriceDate)) < (f.maxold * 86400),
        pd.CostType,
        pi.PriceDate,
        r.ShowPriceName,
        pd.PriceName,
        pi.RowCount,
        prd.MinReq,
        0,
        (r.OrderRegionMask & i.RegionId) > 0,
        supplier.Name as ShortName,
        si.SupplierCategory,
        si.SupplierCategory >= r.BaseFirmCategory,
        Storage,
        dop.VitallyImportantDelay,
        dop.SupplementDelay,
        dop.OtherDelay
FROM Customers.Clients drugstore
  JOIN usersettings.RetClientsSet r ON r.clientcode = drugstore.Id
  join Customers.Intersection i on i.ClientId = drugstore.Id and i.AgencyEnabled = 1
  JOIN usersettings.PricesData pd ON pd.pricecode = i.PriceId
    join usersettings.SupplierIntersection si on si.SupplierId = pd.FirmCode and i.ClientId = si.ClientId
    join usersettings.PriceIntersections pinter on pinter.SupplierIntersectionId = si.Id and pinter.PriceId = pd.PriceCode
    join usersettings.DelayOfPayments dop on dop.PriceIntersectionId = pinter.Id and dop.DayOfWeek = @currentDay
  JOIN usersettings.PricesCosts pc on pc.PriceCode = i.PriceId and exists(select * from userSettings.pricesregionaldata prdd where prdd.PriceCode = pd.PriceCode and prdd.BaseCost=pc.CostCode)
    JOIN usersettings.PriceItems pi on pi.Id = pc.PriceItemId
    JOIN farm.FormRules f on f.Id = pi.FormRuleId
    JOIN Customers.Suppliers supplier ON supplier.Id = pd.firmcode
    JOIN usersettings.PricesRegionalData prd ON prd.regioncode = i.RegionId AND prd.pricecode = pd.pricecode
    JOIN usersettings.RegionalData rd ON rd.RegionCode = i.RegionId AND rd.FirmCode = pd.firmcode
WHERE   supplier.Disabled = 0
    and (supplier.RegionMask & i.RegionId) > 0
    AND (drugstore.maskregion & i.RegionId) > 0
    AND (r.WorkRegionMask & i.RegionId) > 0
    AND pd.agencyenabled = 1
    AND pd.enabled = 1
    AND pd.pricetype <> 1
    AND prd.enabled = 1
    AND if(not r.ServiceClient, supplier.Id != 234, 1)
    AND drugstore.Id = ClientIdParam
group by PriceId, RegionId;

END;

DROP PROCEDURE Customers.GetPricesWithBaseCosts;

CREATE DEFINER=`RootDBMS`@`127.0.0.1` PROCEDURE `GetPricesWithBaseCosts`()
	LANGUAGE SQL
	NOT DETERMINISTIC
	CONTAINS SQL
	SQL SECURITY DEFINER
	COMMENT ''
BEGIN

drop temporary table IF EXISTS Usersettings.Prices;
create temporary table
Usersettings.Prices
(
 FirmCode int Unsigned,
 PriceCode int Unsigned,
 CostCode int Unsigned,
 PriceSynonymCode int Unsigned,
 RegionCode BigInt Unsigned,
 DelayOfPayment decimal(5,3),
 DisabledByClient bool,
 Upcost decimal(7,5),
 Actual bool,
 CostType bool,
 PriceDate DateTime,
 ShowPriceName bool,
 PriceName VarChar(50),
 PositionCount int Unsigned,
 MinReq mediumint Unsigned,
 ControlMinReq bool,
 AllowOrder bool,
 ShortName varchar(50),
 FirmCategory tinyint unsigned,
 MainFirm bool,
 Storage bool,
 VitallyImportantDelay decimal(5,3),
 SupplementDelay decimal(5,3),
 OtherDelay decimal(5,3),
 index (PriceCode),
 index (RegionCode)
)engine = MEMORY;

INSERT
INTO    Usersettings.Prices
SELECT distinct
    pd.firmcode,
    pd.PriceCode as PriceId,
    pc.CostCode,
    ifnull(pd.ParentSynonym, pd.pricecode) PriceSynonymCode,
    prd.RegionCode as RegionId,
    0 as DelayOfPayment,
    0,
    round((1 + pd.UpCost / 100) * (1 + prd.UpCost / 100), 5),
    (to_seconds(now()) - to_seconds(pi.PriceDate)) < (f.maxold * 86400),
    pd.CostType,
    pi.PriceDate,
    1,
    pd.PriceName,
    pi.RowCount,
    prd.MinReq,
    0,
    0,
    supplier.Name as ShortName,
    0,
    0,
    Storage,
    0,
    0,
    0
FROM
    usersettings.TmpPricesRegions TPR
    JOIN usersettings.PricesData pd ON TPR.PriceCode = pd.PriceCode
    JOIN usersettings.PricesCosts pc on pc.PriceCode = pd.PriceCode and exists(select * from userSettings.pricesregionaldata prdd where prdd.PriceCode = pd.PriceCode and prdd.BaseCost=pc.CostCode)
    JOIN usersettings.PriceItems pi on pi.Id = pc.PriceItemId
    JOIN farm.FormRules f on f.Id = pi.FormRuleId
    JOIN Customers.Suppliers supplier ON supplier.Id = pd.firmcode
    JOIN usersettings.PricesRegionalData prd ON prd.pricecode = pd.pricecode AND prd.RegionCode = TPR.RegionCode
    JOIN usersettings.RegionalData rd ON  rd.RegionCode = prd.RegionCode and rd.FirmCode = pd.firmcode
WHERE
    supplier.Disabled = 0
    and (supplier.RegionMask & prd.RegionCode) > 0
    AND pd.agencyenabled = 1
    AND pd.enabled = 1
    AND pd.pricetype <> 1
    AND prd.enabled = 1
group by PriceId, RegionId;

END;

DROP PROCEDURE Customers.GetPricesWithoutMinREq;

CREATE DEFINER=`RootDBMS`@`127.0.0.1` PROCEDURE `GetPricesWithoutMinREq`(IN `UserIdParam` INT UNSIGNED)
	LANGUAGE SQL
	NOT DETERMINISTIC
	CONTAINS SQL
	SQL SECURITY DEFINER
	COMMENT ''
BEGIN

CALL Customers.BaseGetPrices(UserIdParam, 0);

drop temporary table IF EXISTS Usersettings.Prices;
create temporary table
Usersettings.Prices
(
 FirmCode int Unsigned,
 PriceCode int Unsigned,
 CostCode int Unsigned,
 PriceSynonymCode int Unsigned,
 RegionCode BigInt Unsigned,
 DelayOfPayment decimal(5,3),
 DisabledByClient bool,
 Upcost decimal(7,5),
 Actual bool,
 CostType bool,
 PriceDate DateTime,
 ShowPriceName bool,
 PriceName VarChar(50),
 PositionCount int Unsigned,
 AllowOrder bool,
 ShortName varchar(50),
 FirmCategory tinyint unsigned,
 MainFirm bool,
 Storage bool,
 VitallyImportantDelay decimal(5,3),
 SupplementDelay decimal(5,3),
 OtherDelay decimal(5,3),
 index (PriceCode),
 index (RegionCode)
)engine = MEMORY;

INSERT INTO Usersettings.Prices (
  FirmCode,
  PriceCode,
  CostCode,
  PriceSynonymCode,
  RegionCode,
  DelayOfPayment,
  DisabledByClient,
  Upcost,
  Actual,
  CostType,
  PriceDate,
  ShowPriceName,
  PriceName,
  PositionCount,
  AllowOrder,
  ShortName,
  FirmCategory,
  MainFirm,
  Storage,
  VitallyImportantDelay,
  SupplementDelay,
  OtherDelay
)
SELECT
  FirmCode,
  PriceCode,
  CostCode,
  PriceSynonymCode,
  RegionCode,
  DelayOfPayment,
  DisabledByClient,
  Upcost,
  Actual,
  CostType,
  PriceDate,
  ShowPriceName,
  PriceName,
  PositionCount,
  AllowOrder,
  ShortName,
  FirmCategory,
  MainFirm,
  Storage,
  VitallyImportantDelay,
  SupplementDelay,
  OtherDelay
FROM
  Customers.BasePrices;

DROP TEMPORARY TABLE IF EXISTS Customers.BasePrices;

END;
