DROP DATABASE IF EXISTS companyweb;
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

CREATE TABLE users(
    Id int auto_increment primary key NOT NULL,
    Email nvarchar(255) NOT NULL,
    PasswordHash nvarchar(500) NOT NULL,
    IsActive bit NOT NULL DEFAULT 1,
    FailedLoginAttempts int NOT NULL DEFAULT 0,
    LastFailedLogin datetime NULL,
    LockedUntil datetime NULL,
    CreatedAt datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
    LastLoginAt datetime NULL,
    UNIQUE KEY UQ_users_Email (Email)) COMMENT 'Users for the login system';
