﻿using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace AutoClosedUnionGenerator;

[Generator]
public class CaseGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider.ForAttributeWithMetadataName(
            "ExhaustiveMatching.AutoClosedAttribute",
            Filter,
            Transform
        );

        context.RegisterSourceOutput(provider, Execute);
    }

    private static bool Filter(SyntaxNode node, CancellationToken _) =>
        node.As<TypeDeclarationSyntax>().Modifiers.Any(token => token.IsKind(SyntaxKind.PartialKeyword));

    private static Data Transform(GeneratorAttributeSyntaxContext context, CancellationToken token)
    {
        var containingTypeIdentifier = context.TargetNode.As<TypeDeclarationSyntax>()
                                              .Apply(x => x.Identifier + x.TypeParameterList?.ToString());

        var classSymbol = context.TargetSymbol.As<INamedTypeSymbol>();

        var query = from member in classSymbol.GetTypeMembers()
                    let syntax = member.GetSyntax()
                    where syntax.IsPartial()
                    select new CaseType(member.Name, syntax.GetDeclarationSyntax(), GetArgs(member));

        var members = query.ToImmutableArray();

        var declaration = context.TargetNode.As<TypeDeclarationSyntax>()
                                 .Apply(x => $"{x.Modifiers} {x.Keyword} {containingTypeIdentifier}");

        var ns = classSymbol.ContainingNamespace.ToString();

        return new(classSymbol.Name, containingTypeIdentifier, declaration, ns, members);

        static ImmutableArray<CaseTypeArg> GetArgs(INamedTypeSymbol symbol)
        {
            var constructor = symbol.Constructors.FirstOrDefault(x => x.DeclaredAccessibility == Accessibility.Public);
            if (constructor is null)
            {
                return ImmutableArray<CaseTypeArg>.Empty;
            }

            return constructor.Parameters
                              .Select(x => new CaseTypeArg(x.Type.ToString(), x.Name))
                              .ToImmutableArray();
        }
    }

    private static void Execute(SourceProductionContext context, Data data)
    {
        var types = data.NestedTypes
                        .Select(x => $"typeof({x.Name})")
                        .Apply(x => string.Join(", ", x));

        var memberQuery = from t in data.NestedTypes
                          let s = $"    {t.Declaration}: {data.TypeIdentifier};"
                          group s by true
                          into g
                          select string.Join("\r\n\r\n", g);

        var members = memberQuery.FirstOrDefault();

        var consQuery = from t in data.NestedTypes
                        let s = Cons(data.TypeIdentifier, t)
                        group s by true
                        into g
                        select string.Join("\r\n\r\n", g);

        var cons = consQuery.FirstOrDefault();

        var code = $$"""
            // <auto-generated/>

            using System;
            using ExhaustiveMatching;

            namespace {{data.Namespace}};

            [Closed({{types}})]
            {{data.Declaration}}
            {
                private {{data.Name}}() { }

            {{members}}
            
                public static class Cons
                {
            {{cons}}
                }
            }
            """;

        var sourceText = SourceText.From(code, Encoding.UTF8);
        context.AddSource($"{data.Name}.g.cs", sourceText);

        static string Cons(string unionName, CaseType type)
        {
            var builder = new StringBuilder();
            builder.Append($"        public static {unionName} {type.Name}");

            if (type.Args.IsDefaultOrEmpty)
            {
                builder.Append($$""" { get; } = new {{type.Name}}();""");
            }
            else
            {
                var p = type.Args.Select(x => $"{x.Type} {x.Name}").Apply(x => string.Join(", ", x));
                var a = type.Args.Select(x => $"{x.Name}").Apply(x => string.Join(", ", x));
                builder.Append($"({p}) => new {type.Name}({a});");
            }

            return builder.ToString();
        }
    }

    private record Data(string Name, string TypeIdentifier, string Declaration, string Namespace, ImmutableArray<CaseType> NestedTypes);

    private record CaseTypeArg(string Type, string Name);

    private record CaseType(string Name, string Declaration, ImmutableArray<CaseTypeArg> Args);
}
