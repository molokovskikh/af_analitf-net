create temporary table tmpNull (Num int(2));
insert into tmpNull (Num) values (1);

insert into Customers.suppliers (Id, Name, FullName, NotifyWeekEnd)
	select (select max(Id) + 1 from Customers.suppliers), 'Собственный поставщик', 'Собственный поставщик', 0
		from tmpNull
		where not exists(select 1 from Customers.suppliers where Name = FullName and Name = 'Собственный поставщик');

drop table tmpNull;
