DROP DATABASE companyweb;
CREATE DATABASE companyweb DEFAULT CHARACTER SET utf8 ;;

USE companyweb;

# table names: lower case for compability (Win+Mac/Linux)

CREATE TABLE company(
	Id int auto_increment primary key NOT NULL,
	Name nvarchar(255) NOT NULL,
	CEO nvarchar(255) NOT NULL COMMENT 'CEO is a person, Chief executive officer of the company',
	Founded datetime NOT NULL,
	Founder nvarchar(255) NULL,
	WebSite nvarchar(255) NULL COMMENT 'This is used as a link, so do include the protocol, http:// to the address.',
    LogoUrl nvarchar(255) NULL,
    LastUpdate datetime NOT NULL) COMMENT 'Company table is a list of all the organisations we have';
