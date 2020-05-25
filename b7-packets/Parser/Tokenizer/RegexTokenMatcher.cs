using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace b7.Packets
{
    class RegexTokenMatcher : ITokenMatcher
    {
        public TokenType Type { get; private set; }
        public Regex Regex { get; private set; }
        public int Precedence { get; private set; }

        public RegexTokenMatcher(TokenType tokenType, string pattern, int precedence = 1)
        {
            Type = tokenType;
            Regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            Precedence = precedence;
        }

        public IEnumerable<TokenMatch> FindMatches(string input)
        {
            var match = Regex.Match(input);
            while (match.Success)
            {
                yield return CreateTokenMatch(match);
                match = match.NextMatch();
            }
        }

        protected virtual TokenMatch CreateTokenMatch(Match match)
        {
            return new TokenMatch() {
                Type = Type,
                Value = match.Value,
                StartIndex = match.Index,
                EndIndex = match.Index + match.Length,
                Precedence = Precedence
            };
        }
    }
}
