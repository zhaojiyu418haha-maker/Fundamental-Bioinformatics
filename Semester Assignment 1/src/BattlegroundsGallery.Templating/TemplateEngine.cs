using System.Collections;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace BattlegroundsGallery.Templating;

/// <summary>
/// Small Razor-like template engine implemented without ASP.NET Core.
///
/// Supported syntax:
///   {{ model.Title }}                 encoded output
///   {{{ model.Html }}}                raw output
///   @if (model.Items.Count > 0) ... @else ... @endif
///   @foreach (var item in model.Items) ... @endforeach
/// </summary>
public sealed partial class TemplateEngine : ITemplateEngine
{
    public string Render(string template, object? model = null)
    {
        ArgumentNullException.ThrowIfNull(template);

        var nodes = new Parser(template).Parse();
        var output = new StringBuilder(template.Length + 64);
        var context = new RenderContext(model);

        foreach (var node in nodes)
        {
            node.Render(output, context);
        }

        return output.ToString();
    }

    private sealed class RenderContext
    {
        private readonly Dictionary<string, object?> _locals =
            new(StringComparer.OrdinalIgnoreCase);

        public RenderContext(object? model, RenderContext? parent = null)
        {
            Model = parent?.Model ?? model;
            Parent = parent;
        }

        public object? Model { get; }

        public RenderContext? Parent { get; }

        public void SetLocal(string name, object? value) => _locals[name] = value;

        public RenderContext CreateChild() => new(Model, this);

        public bool TryResolve(string name, out object? value)
        {
            if (_locals.TryGetValue(name, out value))
            {
                return true;
            }

            if (name.Equals("model", StringComparison.OrdinalIgnoreCase))
            {
                value = Model;
                return true;
            }

            if (Parent is not null)
            {
                return Parent.TryResolve(name, out value);
            }

            value = null;
            return false;
        }
    }

    private abstract class Node
    {
        public abstract void Render(StringBuilder output, RenderContext context);
    }

    private sealed class TextNode : Node
    {
        private readonly string _text;

        public TextNode(string text)
        {
            _text = text;
        }

        public override void Render(StringBuilder output, RenderContext context)
            => output.Append(_text);
    }

    private sealed class ExpressionNode : Node
    {
        private readonly string _expression;
        private readonly bool _raw;

        public ExpressionNode(string expression, bool raw)
        {
            _expression = expression;
            _raw = raw;
        }

        public override void Render(StringBuilder output, RenderContext context)
        {
            var value = ExpressionEvaluator.Evaluate(_expression, context);
            var text = ExpressionEvaluator.Format(value);
            output.Append(_raw ? text : WebUtility.HtmlEncode(text));
        }
    }

    private sealed class IfNode : Node
    {
        private readonly string _condition;
        private readonly IReadOnlyList<Node> _trueNodes;
        private readonly IReadOnlyList<Node> _falseNodes;

        public IfNode(string condition, IReadOnlyList<Node> trueNodes, IReadOnlyList<Node> falseNodes)
        {
            _condition = condition;
            _trueNodes = trueNodes;
            _falseNodes = falseNodes;
        }

        public override void Render(StringBuilder output, RenderContext context)
        {
            var branch = ExpressionEvaluator.IsTruthy(
                ExpressionEvaluator.Evaluate(_condition, context))
                ? _trueNodes
                : _falseNodes;

            foreach (var node in branch)
            {
                node.Render(output, context);
            }
        }
    }

    private sealed class ForEachNode : Node
    {
        private readonly string _variableName;
        private readonly string _sourceExpression;
        private readonly IReadOnlyList<Node> _body;

        public ForEachNode(string variableName, string sourceExpression, IReadOnlyList<Node> body)
        {
            _variableName = variableName;
            _sourceExpression = sourceExpression;
            _body = body;
        }

        public override void Render(StringBuilder output, RenderContext context)
        {
            var source = ExpressionEvaluator.Evaluate(_sourceExpression, context);

            if (source is null or string || source is not IEnumerable items)
            {
                return;
            }

            foreach (var item in items)
            {
                var child = context.CreateChild();
                child.SetLocal(_variableName, item);

                foreach (var node in _body)
                {
                    node.Render(output, child);
                }
            }
        }
    }

    private sealed class Parser
    {
        private readonly string _source;
        private int _position;

        public Parser(string source)
        {
            _source = source;
        }

        public IReadOnlyList<Node> Parse()
        {
            var nodes = ParseNodes();

            if (_position < _source.Length)
            {
                throw Error($"Unexpected '{_source[_position]}'");
            }

            return nodes;
        }

