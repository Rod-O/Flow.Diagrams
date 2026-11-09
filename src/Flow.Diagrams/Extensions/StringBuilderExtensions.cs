using System.Text;

namespace Flow.Diagrams.Extensions;

public static class StringBuilderExtensions
{
    public static StringBuilder AppendIf(this StringBuilder builder, string str, bool condition)
    {
        if (condition) builder.Append(str);

        return builder;
    }
}