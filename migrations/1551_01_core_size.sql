alter table Farm.Core0
change column Note Note varchar(255) default null,
change column Doc Doc varchar(255) not null,
change column Series Series varchar(255) default null,
change column Unit Unit varchar(255) not null,
change column Volume Volume varchar(255) not null;
