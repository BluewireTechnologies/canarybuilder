DotNet wrapper for Git
======================

Goal: expose a .NET API wrapper around the Git command-line tool.

This is NOT an implementation of Git. It is merely a convenience for integrating
with git.exe.

Using this API
--------------

GitSession is used to run Git commands against GitWorkingCopies and GitRepositories.

    // Find git.exe using the PATH environment variable, just as the shell would:
    var git = await GitFinder.FromEnvironment();
    // Create a session which will use this git.exe:
    var session = new GitSession(git);

    var workingCopy = new GitWorkingCopy("@e:\path\to\working\copy");
    
    // Now we have a working copy and a session, let's create a new feature branch from master and start working on it:
    await session.CreateBranchAndCheckout(workingCopy, new Ref("feature/shiny-toy"), new Ref("master")));
    ...
    // Do stuff.
    ...
    await session.AddFile(workingCopy, @"src\project\script.py");
    await session.Commit(workingCopy, "Added a Python script to do shiny stuff");
