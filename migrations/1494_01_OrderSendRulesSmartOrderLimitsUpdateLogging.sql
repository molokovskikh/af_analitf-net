CREATE TABLE  `logs`.`SmartOrderLimitLogs` (
  `Id` int unsigned NOT NULL AUTO_INCREMENT,
  `LogTime` datetime NOT NULL,
  `OperatorName` varchar(50) NOT NULL,
  `OperatorHost` varchar(50) NOT NULL,
  `Operation` tinyint(3) unsigned NOT NULL,
  `LimitId` int(10) unsigned not null,
  `Value` decimal(19,5),
  `SupplierId` int(10) unsigned,
  `AddressId` int(10) unsigned,

  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=cp1251;
DROP TRIGGER IF EXISTS OrderSendRules.SmartOrderLimitLogDelete;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER OrderSendRules.SmartOrderLimitLogDelete AFTER DELETE ON OrderSendRules.SmartOrderLimits
FOR EACH ROW BEGIN
	INSERT
	INTO `logs`.SmartOrderLimitLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 2,
		LimitId = OLD.Id,
		Value = OLD.Value,
		SupplierId = OLD.SupplierId,
		AddressId = OLD.AddressId;
END;
DROP TRIGGER IF EXISTS OrderSendRules.SmartOrderLimitLogUpdate;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER OrderSendRules.SmartOrderLimitLogUpdate AFTER UPDATE ON OrderSendRules.SmartOrderLimits
FOR EACH ROW BEGIN
	INSERT
	INTO `logs`.SmartOrderLimitLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 1,
		LimitId = OLD.Id,
		Value = NULLIF(NEW.Value, OLD.Value),
		SupplierId = NULLIF(NEW.SupplierId, OLD.SupplierId),
		AddressId = NULLIF(NEW.AddressId, OLD.AddressId);
END;
DROP TRIGGER IF EXISTS OrderSendRules.SmartOrderLimitLogInsert;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER OrderSendRules.SmartOrderLimitLogInsert AFTER INSERT ON OrderSendRules.SmartOrderLimits
FOR EACH ROW BEGIN
	INSERT
	INTO `logs`.SmartOrderLimitLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 0,
		LimitId = NEW.Id,
		Value = NEW.Value,
		SupplierId = NEW.SupplierId,
		AddressId = NEW.AddressId;
END;
