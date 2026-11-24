$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$fixture = Join-Path $root 'test\Shorthand.UmbracoCodeFirst.DependencyConsumer\Shorthand.UmbracoCodeFirst.DependencyConsumer.csproj'
$config = Join-Path $root 'test\Shorthand.UmbracoCodeFirst.PackageConsumer.Tests\nuget.config'
$feed = Join-Path $root 'artifacts\package-consumer-feed'
$version = "0.0.2-dependencies-$([Guid]::NewGuid().ToString('N'))"
$sdk10Directory = Join-Path ([System.IO.Path]::GetTempPath()) "codefirst-sdk10-$([Guid]::NewGuid().ToString('N'))"
$packages = @('Shorthand.UmbracoCodeFirst.Core', 'Shorthand.UmbracoCodeFirst.Backoffice', 'Shorthand.UmbracoCodeFirst', 'Shorthand.UmbracoCodeFirst.SourceGenerators', 'Shorthand.UmbracoCodeFirst.Analyzers')
New-Item -ItemType Directory -Force -Path $feed | Out-Null
New-Item -ItemType Directory -Path $sdk10Directory | Out-Null
[System.IO.File]::WriteAllText((Join-Path $sdk10Directory 'global.json'), '{"sdk":{"version":"10.0.100","rollForward":"latestFeature"}}')

try {
    foreach ($name in $packages) {
        $project = Join-Path $root "src\$name\$name.csproj"
        dotnet pack $project --configuration Release --no-restore --output $feed "-p:PackageVersion=$version" --verbosity quiet
        if ($LASTEXITCODE -ne 0) { throw "Packing $name failed." }

        $package = Join-Path $feed "$name.$version.nupkg"
        $zip = [System.IO.Compression.ZipFile]::OpenRead($package)
        try {
            $entry = @($zip.Entries | Where-Object { $_.FullName -like '*.nuspec' })
            if ($entry.Count -ne 1) { throw "Expected one nuspec in $name." }
            $reader = [System.IO.StreamReader]::new($entry[0].Open())
            try { [xml]$nuspec = $reader.ReadToEnd() } finally { $reader.Dispose() }
            $dependencies = @($nuspec.package.metadata.dependencies.group.dependency | Where-Object { $_.id })
            foreach ($dependency in $dependencies) {
                if ($dependency.id -match '^(Microsoft|System)\.' -and
                    $dependency.id -ne 'Microsoft.AspNetCore.OpenApi' -and
                    ($dependency.version -match ',\s*[^)\]]+[\)\]]$' -or
                     $dependency.version -match '^\[[^,\]]+\]$')) {
                    throw "$name has an unexpected upper bound on $($dependency.id): $($dependency.version)"
                }
            }

            $ids = @($dependencies | ForEach-Object { $_.id })
            $unexpected = @(switch ($name) {
                'Shorthand.UmbracoCodeFirst' { @($ids | Where-Object { $_ -notin @('Shorthand.UmbracoCodeFirst.Core', 'Shorthand.UmbracoCodeFirst.Backoffice') }) }
                'Shorthand.UmbracoCodeFirst.Backoffice' { @($ids | Where-Object { $_ -in @('Umbraco.Cms.Web.Website', 'Microsoft.Extensions.Http.Resilience', 'Microsoft.Extensions.Logging.Abstractions') }) }
                'Shorthand.UmbracoCodeFirst.SourceGenerators' { @($ids | Where-Object { $_ -match '^Microsoft\.(CodeAnalysis|Extensions\.Logging)' }) }
                'Shorthand.UmbracoCodeFirst.Analyzers' { @($ids | Where-Object { $_ -match '^Microsoft\.(CodeAnalysis|Extensions\.Logging)' }) }
                default { @() }
            })
            if ($unexpected.Count -ne 0) { throw "$name has unexpected dependencies: $($unexpected -join ', ')" }
        } finally {
            $zip.Dispose()
        }
    }

    foreach ($case in @(
        @{ Framework = 'net10.0'; Version = '10.0.15'; ExtensionsVersion = '10.11.0' },
        @{ Framework = 'net11.0'; Version = '10.0.15'; ExtensionsVersion = '10.11.0' },
        @{ Framework = 'net11.0'; Version = '11.0.0'; ExtensionsVersion = '11.0.0' }
    )) {
        $properties = @("-p:CodeFirstPackageVersion=$version", "-p:ConsumerTargetFramework=$($case.Framework)", "-p:ConsumerPackageVersion=$($case.Version)", "-p:ConsumerExtensionsPackageVersion=$($case.ExtensionsVersion)")
        if ($case.Framework -eq 'net10.0') { Push-Location $sdk10Directory }
        try {
            dotnet restore $fixture --configfile $config @properties --verbosity quiet
            if ($LASTEXITCODE -ne 0) { throw "Dependency consumer restore failed for $($case.Framework)." }

            $assets = Get-Content (Join-Path (Split-Path $fixture -Parent) 'obj\project.assets.json') -Raw | ConvertFrom-Json
            $resolved = $assets.targets.($case.Framework).PSObject.Properties.Name
            foreach ($id in @('Microsoft.Extensions.Logging.Abstractions', 'System.Security.Cryptography.Pkcs', 'System.Text.Json')) {
                if ("$id/$($case.Version)" -notin $resolved) {
                    throw "$($case.Framework) did not resolve $id $($case.Version)."
                }
            }
            if ("Microsoft.Extensions.Http.Resilience/$($case.ExtensionsVersion)" -notin $resolved) {
                throw "$($case.Framework) did not resolve Microsoft.Extensions.Http.Resilience $($case.ExtensionsVersion)."
            }

            dotnet run --project $fixture --configuration Release --no-restore @properties
            if ($LASTEXITCODE -ne 0) { throw "Dependency consumer failed for $($case.Framework)." }

            $output = dotnet build $fixture --configuration Release --no-restore --no-incremental @properties '-p:DefineConstants=BAD108' --verbosity quiet 2>&1 | Out-String
            if ($LASTEXITCODE -eq 0 -or $output -notmatch '\berror UCF108\b' -or
                $output -match '\b(?:AD0001|CS8032|CS9057)\b') {
                throw "Packaged analyzer failed to report UCF108 for $($case.Framework):`n$output"
            }
            Write-Host "Verified package dependencies and OpenAPI registration for $($case.Framework) with Microsoft/System $($case.Version)."
        } finally {
            if ($case.Framework -eq 'net10.0') { Pop-Location }
        }
    }
} finally {
    foreach ($name in $packages) {
        $package = Join-Path $feed "$name.$version.nupkg"
        if (Test-Path -LiteralPath $package) { Remove-Item -LiteralPath $package }
    }
    Remove-Item -LiteralPath (Join-Path $sdk10Directory 'global.json')
    Remove-Item -LiteralPath $sdk10Directory
}
