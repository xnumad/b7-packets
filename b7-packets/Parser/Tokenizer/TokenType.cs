using System;

namespace b7.Packets
{
    public enum TokenType
    {
        Undefined,

        NewLine,

        Identifier,
        String,
        Integer,

        OpenBracket,
        CloseBracket,
        OpenSquareBracket,
        CloseSquareBracket,

        Comma,
        Colon,
        Semicolon,

        Hash,

        Add,
        Subtract,
        Multiply,
        Divide,

        SequenceTerminator,
        ByteArray
    }
}
