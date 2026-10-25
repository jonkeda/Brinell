@echo off
setlocal

rem Generates .gen.cs files from every .tpl.cs file under the Html and Blazor Controls folders
rem (Bruinell.Html and Bruinell.Blazor) using the Bruinell ControlObject generator.

set "SCRIPT_DIR=%~dp0"
set "CLI_PROJECT=%SCRIPT_DIR%..\Bruinell.Cli\Bruinell.Cli.csproj"
set "CLI_EXE=%SCRIPT_DIR%..\Bruinell.Cli\bin\Release\net10.0\bruinell.exe"
set "INPUT=%SCRIPT_DIR%..\..\srcnew\Bruinell.Html\Controls;%SCRIPT_DIR%..\..\srcnew\Bruinell.Blazor\Controls"

echo Building bruinell...
dotnet build "%CLI_PROJECT%" -c Release -v:minimal /nr:false
if errorlevel 1 (
    echo Build failed.
    exit /b 1
)

echo Running generator...
"%CLI_EXE%" controls generate --input "%INPUT%"
if errorlevel 1 (
    echo Generation failed.
    exit /b 1
)

echo Done. Generated output in: %INPUT%
endlocal
