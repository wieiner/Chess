using System.Text;
using Move = RubikApp.NativeRubikEngine.RubikMoveDto;

namespace RubikApp;

internal static class RubikNotation
{
    public static Move[] Parse(string text)
    {
        return Parse(text, 8);
    }

    public static Move[] Parse(string text, int size)
    {
        var parser = new Parser(text, size);
        return parser.ParseAll().ToArray();
    }

    public static string FormatEngineMoves(IEnumerable<Move> moves)
    {
        return string.Join(' ', moves.Select(FormatEngineMove));
    }

    public static string FormatEngineMove(Move move)
    {
        var axis = move.Axis switch
        {
            2 => "X",
            1 => "Y",
            _ => "Z"
        };
        var suffix = NormalizeTurns(move.QuarterTurns) switch
        {
            2 => "x2",
            3 => "'",
            _ => ""
        };
        return $"{axis}{move.Layer + 1}{suffix}";
    }

    private static int NormalizeTurns(int turns)
    {
        turns %= 4;
        return turns < 0 ? turns + 4 : turns;
    }

    private sealed class Parser
    {
        private readonly string _text;
        private readonly int _size;
        private int _index;

        public Parser(string text, int size)
        {
            _text = text ?? "";
            _size = Math.Clamp(size, 2, 32);
        }

        public List<Move> ParseAll()
        {
            var result = ParseSequence(stopOnParen: false);
            SkipTrivia();
            if (_index < _text.Length)
            {
                throw Error($"Unexpected character '{_text[_index]}'.");
            }
            return result;
        }

        private List<Move> ParseSequence(bool stopOnParen)
        {
            var result = new List<Move>();
            while (true)
            {
                SkipTrivia();
                if (_index >= _text.Length)
                {
                    if (stopOnParen)
                    {
                        throw Error("Missing closing parenthesis.");
                    }
                    return result;
                }

                if (_text[_index] == ')')
                {
                    if (!stopOnParen)
                    {
                        throw Error("Unexpected closing parenthesis.");
                    }
                    _index++;
                    return result;
                }

                if (_text[_index] == '(')
                {
                    _index++;
                    var group = ParseSequence(stopOnParen: true);
                    var repeat = ReadSuffixRepeat(out var invert);
                    for (var i = 0; i < repeat; i++)
                    {
                        foreach (var move in invert ? group.AsEnumerable().Reverse() : group)
                        {
                            result.Add(invert ? Invert(move) : move);
                        }
                    }
                    continue;
                }

                result.AddRange(ParseMoveToken());
            }
        }

        private IEnumerable<Move> ParseMoveToken()
        {
            var prefixNumber = ReadNumber();
            if (_index >= _text.Length)
            {
                throw Error("Move token expected.");
            }

            var face = _text[_index];
            if (char.ToUpperInvariant(face) is not ('R' or 'L' or 'U' or 'D' or 'F' or 'B' or 'X' or 'Y' or 'Z'))
            {
                throw Error($"Unsupported move face '{face}'.");
            }
            _index++;

            if (face is 'X' or 'Y' or 'Z' && _index < _text.Length && char.IsDigit(_text[_index]))
            {
                var layer = ReadRequiredNumber() - 1;
                var coordinateTurns = ReadMoveTurns();
                return new[] { CreateCoordinateMove(char.ToUpperInvariant(face), layer, coordinateTurns) };
            }

            var width = 1;
            var innerOnly = false;
            if (face is 'x' or 'y' or 'z')
            {
                width = _size;
            }
            else if (char.IsLower(face))
            {
                innerOnly = true;
            }

            if (_index < _text.Length && IsWideMarker(_text[_index]))
            {
                _index++;
                width = Math.Clamp(prefixNumber ?? 2, 1, _size);
            }
            else if (_index < _text.Length && IsMatchingInner(face, _text[_index]))
            {
                _index++;
                width = Math.Clamp(prefixNumber ?? 2, 1, _size);
            }
            else if (prefixNumber.HasValue && char.ToUpperInvariant(face) is 'R' or 'L' or 'U' or 'D' or 'F' or 'B')
            {
                width = Math.Clamp(prefixNumber.Value, 1, _size);
            }

            var turns = ReadMoveTurns();
            if (innerOnly)
            {
                return new[] { CreateFaceMove(char.ToUpperInvariant(face), 1, turns) };
            }

            if (face is 'x' or 'y' or 'z')
            {
                return Enumerable.Range(0, _size).Select(layer => CreateCoordinateMove(char.ToUpperInvariant(face), layer, turns));
            }

            return Enumerable.Range(0, width).Select(offset => CreateFaceMove(char.ToUpperInvariant(face), offset, turns));
        }

