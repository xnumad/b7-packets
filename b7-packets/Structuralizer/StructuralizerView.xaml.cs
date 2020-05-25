using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using Sulakore.Protocol;

namespace b7.Packets
{
    public partial class StructuralizerView : UserControl, INotifyPropertyChanged
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

        private static readonly Dictionary<StructureType, Brush> structureColors = new Dictionary<StructureType, Brush>()
        {
            { StructureType.Boolean, Brushes.Purple },
            { StructureType.Byte, Brushes.OrangeRed },
            { StructureType.Short, Brushes.SeaGreen },
            { StructureType.Integer, Brushes.DarkCyan },
            { StructureType.String, Brushes.Firebrick },
        };

        private readonly Brush offsetBrush = Brushes.SlateGray;
        private readonly Brush foregroundBrush = Brushes.DarkSlateGray;

        private VmPacketLog currentLog = null;
        private HMessage currentPacket = null;

        // Data view
        private List<TextBlock> hexViews = new List<TextBlock>();
        private List<TextBlock> asciiViews = new List<TextBlock>();
        private Dictionary<int, List<Border>> borders = new Dictionary<int, List<Border>>();

        // Structure list
        private ObservableCollection<VmStructureItem> structureItems
            = new ObservableCollection<VmStructureItem>();

        private string packetSignature;
        public string PacketSignature
        {
            get => packetSignature;
            set => _set(ref packetSignature, value);
        }

        private bool isOutgoing;
        public bool IsOutgoing
        {
            get => isOutgoing;
            set => _set(ref isOutgoing, value);
        }

        private bool canAddBool;
        public bool CanAddBool
        {
            get => canAddBool;
            set => _set(ref canAddBool, value);
        }

        private bool canAddByte;
        public bool CanAddByte
        {
            get => canAddByte;
            set => _set(ref canAddByte, value);
        }

        private bool canAddShort;
        public bool CanAddShort
        {
            get => canAddShort;
            set => _set(ref canAddShort, value);
        }

        private bool canAddInt;
        public bool CanAddInt
        {
            get => canAddInt;
            set => _set(ref canAddInt, value);
        }

        private bool canAddString;
        public bool CanAddString
        {
            get => canAddString;
            set => _set(ref canAddString, value);
        }

        private bool hasStructure;
        public bool HasStructure
        {
            get => hasStructure;
            set => _set(ref hasStructure, value);
        }

        private bool hasPacket;
        public bool HasPacket
        {
            get => hasPacket;
            set => _set(ref hasPacket, value);
        }

        public StructuralizerView()
        {
            InitializeComponent();

            dataPanel.SetValue(FontFamilyProperty, new FontFamily("Consolas"));
            dataPanel.SetValue(FontSizeProperty, 12.0);

            // base address / hex / space / ascii
            for (int i = 0; i < 1 + 16 + 1 + 16; i++)
            {
                gridHeaders.ColumnDefinitions.Add(
                    new ColumnDefinition()
                    {
                        SharedSizeGroup = $"data{i}"
                    }
                );
            }

            gridHeaders.ColumnDefinitions[0].Width = new GridLength(32, GridUnitType.Pixel);

            // hex
            for (int i = 0; i < 16; i++)
            {
                gridHeaders.ColumnDefinitions[1+i].Width = new GridLength(16, GridUnitType.Pixel);
            }

            // space
            gridHeaders.ColumnDefinitions[17].Width = new GridLength(8, GridUnitType.Pixel);

            // ascii
            for (int i = 0; i < 16; i++)
            {
                gridHeaders.ColumnDefinitions[1+16+1+i].Width = new GridLength(0, GridUnitType.Auto);
            }

            for (int i = 0; i < 16; i++)
            {
                var offsetText = new TextBlock() {
                    Margin = new Thickness(2),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Foreground = offsetBrush,
                    FontSize = 11,
                    Text = $"{i:x}"
                };
                offsetText.SetValue(Grid.ColumnProperty, i + 1);
                gridHeaders.Children.Add(offsetText);
            }

            listViewStructure.ItemsSource = structureItems;
        }

