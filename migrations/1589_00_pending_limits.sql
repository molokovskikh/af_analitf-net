create table Logs.PendingLimitLogs (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       RequestId INTEGER UNSIGNED,
       LimitId INTEGER UNSIGNED,
       Value DECIMAL(19,5) default 0  not null,
       ToDay DECIMAL(19,5) default 0  not null,
       primary key (Id)
    );
alter table Logs.PendingLimitLogs add index (RequestId), add constraint FK_Logs_PendingLimitLogs_RequestId foreign key (RequestId) references Logs.RequestLogs (Id) on delete cascade;
alter table Logs.PendingLimitLogs add index (LimitId), add constraint FK_Logs_PendingLimitLogs_LimitId foreign key (LimitId) references OrderSendRules.SmartOrderLimits (Id) on delete cascade;
