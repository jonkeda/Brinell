#!/usr/bin/env pwsh
# Runs bruinell from this repository's source, for scripts and people inside Bruinell's own repo.
# Anywhere else, install it: dotnet tool install -g Bruinell.Cli, then run bruinell.
#
#   pwsh tools/Scripts/bruinell.ps1 --help
#   pwsh tools/Scripts/bruinell.ps1 scrape status --solution Scraper.Sites --project Bruinell.Html.React

$project = Join-Path $PSScriptRoot '..' 'Bruinell.Cli' 'Bruinell.Cli.csproj'
& dotnet run --project $project -v:q -- @args
exit $LASTEXITCODE
