$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$fixture = Join-Path $root 'test\Shorthand.UmbracoCodeFirst.DiagnosticsConsumer.Tests\Shorthand.UmbracoCodeFirst.DiagnosticsConsumer.Tests.csproj'
$config = Join-Path $root 'test\Shorthand.UmbracoCodeFirst.PackageConsumer.Tests\nuget.config'
$feed = Join-Path $root 'artifacts\package-consumer-feed'
$version = "0.0.2-diagnostics-$([Guid]::NewGuid().ToString('N'))"
$packages = @('Shorthand.UmbracoCodeFirst.Core', 'Shorthand.UmbracoCodeFirst.SourceGenerators', 'Shorthand.UmbracoCodeFirst.Analyzers')
New-Item -ItemType Directory -Force -Path $feed | Out-Null

try {
    foreach ($name in $packages) {
        $project = Join-Path $root "src\$name\$name.csproj"
        dotnet pack $project --configuration Release --no-restore --output $feed "-p:PackageVersion=$version" --verbosity quiet
        if ($LASTEXITCODE -ne 0) { throw "Packing $name failed with exit code $LASTEXITCODE." }
    }

    dotnet restore $fixture --configfile $config "-p:CodeFirstPackageVersion=$version" --verbosity quiet
    if ($LASTEXITCODE -ne 0) { throw "Diagnostic consumer restore failed with exit code $LASTEXITCODE." }

    $source = Get-Content (Join-Path (Split-Path $fixture -Parent) 'Diagnostics.cs')
    foreach ($case in @(
        @{ Symbol = 'BAD109'; Id = 'UCF109'; Line = 'public partial string? Image'; Token = 'Image'; Message = "Property 'Image'" },
        @{ Symbol = 'BAD108'; Id = 'UCF108'; Line = 'public partial class Picker;'; Token = 'Picker'; Message = 'Umbraco.UnknownPicker' },
        @{ Symbol = 'BAD119'; Id = 'UCF119'; Line = 'DefaultTemplateAlias = "page", AllowedTemplateAliases = new[] { "other" }'; Token = '"page"'; Message = "Default template 'page'" },
        @{ Symbol = 'BAD123'; Id = 'UCF123'; Line = '[UCFProperty("Image")]'; Token = 'UCFProperty'; Message = "Property 'Image'" }
    )) {
        $matches = @($source | Select-String -SimpleMatch $case.Line)
        if ($matches.Count -ne 1) { throw "Expected one source line for $($case.Symbol)." }
        $line = $matches[0].LineNumber
        $column = $matches[0].Line.IndexOf($case.Token, [StringComparison]::Ordinal) + 1
        if ($column -eq 0) { throw "Missing source token for $($case.Symbol)." }
        $location = "Diagnostics\.cs\($line,$column\)"
        $output = dotnet build $fixture --configuration Release --no-restore --no-incremental --verbosity quiet "-p:CodeFirstPackageVersion=$version" "-p:DiagnosticCase=$($case.Symbol)" 2>&1 | Out-String
        $exitCode = $LASTEXITCODE
        if ($exitCode -eq 0 -or $output -notmatch "${location}: error $($case.Id)\b" -or $output -notmatch [regex]::Escape($case.Message)) {
            throw "Expected $($case.Id) at the source location for $($case.Symbol).`n$output"
        }
        if ($output -match "\berror (?!$($case.Id)\b)[A-Z]+\d+" -or $output -match '\b(?:AD0001|CS8032)\b') {
            throw "Unexpected compiler or analyzer failure for $($case.Symbol).`n$output"
        }
        Write-Host "Verified $($case.Id) ($($case.Symbol))."
    }

    $output = dotnet build $fixture --configuration Release --no-restore --no-incremental --verbosity quiet "-p:CodeFirstPackageVersion=$version" '-p:DiagnosticCase=VALID' 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0 -or $output -match '\b(?:UCF\d+|AD0001|CS8032)\b') {
        throw "Corrected diagnostic consumer did not build cleanly.`n$output"
    }
    Write-Host 'Verified corrected consumer build.'
} finally {
    foreach ($name in $packages) {
        $package = Join-Path $feed "$name.$version.nupkg"
        if (Test-Path -LiteralPath $package) { Remove-Item -LiteralPath $package }
    }
}
