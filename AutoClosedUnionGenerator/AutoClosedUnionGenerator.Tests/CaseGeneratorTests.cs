using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace AutoClosedUnionGenerator.Tests;

public class CaseGeneratorTests
{
    private const string UnionClassText = """
        namespace ExhaustiveMatching
        {
            public class AutoClosedAttribute : Attribute { }
        }

        namespace TestNamespace
        {
            [ExhaustiveMatching.AutoClosedAttribute]
            public partial abstract record TokenKind
            {
                public partial sealed record Number(string Value);
                public partial sealed record Plus;
                public partial sealed record Minus;
            }
            
            [ExhaustiveMatching.AutoClosedAttribute]
            public partial abstract class TokenKind2
            {
                public partial sealed class Number(string Value);
                public partial sealed class Plus;
                public partial sealed class Minus;
            }
        }
        """;

    private const string ExpectedGeneratedRecordText = """
        // <auto-generated/>
        
        using System;
        using ExhaustiveMatching;
        
        namespace TestNamespace;
        
        [Closed(typeof(Number), typeof(Plus), typeof(Minus))]
        public partial abstract record TokenKind
        {
            private TokenKind() { }
        
            public partial sealed record Number: TokenKind;
        
            public partial sealed record Plus: TokenKind;
        
            public partial sealed record Minus: TokenKind;
        
            public static class Cons
            {
                public static TokenKind Number(string Value) => new Number(Value);
        
                public static TokenKind Plus { get; } = new Plus();
        
                public static TokenKind Minus { get; } = new Minus();
            }
        }
        """;

    private const string ExpectedGeneratedClassText = """
        // <auto-generated/>
        
        using System;
        using ExhaustiveMatching;
        
        namespace TestNamespace;
        
        [Closed(typeof(Number), typeof(Plus), typeof(Minus))]
        public partial abstract class TokenKind2
        {
            private TokenKind2() { }
        
            public partial sealed class Number: TokenKind2;
        
            public partial sealed class Plus: TokenKind2;
        
            public partial sealed class Minus: TokenKind2;
        
            public static class Cons
            {
                public static TokenKind2 Number(string Value) => new Number(Value);
        
                public static TokenKind2 Plus { get; } = new Plus();
        
                public static TokenKind2 Minus { get; } = new Minus();
            }
        }
        """;

    [Fact]
    public void GenerateReportMethod()
    {
        // Create an instance of the source generator.
        var generator = new CaseGenerator();

        // Source generators should be tested using 'GeneratorDriver'.
        var driver = CSharpGeneratorDriver.Create(generator);

        // We need to create a compilation with the required source code.
        var compilation = CSharpCompilation.Create(nameof(AutoClosedUnionGenerator),
                                                   new[] { CSharpSyntaxTree.ParseText(UnionClassText) },
                                                   new[]
                                                   {
                                                       // To support 'System.Attribute' inheritance, add reference to 'System.Private.CoreLib'.
                                                       MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
                                                   });

        // Run generators and retrieve all results.
        var runResult = driver.RunGenerators(compilation).GetRunResult();

        // All generated files can be found in 'RunResults.GeneratedTrees'.
        var generatedFileSyntax = runResult.GeneratedTrees
                                           .Where(t => t.FilePath.EndsWith(".g.cs"))
                                           .Select(x => x.GetText().ToString())
                                           .ToArray();
        
        string[] expected = [ExpectedGeneratedRecordText, ExpectedGeneratedClassText];

        // Complex generators should be tested using text comparison.
        Assert.Equal(
            expected,
            generatedFileSyntax
        );
    }
}
