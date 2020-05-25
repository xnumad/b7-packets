using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace b7.Packets
{
    public class MessageDefinition : INotifyPropertyChanged
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

        public bool IsOutgoing { get; set; }

        public string Group { get; set; }
        public ushort Header { get; set; }
        public string Name { get; set; }
        public string Hash { get; set; }
        public string ClassName { get; set; }
        public string ParserName { get; set; }
        public string Structure { get; set; }

        private bool isBlocked;
        public bool IsBlocked
        {
            get => isBlocked;
            set => _set(ref isBlocked, value);
        }

        public MessageDefinition() { }
    }
}
