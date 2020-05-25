using System;

namespace b7.Packets
{
    public class VmStructureItem : VmListViewItem
    {
        public int Position { get; set; }
        public int Length { get; set; }
        public StructureType Type { get; set; }
        public object Value { get; set; }

        public string ValueView
        {
            get
            {
                if (Value == null)
                    return "";

                if (Value is bool b)
                    return b ? "true" : "false";
                else if (Value is byte[] ba)
                    return $"byte[{ba.Length}]";
                else if (Value is string s)
                    return PacketUtil.EscapeString(s);
                else
                    return Value.ToString();
            }
        }
    }
}
