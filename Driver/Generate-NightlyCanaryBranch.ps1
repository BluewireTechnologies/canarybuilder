param(
    [string]$createBranch,
    [string]$pushTarget,
    [string]$youtrackUri,
    [string]$workingCopy = "workingCopy"
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

Try {
    
    Run-Git fetch;
    # Clean up the previous build, if necessary.
    Run-Git branch -D "${createBranch}";# 2> $null;
    
    
    $branches = @(./CanaryCollector.exe --youtrack "${youtrackUri}" --repo "${workingCopy}" --pending -v);
    if(!$?) { exit 1; }
    
    "
start at: master
produce branch: ${createBranch}
$(${branches} |% { "merge: $_" } | Out-String)
" | Set-Content canary.merge

    ./CanaryBuilder.exe merge canary.merge "${workingCopy}"
    if(!$?) { exit 1; }
    
    Run-Git push -f "${pushTarget}" "${createBranch}";
    
} catch {
    Write-Host $_;
    exit 1;
}

