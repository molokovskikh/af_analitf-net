
    create table Logs.PendingDocLogs (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       SendLogId INTEGER UNSIGNED,
       UserId INTEGER UNSIGNED,
       primary key (Id)
    );
alter table Logs.PendingDocLogs add index (SendLogId), add constraint FK_Logs_PendingDocLogs_SendLogId foreign key (SendLogId) references Logs.DocumentSendLogs (Id) on delete cascade;
alter table Logs.PendingDocLogs add index (UserId), add constraint FK_Logs_PendingDocLogs_UserId foreign key (UserId) references Customers.Users (Id) on delete cascade;
