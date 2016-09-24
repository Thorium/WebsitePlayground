@echo off

echo Installing Chockolatey package manager:
@powershell -NoProfile -ExecutionPolicy Bypass -Command "iex ((new-object net.webclient).DownloadString('https://chocolatey.org/install.ps1'))" && SET PATH=%PATH%;%ALLUSERSPROFILE%\chocolatey\bin

echo Installing 7zip

choco install 7zip.commandline -y

echo Installing .NET
choco install dotnet4.6 -y
choco install visualfsharptools -y

rem open http ports
netsh advfirewall firewall add rule name="Open Port HTTP" dir=in action=allow protocol=TCP localport=80
netsh advfirewall firewall add rule name="Open Port HTTPS" dir=in action=allow protocol=TCP localport=443

rem SSL
rem CERTUTIL -f -p password -importpfx "myCertificate.pfx"
