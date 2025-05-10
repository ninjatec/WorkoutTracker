#!/bin/bash
# Shell script to create development database copy

echo "Creating development database from production..."

# Use environment variable for password
password="$DB_PASSWORD"

# Run the SQL script using sqlcmd
sqlcmd -S 192.168.0.172 -U marc.coxall -P "$password" -i create_dev_database.sql

if [ $? -eq 0 ]; then
    echo "Database copy completed successfully."
else
    echo "Error copying database. Check the output above for details."
    exit 1
fi