using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace b7.Packets
{
    class Tokenizer
    {
        public List<ITokenMatcher> TokenDefinitions { get; }

        public Tokenizer()
        {
            TokenDefinitions = new List<ITokenMatcher>() {
                new RegexTokenMatcher(TokenType.NewLine, @"\r?\n", 0),
                new StringTokenMatcher(0),
                new RegexTokenMatcher(TokenType.Identifier, @"\b[a-z]+\b"),
                new RegexTokenMatcher(TokenType.Integer, @"\b\d+\b"),
                // Brackets
                new RegexTokenMatcher(TokenType.OpenBracket, @"\(", 5),
                new RegexTokenMatcher(TokenType.CloseBracket, @"\)", 5),
                new RegexTokenMatcher(TokenType.OpenSquareBracket, @"\[", 5),
                new RegexTokenMatcher(TokenType.CloseSquareBracket, @"\]", 5),
                // Separators
                new RegexTokenMatcher(TokenType.Comma, @",", 5),
                new RegexTokenMatcher(TokenType.Colon, @":", 5),
                new RegexTokenMatcher(TokenType.Semicolon, @";", 5),
                // Symbols
                new RegexTokenMatcher(TokenType.Hash, @"#", 5),
                new RegexTokenMatcher(TokenType.Add, @"\+", 5),
                new RegexTokenMatcher(TokenType.Subtract, @"-", 5),
                new RegexTokenMatcher(TokenType.Multiply, @"\*", 5),
                new RegexTokenMatcher(TokenType.Divide, @"/", 5),

                new RegexTokenMatcher(TokenType.ByteArray, @"\[\s*([0-9a-f]{2}(\s+[0-9a-f]{2})*)?\s*\]")
            };
        }

        public IEnumerable<Token> Tokenize(string input)
        {
            var lineIndices = new List<int>();
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == '\n')
                    lineIndices.Add(i + 1);
            }
            lineIndices.Add(input.Length);

            int[] getLinePos(int index)
            {
                for (int i = 0; i < lineIndices.Count; i++)
                {
                    int lineIndex = lineIndices[i];
                    if (index < lineIndex)
                        return new int[] { i + 1, (i == 0 ? index : index - lineIndices[i - 1]) + 1 };
                }
                return new int[] { -1, -1 };
            }

            var tokenMatches = new List<TokenMatch>();
            foreach (var def in TokenDefinitions)
                tokenMatches.AddRange(def.FindMatches(input));

            var groupedByIndex = tokenMatches
                .GroupBy(x => x.StartIndex)
                .OrderBy(x => x.Key)
                .ToList();
            
            TokenMatch lastMatch = null;
            int lastMatchEnd = 0;

            for (int i = 0; i < groupedByIndex.Count; i++)
            {
                var bestMatch = groupedByIndex[i].OrderBy(x => x.Precedence).First();
                // Ignore matches within the last best match
                if (lastMatch != null && bestMatch.StartIndex < lastMatch.EndIndex)
                    continue;

                if (bestMatch.StartIndex > lastMatchEnd && !IsWhiteSpace(input, lastMatchEnd, bestMatch.StartIndex))
                    yield return UncapturedText(input, lastMatchEnd, bestMatch.StartIndex, getLinePos(lastMatchEnd));

                lastMatch = bestMatch;
                lastMatchEnd = lastMatch.EndIndex;

                var p = getLinePos(bestMatch.StartIndex);
                yield return new Token() {
                    Line = p[0],
                    Position = p[1],
                    Index = bestMatch.StartIndex,
                    Length = bestMatch.EndIndex - bestMatch.StartIndex,
                    Type = bestMatch.Type,
                    Value = bestMatch.Value
                };
            }

            if (lastMatchEnd < input.Length && !IsWhiteSpace(input, lastMatchEnd, input.Length))
                yield return UncapturedText(input, lastMatchEnd, input.Length - lastMatchEnd, getLinePos(lastMatchEnd));

            var pos = getLinePos(input.Length);
            yield return new Token() {
                Type = TokenType.SequenceTerminator,
                Line = pos[0],
                Position = pos[1],
                Index = input.Length,
                Length = 0,
                Value = string.Empty
            };
        }

        private static bool IsWhiteSpace(string input, int startIndex, int endIndex)
        {
            for (int i = startIndex; i < endIndex; i++)
                if (!char.IsWhiteSpace(input, i))
                    return false;
            return true;
        }

        private Token UncapturedText(string input, int startIndex, int endIndex, int[] pos)
        {
            return new Token() {
                Line = pos[0],
                Position = pos[1],
                Index = startIndex,
                Length = endIndex - startIndex,
                Value = input.Substring(startIndex, endIndex - startIndex).Trim(),
            };
        }
    }
}
