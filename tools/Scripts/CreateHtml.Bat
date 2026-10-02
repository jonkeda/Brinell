@echo off
setlocal

rem Generates .gen.cs files from every .tpl.cs file under the Html and Blazor Controls folders
rem (Brinell.Html and Brinell.Blazor) using the Brinell ControlObject generator.

set "SCRIPT_DIR=%~dp0"
set "CLI_PROJECT=%SCRIPT_DIR%..\Brinell.Cli\Brinell.Cli.csproj"
set "CLI_EXE=%SCRIPT_DIR%..\Brinell.Cli\bin\Release\net10.0\brinell.exe"
set "INPUT=%SCRIPT_DIR%..\..\srcnew\Brinell.Html\Controls;%SCRIPT_DIR%..\..\srcnew\Brinell.Blazor\Controls"

echo Building brinell...
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
