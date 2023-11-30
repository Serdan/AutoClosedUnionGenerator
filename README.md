# AutoClosedUnionGenerator

This package includes a dependency on ExhaustiveMatching.Analyzer

The following:

```csharp
[AutoClosed]
public partial record AgentMessage
{
    partial record ListProjectsMessage;

    partial record OpenProjectMessage(string ProjectName);

    partial record ListProjectDirectoryMessage(string ProjectName, string Path);
}
```

Generates this:

```csharp
[Closed(typeof(ListProjectsMessage), typeof(OpenProjectMessage), typeof(ListProjectDirectoryMessage))]
abstract public partial record AgentMessage
{
    private AgentMessage() { }

    public sealed partial record ListProjectsMessage: AgentMessage;

    public sealed partial record OpenProjectMessage: AgentMessage;

    public sealed partial record ListProjectDirectoryMessage: AgentMessage;

    public static partial class Cons
    {
        public static AgentMessage ListProjectsMessage { get; } = new ListProjectsMessage();

        public static AgentMessage OpenProjectMessage(string ProjectName) => new OpenProjectMessage(ProjectName);

        public static AgentMessage ListProjectDirectoryMessage(string ProjectName, string Path) => new ListProjectDirectoryMessage(ProjectName, Path);
    }
}
```

Support for JSON serialization will be generated if `true` is passed to the attribute constructor. This does NOT work with generic unions (mainly a question of will, as it should be relatively easy to implement).
