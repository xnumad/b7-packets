using System;
using System.Linq;
using System.Text;

using Sulakore.Protocol;

namespace b7.Packets
{
    public static class PacketUtil
    {
        public static StructureType[] ToStructureTypes(string[] types)
            => types.Select(type => ToStructureType(type)).ToArray();

        public static StructureType ToStructureType(string type)
        {
            switch (type.ToLower())
            {
                case "bool":
                case "boolean":
                    return StructureType.Boolean;
                case "byte":
                    return StructureType.Byte;
                case "short":
                case "ushort":
                    return StructureType.Short;
                case "int":
                    return StructureType.Integer;
                case "string":
                    return StructureType.String;
                case "bytearray":
                    return StructureType.ByteArray;
                default:
                    throw new Exception($"Unknown structure type: {type}");
            }
        }

        public static void AppendPacketStructure(StringBuilder sb, HMessage packet, StructureType[] structure)
        {
            int originalPosition = packet.Position;

            try
            {
                packet.Position = 0;

                for (int i = 0; i < structure.Length; i++)
                {
                    sb.Append(" ");

                    switch (structure[i])
                    {
                        case StructureType.Boolean:
                            sb.Append(packet.ReadBoolean() ? "true" : "false");
                            break;
                        case StructureType.Byte:
                            sb.Append($"b:{packet.ReadBytes(1)[0]}");
                            break;
                        case StructureType.Short:
                            sb.Append($"s:{(short)packet.ReadShort()}");
                            break;
                        case StructureType.Integer:
                            sb.Append($"{packet.ReadInteger()}");
                            break;
                        case StructureType.String:
                            sb.Append($"\"{EscapeString(packet.ReadString())}\"");
                            break;
                        case StructureType.ByteArray:
                            int len = packet.ReadInteger();
                            byte[] bytes = packet.ReadBytes(len);
                            sb.Append("a:[");
                            for (int j = 0; j < bytes.Length; j++)
                            {
                                if (j > 0) sb.Append(" ");
                                sb.Append(bytes[j].ToString("x2"));
                            }
                            sb.Append("]");
                            break;
                        default:
                            throw new Exception($"Unknown/unsupported structure type: {structure[i]}");
                    }
                }

                if (packet.Readable > 0)
                {
                    byte[] extra = packet.ReadBytes(packet.Readable);
                    sb.Append(" [");
                    for (int i = 0; i < extra.Length; i++)
                    {
                        if (i > 0) sb.Append(" ");
                        sb.Append(extra[i].ToString("x2"));
                    }
                    sb.Append("]");
                }
            }
            finally { packet.Position = originalPosition; }
        }

        public static string EscapeString(string s)
        {
            return s
                .Replace("\"", "\\\"")
                .Replace("\t", "\\t")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n");
        }
    }
}
