using System;
using System.Collections.Generic;

using Sulakore.Habbo;
using Sulakore.Habbo.Messages;
using Sulakore.Modules;
using Sulakore.Protocol;

namespace b7.Packets
{
    public class PacketParser
    {
        public IModule Module { get; }
        public HGame Game => Module.Installer?.Game;
        public Incoming In => Module.Installer?.In;
        public Outgoing Out => Module.Installer?.Out;

        private Tokenizer tokenizer;

        public PacketParser(IModule module)
        {
            Module = module;
            tokenizer = new Tokenizer();
        }

        public HMessage Compose(string text, bool isOutgoing) => Compose(tokenizer.Tokenize(text), isOutgoing);
        public HMessage Compose(IEnumerable<Token> tokens, bool isOutgoing) => Compose(tokens.GetEnumerator(), isOutgoing);
        public HMessage Compose(IEnumerator<Token> e, bool isOutgoing, bool consumeToken = true)
        {
            if (Game == null)
                throw new Exception("The game has not been disassembled, unable to parse packet");

            ushort header;

            if (consumeToken)
                e.AssertTokenType("header", new[] { TokenType.Integer, TokenType.Identifier });
            else
                e.Current.AssertTokenType("header", new[] { TokenType.Integer, TokenType.Identifier });

            if (e.Current.Type == TokenType.Integer)
            {
                header = ushort.Parse(e.Current.Value);
            }
            else
            {
                string messageName = e.Current.Value;
                var identifiers = isOutgoing ? (Identifiers)Out : In;
                if (!identifiers.TryGetId(messageName, out header))
                    throw new Exception($"Unknown {(isOutgoing ? "outgoing" : "incoming")} message name: {messageName}");
            }

            var packet = new HMessage(header);

            while (e.MoveNext())
            {
                if (e.Current.Type == TokenType.NewLine ||
                    e.Current.Type == TokenType.SequenceTerminator)
                    break;

                switch (e.Current.Type)
                {
                    case TokenType.Identifier:
                        {
                            var identifier = e.Current.Value.ToLower();
                            switch (identifier)
                            {
                                case "true": packet.WriteBoolean(true); break;
                                case "false": packet.WriteBoolean(false); break;
                                case "b": // byte
                                    {
                                        e.AssertTokenType("':'", TokenType.Colon);
                                        byte[] bytes = new byte[1] { e.ParseByte() };
                                        packet.WriteBytes(bytes);
                                    }
                                    break;
                                case "s": // short
                                    {
                                        e.AssertTokenType("':'", TokenType.Colon);
                                        packet.WriteShort((ushort)e.ParseShort());
                                    }
                                    break;
                                case "i": // int
                                    {
                                        e.AssertTokenType("':'", TokenType.Colon);
                                        packet.WriteInteger(e.ParseInt());
                                    }
                                    break;
                                case "a": // byte array
                                    {
                                        e.AssertTokenType("':'", TokenType.Colon);
                                        byte[] bytes = e.ParseByteArray();
                                        packet.WriteInteger(bytes.Length);
                                        packet.WriteBytes(bytes);
                                    }
                                    break;
                                default:
                                    throw new Exception($"Unexpected identifier '{identifier}'");
                            }
                        }
                        break;
                    case TokenType.Subtract:
                    case TokenType.Integer:
                        {
                            bool negate = e.Current.Type == TokenType.Subtract;
                            if (negate)
                                e.AssertTokenType("integer", TokenType.Integer);
                            var s = (negate ? "-" : "") + e.Current.Value;
                            if (!int.TryParse(s, out int value))
                                throw new Exception($"Invalid integer value: {s}");
                            packet.WriteInteger(value);
                        }
                        break;
                    case TokenType.String: packet.WriteString(e.Current.Value); break;
                    case TokenType.ByteArray:
                        {
                            byte[] bytes = ParserExtensions.ParseByteArray(e.Current);
                            packet.WriteBytes(bytes);
                        }
                        break;
                    case TokenType.NewLine: break;
                    default:
                        throw new Exception($"Unexpected token type {e.Current.Type}");
                }
            }

            return packet;
        }
    }
}