        private int ReadMoveTurns()
        {
            var turns = 1;
            if (_index < _text.Length && _text[_index] is 'x' or 'X')
            {
                var next = _index + 1;
                if (next < _text.Length && _text[next] == '2')
                {
                    _index += 2;
                    turns = 2;
                }
            }
            else if (_index < _text.Length && _text[_index] == '2')
            {
                _index++;
                turns = 2;
            }

            if (_index < _text.Length && _text[_index] == '\'')
            {
                _index++;
                turns = turns == 2 ? 2 : 3;
            }

            return turns;
        }

        private int ReadSuffixRepeat(out bool invert)
        {
            var repeat = 1;
            invert = false;
            if (_index < _text.Length && _text[_index] == '2')
            {
                _index++;
                repeat = 2;
            }
            if (_index < _text.Length && _text[_index] == '\'')
            {
                _index++;
                invert = true;
            }
            return repeat;
        }

        private int? ReadNumber()
        {
            var start = _index;
            while (_index < _text.Length && char.IsDigit(_text[_index]))
            {
                _index++;
            }
            if (_index == start)
            {
                return null;
            }
            return int.Parse(_text[start.._index]);
        }

        private int ReadRequiredNumber()
        {
            return ReadNumber() ?? throw Error("Layer number expected.");
        }

        private Move CreateCoordinateMove(char axisName, int layer, int turns)
        {
            var axis = axisName switch
            {
                'X' => 2,
                'Y' => 1,
                _ => 0
            };
            return new Move { Axis = axis, Layer = Math.Clamp(layer, 0, _size - 1), QuarterTurns = NormalizeTurns(turns) };
        }

        private Move CreateFaceMove(char face, int offsetFromFace, int turns)
        {
            return face switch
            {
                'R' => new Move { Axis = 2, Layer = _size - 1 - offsetFromFace, QuarterTurns = NormalizeTurns(turns) },
                'L' => new Move { Axis = 2, Layer = offsetFromFace, QuarterTurns = NormalizeTurns(4 - turns) },
                'U' => new Move { Axis = 1, Layer = _size - 1 - offsetFromFace, QuarterTurns = NormalizeTurns(turns) },
                'D' => new Move { Axis = 1, Layer = offsetFromFace, QuarterTurns = NormalizeTurns(4 - turns) },
                'F' => new Move { Axis = 0, Layer = _size - 1 - offsetFromFace, QuarterTurns = NormalizeTurns(turns) },
                'B' => new Move { Axis = 0, Layer = offsetFromFace, QuarterTurns = NormalizeTurns(4 - turns) },
                _ => throw new InvalidOperationException($"Unsupported face '{face}'.")
            };
        }

        private void SkipTrivia()
        {
            while (_index < _text.Length)
            {
                var ch = _text[_index];
                if (char.IsWhiteSpace(ch) || ch is ',' or ';')
                {
                    _index++;
                    continue;
                }
                if (ch == '#')
                {
                    while (_index < _text.Length && _text[_index] is not '\r' and not '\n')
                    {
                        _index++;
                    }
                    continue;
                }
                break;
            }
        }

        private InvalidOperationException Error(string message)
        {
            return new InvalidOperationException($"{message} Position {_index + 1}.");
        }
    }

    private static Move Invert(Move move)
    {
        return new Move
        {
            Axis = move.Axis,
            Layer = move.Layer,
            QuarterTurns = NormalizeTurns(4 - move.QuarterTurns)
        };
    }

    private static bool IsWideMarker(char ch)
    {
        return ch is 'w' or 'W';
    }

    private static bool IsMatchingInner(char outer, char inner)
    {
        return char.IsUpper(outer) && char.ToLowerInvariant(outer) == inner;
    }
}
