use logs;    
    CREATE TABLE logs.barcodeproductlogs (
		Id INT(10) UNSIGNED NOT NULL AUTO_INCREMENT,
		LogTime DATETIME NOT NULL,
		OperatorName VARCHAR(50) NOT NULL,
		OperatorHost VARCHAR(50) NOT NULL,
		Operation TINYINT(3) UNSIGNED NOT NULL,
		barcodeproductId INT(10) UNSIGNED NOT NULL,
		ProductId int unsigned,
		ProducerId int unsigned,
      Barcode VARCHAR(255),
	PRIMARY KEY (Id),
	INDEX LogTimeAndOperationIndex (LogTime, Operation)
);