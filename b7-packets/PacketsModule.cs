using System;
using System.Collections.Generic;

using Sulakore.Communication;
using Sulakore.Modules;

using TanjiWPF;

namespace b7.Packets
{
    [Module("b7 packets", "")]
    [Author("b7")]
    public class PacketsModule : WpfModuleLoader<MainWindow>
    {
        private readonly HashSet<ushort>
            blockedIn = new HashSet<ushort>(),
            blockedOut = new HashSet<ushort>();

        public bool SetBlocked(ushort header, bool isOutgoing, bool isBlocked)
        {
            var map = isOutgoing ? blockedOut : blockedIn;
            lock (map)
            {
                if (isBlocked)
                    return map.Add(header);
                else
                    return map.Remove(header);
            }
        }

        public bool IsBlocked(ushort header, bool isOutgoing)
        {
            var map = isOutgoing ? blockedOut : blockedIn;
            lock (map) return map.Contains(header);
        }

        protected override void HandleData(DataInterceptedEventArgs e)
        {
            if (IsBlocked(e.Packet.Header, e.IsOutgoing))
                e.IsBlocked = true;

            base.HandleData(e);
        }
    }
}
