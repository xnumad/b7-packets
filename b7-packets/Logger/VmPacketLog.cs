using System;
using System.ComponentModel;

using Sulakore.Communication;
using Sulakore.Habbo;
using Sulakore.Habbo.Messages;
using Sulakore.Protocol;

using TanjiWPF;

namespace b7.Packets
{
    public class VmPacketLog : INotifyPropertyChanged
    {
        public HMessage Packet { get; }

        public bool IsOutgoing { get; }
        public DateTime Time { get; }
        public ushort ID { get; }
        public string Hash { get; }
        public string ClassName { get; }
        public string ParserName { get; }
        public bool HasName { get; }
        public string Name { get; }
        public int Length => Packet.Length - 2;

        private bool isBlocked;
        public bool IsBlocked {
            get => isBlocked;
            set
            {
                if (isBlocked == value)
                    return;
                isBlocked = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsBlocked"));
            }
        }

        private bool isModified;
        public bool IsModified
        {
            get => isModified;
            set
            {
                if (isModified == value)
                    return;
                isModified = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsModified"));
            }
        }

        public string DirectionPointer => IsOutgoing ? ">>" : "<<";

        public string[] Structure { get; }

        internal DataInterceptedEventArgs args;

        public event PropertyChangedEventHandler PropertyChanged;

        public VmPacketLog(WpfModule module, DataInterceptedEventArgs e)
        {
            args = e;

            Packet = e.Packet;

            IsOutgoing = e.IsOutgoing;
            Time = e.Timestamp;
            ID = e.Packet.Header;

            var identifiers = (e.IsOutgoing ? module.Out : (Identifiers)module.In);
            if ((e.IsOutgoing ? module.Game.OutMessages : module.Game.InMessages).TryGetValue(ID, out MessageItem msgItem))
            {
                Hash = msgItem.Hash;
                ClassName = msgItem.ClassName;
                ParserName = msgItem.ParserName;
                Structure = msgItem.Structure;
            }
            else
                Hash = null;

            var name = identifiers.GetName(ID);
            HasName = name != null;
            Name = name ?? ID.ToString();

            IsBlocked = e.IsBlocked;
            IsModified = !e.IsOriginal;
        }
    }
}
