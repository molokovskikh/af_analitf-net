alter table Logs.PendingDocLogs add column WriteTime DATETIME;

    create table Logs.PendingOrderLogs (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       WriteTime DATETIME not null,
       UserId INTEGER UNSIGNED,
       OrderId INTEGER UNSIGNED,
       ExportId INTEGER UNSIGNED not null,
       primary key (Id)
    );
alter table Logs.PendingOrderLogs add index (UserId), add constraint FK_Logs_PendingOrderLogs_UserId foreign key (UserId) references Customers.Users (Id) on delete cascade;
alter table Logs.PendingOrderLogs add index (OrderId), add constraint FK_Logs_PendingOrderLogs_OrderId foreign key (OrderId) references Orders.OrdersHead (RowId) on delete cascade;
