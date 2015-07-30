CREATE USER 'webuser'@'localhost' IDENTIFIED BY 'p4ssw0rd';
CREATE DATABASE companyweb;
GRANT ALL PRIVILEGES ON companyweb.* TO 'webuser'@'localhost';
FLUSH PRIVILEGES;