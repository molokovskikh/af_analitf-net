create table Logs.AcceptedOrderLogs (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       RequestId INTEGER UNSIGNED,
       OrderId INTEGER UNSIGNED,
       primary key (Id)
    );
alter table Logs.AcceptedOrderLogs add index (RequestId), add constraint FK_Logs_AcceptedOrderLogs_RequestId foreign key (RequestId) references Logs.RequestLogs (Id) on delete cascade;
alter table Logs.AcceptedOrderLogs add index (OrderId), add constraint FK_Logs_AcceptedOrderLogs_OrderId foreign key (OrderId) references Orders.OrdersHead (RowId) on delete cascade;
