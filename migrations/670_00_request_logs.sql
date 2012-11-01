
    create table logs.RequestLogs (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       UserId INTEGER UNSIGNED,
       CreatedOn DATETIME,
       IsReady TINYINT(1),
       IsBroken TINYINT(1),
       Error VARCHAR(255),
       primary key (Id)
    );
alter table logs.RequestLogs add index (UserId), add constraint FK_logs_RequestLogs_UserId foreign key (UserId) references Customers.Users (Id);
