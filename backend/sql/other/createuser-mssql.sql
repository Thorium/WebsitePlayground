IF NOT EXISTS(SELECT principal_id FROM sys.database_principals WHERE name = 'webuser') BEGIN
    BEGIN TRY
        -- Replace with proper user and password secure enough
        -- Login failed? Check server authentication mode: https://stackoverflow.com/a/58329203
        CREATE LOGIN webuser WITH PASSWORD = N'p4ssw0rd';
    END TRY
    BEGIN CATCH
        SELECT 'Login webuser not created' as Msg,
            ERROR_NUMBER() AS ErrorNumber
           ,ERROR_MESSAGE() AS ErrorMessage;
    END CATCH

    BEGIN TRY
        CREATE USER webuser FOR LOGIN webuser
    END TRY
    BEGIN CATCH
        SELECT 'Login webuser not assigned' as Msg,
            ERROR_NUMBER() AS ErrorNumber
           ,ERROR_MESSAGE() AS ErrorMessage;
    END CATCH

    -- Replace with appropriate permissions
    EXEC sp_addrolemember 'db_datareader', 'webuser';
    EXEC sp_addrolemember 'db_datawriter', 'webuser';
    GRANT EXECUTE TO webuser
    GRANT CONNECT TO webuser
    GRANT UNMASK TO webuser
END