        private List<Node> ParseNodes(params string[] stopDirectives)
        {
            var nodes = new List<Node>();
            var text = new StringBuilder();

            void FlushText()
            {
                if (text.Length == 0)
                {
                    return;
                }

                nodes.Add(new TextNode(text.ToString()));
                text.Clear();
            }

            while (_position < _source.Length)
            {
                if (IsAnyDirective(stopDirectives))
                {
                    break;
                }

                if (IsDirective("@if"))
                {
                    FlushText();
                    nodes.Add(ParseIf());
                    continue;
                }

                if (IsDirective("@foreach"))
                {
                    FlushText();
                    nodes.Add(ParseForEach());
                    continue;
                }

                if (IsDirective("@else") || IsDirective("@endif") || IsDirective("@endforeach"))
                {
                    throw Error($"Unexpected directive at position {_position}");
                }

                if (StartsWith("{{"))
                {
                    FlushText();
                    nodes.Add(ParseExpression());
                    continue;
                }

                text.Append(_source[_position]);
                _position++;
            }

            FlushText();
            return nodes;
        }

        private Node ParseIf()
        {
            ExpectDirective("@if");
            var condition = ReadParenthesized("if condition");

            var trueNodes = ParseNodes("@else", "@endif");
            var falseNodes = new List<Node>();

            if (TryConsumeDirective("@else"))
            {
                falseNodes = ParseNodes("@endif");
            }

            ExpectDirective("@endif");
            return new IfNode(condition, trueNodes, falseNodes);
        }

        private Node ParseForEach()
        {
            ExpectDirective("@foreach");
            var header = ReadParenthesized("foreach header");

            var match = ForEachHeaderPattern().Match(header);
            if (!match.Success)
            {
                throw Error("Expected '@foreach (var item in collection)'");
            }

            var body = ParseNodes("@endforeach");
            ExpectDirective("@endforeach");

            return new ForEachNode(
                match.Groups["name"].Value,
                match.Groups["source"].Value.Trim(),
                body);
        }

        private Node ParseExpression()
        {
            var raw = StartsWith("{{{");
            var open = raw ? "{{{" : "{{";
            var close = raw ? "}}}" : "}}";

            _position += open.Length;
            var end = _source.IndexOf(close, _position, StringComparison.Ordinal);

            if (end < 0)
            {
                throw Error($"Missing closing '{close}'");
            }

            var expression = _source[_position..end].Trim();
            if (expression.Length == 0)
            {
                throw Error("Empty expression");
            }

            _position = end + close.Length;
            return new ExpressionNode(expression, raw);
        }

        private string ReadParenthesized(string description)
        {
            SkipWhitespace();

            if (_position >= _source.Length || _source[_position] != '(')
            {
                throw Error($"Expected '(' after {description}");
            }

            _position++;
            var start = _position;
            var depth = 1;
            var quote = '\0';

            while (_position < _source.Length)
            {
                var current = _source[_position];

                if (quote != '\0')
                {
                    if (current == '\\')
                    {
                        _position += 2;
                        continue;
                    }

                    if (current == quote)
                    {
                        quote = '\0';
                    }

                    _position++;
                    continue;
                }

                if (current is '\'' or '"')
                {
                    quote = current;
                    _position++;
                    continue;
                }

                if (current == '(')
                {
                    depth++;
                }
                else if (current == ')')
                {
                    depth--;

                    if (depth == 0)
                    {
                        var content = _source[start.._position];
                        _position++;
                        return content;
                    }
                }

                _position++;
            }

            throw Error("Unclosed parenthesis");
        }

        private void SkipWhitespace()
        {
            while (_position < _source.Length && char.IsWhiteSpace(_source[_position]))
            {
                _position++;
            }
        }

        private bool StartsWith(string value)
            => _source.AsSpan(_position).StartsWith(value, StringComparison.Ordinal);

        private bool IsAnyDirective(params string[] names)
            => names.Any(IsDirective);

