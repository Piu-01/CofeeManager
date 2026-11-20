using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

var root = Path.GetFullPath(args.Length > 1 ? args[1] : ".");
var mode = args.FirstOrDefault() ?? "audit";
if (mode is not ("audit" or "ascii")) throw new ArgumentException("Use audit or ascii.");
var entries = new List<Entry>();
foreach (var path in Directory.EnumerateFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories)
    .Where(p => !p.Split(Path.DirectorySeparatorChar).Any(s => s is "bin" or "obj") && !p.Contains("Localization")))
{
    var source = File.ReadAllText(path);
    var syntax = CSharpSyntaxTree.ParseText(source).GetRoot();
    if (mode == "ascii")
    {
        foreach (var literal in syntax.DescendantNodes().OfType<LiteralExpressionSyntax>().Where(n => n.IsKind(SyntaxKind.StringLiteralExpression)))
        {
            var value = literal.Token.ValueText;
            if (value.Length > 1 && value.Length < 150 && value.All(c => c < 128) && value.Any(char.IsLetter) &&
                (value.Contains(' ') || literal.Parent is AssignmentExpressionSyntax a && a.Left.ToString().EndsWith("Text")) &&
                !literal.Ancestors().OfType<InvocationExpressionSyntax>().Any(i => i.Expression.ToString() is "L.T" or "L.F"))
                Console.WriteLine(Path.GetRelativePath(root,path) + ":" + literal.GetLocation().GetLineSpan().StartLinePosition.Line + " | " + value);
        }
        continue;
    }
    new LiteralCollector(Path.GetRelativePath(root, path).Replace('\\', '/'), entries).Visit(syntax);
}
if (mode == "ascii") return;
var output = Path.Combine(root, "docs/tasks/localization-vi-en");
Directory.CreateDirectory(output);
var dictionaryPath = Path.Combine(root, "src/CafeManager.Models/Localization/Translations/en.json");
using var document = JsonDocument.Parse(File.ReadAllText(dictionaryPath));
var properties = document.RootElement.EnumerateObject().ToArray();
var translations = properties.ToDictionary(p => p.Name, p => p.Value.GetString()!);
var problems = new List<string>();
var used = entries.Where(e => e.Status == "localized" || e.Reason == "Stable command/profile; translated at consumer").Select(e => e.Text.Replace("\r\n", "\n")).ToHashSet();
foreach (var entry in entries.Where(e => e.Status == "translate")) problems.Add($"Unwrapped: {entry.File}:{entry.Line}: {entry.Text}");
foreach (var key in used) if (!translations.ContainsKey(key)) problems.Add($"Missing: {key}");
foreach (var (key, value) in translations)
{
    if (!used.Contains(key)) problems.Add($"Unused: {key}");
    if (string.IsNullOrWhiteSpace(value)) problems.Add($"Empty: {key}");
    var sourceFormat = System.Text.CompositeFormat.Parse(key);
    var targetFormat = System.Text.CompositeFormat.Parse(value);
    if (sourceFormat.MinimumArgumentCount != targetFormat.MinimumArgumentCount ||
        string.Join("|", Regex.Matches(key, @"(?<!\{)\{\d+[^}]*\}").Select(m => m.Value).Order()) !=
        string.Join("|", Regex.Matches(value, @"(?<!\{)\{\d+[^}]*\}").Select(m => m.Value).Order())) problems.Add($"Placeholder: {key}");
}
static string Csv(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";
File.WriteAllLines(Path.Combine(output, "inventory.csv"), new[] { "file,line,vi,en,status,reason" }.Concat(entries.Select(e =>
    string.Join(",", Csv(e.File), e.Line, Csv(e.Text), Csv(translations.GetValueOrDefault(e.Text.Replace("\r\n", "\n"), "")), Csv(e.Status == "localized" ? "translated" : e.Status), Csv(e.Reason)))), new UTF8Encoding(true));
Console.WriteLine($"{used.Count} used keys; {translations.Count} dictionary keys; {entries.Count(e => e.Status == "keep")} kept literals; {problems.Count} violations.");
foreach (var problem in problems) Console.WriteLine(problem);
Environment.ExitCode = problems.Count == 0 ? 0 : 1;

record Entry(string File, int Line, string Text, string Status, string Reason);

sealed class LiteralCollector(string file, List<Entry> entries) : CSharpSyntaxWalker
{
    private static bool Vietnamese(string value) => value.Any(c => c is >= '\u00c0' and <= '\u01b0' or >= '\u1ea0' and <= '\u1ef9');
    private static bool DisplayText(string value) => Vietnamese(value) || AsciiLabels.Contains(value) || Regex.IsMatch(value, @"\bTrang ");
    private static readonly HashSet<string> AsciiLabels = ["SL", "In…", "SDT", "&SDT", "SĐT", "NCC", "Thu", "Chi", "Kho", "Thu chi", "Ghi tay", "Ca", "Ra", "Trang sau", " trong trang", "Menu & kho", "Thu / chi", " theo SQL Server.", " ly", "Tra request ID", "Xem preview SQL"];
    private string Keep(SyntaxNode node)
    {
        if (node.Ancestors().Any(a => a is ConstantPatternSyntax or CaseSwitchLabelSyntax or AttributeSyntax)) return "Constant pattern/attribute; logic code";
        if (node.Ancestors().Any(a => a is ParameterSyntax)) return "Default parameter; translated at consumer";
        if (node.Parent is BinaryExpressionSyntax b && b.Kind() is SyntaxKind.EqualsExpression or SyntaxKind.NotEqualsExpression) return "Stable command/comparison";
        if (node.Parent is ArgumentSyntax arg && arg.Parent?.Parent is InvocationExpressionSyntax call)
        {
            var name = call.Expression.ToString().Split('.').Last();
            var index = call.ArgumentList.Arguments.IndexOf(arg);
            if ((name is "AddAction" or "Register" && index == (name == "Register" ? 1 : 0)) ||
                (name == "Configure" && index == 0 && file.Contains("Modules/"))) return "Stable command/profile; translated at consumer";
        }
        if (node.Parent is AssignmentExpressionSyntax a && a.Left.ToString().Split('.').Last() is "Name" or "DataPropertyName" or "Tag") return "Control/data identity";
        if (file.EndsWith("/Program.cs") && node.Ancestors().OfType<AssignmentExpressionSyntax>().Any(a => a.Left.ToString() == "configurationError")) return "Stable command/profile; translated at consumer";
        if (file.EndsWith("FinanceControl.cs") && node.Parent is SwitchExpressionArmSyntax && node is LiteralExpressionSyntax code && code.Token.ValueText is "Thu" or "Chi") return "Data contract code";
        return "";
    }
    private void Add(SyntaxNode node, string text, string status, string reason = "") => entries.Add(new(file, node.SyntaxTree.GetLineSpan(node.Span).StartLinePosition.Line + 1, text, status, reason));
    public override void VisitLiteralExpression(LiteralExpressionSyntax node)
    {
        if (!node.IsKind(SyntaxKind.StringLiteralExpression)) return;
        var value = node.Token.ValueText;
        var localized = node.Parent is ArgumentSyntax && node.Parent.Parent?.Parent is InvocationExpressionSyntax inv && (inv.Expression.ToString() is "L.T" or "L.F");
        if (localized) { Add(node, value, "localized"); return; }
        if (!Vietnamese(value) && !(AsciiLabels.Contains(value) && file.Contains("WinForms"))) return;
        var reason = Keep(node);
        // ASCII domain codes require a deliberate display wrapper at their consumer.
        Add(node, value, reason.Length == 0 ? "translate" : "keep", reason);
    }
    public override void VisitInterpolatedStringExpression(InterpolatedStringExpressionSyntax node)
    {
        base.VisitInterpolatedStringExpression(node);
        if (!node.Contents.OfType<InterpolatedStringTextSyntax>().Any(t => DisplayText(t.TextToken.ValueText))) return;
        var format = new StringBuilder(); var index = 0;
        foreach (var content in node.Contents)
        {
            if (content is InterpolatedStringTextSyntax t) format.Append(t.TextToken.ValueText);
            else if (content is InterpolationSyntax i)
            {
                format.Append('{').Append(index++).Append(i.AlignmentClause).Append(i.FormatClause).Append('}');
            }
        }
        Add(node, format.ToString(), "translate");
    }
}
