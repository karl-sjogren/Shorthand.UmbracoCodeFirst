$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$fixture = Join-Path $root 'test\Shorthand.UmbracoCodeFirst.PackageConsumer.Tests\Shorthand.UmbracoCodeFirst.PackageConsumer.Tests.csproj'
$feed = Join-Path $root 'artifacts\package-consumer-feed'
$version = "0.0.2-fixture-$([Guid]::NewGuid().ToString('N'))"
$packages = @('Shorthand.UmbracoCodeFirst.Core', 'Shorthand.UmbracoCodeFirst.Backoffice', 'Shorthand.UmbracoCodeFirst', 'Shorthand.UmbracoCodeFirst.SourceGenerators')
New-Item -ItemType Directory -Force -Path $feed | Out-Null

try {
    foreach ($name in $packages) {
        $project = Join-Path $root "src\$name\$name.csproj"
        dotnet pack $project --configuration Release --no-restore --output $feed "-p:PackageVersion=$version"
        if ($LASTEXITCODE -ne 0) { throw "Packing $name failed with exit code $LASTEXITCODE." }
    }

    dotnet restore $fixture --configfile (Join-Path (Split-Path $fixture -Parent) 'nuget.config') "-p:CodeFirstPackageVersion=$version"
    if ($LASTEXITCODE -ne 0) { throw "Package consumer restore failed with exit code $LASTEXITCODE." }
    dotnet test $fixture --configuration Release --no-restore "-p:CodeFirstPackageVersion=$version"
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    $items = (dotnet msbuild $fixture "-p:CodeFirstPackageVersion=$version" '-getItem:UmbracoJsonSchemaFiles' | Out-String) | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0) { throw "Could not inspect the package consumer schema items." }
    $schemaName = 'appsettings-schema.Shorthand.UmbracoCodeFirst.json'
    $packageSchemas = @($items.Items.UmbracoJsonSchemaFiles | Where-Object {
        $_.Filename + $_.Extension -eq $schemaName -and $_.FullPath.Contains($version, [StringComparison]::OrdinalIgnoreCase)
    })
    if ($packageSchemas.Count -ne 1 -or -not (Test-Path -LiteralPath $packageSchemas[0].FullPath)) {
        throw "The packed schema is not registered with Umbraco."
    }

    dotnet build $fixture --configuration Release --no-restore "-p:CodeFirstPackageVersion=$version"
    if ($LASTEXITCODE -ne 0) { throw "Package consumer rebuild failed with exit code $LASTEXITCODE." }

    $fixtureDirectory = Split-Path $fixture -Parent
    $references = @((Get-Content (Join-Path $fixtureDirectory 'appsettings-schema.json') -Raw | ConvertFrom-Json).allOf |
        ForEach-Object { $_.'$ref' })
    if (@($references | Where-Object { $_ -eq "$schemaName#" }).Count -ne 1 -or
        $references -notcontains 'appsettings-schema.Umbraco.Cms.json#' -or
        $references -notcontains 'https://json.schemastore.org/appsettings.json') {
        throw "The consumer appsettings schema does not contain the expected references exactly once."
    }

    $schema = Get-Content (Join-Path $fixtureDirectory $schemaName) -Raw | ConvertFrom-Json
    foreach ($setting in @('DisableStartupSynchronization', 'EnableClusterCoordination')) {
        $property = $schema.properties.Shorthand.properties.UmbracoCodeFirst.properties.$setting
        if ($property.type -ne 'boolean' -or $property.default -ne $false -or [string]::IsNullOrWhiteSpace($property.description)) {
            throw "The consumer schema does not describe $setting correctly."
        }
    }
} finally {
    foreach ($name in $packages) {
        $package = Join-Path $feed "$name.$version.nupkg"
        if (Test-Path -LiteralPath $package) { Remove-Item -LiteralPath $package }
    }
}
