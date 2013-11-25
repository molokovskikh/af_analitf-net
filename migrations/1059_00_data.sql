create table Customers.AnalitfNetDatas (
    UserId INTEGER UNSIGNED NOT NULL,
   LastUpdateAt DATETIME not null,
   primary key (UserId),
   constraint `FK_Customers_AnalitfNetDatas` foreign key (UserId)
     references Customers.Users(Id) on delete cascade
);
