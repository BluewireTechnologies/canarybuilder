Bluewire.Common.Console.Client
==============================

Library for invoking CLI applications, consuming their output, and responding to errors.

Console Application Environment
-------------------------------

A Windows process invocation consists of a path to an executable and an argument string. Conceptually the argument string is an array of strings, formatted and escaped appropriately to retain whitespace and other significant characters when it is parsed. When the process terminates it returns an 'exit code', an integer value typically between 0 and 255 inclusive.

Because error conditions may be numerous but success is only success, a 0 exit code indicates successful completion of whatever the process was invoked to do. Any nonzero value indicates failure.
While many Windows applications return 0 unconditionally, any properly-written console tool will only return 0 on success.

A console application is initially connected to three character streams:
* STDIN, the Standard Input. Usually connected to the keyboard device.
* STDOUT, the Standard Output. Usually connected to the terminal or redirected to a file.
* STDERR, the Standard Error. Usually connected to the terminal.

Any or all of these streams may be redirected.

By convention:
* Output on STDOUT should always be 'operations normal'. Error information must not be written to STDOUT. Ideally STDOUT should be machine-parseable, or the application should provide a command-line option to request machine-parseable output.
* Errors, warnings, any information intended specifically for human consumption should be placed on STDERR. While this stream may be read by a machine, it doesn't usually need to be parsed.
* If the tool is not running interactively it must never expect user input on STDIN.


Using this API
--------------

Creating a CLI process and waiting on its completion can be done as follows:

    var cmd = new CommandLine(@"c:\path\to\git.exe", "commit", "--message", "This is a \"commit message\" containing whitespace and double-quotes.");
    var process = cmd.RunFrom(@"e:\path\to\working\copy");
    var exitCode = await process.Completed;

The StdOut and StdErr properties are both IObservable<string>, one line per event. If the amount of output is not large, it can be consumed as follows:

    // Note that this will not complete until the process exits.
    var errors = await process.StdErr.ReadAllLinesAsync();

StdOut and StdErr will both buffer output in memory. Every time you use ReadAllLinesAsync() you'll get the entire buffer again. If the process will generate a lot of output this might not be what you want. To parse StdOut on a line-by-line basis and keep only the parsed results:

    // Note that you must call .ToTask() to start consuming the stream before you use .StopBuffering(), otherwise
    // some output may be lost.
    var parsedOutput = process.StdOut.Select(l => parser.Parse(l)).ToArray().ToTask();
    process.StdOut.StopBuffering();
    var exitCode = await process.Completed;
    return await statusEntries;
