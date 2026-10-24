param([string]$Version = '1.0.0')
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$publishPath = Join-Path $projectRoot "artifacts\RouteLab-$Version-win-x64"
$zipPath = Join-Path $projectRoot "artifacts\RouteLab-$Version-win-x64.zip"
dotnet run --project (Join-Path $projectRoot 'tests\RouteLab.Regression') -c Release
if ($LASTEXITCODE -ne 0) { throw 'Regression checks failed.' }
dotnet publish (Join-Path $projectRoot 'src\RouteLab.App') -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:Version=$Version -o $publishPath
if ($LASTEXITCODE -ne 0) { throw 'Windows publish failed.' }
Copy-Item -LiteralPath (Join-Path $projectRoot 'README.md') -Destination $publishPath
$noticePath = Join-Path $publishPath 'licenses'
New-Item -ItemType Directory -Path $noticePath -Force | Out-Null
foreach ($notice in @('dotnet-runtime-LICENSE.txt', 'dotnet-runtime-THIRD-PARTY-NOTICES.txt', 'dotnet-winforms-LICENSE.txt')) {
    Copy-Item -LiteralPath (Join-Path $projectRoot "licenses/$notice") -Destination (Join-Path $noticePath $notice)
}
Compress-Archive -Path (Join-Path $publishPath '*') -DestinationPath $zipPath -Force
$archive = Get-Item -LiteralPath $zipPath
[PSCustomObject]@{ File = $archive.FullName; Bytes = $archive.Length; SHA256 = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash } | ConvertTo-Json
