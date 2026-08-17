@echo off
setlocal

rem Generates ViewBase.gen.cs from ViewBase.cs using the Brinell ControlObject generator.

set "SCRIPT_DIR=%~dp0"
set "CLI_PROJECT=%SCRIPT_DIR%..\Brinell.Generator.Cli\Brinell.Generator.Cli.csproj"
set "CLI_EXE=%SCRIPT_DIR%..\Brinell.Generator.Cli\bin\Release\net10.0\Brinell.Generator.Cli.exe"
set "INPUT=%SCRIPT_DIR%..\..\srcnew\Brinell.Maui\Controls\Base\ViewBase.cs"
set "OUTPUT=%SCRIPT_DIR%..\..\srcnew\Brinell.Maui\Controls\Base\ViewBase.gen.cs"

echo Building Brinell.Generator.Cli...
dotnet build "%CLI_PROJECT%" -c Release -v:minimal /nr:false
if errorlevel 1 (
    echo Build failed.
    exit /b 1
)

echo Running generator...
"%CLI_EXE%" --input "%INPUT%" --output "%OUTPUT%"
if errorlevel 1 (
    echo Generation failed.
    exit /b 1
)

echo Done. Generated: %OUTPUT%
endlocal
