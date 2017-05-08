param(
    [string]$createBranch,
    [string]$jiraUri,
    [string]$teamcityProjectId,
    [string]$downstream,
    [string]$tagPrefix,
    [string]$workingCopy = "workingCopy",
    [string]$baseBranch = "master" # Only master is currently supported
)

if($createBranch -eq "master")
{
    "Cannot overwrite the 'master' branch.";
    exit 2;
}
if($createBranch -like "feature/*" -or
    $createBranch -like "bugfix/*" -or
    $createBranch -like "refactor/*" -or
    $createBranch -like "performance/*" -or
    $createBranch -like "release/*")
{
    "Cannot overwrite branch '${createBranch}'";
    exit 2;
}

$verifierCommand = '"C:\Program Files (x86)\MSbuild\14.0\Bin\MSBuild.exe" /v:minimal VerifyCanary.Task.proj';
$datestamp = $(Get-Date).ToString("yyyyMMdd-HHmm");

function Run-Git()
{
    pushd "${workingCopy}"
    Try {
        git @args
        if(!$?) { throw "Exit code: $LASTEXITCODE"; }
    } finally {
        popd
    }
}

function Run-CanaryCollector()
{
    ./CanaryCollector.exe --jira "${jiraUri}" --repo "${workingCopy}" --tag Canary --pending -v;
    if(!$?) { throw "CanaryCollector exited with code $LASTEXITCODE"; }
}

function Put-Parameter($name, $value)
{
	Write-Host "##teamcity[setParameter name='$name' value='$value']"
}

function Configure-TeamCityOutputProperties()
{
    if(!$teamcityProjectId) { return; }
    "Configuring TeamCity output";
    Put-Parameter "teamcity.build.branch" "${createBranch}";
    Put-Parameter "vcsroot.branch" "${createBranch}";
    Put-Parameter "vcsroot.${teamcityProjectId}.branch" "${createBranch}";
}

Try {
    "Fetching all branches from remotes";
    Run-Git fetch;
    
    Try {
        $existingTargetBranch = Run-Git rev-parse "${createBranch}";
        if($existingTargetBranch)
        {
            "Previous ${createBranch}: ${existingTargetBranch}";
            "Cleaning old target branch";
            # Clean up the previous build, if necessary.
            Run-Git branch -D "${createBranch}";
        }
        else
        {
            "No previous ${createBranch} found.";
        }
    } catch {
       "Error cleaning up old target branch"
    }
    
    "Collecting candidate branches";
    $branches = @( Run-CanaryCollector );
    
    "
start at: ${baseBranch}
produce branch: ${createBranch}
verify with: ${verifierCommand}
verify merges with: ${verifierCommand}
$(${branches} |% { "merge: $_" } | Out-String)
" | Set-Content canary.merge

    if($tagPrefix)
    {
        "produce tag: ${tagPrefix}${datestamp}" | Add-Content canary.merge;
    }

    "Building target branch";
    ./CanaryBuilder.exe merge canary.merge "${workingCopy}"
    if(!$?) { throw "CanaryBuilder exited with code $LASTEXITCODE"; }
    
    "Pushing target branch";
    Run-Git push -f "${downstream}" "${createBranch}";
    
    if($tagPrefix)
    {
        "Pushing target tag";
        Run-Git push -f "${downstream}" "${tagPrefix}${datestamp}";
    }
    
    Configure-TeamCityOutputProperties;
    
} catch {
    Write-Host $_;
    exit 1;
}

