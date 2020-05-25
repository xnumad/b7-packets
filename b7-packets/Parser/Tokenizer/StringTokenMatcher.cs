using System;
using System.Collections.Generic;

using b7.Packets.Util;

namespace b7.Packets
{
    class StringTokenMatcher : ITokenMatcher
    {
        public int Precedence { get; }

        public StringTokenMatcher(int precedence)
        {
            Precedence = precedence;
        }

        public IEnumerable<TokenMatch> FindMatches(string input)
        {
            int matchStart = 0;

            bool inString = false;
            bool escaping = false;

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];

                if (inString)
                {
                    if (c == '\r' || c == '\n')
                    {
                        inString = false;
                    }
                    else
                    {
                        if (escaping)
                        {
                            escaping = false;
                        }
                        else
                        {
                            if (c == '\\')
                                escaping = true;
                            else if (c == '"')
                            {
                                inString = false;
                                yield return StringToken(input, matchStart, i + 1);
                            }
                        }
                    }
                }
                else
                {
                    if (c == '"')
                    {
                        matchStart = i;
                        inString = true;
                        escaping = false;
                    }
                }
            }
        }

        private TokenMatch StringToken(string input, int startIndex, int endIndex)
        {
            string value = input.Substring(startIndex + 1, endIndex - startIndex - 2);
            value = StringUtil.Unescape(value, c =>
            {
                switch (c)
                {
                    case '\\':
                    case '"':
                        return c.ToString();
                    case 't': return "\t";
                    case 'r': return "\r";
                    case 'n': return "\n";
                    default: return "\\" + c;
                }
            });

            return new TokenMatch() {
                Type = TokenType.String,
                Value = value,
                StartIndex = startIndex,
                EndIndex = endIndex,
                Precedence = Precedence
            };
        }
    }
}
