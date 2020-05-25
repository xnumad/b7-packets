using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace b7.Packets
{
    static class ParserExtensions
    {
        private static bool IsValidTokenType(Token t, params TokenType[] allowedTypes) => allowedTypes.Contains(t.Type);

        public static void AssertMoveNext(this IEnumerator<Token> e)
        {
            if (!e.MoveNext())
                throw new Exception($"Unexpected end of text");
        }

        public static void AssertTokenType(this IEnumerator<Token> e, string expectedTypeName, params TokenType[] allowedTypes)
        {
            e.AssertMoveNext();
            e.Current.AssertTokenType(expectedTypeName, allowedTypes);
        }

        public static void AssertTokenType(this Token t, string expectedTypeName, params TokenType[] allowedTypes)
        {
            if (!IsValidTokenType(t, allowedTypes))
                throw new Exception($"Expected {expectedTypeName}");
        }

        private static string GetIntegerString(IEnumerator<Token> e, bool allowNegative = true)
        {
            e.AssertTokenType("integer",
                allowNegative ? 
                new[] { TokenType.Integer, TokenType.Subtract } :
                new[] { TokenType.Integer }
            );

            string text;
            bool negate = e.Current.Type == TokenType.Subtract;
            if (negate)
            {
                e.AssertTokenType("integer", TokenType.Integer);
                text = "-" + e.Current.Value;
            }
            else
                text = e.Current.Value;

            return text;
        }

        public static byte ParseByte(this IEnumerator<Token> e)
        {
            string text = GetIntegerString(e, false);
            if (!byte.TryParse(text, out byte value))
                throw new Exception($"{text} is not valid for a byte");
            return value;
        }

        public static short ParseShort(this IEnumerator<Token> e)
        {
            string text = GetIntegerString(e, true);
            if (!short.TryParse(text, out short value))
                throw new Exception($"{text} is not valid for a short");

            return value;
        }

        public static int ParseInt(this IEnumerator<Token> e)
        {
            string text = GetIntegerString(e);
            if (!int.TryParse(text, out int value))
                throw new Exception($"{text} is not valid for an integer");

            return value;
        }

        public static byte[] ParseByteArray(this IEnumerator<Token> e)
        {
            e.AssertMoveNext();
            return ParseByteArray(e.Current);
        }

        public static byte[] ParseByteArray(Token token)
        {
            token.AssertTokenType("byte array", TokenType.ByteArray);
            string[] values =
                Regex.Split(token.Value.Substring(1, token.Value.Length - 2), @"\s+")
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToArray();
            byte[] bytes = new byte[values.Length];
            for (int i = 0; i < values.Length; i++)
                bytes[i] = byte.Parse(values[i], NumberStyles.HexNumber);

            return bytes;
        }
    }
}
