#!/bin/bash
# To release build: sh ./build.sh package "Configuration=Release"
kill `pgrep gulp` > /dev/null 2>&1
if test "$OS" = "Windows_NT"
then
  cmd /C build.cmd
else
  dotnet tool restore
  dotnet paket restore
  dotnet fake run build.fsx $@ "parallel-jobs=4" npmrestore
fi
