@echo off
setlocal

rem Generates .gen.cs files from every .tpl.cs file in the Controls\Base folder
rem using the Brinell ControlObject generator.

set "SCRIPT_DIR=%~dp0"
set "CLI_PROJECT=%SCRIPT_DIR%..\Brinell.Generator.Cli\Brinell.Generator.Cli.csproj"
set "CLI_EXE=%SCRIPT_DIR%..\Brinell.Generator.Cli\bin\Release\net10.0\Brinell.Generator.Cli.exe"
set "INPUT=%SCRIPT_DIR%..\..\srcnew\Brinell.Maui\Controls\Base"

echo Building Brinell.Generator.Cli...
dotnet build "%CLI_PROJECT%" -c Release -v:minimal /nr:false
if errorlevel 1 (
    echo Build failed.
    exit /b 1
)

echo Running generator...
"%CLI_EXE%" --input "%INPUT%"
if errorlevel 1 (
    echo Generation failed.
    exit /b 1
)

echo Done. Generated output in: %INPUT%
endlocal
