use catalogs;
    create table catalogs.barcodeproducts (
      Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
		ProductId int unsigned,
		ProducerId int unsigned,
      Barcode VARCHAR(255),
		UpdateTime TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
      primary key (Id)
    );