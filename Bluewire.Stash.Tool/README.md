# Bluewire.Stash

Implements file storage 'alongside' a topologically versioned Git repository.

Original purpose: enable easy snapshotting and usage of sample databases against the appropriate codebase
version, for the Epro project.

## Design

The original use case here is to find 'appropriate' database snapshots which can be safely migrated for use by the codebase of the
current commit or version.
* On developer machines we're working with commits and have access to topological information for the repository.
* On a test server we're working with a version number only, and repository topology is probably not available.
  * However, a test server is probably running one of our 'canonical' versions and detailed topological info is not necessary.

To make snapshots available to test servers, there will need to be some kind of central storage. This means that this tool will
need to be able to operate against remote storage, ie. cannot assume fast access to local disk. Algorithm design should keep this
in mind, but for developer use we will almost certainly be operating against local-only snapshots.

Let's take an idea from Git and define the following operations:
* ##commit##: locally store the contents of a directory against a version/hash.
* ##checkout##: given a version/hash, find the 'closest' one in the local stash and populate a directory with that.
* ##pull##: given a version/hash, find the 'closest' one in the remote stash and copy it to the local stash.
* ##push##: given a version/hash, find the 'closest' one in the local stash and copy it to the remote stash.

We will also need the following, for maintenance of the stash:
* ##list##: list all version/hash entries in the local stash.
* ##show##: given a version/hash, output the 'closest' one in the local stash.
* ##diagnostics##, ##diag##: show configuration/environment information.
* ##delete##: delete a specific version/hash from the local stash.
* ##gc##: clean up any temporary or orphaned files.

For now, assume that remotes are append-only.

Initial implementation will not consider remotes, providing only local stashes.

## Appendix A: The Git repository must be 'topologically versioned'

This means that a commit's version number depends canonically upon the repository structure.

In practice this is difficult to achieve, since the Git commit graph can be complex and generally defies
description with a single version number.

So, we're going to assume the following to make things tractable:
* The version number format is ##major.minor.build-tag##.
* For consistency over time, the version number *may only depend on the ancestry of the subject commit*. No siblings may be considered.
* ##major.minor## are determined reliably somehow, ideally by a ##.current-version## file in the root. This also points to a tag
  in the first-parent ancestry of every commit.
  * The tagged commit is also the one which updates this file, eg. the commit tagged '20.21' stored '20.21' in ##.current-version##.
* ##build## is the total number of commits between the subject commit and the ##major.minor## tag.
  * So the tagged commit is always ##major.minor.0##.
* The ##-tag## disambiguates ##build##.
  * Because a Git repository is a tree, there can be an arbitrary number of commits described as 'X commits away'.
  * The ##-tag## usually describes an *end point*. The subject commit lies in the *first-parent ancestry* of this end point.
  * Known tag types:
    * ##-release## maps to the branch ##release/major.minor##
    * ##-rc## maps to the branch ##candidate/major.minor##
    * ##-beta## maps to the branch ##backport/major.minor##, or ##master## if that does not exist.
      * The assumption here is that ##backport/major.minor## diverges from ##master## immediately before the next ##major.minor##
        version's tag.
    * All other tag types are 'non-canonical' and indicate that the version number alone cannot identify a single commit.

See https://xwiki.epro.com/xwiki/bin/view/Dev/Developer%20Information/Versioning/

The original design of this versioning system was ambiguous and assumed that the end point was known up-front, ie. that it was
the branch being checked out on the CI server.

As part of the development of ##Bluewire.Stash## it needed to be possible to version commits without prior knowledge of the end
point, or more accurately, we needed a way to determine the end point from the commit. The algorithm for doing so is as follows:

1. If the commit is in the first-parent ancestry of ##backport/major.minor##, the tag is ##-beta##.
1. If the commit is in the first-parent ancestry of ##master##, the tag is ##-beta##.
1. If the commit is in the first-parent ancestry of ##candidate/major.minor##, the tag is ##-rc##.
1. If the commit is in the first-parent ancestry of ##release/major.minor##, the tag is ##-release##.
1. Otherwise, the tag is ##-alpha.gxxxxxxxxxx## where ##xxxxxxxxxx## is the first ten characters of the commit's SHA1.

These algorithms are implemented/referenced in [GitCommitTopology.cs](Bluewire.Stash.Tool/GitCommitTopology.cs).
