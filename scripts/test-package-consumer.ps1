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
} finally {
    foreach ($name in $packages) {
        $package = Join-Path $feed "$name.$version.nupkg"
        if (Test-Path -LiteralPath $package) { Remove-Item -LiteralPath $package }
    }
}
