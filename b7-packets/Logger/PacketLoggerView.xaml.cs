using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;

using Sulakore.Communication;
using Sulakore.Protocol;

using b7.Util;

namespace b7.Packets
{
    partial class PacketLoggerView : UserControl, INotifyPropertyChanged
    {
        #region - INotifyPropertyChanged -
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        protected bool _set<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        #endregion

        public MainWindow Window { get; set; }

        public PacketsModule Module => Window.Module;
        public IHConnection Connection => Module?.Connection;

        private ConcurrentQueue<VmPacketLog> intercepted = new ConcurrentQueue<VmPacketLog>();

        private bool loggingIn = false;
        private bool loggingOut = false;

        private readonly ObservableCollection<VmPacketLog> packetLogs
            = new ObservableCollection<VmPacketLog>();

        private Filter<VmPacketLog> filter = new Filter<VmPacketLog>();

        private DispatcherTimer packetProcessTimer;

        private bool isLogging;
        public bool IsLogging
        {
            get => isLogging;
            set => _set(ref isLogging, value);
        }

        private bool blockTraffic;
        public bool BlockTraffic
        {
            get => blockTraffic;
            set => _set(ref blockTraffic, value);
        }

        private bool blockIn;
        public bool BlockIn
        {
            get => blockIn;
            set => _set(ref blockIn, value);
        }

        private bool blockOut;
        public bool BlockOut
        {
            get => blockOut;
            set => _set(ref blockOut, value);
        }

        private bool blockAll = true;
        public bool BlockAll
        {
            get => blockAll;
            set => _set(ref blockAll, value);
        }

        public PacketLoggerView()
        {
            InitializeComponent();

            listViewPacketLogs.ItemsSource = packetLogs;
            var view = (CollectionView)CollectionViewSource.GetDefaultView(listViewPacketLogs.ItemsSource);
            view.Filter = Filter;

            packetProcessTimer = new  DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(10), };
            packetProcessTimer.Tick += Timer_Tick;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            packetProcessTimer.IsEnabled = true;
            packetProcessTimer.Start();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            packetProcessTimer?.Stop();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            while (intercepted.TryPeek(out VmPacketLog peek) &&
                (DateTime.Now - peek.Time).TotalMilliseconds > 100 &&
                intercepted.TryDequeue(out VmPacketLog log)) 
            {
                log.IsBlocked = log.args.IsBlocked;
                log.IsModified = !log.args.IsOriginal;
            }
        }

