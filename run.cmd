@echo off
taskkill /fi "WINDOWTITLE eq gulp" > NUL 2> NUL
taskkill /im WebsitePlayground.exe > NUL 2> NUL
start "WWW-Server" dotnet run --project backend
start "Javascript file monitor" gulp
