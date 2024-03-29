﻿using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Bluewire.Stash.Tool;

namespace Bluewire.Stash.IntegrationTests.Tool
{
    public class StubApplication : IApplication
    {
        public virtual string GetCurrentDirectory() => @"z:\not set";
        public virtual string GetTemporaryDirectory() => @"z:\not set";
        public virtual string GetUserDataDirectory() => @"z:\not set";
        public virtual string? GetEnvironmentVariable(string name) => @"z:\not set";

        public async Task ShowDiagnostics(TextWriter stdout, DiagnosticsArguments model, CancellationToken token) => Invocations.Add(model);
        public async Task Authenticate(TextWriter stdout, AuthenticateArguments model, CancellationToken token) => Invocations.Add(model);
        public async Task Commit(TextWriter stderr, CommitArguments model, CancellationToken token) => Invocations.Add(model);
        public async Task Checkout(TextWriter stderr, CheckoutArguments model, CancellationToken token) => Invocations.Add(model);
        public async Task List(TextWriter stdout, TextWriter stderr, ListArguments model, CancellationToken token) => Invocations.Add(model);
        public async Task Show(TextWriter stdout, TextWriter stderr, ShowArguments model, CancellationToken token) => Invocations.Add(model);
        public async Task Delete(TextWriter stderr, DeleteArguments model, CancellationToken token) => Invocations.Add(model);
        public async Task RemoteDelete(TextWriter stderr, RemoteDeleteArguments model, CancellationToken token) => Invocations.Add(model);
        public async Task GarbageCollect(TextWriter stderr, GCArguments model, CancellationToken token) => Invocations.Add(model);
        public async Task Push(TextWriter stderr, PushArguments model, CancellationToken token) => Invocations.Add(model);
        public async Task Pull(TextWriter stderr, PullArguments model, CancellationToken token) => Invocations.Add(model);

        public List<object> Invocations { get; } = new List<object>();
    }
}
