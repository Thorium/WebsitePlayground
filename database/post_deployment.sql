/*
Post-Deployment Script Template
--------------------------------------------------------------------------------------
 This file contains SQL statements that will be appended to the build script.
 Use SQLCMD syntax to include a file in the post-deployment script.
 Example:      :r .\myfile.sql
 Use SQLCMD syntax to reference a variable in the post-deployment script.
 Example:      :setvar TableName MyTable
               SELECT * FROM [$(TableName)]
--------------------------------------------------------------------------------------
*/

print 'Adding users and permissions'
:r .\createuser-mssql.sql

IF ('$(DeployDemoData)' = 'True') and (NOT EXISTS(SELECT TOP 1 * FROM Companyweb.Company where Name = 'My New Company'))
   and (select case when count(1) > 1000 then 'hasData' else 'isEmpty' end from Companyweb.Company) = 'isEmpty'
BEGIN
    print 'Inserting demo-data'
    :r .\createdemodata-mssql.sql
END
