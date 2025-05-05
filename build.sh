#!/bin/bash
# To release build: sh ./build.sh package "Configuration=Release"
kill `pgrep gulp` > /dev/null 2>&1

dotnet tool restore
exit_code=$?
if [ $exit_code -ne 0 ]; then
exit $exit_code
fi

dotnet paket restore
exit_code=$?
if [ $exit_code -ne 0 ]; then
exit $exit_code
fi

#dotnet fsi $@ --fsiargs -d:MONO build.fsx "parallel-jobs=4" npmrestore DataSource=localhost
dotnet fake -v build --parallel 4 -e MONO=1 -e npmrestore=1 -e DataSource=localhost $@
