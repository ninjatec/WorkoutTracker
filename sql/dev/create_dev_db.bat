@echo off
echo Creating development database from production...
set password=%DB_PASSWORD%
sqlcmd -S 192.168.0.172 -U marc.coxall -P %password% -i create_dev_database.sql
if %ERRORLEVEL% == 0 (
    echo Database copy completed successfully.
) else (
    echo Error copying database. Check the output above for details.
)
pause