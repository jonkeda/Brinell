@echo off
setlocal

rem Generates .gen.cs files from every .tpl.cs file under the MAUI Controls folders
rem (Brinell.Maui and Brinell.Maui.CommunityToolkit) using the Brinell ControlObject generator.

set "SCRIPT_DIR=%~dp0"
set "CLI_PROJECT=%SCRIPT_DIR%..\Brinell.Generator\Brinell.Generator.csproj"
set "CLI_EXE=%SCRIPT_DIR%..\Brinell.Generator\bin\Release\net10.0\Brinell.Generator.exe"
set "INPUT=%SCRIPT_DIR%..\..\srcnew\Brinell.Maui\Controls;%SCRIPT_DIR%..\..\srcnew\Brinell.Maui.CommunityToolkit\Controls"

echo Building Brinell.Generator...
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
