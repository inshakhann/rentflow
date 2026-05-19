@echo off
title RentFlow Server
color 0A

echo Starting RentFlow Property Portal...
echo ====================================

cd /d "%~dp0"
dotnet run --project RentFlow.Server

pause
