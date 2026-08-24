#Requires -Version 5.1
<#
.SYNOPSIS
    Computes CRAP (Change Risk Anti-Patterns) scores from OpenCover coverage reports.

.DESCRIPTION
    CRAP = complexity^2 * (1 - coverage)^3 + complexity

    It answers the question neither coverage nor complexity answers alone: which
    complex code is also untested? A simple method is cheap to change however
    untested it is; a complex method with full coverage is defended. The product
    of the two is where change actually breaks things.

    Worked example, a method with cyclomatic complexity 10:
        100% covered -> 10^2 * 0^3 + 10 =  10   (complex but defended)
         50% covered -> 10^2 * 0.5^3 + 10 = 22.5
          0% covered -> 10^2 * 1^3 + 10 = 110   (the code that bites you)

    ReportGenerator gates cyclomatic complexity directly but does not render a
    CRAP Score column for coverlet-produced OpenCover reports, so this script
    computes it from the per-method cyclomaticComplexity and sequenceCoverage
    attributes that are present in that XML.

.PARAMETER Path
    Glob of OpenCover XML reports.

.PARAMETER MaximumCrapScore
    Fail (exit 1) when any method scores above this. Omit or set 0 to report only.

.PARAMETER Top
    How many hotspots to list.

.PARAMETER SummaryPath
    Optional Markdown output, for example $env:GITHUB_STEP_SUMMARY.

.EXAMPLE
    pwsh build/Get-CrapScore.ps1 -Path 'artifacts/opencover/*.xml' -MaximumCrapScore 120
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $Path,

    [double] $MaximumCrapScore = 0,

    [int] $Top = 25,

    [string] $SummaryPath,

    # Source-generator output (regex, OpenAPI, protobuf) carries huge complexity
    # that nobody wrote and nobody can test. Counting it would bury the real
    # hotspots under noise.
    [string[]] $ExcludeClassPattern = @(
        '\.Generated\.',
        '^System\.',
        '^Microsoft\.',
        '<[A-Za-z]+_g>',
        '_generated'
    )
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$files = @(Get-ChildItem -Path $Path -ErrorAction SilentlyContinue)
if ($files.Count -eq 0) {
    throw "No OpenCover reports matched '$Path'."
}

$methodMap = @{}

foreach ($file in $files) {
    [xml] $document = Get-Content -LiteralPath $file.FullName -Raw

    # XPath rather than property navigation: a module with no classes, or a
    # class with no methods, is normal in these reports and must not throw.
    foreach ($module in $document.SelectNodes('/CoverageSession/Modules/Module')) {
        $assemblyNode = $module.SelectSingleNode('ModuleName')
        $assembly = if ($null -ne $assemblyNode) { $assemblyNode.InnerText } else { '(unknown)' }
        $filePaths = @{}
        foreach ($sourceFile in $module.SelectNodes('Files/File')) {
            $filePaths[$sourceFile.GetAttribute('uid')] = $sourceFile.GetAttribute('fullPath')
        }

        foreach ($class in $module.SelectNodes('Classes/Class')) {
            $classNode = $class.SelectSingleNode('FullName')
            $className = if ($null -ne $classNode) { $classNode.InnerText } else { '(unknown)' }

            $excluded = $false
            foreach ($pattern in $ExcludeClassPattern) {
                if ($className -match $pattern) { $excluded = $true; break }
            }
            if ($excluded) { continue }

            foreach ($method in $class.SelectNodes('Methods/Method')) {
                # Use GetAttribute so a missing attribute yields '' rather than
                # tripping StrictMode on XmlElement property access.
                $rawComplexity = $method.GetAttribute('cyclomaticComplexity')
                if ([string]::IsNullOrEmpty($rawComplexity)) { continue }

                $complexity = [double]::Parse(
                    $rawComplexity,
                    [System.Globalization.CultureInfo]::InvariantCulture)
                $nameNode = $method.SelectSingleNode('Name')
                $name = if ($null -ne $nameNode) { $nameNode.InnerText } else { '(unknown)' }
                $metadataToken = $method.GetAttribute('metadataToken')
                $methodKey = "$assembly`n$className`n$metadataToken`n$name"

                if (-not $methodMap.ContainsKey($methodKey)) {
                    $methodMap[$methodKey] = [pscustomobject]@{
                        Assembly          = $assembly
                        Class             = $className
                        Method            = $name
                        Complexity        = $complexity
                        SequencePoints     = [System.Collections.Generic.HashSet[string]]::new(
                            [StringComparer]::Ordinal)
                        CoveredPoints      = [System.Collections.Generic.HashSet[string]]::new(
                            [StringComparer]::Ordinal)
                        FallbackCoverage   = 1.0
                        HasCoverageReading = $false
                    }
                }

                $aggregate = $methodMap[$methodKey]
                $aggregate.Complexity = [Math]::Max($aggregate.Complexity, $complexity)

                # Merge unique sequence points across reports. Treating every
                # test-project report independently produces false zero-coverage
                # hotspots for production methods covered by another suite.
                foreach ($sequencePoint in $method.SelectNodes('SequencePoints/SequencePoint')) {
                    $fileId = $sequencePoint.GetAttribute('fileid')
                    $sourcePath = if ($filePaths.ContainsKey($fileId)) {
                        $filePaths[$fileId]
                    }
                    else {
                        "fileid:$fileId"
                    }
                    $pointKey = '{0}|{1}|{2}|{3}|{4}|{5}' -f @(
                        $sourcePath,
                        $sequencePoint.GetAttribute('sl'),
                        $sequencePoint.GetAttribute('sc'),
                        $sequencePoint.GetAttribute('el'),
                        $sequencePoint.GetAttribute('ec'),
                        $sequencePoint.GetAttribute('offset'))
                    [void] $aggregate.SequencePoints.Add($pointKey)
                    if ([int] $sequencePoint.GetAttribute('vc') -gt 0) {
                        [void] $aggregate.CoveredPoints.Add($pointKey)
                    }
                }

                # Retain sequenceCoverage only as a fallback for report producers
                # that omit the underlying sequence-point elements.
                $rawCoverage = $method.GetAttribute('sequenceCoverage')
                if (-not [string]::IsNullOrEmpty($rawCoverage)) {
                    $fallbackCoverage = [double]::Parse(
                        $rawCoverage,
                        [System.Globalization.CultureInfo]::InvariantCulture) / 100.0
                    if (-not $aggregate.HasCoverageReading) {
                        $aggregate.FallbackCoverage = $fallbackCoverage
                        $aggregate.HasCoverageReading = $true
                    }
                    else {
                        $aggregate.FallbackCoverage = [Math]::Max(
                            $aggregate.FallbackCoverage,
                            $fallbackCoverage)
                    }
                }
            }
        }
    }
}

