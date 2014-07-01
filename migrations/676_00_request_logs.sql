
    create table logs.RequestLogs (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       UserId INTEGER UNSIGNED,
       CreatedOn DATETIME,
       IsCompleted TINYINT(1),
       IsFaulted TINYINT(1),
       Error VARCHAR(255),
       primary key (Id)
    );
alter table logs.RequestLogs add index (UserId), add constraint FK_logs_RequestLogs_UserId foreign key (UserId) references Customers.Users (Id);
