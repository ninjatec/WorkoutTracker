# PowerShell script to create development database copy
$ErrorActionPreference = 'Stop'

Write-Host "Creating development database from production..." -ForegroundColor Cyan

try {
    # Retrieve password from environment variable
    $password = $env:DB_PASSWORD

    # Run the SQL script using sqlcmd
    sqlcmd -S 192.168.0.172 -U marc.coxall -P $password -i create_dev_database.sql
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Database copy completed successfully." -ForegroundColor Green
    } else {
        Write-Host "Error copying database. Exit code: $LASTEXITCODE" -ForegroundColor Red
    }
} catch {
    Write-Host "An error occurred: $_" -ForegroundColor Red
}

Write-Host "Press any key to continue..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")