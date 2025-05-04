@echo off
echo Creating development database from production...
sqlcmd -S 192.168.0.172 -U marc.coxall -P Donald640060! -i create_dev_database.sql
if %ERRORLEVEL% == 0 (
    echo Database copy completed successfully.
) else (
    echo Error copying database. Check the output above for details.
)
pause