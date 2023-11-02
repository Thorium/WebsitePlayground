CREATE USER IF NOT EXISTS 'webuser'@'localhost' IDENTIFIED BY 'p4ssw0rd';
CREATE DATABASE IF NOT EXISTS companyweb;
GRANT ALL PRIVILEGES ON companyweb.* TO 'webuser'@'localhost';
FLUSH PRIVILEGES;