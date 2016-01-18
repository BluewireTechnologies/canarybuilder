param(
    [string]$createBranch,
    [string]$youtrackUri,
    [string]$teamcityProjectId,
    [string]$downstream,
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


function Run-Git()
{
    pushd "${workingCopy}"
    Try {
        git @args
    } finally {
        popd
    }
}

function Run-CanaryCollector()
{
    ./CanaryCollector.exe --youtrack "${youtrackUri}" --repo "${workingCopy}" --pending -v;
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
    "Cleaning old target branch";
    # Clean up the previous build, if necessary.
    Run-Git branch -D "${createBranch}" 2> $null;
    
    "Collecting candidate branches";
    $branches = @( Run-CanaryCollector );
    
    "
start at: ${baseBranch}
produce branch: ${createBranch}
$(${branches} |% { "merge: $_" } | Out-String)
" | Set-Content canary.merge

    "Building target branch";
    ./CanaryBuilder.exe merge canary.merge "${workingCopy}"
    if(!$?) { throw "CanaryBuilder exited with code $LASTEXITCODE"; }
    
    "Pushing target branch";
    Run-Git push -f "${downstream}" "${createBranch}";
    
    Configure-TeamCityOutputProperties;
    
} catch {
    Write-Host $_;
    exit 1;
}

