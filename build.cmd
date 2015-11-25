@echo off
cls
.\.paket\paket.bootstrapper.exe --prefer-nuget
if errorlevel 1 (exit /b %errorlevel%)

.\.paket\paket.exe restore
if errorlevel 1 (exit /b %errorlevel%)

IF "x%NUMBER_OF_PROCESSORS%" == "x" SET NUMBER_OF_PROCESSORS=4
.\packages\Fake\tools\Fake.exe build.fsx %* "parallel-jobs=%NUMBER_OF_PROCESSORS%" npmrestore
