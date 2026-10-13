<#
.SYNOPSIS
    Turns a Cobertura coverage report into a Shields.io endpoint badge and a Markdown summary.

.DESCRIPTION
    Only the product assemblies are counted (the test assembly and third-party packages such as
    Spectre.Console are instrumented too but say nothing about the code we ship).

.EXAMPLE
    ./coverage-badge.ps1 -ReportPath coverage/coverage.cobertura.xml -BadgePath badge/coverage.json
#>
param(
    [Parameter(Mandatory = $true)] [string] $ReportPath,
    [Parameter(Mandatory = $true)] [string] $BadgePath,
    [string] $SummaryPath,
    [string[]] $Assemblies = @('byo', 'BYO.SDK', 'BYO.Integrations')
)

$ErrorActionPreference = 'Stop'

[xml] $report = Get-Content -LiteralPath $ReportPath
$rows = @()
$totalValid = 0
$totalCovered = 0

foreach ($package in @($report.coverage.packages.package)) {
    if (-not $package -or $Assemblies -notcontains $package.name) { continue }

    $valid = 0
    $covered = 0
    foreach ($class in @($package.classes.class)) {
        foreach ($line in @($class.lines.line)) {
            if (-not $line) { continue }
            $valid++
            if ([int] $line.hits -gt 0) { $covered++ }
        }
    }

    $rows += [pscustomobject] @{ Assembly = $package.name; Covered = $covered; Lines = $valid }
    $totalValid += $valid
    $totalCovered += $covered
}

$percent = if ($totalValid -eq 0) { 0 } else { [math]::Round(100 * $totalCovered / $totalValid, 1) }
$color = if ($percent -ge 80) { 'brightgreen' } elseif ($percent -ge 60) { 'green' } elseif ($percent -ge 40) { 'yellow' } else { 'red' }

$badge = [ordered] @{ schemaVersion = 1; label = 'coverage'; message = "$percent%"; color = $color }
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $BadgePath) | Out-Null
Set-Content -LiteralPath $BadgePath -Value ($badge | ConvertTo-Json -Compress) -NoNewline

$summary = @("## Code coverage", "", "| Assembly | Lines covered | Lines | Coverage |", "| --- | ---: | ---: | ---: |")
foreach ($row in $rows) {
    $rowPercent = if ($row.Lines -eq 0) { 0 } else { [math]::Round(100 * $row.Covered / $row.Lines, 1) }
    $summary += "| $($row.Assembly) | $($row.Covered) | $($row.Lines) | $rowPercent% |"
}
$summary += "| **Total** | **$totalCovered** | **$totalValid** | **$percent%** |"

$summaryText = $summary -join "`n"
Write-Host $summaryText
if ($SummaryPath) {
    Add-Content -LiteralPath $SummaryPath -Value $summaryText
}
