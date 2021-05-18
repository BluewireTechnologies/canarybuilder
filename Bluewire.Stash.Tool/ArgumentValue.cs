namespace Bluewire.Stash.Tool
{
    public readonly struct ArgumentValue<T>
    {
        public T Value { get; }
        public ArgumentSource Source { get; }

        public ArgumentValue(T value, ArgumentSource source)
        {
            Value = value;
            Source = source;
        }

        public override string ToString() => $"{(Value is null ? "<null>" : Value.ToString())} (from {Source})";
    }
}
