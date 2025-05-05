@echo off
dotnet tool restore
dotnet paket restore
rem To release build: build.cmd package "Configuration=Release"
taskkill /fi "WINDOWTITLE eq gulp" > NUL 2> NUL
taskkill /im WebsitePlayground.exe > NUL 2> NUL
cls


IF "x%NUMBER_OF_PROCESSORS%" == "x" SET NUMBER_OF_PROCESSORS=4
rem dotnet fsi build.fsx %*
dotnet fake -v build --parallel %NUMBER_OF_PROCESSORS% -e npmrestore=1 -e DataSource=localhost %*