        private bool IsDirective(string name)
        {
            if (!_source.AsSpan(_position).StartsWith(name, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var next = _position + name.Length;
            return next >= _source.Length
                   || (!char.IsLetterOrDigit(_source[next]) && _source[next] != '_');
        }

        private bool TryConsumeDirective(string name)
        {
            if (!IsDirective(name))
            {
                return false;
            }

            _position += name.Length;
            return true;
        }

        private void ExpectDirective(string name)
        {
            if (!TryConsumeDirective(name))
            {
                throw Error($"Expected '{name}'");
            }
        }

        private InvalidOperationException Error(string message)
            => new($"Template parse error at position {_position}: {message}");
    }

    private static class ExpressionEvaluator
    {
        public static object? Evaluate(string expression, RenderContext context)
        {
            expression = expression.Trim();
            if (expression.Length == 0)
            {
                return null;
            }

            if (expression.StartsWith('!') && !expression.StartsWith("!=", StringComparison.Ordinal))
            {
                return !IsTruthy(Evaluate(expression[1..], context));
            }

            if (TryReadString(expression, out var stringValue))
            {
                return stringValue;
            }

            if (bool.TryParse(expression, out var boolValue))
            {
                return boolValue;
            }

            if (decimal.TryParse(expression, NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
            {
                return number;
            }

            foreach (var operation in ComparisonOperators)
            {
                var index = expression.IndexOf(operation, StringComparison.Ordinal);
                if (index <= 0)
                {
                    continue;
                }

                var left = Evaluate(expression[..index], context);
                var right = Evaluate(expression[(index + operation.Length)..], context);

                return operation switch
                {
                    "==" => AreEqual(left, right),
                    "!=" => !AreEqual(left, right),
                    ">" => Compare(left, right) > 0,
                    ">=" => Compare(left, right) >= 0,
                    "<" => Compare(left, right) < 0,
                    "<=" => Compare(left, right) <= 0,
                    _ => false,
                };
            }

            return ResolvePath(expression, context);
        }

        public static bool IsTruthy(object? value)
        {
            return value switch
            {
                null => false,
                bool boolean => boolean,
                string text => text.Length > 0,
                byte number => number != 0,
                short number => number != 0,
                int number => number != 0,
                long number => number != 0,
                float number => number != 0,
                double number => number != 0,
                decimal number => number != 0,
                IEnumerable sequence => sequence.GetEnumerator().MoveNext(),
                _ => true,
            };
        }

        public static string Format(object? value)
        {
            return value switch
            {
                null => string.Empty,
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString() ?? string.Empty,
            };
        }

        private static object? ResolvePath(string expression, RenderContext context)
        {
            var parts = expression.Split(
                '.',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (parts.Length == 0 || !context.TryResolve(parts[0], out var current))
            {
                return null;
            }

            foreach (var part in parts.Skip(1))
            {
                if (current is null)
                {
                    return null;
                }

                var property = current.GetType().GetProperty(
                    part,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                if (property is null)
                {
                    return null;
                }

                current = property.GetValue(current);
            }

            return current;
        }

        private static bool TryReadString(string expression, out string value)
        {
            value = string.Empty;

            if (expression.Length < 2)
            {
                return false;
            }

            var quote = expression[0];
            if (quote is not ('\'' or '"') || expression[^1] != quote)
            {
                return false;
            }

            value = expression[1..^1];
            return true;
        }

        private static bool AreEqual(object? left, object? right)
        {
            if (left is null || right is null)
            {
                return left is null && right is null;
            }

            if (TryConvertDecimal(left, out var leftNumber)
                && TryConvertDecimal(right, out var rightNumber))
            {
                return leftNumber == rightNumber;
            }

            return string.Equals(
                Format(left),
                Format(right),
                StringComparison.Ordinal);
        }

        private static int Compare(object? left, object? right)
        {
            if (TryConvertDecimal(left, out var leftNumber)
                && TryConvertDecimal(right, out var rightNumber))
            {
                return leftNumber.CompareTo(rightNumber);
            }

            return string.CompareOrdinal(Format(left), Format(right));
        }

        private static bool TryConvertDecimal(object? value, out decimal result)
        {
            switch (value)
            {
                case byte number:
                    result = number;
                    return true;
                case short number:
                    result = number;
                    return true;
                case int number:
                    result = number;
                    return true;
                case long number:
                    result = number;
                    return true;
                case float number:
                    result = (decimal)number;
                    return true;
                case double number:
                    result = (decimal)number;
                    return true;
                case decimal number:
                    result = number;
                    return true;
                case string text:
                    return decimal.TryParse(
                        text,
                        NumberStyles.Number,
                        CultureInfo.InvariantCulture,
                        out result);
                default:
                    result = 0;
                    return false;
            }
        }

        private static readonly string[] ComparisonOperators =
        [
            ">=",
            "<=",
            "==",
            "!=",
            ">",
            "<",
        ];
    }

    [GeneratedRegex(
        @"^\s*(?:var\s+)?(?<name>[A-Za-z_][A-Za-z0-9_]*)\s+in\s+(?<source>.+?)\s*$",
        RegexOptions.Compiled | RegexOptions.Singleline)]
    private static partial Regex ForEachHeaderPattern();
}
