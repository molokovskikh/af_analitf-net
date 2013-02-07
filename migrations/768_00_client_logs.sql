
    create table Logs.ClientAppLogs (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       CreatedOn DATETIME not null,
       UserId INTEGER UNSIGNED,
       Text TEXT,
       primary key (Id)
    );
alter table Logs.ClientAppLogs add index (UserId), add constraint FK_Logs_ClientAppLogs_UserId foreign key (UserId) references Customers.Users (Id)
on delete cascade;
