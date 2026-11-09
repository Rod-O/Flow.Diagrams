using System.Text;

namespace Flow.Diagrams.Extensions;

/// <summary>Helpers on <see cref="StringBuilder"/>.</summary>
public static class StringBuilderExtensions
{
    /// <summary>Appends <paramref name="str"/> when <paramref name="condition"/> is <c>true</c>; returns the builder either way, so calls chain.</summary>
    public static StringBuilder AppendIf(this StringBuilder builder, string str, bool condition)
    {
        if (condition) builder.Append(str);

        return builder;
    }
}