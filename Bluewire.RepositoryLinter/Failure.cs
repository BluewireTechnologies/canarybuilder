using Bluewire.Common.GitWrapper.Model;

namespace Bluewire.RepositoryLinter;

public struct Failure
{
    public SubjectRepository Subject { get; set; }
    public string Message { get; set; }
    public Ref Branch { get; set; }
    public ProjectFile ProjectFile { get; set; }
}
