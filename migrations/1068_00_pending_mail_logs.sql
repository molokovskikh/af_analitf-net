
    create table Logs.PendingMailLogs (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       WriteTime DATETIME not null,
       SendLogId INTEGER UNSIGNED,
       UserId INTEGER UNSIGNED,
       primary key (Id)
    );
alter table Logs.PendingMailLogs add index (SendLogId), add constraint FK_Logs_PendingMailLogs_SendLogId foreign key (SendLogId) references Logs.MailSendLogs (Id) on delete cascade;
alter table Logs.PendingMailLogs add index (UserId), add constraint FK_Logs_PendingMailLogs_UserId foreign key (UserId) references Customers.Users (Id) on delete cascade;
