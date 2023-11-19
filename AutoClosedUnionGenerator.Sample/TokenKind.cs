using ExhaustiveMatching;

namespace AutoClosedUnionGenerator.Sample;

[AutoClosed(true)]
public partial record TokenKind
{
    partial record Name;

    partial record Age(int Value);
}
