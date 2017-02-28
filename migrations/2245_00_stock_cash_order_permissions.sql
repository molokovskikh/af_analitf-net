create temporary table tmpNull (Id int(2));
insert into tmpNull values (1);

insert into usersettings.userpermissions (Name, Shortcut, AvailableFor, `Type`, AssignDefaultValue, SecurityMask, OrderIndex)
	select 'Доступ к складским функциям', 'STCK', 1, 1, 0, null, 0
		from tmpNull
		where not exists(select * from usersettings.userpermissions where Shortcut = 'STCK');
insert into usersettings.userpermissions (Name, Shortcut, AvailableFor, `Type`, AssignDefaultValue, SecurityMask, OrderIndex)
	select 'Доступ к кассовым операциям', 'CASH', 1, 1, 0, null, 0
		from tmpNull
		where not exists(select * from usersettings.userpermissions where Shortcut = 'CASH');
insert into usersettings.userpermissions (Name, Shortcut, AvailableFor, `Type`, AssignDefaultValue, SecurityMask, OrderIndex)
	select 'Доступ к заказам', 'ORDR', 1, 1, 0, null, 0
		from tmpNull
		where not exists(select * from usersettings.userpermissions where Shortcut = 'ORDR');

drop table tmpNull;

insert into usersettings.assignedpermissions (UserId, PermissionId)
	select u.Id, up.Id
		from customers.users u
			cross join usersettings.userpermissions up
			left join usersettings.assignedpermissions ap on u.Id = ap.UserId and up.Id = ap.PermissionId
		where up.Shortcut in ('STCK', 'CASH', 'ORDR')
			and ap.UserId is null;
