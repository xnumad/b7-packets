using System;
using System.Windows.Input;

namespace b7.Packets.WPF
{
    public static class MessageCommands
    {
        public static RoutedCommand BlockMessagesCommand = new RoutedCommand();

        static MessageCommands()
        {
            BlockMessagesCommand.InputGestures.Add(new KeyGesture(Key.B, ModifierKeys.Control));
        }
    }
}
