param(
    [Parameter(Mandatory)]
    [string]$SourcePath,
    [Parameter(Mandatory)]
    [string]$ExpectedSite,
    [switch]$Backend
)

$ErrorActionPreference = 'Stop'
foreach ($name in @('SMARTERASP_SITE_NAME', 'SMARTERASP_SERVER_URL', 'SMARTERASP_USERNAME', 'SMARTERASP_PASSWORD')) {
    if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($name))) {
        throw "Missing GitHub repository secret: $name. See DEPLOYMENT.md."
    }
}
if ($env:SMARTERASP_SITE_NAME -ne $ExpectedSite) {
    throw 'The configured Web Deploy site does not match this repository. Check SMARTERASP_SITE_NAME.'
}
$serverUri = [uri]$env:SMARTERASP_SERVER_URL
if (-not $serverUri.IsAbsoluteUri -or $serverUri.Scheme -ne 'https') {
    throw 'SMARTERASP_SERVER_URL must be the HTTPS Web Deploy service URL from VSDeploy.'
}

$source = (Resolve-Path -LiteralPath $SourcePath).Path
$requiredFile = if ($Backend) { 'H2-Trainning.dll' } else { 'index.html' }
if (-not (Test-Path -LiteralPath (Join-Path $source $requiredFile))) {
    throw "Build output is missing $requiredFile."
}

$msdeploy = Join-Path ${env:ProgramFiles} 'IIS\Microsoft Web Deploy V3\msdeploy.exe'
if (-not (Test-Path -LiteralPath $msdeploy)) {
    & choco install webdeploy --yes --no-progress
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $msdeploy)) {
        throw 'Unable to install Microsoft Web Deploy on the GitHub runner.'
    }
}

# Quoting is for the Web Deploy provider syntax; no shell command is constructed.
function Quote-ProviderValue([string]$Value) {
    if (-not $Value.Contains("'")) { return "'$Value'" }
    if (-not $Value.Contains('"')) { return '"' + $Value + '"' }
    throw 'A Web Deploy credential contains both quote characters. Rotate it to a password without quotes.'
}

$destination = '-dest:contentPath={0},computerName={1},userName={2},password={3},authType=Basic' -f @(
    (Quote-ProviderValue $env:SMARTERASP_SITE_NAME),
    (Quote-ProviderValue $env:SMARTERASP_SERVER_URL),
    (Quote-ProviderValue $env:SMARTERASP_USERNAME),
    (Quote-ProviderValue $env:SMARTERASP_PASSWORD)
)
$deployArgs = @(
    '-verb:sync',
    ('-source:contentPath={0}' -f (Quote-ProviderValue $source)),
    $destination,
    '-enableRule:DoNotDeleteRule',
    '-retryAttempts:3',
    '-retryInterval:3000',
    "-skip:objectName=dirPath,absolutePath='(?i)[\\/]\.well-known([\\/]|$)'"
)
if ($Backend) {
    # Keep live connection strings, JWT settings, IIS environment variables and uploads.
    $deployArgs += '-enableRule:AppOffline'
    $deployArgs += "-skip:objectName=filePath,absolutePath='(?i)[\\/](appsettings(\.[^\\/]+)?\.json|web\.config)$'"
    $deployArgs += "-skip:objectName=dirPath,absolutePath='(?i)[\\/](uploads|App_Data|logs)([\\/]|$)'"
}

& $msdeploy @deployArgs 2>&1 | ForEach-Object {
    Write-Output ("$_".Replace($env:SMARTERASP_PASSWORD, '***'))
}
if ($LASTEXITCODE -ne 0) {
    throw "Web Deploy failed with exit code $LASTEXITCODE. Check the Actions log."
}
