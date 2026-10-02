#!/usr/bin/env pwsh
# Runs brinell from this repository's source, for scripts and people inside Brinell's own repo.
# Anywhere else, install it: dotnet tool install -g Brinell.Cli, then run brinell.
#
#   pwsh tools/Scripts/brinell.ps1 --help
#   pwsh tools/Scripts/brinell.ps1 scrape status --solution Scraper.Sites --project Brinell.Html.React

$project = Join-Path $PSScriptRoot '..' 'Brinell.Cli' 'Brinell.Cli.csproj'
& dotnet run --project $project -v:q -- @args
exit $LASTEXITCODE
