# Packs the Brinell library projects that are part of srcnew\Brinell.sln.
# Working directory: Brinell root.
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
    'Brinell.Core',
    'Brinell.Automation',
    'Brinell.Mocking',
    'Brinell.Uat',
    'Brinell.Wpf',
    'Brinell.WinForms',
    'Brinell.Blazor',
    'Brinell.Html',
    'Brinell.Html.Playwright',
    'Brinell.Stride',
    'Brinell.Maui',
    'Brinell.Maui.CommunityToolkit',
    'Brinell.Maui.FlaUI',
    'Brinell.Uia.Contracts'
)

foreach ($name in $libraries) {
    dotnet pack "srcnew/$name/$name.csproj" -c Release -o $Output -v:minimal /nr:false "-p:Version=$Version" -p:GeneratePackageOnBuild=false
    if ($LASTEXITCODE -ne 0) { throw "Pack failed: $name" }
}
