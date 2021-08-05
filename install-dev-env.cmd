@echo off

rem   First install Chocolatey https://chocolatey.org/ with the command:

rem   Then install programs

IF "x%1" == "xinit" goto init
IF "x%1" == "xessentials" goto essentials
IF "x%1" == "xadditionals" goto additionals

echo  ---
echo      Usage: install-dev-env.cmd init
echo       then: install-dev-env.cmd essentials

goto end
:init

powershell -NoProfile -ExecutionPolicy Bypass -Command "iex ((new-object net.webclient).DownloadString('https://chocolatey.org/install.ps1'))" && SET PATH=%PATH%;%ALLUSERSPROFILE%\chocolatey\bin

rem show file extensions:
reg add HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced /v HideFileExt /t REG_DWORD /d 0 /f

echo  cinst installed
echo  Now run: install-dev-env.cmd essentials

goto end
:essentials
rem %ALLUSERSPROFILE%\chocolatey\bin\cinst chocolatey.config -y --pre
%ALLUSERSPROFILE%\chocolatey\bin\cinst chocolatey.config -y
%ProgramFiles%\nodejs\npm install -g npm
%ProgramFiles%\nodejs\npm install -g gulp jshint eslint
"%ProgramFiles%\Microsoft VS Code\Code" --install-extension Ionide.ionide-fsharp
echo  If some of these failed, try to run again. :-)
echo  If you want more programs run: install-dev-env.cmd additionals

:end
