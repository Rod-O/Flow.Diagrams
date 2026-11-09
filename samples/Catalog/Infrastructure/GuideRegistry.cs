using System.Text.RegularExpressions;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Catalog.Infrastructure;

/// <summary>A piece of a guide: rendered Markdown, a live example, a source excerpt, or a reference that did not resolve.</summary>
public abstract record GuidePart;
public sealed record MarkdownPart(string Html) : GuidePart;
public sealed record ExamplePart(ExampleInfo Example) : GuidePart;
public sealed record SourcePart(string FileName, string Text) : GuidePart;
public sealed record BrokenPart(string Message) : GuidePart;

public sealed record GuideInfo(string Id, int Order, string Title, string Summary, string Markdown);

/// <summary>
/// Every guide in <c>Guides/*.md</c>, named <c>NN-id.md</c>. A guide is Markdown with two directives on lines of their own:
/// <c>{{example id}}</c> embeds that example live, and <c>{{source id File.razor [region]}}</c> shows one of its source
/// files, or the lines between <c>#region name</c> and <c>#endregion</c>, so guide code is always code that compiles.
/// Links to <c>xref:Uid</c> go to the API reference.
/// </summary>
public static partial class GuideRegistry
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    public static IReadOnlyList<GuideInfo> All { get; } = Load();

    public static GuideInfo? Find(string id) => All.FirstOrDefault(g => g.Id == id);

    public static IReadOnlyList<GuidePart> Render(GuideInfo guide)
    {
        var parts = new List<GuidePart>();
        var markdown = new List<string>();
        foreach (var line in BodyLines(guide.Markdown))
        {
            var directive = Directive().Match(line);
            if (!directive.Success)
            {
                markdown.Add(line);
                continue;
            }

            Flush();
            parts.Add(Resolve(directive.Groups["kind"].Value, directive.Groups["args"].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries)));
        }
        Flush();
        return parts;

        void Flush()
        {
            if (markdown.Count == 0)
                return;
            parts.Add(new MarkdownPart(ToHtml(string.Join('\n', markdown))));
            markdown.Clear();
        }
    }

    private static GuidePart Resolve(string kind, string[] args)
    {
        if (args.Length == 0 || ExampleRegistry.Find(args[0]) is not { } example)
            return new BrokenPart($"{{{{{kind} {string.Join(' ', args)}}}}}: no such example.");
        if (kind == "example")
            return new ExamplePart(example);
        if (args.Length < 2)
            return new BrokenPart($"{{{{source {args[0]}}}}}: which file?");

        var file = ExampleRegistry.Source(example).FirstOrDefault(f => f.Name == args[1]);
        if (file.Name == null)
            return new BrokenPart($"{{{{source {args[0]} {args[1]}}}}}: the example has no such file.");
        if (args.Length < 3)
            return new SourcePart(file.Name, file.Text.Trim());

        var region = Region(file.Text, args[2]);
        return region == null
            ? new BrokenPart($"{{{{source {args[0]} {args[1]} {args[2]}}}}}: no #region {args[2]} in the file.")
            : new SourcePart(file.Name, region);
    }

    private static string? Region(string text, string name)
    {
        var lines = text.Split('\n').Select(l => l.TrimEnd('\r')).ToList();
        var start = lines.FindIndex(l => Regex.IsMatch(l, $@"#region\s+{Regex.Escape(name)}\b"));
        if (start < 0)
            return null;
        var end = lines.FindIndex(start + 1, l => l.Contains("#endregion"));
        if (end < 0)
            return null;

        var body = lines.Skip(start + 1).Take(end - start - 1).ToList();
        var indent = body.Where(l => l.Trim().Length > 0).Select(l => l.Length - l.TrimStart().Length).DefaultIfEmpty(0).Min();
        return string.Join('\n', body.Select(l => l.Length >= indent ? l[indent..] : l.TrimStart()));
    }

    private static string ToHtml(string markdown)
    {
        var document = Markdown.Parse(markdown, Pipeline);
        foreach (var link in document.Descendants<LinkInline>())
            if (link.Url?.StartsWith("xref:", StringComparison.Ordinal) == true)
            {
                var uid = link.Url["xref:".Length..];
                link.Url = $"reference/api/{uid}.html";
                if (link.IsAutoLink)
                {
                    link.Clear();
                    link.AppendChild(new CodeInline(uid[(uid.LastIndexOf('.') + 1)..]));
                }
            }
        return document.ToHtml(Pipeline);
    }

    private static IEnumerable<string> BodyLines(string markdown) =>
        markdown.Split('\n').Select(l => l.TrimEnd('\r')).SkipWhile(l => !l.StartsWith("# ", StringComparison.Ordinal)).Skip(1);

    private static List<GuideInfo> Load()
    {
        var assembly = typeof(GuideRegistry).Assembly;
        return assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith("guides/", StringComparison.Ordinal))
            .Select(n =>
            {
                using var reader = new StreamReader(assembly.GetManifestResourceStream(n)!);
                var markdown = reader.ReadToEnd();
                var name = Path.GetFileNameWithoutExtension(n["guides/".Length..]);
                var dash = name.IndexOf('-');
                var lines = markdown.Split('\n').Select(l => l.TrimEnd('\r')).ToList();
                var title = lines.First(l => l.StartsWith("# ", StringComparison.Ordinal))[2..];
                var summary = BodyLines(markdown).SkipWhile(string.IsNullOrWhiteSpace).TakeWhile(l => !string.IsNullOrWhiteSpace(l));
                return new GuideInfo(name[(dash + 1)..], int.Parse(name[..dash]), title, Markdown.ToPlainText(string.Join(' ', summary)).Trim(), markdown);
            })
            .OrderBy(g => g.Order)
            .ToList();
    }

    [GeneratedRegex(@"^\{\{(?<kind>example|source)\s+(?<args>[^}]*)\}\}\s*$")]
    private static partial Regex Directive();
}
