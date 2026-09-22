# Запускає тести зі збором покриття коду і будує HTML-звіт через ReportGenerator
# Потрібно один раз:  dotnet tool install --global dotnet-reportgenerator-globaltool
# Запуск:             powershell -ExecutionPolicy Bypass -File coverage.ps1

$root    = $PSScriptRoot
$results = Join-Path $root 'TestResults'
$report  = Join-Path $root 'coverage-report'

if (Test-Path $results) { Remove-Item $results -Recurse -Force }
dotnet test (Join-Path $root 'AnalizerFormatTests\AnalizerFormatTests.csproj') `
    --collect "Code Coverage;Format=Cobertura" --results-directory $results

$cobertura = Get-ChildItem $results -Recurse -Filter *.cobertura.xml | Select-Object -First 1
reportgenerator "-reports:$($cobertura.FullName)" "-targetdir:$report" `
    "-reporttypes:Html;TextSummary" "-assemblyfilters:+AnalizerClassLibrary"

Start-Process (Join-Path $report 'index.html')