        private void textBoxFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBoxFilter.Text))
            {
                filter = null;
            }
            else
            {
                string[] inputs = textBoxFilter.Text.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                var filter = new Filter<VmPacketLog>();
                for (int i = 0; i < inputs.Length; i++)
                {
                    string input = inputs[i];
                    bool isExclusive = input.StartsWith("~");
                    if (isExclusive)
                        input = input.Substring(1);

                    if (ushort.TryParse(input, out ushort header))
                    {
                        filter.Add(new FilterRule<VmPacketLog>(x => x.ID == header, isExclusive));
                    }
                    else
                    {
                        input = input.ToLower();
                        filter.Add(new FilterRule<VmPacketLog>(x => x.HasName && x.Name.ToLower().Contains(input), isExclusive));
                    }
                }

                this.filter = filter;
            }

            CollectionViewSource.GetDefaultView(listViewPacketLogs.ItemsSource).Refresh();
        }

        private bool Filter(object o)
        {
            if (string.IsNullOrEmpty(textBoxFilter.Text))
                return true;

            var log = o as VmPacketLog;
            if (log == null) return false;

            return filter?.Check(log) ?? true;
        }

        public void HandleData(DataInterceptedEventArgs e)
        {
            if (BlockTraffic)
            {
                e.IsBlocked =
                    (e.IsOutgoing ? (e.Packet.Header != Module.Out.Pong) : (e.Packet.Header != Module.In.Ping)) &&
                    (BlockAll ||
                    (e.IsOutgoing && BlockOut) ||
                    (!e.IsOutgoing && BlockIn));
            }

            if (!IsLogging || (e.IsOutgoing && !loggingOut) || (!e.IsOutgoing && !loggingIn))
                return;

            var log = new VmPacketLog(Module, e);
            AddPacketLog(log);
            intercepted.Enqueue(log);
        }

        private void AddPacketLog(VmPacketLog log)
        {
            if (!CheckAccess())
            {
                Dispatcher.Invoke(() => AddPacketLog(log));
                return;
            }

            packetLogs.Add(log);
        }

        private void comboBoxDirection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int i = comboBoxDirection.SelectedIndex + 1;
            loggingIn = (i & 1) > 0;
            loggingOut = (i & 2) > 0;
        }

        private void buttonClear_Click(object sender, RoutedEventArgs e)
        {
            packetLogs.Clear();
        }

        private void buttonStartStop_Click(object sender, RoutedEventArgs e)
        {
            SetActive(!IsLogging);
        }

        private void listViewPacketLogs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            StringBuilder sb = new StringBuilder();

            var logs = listViewPacketLogs.SelectedItems.Cast<VmPacketLog>().OrderBy(x => x.Time).ToArray();
            for (int i = 0; i < logs.Length; i++)
            {
                if (i > 0) sb.AppendLine();
                AppendPacketLog(sb, logs[i]);
            }

            textBoxPacket.Text = sb.ToString();
        }

        private void listViewPacketLogs_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var items = listViewPacketLogs.SelectedItems;

            menuItemCopyHex.IsEnabled =
            menuItemStructuralizer.IsEnabled = items.Count == 1;

            menuItemResend.IsEnabled = items.Count > 0;
        }

        private void menuItemCopyHex_Click(object sender, RoutedEventArgs e)
        {
            var item = listViewPacketLogs.SelectedItem as VmPacketLog;
            if (item == null)
                return;

            StringBuilder sb = new StringBuilder();
            byte[] data = item.Packet.ReadBytes(item.Packet.Length - 2, 0);
            
            for (int i = 0; i < data.Length; i++)
            {
                if (i > 0)
                    sb.Append(" ");
                sb.Append($"{data[i]:x2}");
            }

            Clipboard.SetText(sb.ToString());
        }

        private void menuItemComposer_Click(object sender, RoutedEventArgs e)
        {
            var log = listViewPacketLogs.SelectedItem as VmPacketLog;
            if (log == null) return;

            try
            {
                var sb = new StringBuilder();
                sb.Append(log.Name);

                StructureType[] types = new StructureType[0];
                if (log.Structure != null && log.Structure.Length > 0)
                    types = PacketUtil.ToStructureTypes(log.Structure);

                PacketUtil.AppendPacketStructure(sb, log.Packet, types);

                textBoxPacketComposer.Text = sb.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while composing packet: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void menuItemStructuralizer_Click(object sender, RoutedEventArgs e)
        {
            var log = listViewPacketLogs.SelectedItem as VmPacketLog;
            if (log == null) return;

            Window.LoadInStructuralizer(log);
        }

        private async void menuItemResend_Click(object sender, RoutedEventArgs e)
        {
            var items = listViewPacketLogs.SelectedItems.Cast<VmPacketLog>();

            foreach (var item in items.OrderBy(x => x.Time))
            {
                byte[] data = item.Packet.ToBytes();
                if (item.IsOutgoing)
                    await Connection.SendToServerAsync(data);
                else
                    await Connection.SendToClientAsync(data);
            }
        }

        private void SetActive(bool active)
        {
            IsLogging = active;
        }

        private void AppendPacketLog(StringBuilder sb, VmPacketLog log)
        {
            try
            {
                sb.Append($"{log.DirectionPointer} [{log.Time:HH:mm:ss}] ");
                if (log.HasName)
                    sb.AppendLine($"{log.Name} ({log.ID})");
                else
                    sb.AppendLine($"{log.ID}");

                if (log.ClassName != null)
                {
                    sb.Append("Class: " + log.ClassName);
                    if (log.ParserName != null)
                        sb.Append(" / " + log.ParserName);
                    if (log.Hash != null)
                        sb.Append($" ({log.Hash})");
                    sb.AppendLine();
                }
                else if (log.Hash != null)
                    sb.AppendLine("Hash: " + log.Hash);

                if (log.Packet.Length <= 2)
                    return;

                sb.AppendLine();

                byte[] data = log.Packet.ReadBytes(log.Packet.Length - 2, 0);
                for (int i = 0; i < data.Length; i++)
                {
                    if (i > 0 && i % 16 == 0)
                    {
                        sb.Append("   ");
                        for (int j = 0; j < 16; j++)
                        {
                            byte c = data[(i / 16 - 1) * 16 + j];
                            if (32 <= c && c <= 126)
                                sb.Append((char)c);
                            else
                                sb.Append('.');
                        }
                        sb.AppendLine();
                    }

                    sb.Append($"{data[i]:x2} ");
                }

                int leftover = data.Length % 16;
                if (leftover == 0)
                    leftover = 16;

                if (leftover > 0)
                {
                    sb.Append("".PadRight((16 - leftover + 1) * 3));
                    for (int i = 0; i < leftover; i++)
                    {
                        byte c = data[data.Length - leftover + i];
                        if (32 <= c && c <= 126)
                            sb.Append((char)c);
                        else
                            sb.Append('.');
                    }
                    sb.AppendLine();
                }

                if (log.Structure != null && log.Structure.Length > 0)
                {
                    try
                    {
                        var sbComposed = new StringBuilder();
                        sbComposed.Append(log.Name);
                        PacketUtil.AppendPacketStructure(sbComposed, log.Packet, PacketUtil.ToStructureTypes(log.Structure));

                        sb.AppendLine();
                        sb.AppendLine(sbComposed.ToString());
                    }
                    catch (Exception ex)
                    {
                        sb.AppendLine();
                        sb.AppendLine($"an error ocurred while parsing the packet structure: {ex.Message}");
                    }
                }
            }
            catch { }
        }

        private void buttonSendToServer_Click(object sender, RoutedEventArgs e) => SendPacket(true);
        private void buttonSendToClient_Click(object sender, RoutedEventArgs e) => SendPacket(false);

        private void SendPacket(bool isOutgoing)
        {
            HMessage message;
            try
            {
                message = new PacketParser(Module).Compose(textBoxPacketComposer.Text, isOutgoing);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error composing packet: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (isOutgoing)
                Connection.SendToServerAsync(message);
            else
                Connection.SendToClientAsync(message);
        }
    }
}
