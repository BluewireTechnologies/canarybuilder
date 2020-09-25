# Bluewire.Tools.RepoHotspots

Identifies paths touched by tickets.

## Usage

You will need a RavenDB instance running on an accessible machine.

```
Bluewire.Tools.RepoHotspots.exe --ravendb [Database@]<ravendb-url> --root-name <root>  [--repo <repository-path>] [--jira <jira-url>]
```

* `ravendb-url` is an endpoint with an optional database specified (default database: CodeStats).
* `root` is a name describing the repo and/or Jira instance in use, eg. 'epro'.
* `repository-path` is the path to either a working copy or the `.git` folder itself.
* `jira-url` is the URL of a Jira instance, with appropriate credentials for reading ticket info.

If neither `--repo` nor `--jira` are specified, the tool does nothing.

Git commits will be written to the database as documents keyed as `<root>.git/<sha1>`.
Jira tickets will be written to the database as documents keyed as `<root>.jira/<ticket-id>`

## Approximate Design

Epro repo is currently ~35000 commits. Jira has ~36000 E tickets.

Information we need per commit:
* Pathnames affected.
* Related ticket.

Determining the related ticket is often doable from commit message, but sometimes we must look at the merge commit to get a branch name.

What do we do with commits with no ticket? - Ignore.

1. Gather commit info and topology.
2. Propagate commit info from merges.
3. Write the whole lot to a RavenDB instance.

### Gather commit info and topology

`git log master --name-status`
`git log master..<branch> --name-status`

* Assume ~80 chars per pathname (fairly normal in our codebase). 
* Assume ~5 files per commit.
* 400 chars per commit, for pathname.
* ~35K commits. Assume ~30K are not merges.

120M characters of pathnames, or 240MB, not including object reference overheads.

However, a lot of the path segments are heavily repeated. We only have a few tens of thousands of files.

We can use string-interning for the pathnames, or an upsert table in SQLite.
* Better: use string-interning for the path segments, and immutable arrays for pathnames. Enables easier tree construction too.

Info per commit:
* Is this a merge?
* Ticket numbers
* Pathnames

### Propagate commit info from merges

Start at branch tips and work backwards: `master` and `release/*`

Rules:
* Skip any commits already handled by a previous branch trace.
* Do `master` first, then `release/*` in major.minor order.
* Trace first-parent first.
* Trace 2nd+ merge parents second, remembering ticket context based on the merge commit (if available).
* Trace merges oldest-first, ie. LIFO as we work backwards during the scan.

So we need a LIFO queue of commits to trace from, and a set of already-seen commits.

* LIFO queue of SHA1
* Dictionary of SHA1 to commit info.

### Persistence?

Once all data has been hauled into memory and patched up, bulk-upsert into a RavenDB instance.
