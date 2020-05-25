using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

using Sulakore.Habbo;
using Sulakore.Habbo.Messages;

namespace b7.Packets
{
    public partial class MessagesView : UserControl
    {
        private static readonly Regex
            regexHash = new Regex(@"^[0-9a-f]{32}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private PacketsModule module;

        private ObservableCollection<MessageDefinition> messageDefinitions
            = new ObservableCollection<MessageDefinition>();

        public MessagesView()
        {
            InitializeComponent();

            Loaded += MessagesView_Loaded;

            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(messageDefinitions);

            view.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
            view.SortDescriptions.Add(new SortDescription("Header", ListSortDirection.Ascending));

            view.Filter = FilterMessage;

            listViewMessages.ItemsSource = view;
        }

        public void LoadMessages(HGame game)
        {
            var defs = new List<MessageDefinition>();

            HashSet<string>
                definedIncoming = new HashSet<string>(),
                definedOutgoing = new HashSet<string>();

            foreach (var messageItem in game.InMessages.Values
                .Concat(game.OutMessages.Values))
            {
                var ids = messageItem.IsOutgoing ? module.Installer.Out : (Identifiers)module.Installer.In;

                var def = new MessageDefinition()
                {
                    IsOutgoing = messageItem.IsOutgoing,
                    Group = messageItem.IsOutgoing ? "Outgoing" : "Incoming",
                    Header = messageItem.Id,
                    Name = ids.GetName(messageItem.Id) ?? string.Empty,
                    Hash = messageItem.Hash,
                    ClassName = messageItem.ClassName,
                    ParserName = messageItem.ParserName,
                    IsBlocked = module.IsBlocked(messageItem.Id, messageItem.IsOutgoing)
                };

                if (messageItem.Structure != null)
                {
                    string structure = "";

                    if (messageItem.Structure.Length > 0)
                    {
                        for (int i = 0; i < messageItem.Structure.Length; i++)
                        {
                            if (i > 0)
                                structure += ",";

                            switch (messageItem.Structure[i].ToLower())
                            {
                                case "boolean": structure += "bool"; break;
                                case "byte": structure += "byte"; break;
                                case "short":
                                case "ushort": structure += "short"; break;
                                case "int": structure += "int"; break;
                                case "string": structure += "str"; break;
                                case "bytearray": structure += "byte array"; break;
                                default: structure += messageItem.Structure[i]; break;
                            }
                        }
                    }
                    else
                        structure = "-";

                    def.Structure = structure;
                }

                (def.IsOutgoing ? definedOutgoing : definedIncoming).Add(def.Name);
                defs.Add(def);
            }

            var inProperties = typeof(Incoming).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty);
            foreach (var prop in inProperties)
            {
                if (definedIncoming.Add(prop.Name))
                {
                    defs.Add(new MessageDefinition()
                    {
                        IsOutgoing = false,
                        Group = "Incoming",
                        Name = prop.Name
                    });
                }
            }

            var outProperties = typeof(Outgoing).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty);
            foreach (var prop in outProperties)
            {
                if (definedOutgoing.Add(prop.Name))
                {
                    defs.Add(new MessageDefinition()
                    {
                        IsOutgoing = true,
                        Group = "Outgoing",
                        Name = prop.Name
                    });
                }
            }

            Dispatcher.InvokeAsync(() =>
            {
                foreach (var def in defs.OrderBy(x => x.Name))
                    messageDefinitions.Add(def);
            });
        }

        private void MessagesView_Loaded(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this) as MainWindow;
            if (window == null) return;

            Loaded -= MessagesView_Loaded;

            module = window.Module;
        }

        private void TextBoxFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            CollectionViewSource.GetDefaultView(listViewMessages.ItemsSource).Refresh();
        }

        private bool FilterMessage(object o)
        {
            if (string.IsNullOrWhiteSpace(textBoxFilter.Text))
                return true;

            var def = o as MessageDefinition;
            if (o == null)
                return true;

            bool match = false;

            var filters = textBoxFilter.Text.Split(',');
            for (int i = 0; i < filters.Length; i++)
            {
                bool m = false;

                string filter = filters[i];
                bool exclude = filter.StartsWith("-");
                if (exclude)
                    filter = filter.Substring(1);

                if (match && !exclude ||
                    string.IsNullOrWhiteSpace(filter))
                    continue;

                if (regexHash.IsMatch(filter))
                {
                    if (def.Hash != null && def.Hash.Equals(filter, StringComparison.InvariantCultureIgnoreCase))
                        m = true;
                }
                else
                {
                    if (ushort.TryParse(filter, out ushort header) &&
                        def.Header == header)
                    {
                        m = true;
                    }
                    else if (def.Name.ToLower().Contains(filter.ToLower()))
                    {
                        m = true;
                    }
                    else if (filter.Equals(def.ClassName) || filter.Equals(def.ParserName))
                    {
                        m = true;
                    }
                }

                if (m)
                {
                    if (exclude)
                        return false;
                    match = true;
                }
            }

            return match;
        }

        private void BlockCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            foreach (MessageDefinition def in listViewMessages.SelectedItems)
            {
                def.IsBlocked = !def.IsBlocked;
                module.SetBlocked(def.Header, def.IsOutgoing, def.IsBlocked);
            }
        }

        private void MenuItemCopy_Click(object sender, RoutedEventArgs e)
        {
            var def = listViewMessages.SelectedItem as MessageDefinition;
            if (def == null) return;

            string tag = ((MenuItem)sender).Tag as string;
            if (tag == null) return;

            string text = "";
            switch (tag)
            {
                case "header": text = def.Header.ToString(); break;
                case "name": text = def.Name ?? ""; break;
                case "hash": text = def.Hash ?? ""; break;
                case "class": text = def.ClassName ?? ""; break;
                case "parser": text = def.ParserName ?? ""; break;
                case "structure": text = def.Structure ?? ""; break;
                default: break;
            }

            Clipboard.SetText(text);
        }
    }
}
