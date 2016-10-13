create table logs.OrderRecordLogs (
       Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       WriteTime DATETIME not null,
       UserId INTEGER UNSIGNED,
       OrderId INTEGER UNSIGNED,
       ExportId INTEGER UNSIGNED not null,
	   RecordType TINYINT UNSIGNED not null,
       primary key (Id)
    );