$methods = [System.Collections.Generic.List[pscustomobject]]::new()
foreach ($aggregate in $methodMap.Values) {
    $coverage = if ($aggregate.SequencePoints.Count -gt 0) {
        $aggregate.CoveredPoints.Count / [double] $aggregate.SequencePoints.Count
    }
    elseif ($aggregate.HasCoverageReading) {
        $aggregate.FallbackCoverage
    }
    else {
        # Abstract/interface methods have no executable sequence points and
        # cannot meaningfully be classified as uncovered hotspots.
        1.0
    }

    $coverage = [Math]::Min(1.0, [Math]::Max(0.0, $coverage))
    $uncovered = 1.0 - $coverage
    $crap = ($aggregate.Complexity * $aggregate.Complexity * [Math]::Pow($uncovered, 3)) +
        $aggregate.Complexity
    $methods.Add([pscustomobject]@{
            Assembly   = $aggregate.Assembly
            Class      = $aggregate.Class
            Method     = $aggregate.Method
            Complexity = $aggregate.Complexity
            Coverage   = [Math]::Round($coverage * 100, 1)
            Crap       = [Math]::Round($crap, 1)
        })
}

if ($methods.Count -eq 0) {
    throw 'Parsed the reports but found no methods with cyclomaticComplexity. Confirm the reports are OpenCover format.'
}

$ranked = $methods | Sort-Object -Property Crap -Descending
$worst = $ranked[0]
$hotspots = @($ranked | Select-Object -First $Top)

Write-Host ''
Write-Host "CRAP analysis over $($methods.Count) methods from $($files.Count) report(s)"
Write-Host "Highest CRAP score: $($worst.Crap)  ($($worst.Class).$($worst.Method))"
Write-Host ''
$hotspots | Format-Table -AutoSize -Property Crap, Complexity, @{ Name = 'Cov%'; Expression = { $_.Coverage } }, Assembly, Class, Method

if ($SummaryPath) {
    $lines = [System.Collections.Generic.List[string]]::new()
    $lines.Add('### CRAP risk hotspots')
    $lines.Add('')
    $lines.Add("Highest CRAP score **$($worst.Crap)** across $($methods.Count) methods. CRAP = complexity squared times (1 - coverage) cubed, plus complexity.")
    $lines.Add('')
    $lines.Add('| CRAP | Complexity | Coverage | Class | Method |')
    $lines.Add('| ---: | ---: | ---: | --- | --- |')
    foreach ($hotspot in $hotspots) {
        $shortClass = ($hotspot.Class -split '\.')[-1]
        $lines.Add("| $($hotspot.Crap) | $($hotspot.Complexity) | $($hotspot.Coverage)% | $shortClass | $($hotspot.Method) |")
    }
    Add-Content -LiteralPath $SummaryPath -Value ($lines -join [Environment]::NewLine)
}

if ($MaximumCrapScore -gt 0 -and $worst.Crap -gt $MaximumCrapScore) {
    Write-Host ''
    throw "CRAP score $($worst.Crap) exceeds the maximum of $MaximumCrapScore ($($worst.Class).$($worst.Method)). Add tests for that method or reduce its complexity."
}

if ($MaximumCrapScore -gt 0) {
    Write-Host "CRAP gate passed (maximum allowed $MaximumCrapScore)."
}
else {
    Write-Host 'CRAP analysis completed (report-only mode).'
}
