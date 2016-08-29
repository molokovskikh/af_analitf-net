drop database if exists Inventory;
create database Inventory;
use Inventory;
    create table Checks (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
        ClientPrimaryKey INTEGER UNSIGNED NOT NULL,
		UserId integer unsigned not null,
       CheckType INTEGER default 0  not null,
       Date DATETIME default '0001-01-01'  not null,
       ChangeOpening DATETIME default '0001-01-01'  not null,
       Status INTEGER default 0  not null,
       Clerk VARCHAR(255),
       DepartmentId INTEGER UNSIGNED,
       KKM VARCHAR(255),
       PaymentType INTEGER default 0  not null,
       SaleType INTEGER default 0  not null,
       Discont INTEGER UNSIGNED default 0  not null,
       ChangeId INTEGER UNSIGNED default 0  not null,
       ChangeNumber INTEGER UNSIGNED default 0  not null,
       Cancelled TINYINT(1) default 0  not null,
       RetailSum DECIMAL(19,5) default 0  not null,
       DiscountSum DECIMAL(19,5) default 0  not null,
       SupplySum DECIMAL(19,5) default 0  not null,
       SaleCheck VARCHAR(255),
       DiscountCard VARCHAR(255),
       Recipe VARCHAR(255),
       Agent VARCHAR(255),
       Timestamp DATETIME,
       primary key (Id)
    );

    create table CheckLines (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
        ClientPrimaryKey INTEGER UNSIGNED NOT NULL,
       RetailCost DECIMAL(19,5) default 0  not null,
       Cost DECIMAL(19,5) default 0  not null,
       Quantity DECIMAL(19,5) default 0  not null,
       DiscontSum DECIMAL(19,5) default 0  not null,
       CheckId INTEGER UNSIGNED default 0  not null,
       ProductKind INTEGER UNSIGNED,
       Divider INTEGER UNSIGNED,
       MarkupSum DECIMAL(19,5) default 0  not null,
       NDSSum DECIMAL(19,5) default 0  not null,
       NPSum DECIMAL(19,5) default 0  not null,
       NDS INTEGER UNSIGNED,
       NP INTEGER UNSIGNED,
       PartyNumber DECIMAL(19,5) default 0  not null,
       Narcotic TINYINT(1) default 0  not null,
       Toxic TINYINT(1) default 0  not null,
       Combined TINYINT(1) default 0  not null,
       Other TINYINT(1) default 0  not null,
       Barcode VARCHAR(255),
       Product VARCHAR(255),
       ProductId INTEGER UNSIGNED,
       Producer VARCHAR(255),
       ProducerId INTEGER UNSIGNED,
       primary key (Id)
    );
