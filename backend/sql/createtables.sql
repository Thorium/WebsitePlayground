DROP DATABASE companyweb;
CREATE DATABASE companyweb DEFAULT CHARACTER SET utf8 ;;

USE companyweb;

# table names: lower case for compability (Win+Mac/Linux)

CREATE TABLE company(
	Id int auto_increment primary key NOT NULL,
	Name nvarchar(255) NOT NULL,
	CEO nvarchar(255) NOT NULL,
	Founded datetime NOT NULL,
	Founder nvarchar(255) NULL,
	WebSite nvarchar(255) NULL,
    LogoUrl nvarchar(255) NULL,
    LastUpdate datetime NOT NULL);
