# Sample Data Export

WARNING: DO NOT USE ANY TOOLS IN THIS DIRECTORY UNTIL YOU HAVE READ AND
UNDERSTOOD THE FOLLOWING:

* https://xwiki.epro.com/xwiki/bin/view/Dev/Codebases/Epro/Server/Sample%20Data
* The contents of the Snapshot-SampleData.ps1 script.

## Requirements

* Your Azure AD account must be granted the Stash.Add role in order to save
  new snapshots.
* Your Azure AD account must be granted the Stash.Admin role in order to
  overwrite existing snapshots.
* A recent version of Bluewire.Stash.Tool.exe should be placed in `tools/`

## Usage

Syntax is essentially:

```
./Snapshot-SampleData.ps1 <version>
```

where 'version' *MUST* be an installed, currently-running 'Live' Epro instance.