        private Grid AddRow()
        {
            int row = dataPanel.Children.Count - 1;

            var grid = new Grid();
            for (int i = 0; i < 1 + 16 + 1 + 16; i++)
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto, SharedSizeGroup = $"data{i}" });

            var addressLabel = new TextBlock() {
                Margin = new Thickness(2, 2, 6, 2),
                Foreground = offsetBrush,
                FontSize = 11,
                Text = $"{row * 16:x4}"
            };
            grid.Children.Add(addressLabel);

            dataPanel.Children.Add(grid);

            return grid;
        }

        private Grid GetRow(int row)
        {
            return (Grid)dataPanel.Children[row + 1];
        }

        private char ToAscii(byte b) => (0x20 <= b && b <= 0x7E) ? ((char)b) : '.';

        public void LoadMessage(VmPacketLog log)
        {
            Clear();

            PacketSignature = $"{log.DirectionPointer} {log.Name}";
            IsOutgoing = log.IsOutgoing;

            currentLog = log;
            currentPacket = new HMessage(log.Packet.ToBytes());

            byte[] data = currentPacket.ReadBytes(currentPacket.Length - 2);

            int currentRow;
            Grid currentRowGrid = null;

            for (int i = 0; i < data.Length; i++)
            {
                if (i % 16 == 0)
                {
                    currentRow = i / 16;
                    if (currentRow >= (dataPanel.Children.Count - 1))
                        currentRowGrid = AddRow();
                    else
                        currentRowGrid = GetRow(i);
                }

                // hex
                TextBlock tb = new TextBlock() {
                    Margin = new Thickness(2),
                    Foreground = foregroundBrush,
                    FontSize = 11,
                    Text = data[i].ToString("x2")
                };
                tb.SetValue(Grid.ColumnProperty, i % 16 + 1);
                currentRowGrid.Children.Add(tb);
                hexViews.Add(tb);

                // ascii
                tb = new TextBlock() {
                    Margin = new Thickness(1),
                    Foreground = foregroundBrush,
                    FontSize = 11,
                    Text = ToAscii(data[i]).ToString()
                };
                tb.SetValue(Grid.ColumnProperty, i % 16 + 18);
                currentRowGrid.Children.Add(tb);
                asciiViews.Add(tb);
            }

            currentPacket.Position = 0;

            if (log.Structure != null && log.Structure.Length > 0)
            {
                try
                {
                    var types = PacketUtil.ToStructureTypes(log.Structure);
                    foreach (var type in types)
                    {
                        switch (type)
                        {
                            case StructureType.Boolean:
                            case StructureType.Byte:
                            case StructureType.Short:
                            case StructureType.Integer:
                            case StructureType.String:
                                AddStructureItem(type);
                                break;
                            default: throw new Exception($"Unknown/unsupported structure type: {type}");
                        }
                    }
                }
                catch { }
            }

            UpdateButtons();

            HasPacket = true;
        }

        private Border AddBorder(int row, int col, int span, Thickness margin, Brush brush, bool closeLeft, bool closeRight)
        {
            Border border = new Border();
            border.Margin = margin;

            var color = (Color)brush.GetValue(SolidColorBrush.ColorProperty);
            var bg = new SolidColorBrush(Color.FromArgb(25, color.R, color.G, color.B));
            border.Background = bg;

            border.BorderBrush = brush;
            border.CornerRadius = new CornerRadius(
                closeLeft ? 2 : 0,
                closeRight ? 2 : 0,
                closeRight ? 2 : 0,
                closeLeft ? 2 : 0
            );
            border.BorderThickness = new Thickness(
                closeLeft ? 1 : 0,
                1,
                closeRight ? 1 : 0,
                1
            );

            border.SetValue(Grid.ColumnProperty, col);
            border.SetValue(Grid.ColumnSpanProperty, span);
            border.SetValue(Panel.ZIndexProperty, -1);
            ((Grid)dataPanel.Children[row + 1]).Children.Add(border);

            return border;
        }

        private List<Border> AddBorders(int position, int length, Brush brush)
        {
            var list = new List<Border>();

            int currentPosition = position;
            int end = currentPosition + length;

            while (currentPosition < end)
            {
                int row = currentPosition / 16;
                int offset = currentPosition % 16;
                int maxLength = 16 - offset;
                int span = Math.Min(maxLength, end - currentPosition);

                list.Add(AddBorder(row, offset + 1, span, new Thickness(2), brush, position == currentPosition, currentPosition + span == end));
                list.Add(AddBorder(row, offset + 18, span, new Thickness(1), brush, position == currentPosition, currentPosition + span == end));

                currentPosition += span;
            }

            return list;
        }

        private void UpdateButtons()
        {
            if (currentPacket != null)
            {
                int pos = currentPacket.Position;

                CanAddBool =
                    currentPacket.Readable >= 1 &&
                    currentPacket.ReadBytes(1, pos)[0] <= 1;
                CanAddByte = currentPacket.Readable >= 1;
                CanAddShort = currentPacket.Readable >= 2;
                CanAddInt = currentPacket.Readable >= 4;
                CanAddString =
                    currentPacket.Readable >= 2 &&
                    currentPacket.Readable >= (2 + currentPacket.ReadShort(pos));

                HasStructure = structureItems.Count > 0;
            }
            else
            {
                CanAddBool =
                CanAddByte =
                CanAddShort =
                CanAddInt =
                CanAddString =
                HasStructure = false;
            }
        }

        private void buttonAddBoolean_Click(object sender, RoutedEventArgs e) => AddStructureItem(StructureType.Boolean);
        private void buttonAddByte_Click(object sender, RoutedEventArgs e) => AddStructureItem(StructureType.Byte);
        private void buttonAddShort_Click(object sender, RoutedEventArgs e) => AddStructureItem(StructureType.Short);
        private void buttonAddInt_Click(object sender, RoutedEventArgs e) => AddStructureItem(StructureType.Integer);
        private void buttonAddString_Click(object sender, RoutedEventArgs e) => AddStructureItem(StructureType.String);

        private void buttonRemoveLast_Click(object sender, RoutedEventArgs e)
        {
            if (structureItems.Count == 0)
                return;

            var last = structureItems.Last();
            structureItems.RemoveAt(structureItems.Count - 1);

            // Reset colors
            for (int i = 0; i < last.Length; i++)
            {
                hexViews[last.Position + i].Foreground = foregroundBrush;
                asciiViews[last.Position + i].Foreground = foregroundBrush;
            }

            // Remove borders
            foreach (var border in borders[last.Position])
                ((Grid)border.Parent).Children.Remove(border);
            borders.Remove(last.Position);

            currentPacket.Position = last.Position;

            UpdateButtons();
        }

        private void buttonReset_Click(object sender, RoutedEventArgs e) => Reset();

        private void Reset()
        {
            if (currentPacket == null)
                return;

            currentPacket.Position = 0;
            structureItems.Clear();
            UpdateButtons();

            // Reset colors
            for (int i = 0; i < hexViews.Count; i++)
            {
                hexViews[i].Foreground = foregroundBrush;
                asciiViews[i].Foreground = foregroundBrush;
            }

            // Remove all borders
            foreach (var border in borders.SelectMany(x => x.Value))
                ((Grid)border.Parent).Children.Remove(border);
            borders.Clear();
        }

        private void Clear()
        {
            currentLog = null;
            currentPacket = null;
            structureItems.Clear();

            // Clear rows
            dataPanel.Children.RemoveRange(1, dataPanel.Children.Count - 1);

            hexViews.Clear();
            asciiViews.Clear();
            borders.Clear();

            UpdateButtons();
        }

        private void AddStructureItem(StructureType type)
        {
            int pos = currentPacket.Position;

            object value;
            switch (type)
            {
                case StructureType.RawBytes:
                case StructureType.ByteArray:
                    throw new NotImplementedException();
                case StructureType.Boolean:
                    value = currentPacket.ReadBoolean();
                    break;
                case StructureType.Byte:
                    value = currentPacket.ReadBytes(1)[0];
                    break;
                case StructureType.Short:
                    value = (short)currentPacket.ReadShort();
                    break;
                case StructureType.Integer:
                    value = currentPacket.ReadInteger();
                    break;
                case StructureType.String:
                    value = currentPacket.ReadString();
                    break;
                default:
                    throw new Exception($"Unknown structure type: {type}");
            }

            int len = currentPacket.Position - pos;

            structureItems.Add(new VmStructureItem() {
                Position = pos,
                Length = len,
                Type = type,
                Value = value,
                Foreground = structureColors[type]
            });

            for (int i = pos; i < pos + len; i++)
            {
                hexViews[i].Foreground = structureColors[type];
                asciiViews[i].Foreground = structureColors[type];
            }

            borders.Add(pos, AddBorders(pos, len, structureColors[type]));

            UpdateButtons();
        }

        private void MenuItemCopyValue_Click(object sender, RoutedEventArgs e)
        {
            var item = listViewStructure.SelectedItem as VmStructureItem;
            if (item == null || item.Value == null) return;

            Clipboard.SetText(item.Value.ToString());
        }

        private void ButtonCopyComposed_Click(object sender, RoutedEventArgs e)
        {
            if (currentLog == null) return;

            try
            {
                var sb = new StringBuilder();
                sb.Append(currentLog.Name);

                var structure = structureItems.Select(item => item.Type).ToArray();
                PacketUtil.AppendPacketStructure(sb, currentPacket, structure);

                Clipboard.SetText(sb.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
