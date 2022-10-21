param(
	[string]$eproRoot = $null,
	[string]$version = "22.02.0-beta"
)
$ErrorActionPreference = "Stop";

if (-not $eproRoot) {
	$versionPart = $version.Replace(".", "_");
	$eproRoot = "C:\Program Files\Epro_${versionPart}";
}

if (-not (Test-Path $eproRoot)) {
	Write-Host "Path does not exist: '${eproRoot}'";
	exit 2;
}

#
# First, set up and verify our environment.
#

$thisDir = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent;
$eproConfigPath = "${eproRoot}\Web\bin\Epro.config";
$eproDataPath = "C:\EproDocs";
$stashToolPath = Get-Item "${thisDir}\tools\Bluewire.Stash.Tool.exe";
$snapshotToolPath = Get-Item "${eproRoot}\Web\bin\Bluewire.Epro.Snapshot.exe";
$remoteStashRoot = "https://bluewirestashservice20210521130718.azurewebsites.net";
$stashName = "Bluewire.Epro.ApplySampleData";

if (-not (Test-Path "${eproConfigPath}")) {
	Write-Host "Path does not exist: '${eproConfigPath}'";
	exit 2;
}

if (-not (Test-Path $snapshotToolPath)) {
	throw "Did not find snapshot tool at '${snapshotToolPath}'";
}

$temporaryDirectory = [IO.Path]::Combine($thisDir, "temp", [IO.Path]::GetRandomFileName());
# Sanity check: we should not accidentally 'clean up' a pre-existing directory.
if (Test-Path $temporaryDirectory) {
	throw "Temporary directory already exists: ${temporaryDirectory}";
}

Write-Host "Exporting sample data for Epro version ${version}";
Write-Host "If this is NOT the current running version, data may be lost."
$confirmation = Read-Host -Prompt "To proceed, type the version number exactly as it appears above"

if ($confirmation -cne $version) {
	Write-Host "Aborted.";
	exit 5;
}

Write-Host "You may now be asked to authenticate against the Stash Service.";
Write-Host "Please use your NON-ADMIN user account.";
& $stashToolPath --remote-stash-root $remoteStashRoot authenticate;
if(!$?) { throw "Authentication failed with exit code $LASTEXITCODE"; }

Write-Host "Checking for existing sample data";
& $stashToolPath --remote-stash-root $remoteStashRoot pull $stashName $stashName --version $version --ignore;
if(!$?) { throw "Pull failed with exit code $LASTEXITCODE"; }

$existing = @(& $stashToolPath show $stashName --version $version --exact);
if ($existing.Length -gt 0) {
	Write-Host "Sample data is already recorded for Epro version ${version}.";
	$confirmation = Read-Host -Prompt "To overwrite, re-type the version number exactly as it appears above"
	if ($confirmation -cne $version) {
		Write-Host "Aborted.";
		exit 5;
	}
}

Write-Host "Stopping Epro";
iisreset /stop
Write-Host "done";

Write-Host "Creating snapshot";
& $snapshotToolPath --config $eproConfigPath --data $eproDataPath export $temporaryDirectory;
if(!$?) { throw "Checkout failed with exit code $LASTEXITCODE"; }
Write-Host "done";

Write-Host "Starting Epro";
iisreset /start
Write-Host "done";

Write-Host "Committing sample data files";
& $stashToolPath commit $stashName $temporaryDirectory --version $version --force;
if(!$?) { throw "Commit failed with exit code $LASTEXITCODE"; }
Write-Host "done";

Write-Host "Pushing sample data files";
& $stashToolPath --remote-stash-root $remoteStashRoot push $stashName $stashName --version $version --overwrite;
if(!$?) { throw "Commit failed with exit code $LASTEXITCODE"; }
Write-Host "done";

Write-Host "Cleaning commit directory";
Remove-Item -Recurse $temporaryDirectory
Write-Host "done";

