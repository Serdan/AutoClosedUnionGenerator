# AutoClosedUnionGenerator

Using this package requires ExhaustiveMatching.Analyzer

The following:

```csharp
[AutoClosed]
public abstract partial record AgentMessage
{
    public sealed partial record ListProjectsMessage;

    public sealed partial record OpenProjectMessage(string ProjectName);

    public sealed partial record ListProjectDirectoryMessage(string ProjectName, string Path);
}
```

Generates this:

```csharp
[Closed(typeof(ListProjectsMessage), typeof(OpenProjectMessage), typeof(ListProjectDirectoryMessage))]
public abstract partial record AgentMessage
{
    private AgentMessage() { }

    public sealed partial record ListProjectsMessage: AgentMessage;

    public sealed partial record OpenProjectMessage: AgentMessage;

    public sealed partial record ListProjectDirectoryMessage: AgentMessage;

    public static class Cons
    {
        public static AgentMessage ListProjectsMessage { get; } = new ListProjectsMessage();

        public static AgentMessage OpenProjectMessage(string ProjectName) => new OpenProjectMessage(ProjectName);

        public static AgentMessage ListProjectDirectoryMessage(string ProjectName, string Path) => new ListProjectDirectoryMessage(ProjectName, Path);
    }
}
```
