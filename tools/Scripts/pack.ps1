# Packs the Bruinell library projects that are part of Bruinell.sln, and the bruinell tool.
# Working directory: Bruinell root.
#
#   ./tools/Scripts/pack.ps1 -Version 0.2.0 -Output artifacts
#
# Packs project by project instead of the solution: the solution also holds sample apps
# and the Presenter, which are not packages and do not build in Release as part of it.

param(
    [Parameter(Mandatory)] [string] $Version,
    [string] $Output = 'artifacts'
)

$ErrorActionPreference = 'Stop'

$libraries = @(
    'Bruinell.Core',
    'Bruinell.Stride.Automation',
    'Bruinell.Mocking',
    'Bruinell.Uat',
    'Bruinell.Wpf',
    'Bruinell.WinForms',
    'Bruinell.Blazor',
    'Bruinell.Html',
    'Bruinell.Html.Playwright',
    'Bruinell.Stride',
    'Bruinell.Maui',
    'Bruinell.Maui.CommunityToolkit',
    'Bruinell.Maui.FlaUI',
    'Bruinell.Uia.Contracts'
)

foreach ($name in $libraries) {
    dotnet pack "srcnew/$name/$name.csproj" -c Release -o $Output -v:minimal /nr:false "-p:Version=$Version" -p:GeneratePackageOnBuild=false
    if ($LASTEXITCODE -ne 0) { throw "Pack failed: $name" }
}

# bruinell, the command line (AD-028): a dotnet tool, versioned with the libraries.
dotnet pack 'tools/Bruinell.Cli/Bruinell.Cli.csproj' -c Release -o $Output -v:minimal /nr:false "-p:Version=$Version"
if ($LASTEXITCODE -ne 0) { throw 'Pack failed: Bruinell.Cli' }
