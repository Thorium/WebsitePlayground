@echo off
cls
.\.paket\paket.bootstrapper.exe --prefer-nuget
if errorlevel 1 (exit /b %errorlevel%)

.\.paket\paket.exe restore
if errorlevel 1 (exit /b %errorlevel%)

.\packages\Fake\tools\Fake.exe build.fsx %* "parallel-jobs=4" npmrestore
