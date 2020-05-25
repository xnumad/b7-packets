using System;

using System.Diagnostics;

namespace b7.Packets
{
    [DebuggerDisplay("\\{{Type}, {Value}\\}")]
    public class Token
    {
        public int Index { get; set; } = -1;

        public int Line { get; set; } = -1;
        public int Position { get; set; } = -1;
        public int Length { get; set; } = -1;

        public TokenType Type { get; set; } = TokenType.Undefined;
        public string Value { get; set; } = string.Empty;
    }
}
