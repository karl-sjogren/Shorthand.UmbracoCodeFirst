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

    $items = (dotnet msbuild $fixture "-p:CodeFirstPackageVersion=$version" '-p:CodeFirstDuplicateViewInput=true' '-target:ShorthandUmbracoCodeFirstCollectTemplates' '-getItem:AdditionalFiles' | Out-String) | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0) { throw "Could not inspect template inputs." }
    if (@($items.Items.AdditionalFiles | Where-Object { $_.Filename -eq 'packageConsumer' -and $_.Extension -eq '.cshtml' }).Count -ne 1) {
        throw "The template view was added more than once."
    }

    dotnet restore $fixture --configfile (Join-Path $fixtureDirectory 'nuget.config') "-p:CodeFirstPackageVersion=$version" '-p:CodeFirstSplitConsumer=true'
    if ($LASTEXITCODE -ne 0) { throw "Split package consumer restore failed with exit code $LASTEXITCODE." }
    dotnet test $fixture --configuration Release --no-restore "-p:CodeFirstPackageVersion=$version" '-p:CodeFirstSplitConsumer=true'
    if ($LASTEXITCODE -ne 0) { throw "Split package consumer test failed with exit code $LASTEXITCODE." }

    $output = dotnet build $fixture --configuration Release --no-restore "-p:CodeFirstPackageVersion=$version" '-p:CodeFirstSplitConsumer=true' '-p:UmbracoCodeFirstGenerateTemplates=true' --verbosity quiet 2>&1 | Out-String
    if ($LASTEXITCODE -eq 0 -or $output -notmatch 'UCF124') {
        throw "A competing site registry did not report UCF124: $output"
    }

    $modelsDirectory = Join-Path $root 'test\Shorthand.UmbracoCodeFirst.PackageConsumer.Models'
    $models = Join-Path $modelsDirectory 'Shorthand.UmbracoCodeFirst.PackageConsumer.Models.csproj'
    $views = Join-Path ([System.IO.Path]::GetTempPath()) "codefirst-views-$([Guid]::NewGuid().ToString('N'))"
    $mainView = Join-Path $views 'packageConsumer.cshtml'
    $extraView = Join-Path $views 'extra.cshtml'
    try {
        dotnet msbuild $models "-p:CodeFirstPackageVersion=$version" '-p:UmbracoCodeFirstViewsDirectory=' '-target:ShorthandUmbracoCodeFirstCollectTemplates' --verbosity quiet
        if ($LASTEXITCODE -ne 0) { throw "A missing default views directory should be allowed." }
        $output = dotnet msbuild $models "-p:CodeFirstPackageVersion=$version" "-p:UmbracoCodeFirstViewsDirectory=$views" '-target:ShorthandUmbracoCodeFirstCollectTemplates' --verbosity quiet 2>&1 | Out-String
        if ($LASTEXITCODE -eq 0 -or $output -notmatch 'does not exist') {
            throw "A missing explicit views directory was not rejected: $output"
        }

        dotnet msbuild $models "-p:CodeFirstPackageVersion=$version" "-p:UmbracoCodeFirstViewsDirectory=$views" '-p:UmbracoCodeFirstGenerateTemplates=false' '-target:ShorthandUmbracoCodeFirstCollectTemplates' --verbosity quiet
        if ($LASTEXITCODE -ne 0) { throw "A disabled template generator should not require a views directory." }
        New-Item -ItemType Directory -Path $views | Out-Null
        dotnet msbuild $models "-p:CodeFirstPackageVersion=$version" "-p:UmbracoCodeFirstViewsDirectory=$views" '-target:ShorthandUmbracoCodeFirstCollectTemplates' --verbosity quiet
        if ($LASTEXITCODE -ne 0) { throw "An empty explicit views directory should be allowed." }
        Copy-Item (Join-Path $fixtureDirectory 'Views\packageConsumer.cshtml') $mainView
        dotnet build $models --configuration Release --no-restore "-p:CodeFirstPackageVersion=$version" "-p:UmbracoCodeFirstViewsDirectory=$views" --verbosity quiet
        if ($LASTEXITCODE -ne 0) { throw "Initial external views build failed." }
        $generatedDirectory = Join-Path $modelsDirectory 'obj\generated\Shorthand.UmbracoCodeFirst.SourceGenerators\Shorthand.UmbracoCodeFirst.SourceGenerators.CodeFirstRegistryGenerator'
        $aliasesPath = Join-Path $generatedDirectory 'UmbracoTemplates.g.cs'
        $registryPath = Join-Path $generatedDirectory 'CodeFirstRegistry.g.cs'
        $initialRegistry = Get-Content $registryPath -Raw

        Add-Content -LiteralPath $mainView -Value '<p>Changed</p>'
        Set-Content -LiteralPath $extraView -Value '@attribute [Shorthand.UmbracoCodeFirst.Attributes.UCFTemplate("Extra")]'
        dotnet build $models --configuration Release --no-restore "-p:CodeFirstPackageVersion=$version" "-p:UmbracoCodeFirstViewsDirectory=$views" --verbosity quiet
        if ($LASTEXITCODE -ne 0) { throw "Modified external views build failed." }
        if ((Get-Content $registryPath -Raw) -eq $initialRegistry -or (Get-Content $aliasesPath -Raw) -notmatch 'const string Extra') {
            throw "Changed and added views did not update generated output."
        }

        Remove-Item -LiteralPath $extraView
        dotnet build $models --configuration Release --no-restore "-p:CodeFirstPackageVersion=$version" "-p:UmbracoCodeFirstViewsDirectory=$views" --verbosity quiet
        if ($LASTEXITCODE -ne 0 -or (Get-Content $aliasesPath -Raw) -match 'const string Extra') {
            throw "Removing a view did not update generated output."
        }
    } finally {
        if (Test-Path -LiteralPath $extraView) { Remove-Item -LiteralPath $extraView }
        if (Test-Path -LiteralPath $mainView) { Remove-Item -LiteralPath $mainView }
        if (Test-Path -LiteralPath $views) { Remove-Item -LiteralPath $views }
    }
} finally {
    foreach ($name in $packages) {
        $package = Join-Path $feed "$name.$version.nupkg"
        if (Test-Path -LiteralPath $package) { Remove-Item -LiteralPath $package }
    }
}
