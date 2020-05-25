using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace b7.Packets
{
    public class VmListViewItem : INotifyPropertyChanged
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

        private bool isSelected;
        public bool IsSelected
        {
            get { return isSelected; }
            set { _set(ref isSelected, value); }
        }

        private Visibility visibility = Visibility.Visible;
        public Visibility Visibility
        {
            get { return visibility; }
            set { _set(ref visibility, value); }
        }

        private object content;
        public object Content
        {
            get { return content; }
            set { _set(ref content, value); }
        }

        private Brush foreground = Brushes.Black;
        public Brush Foreground
        {
            get => foreground;
            set => _set(ref foreground, value);
        }

        private Brush background;
        public Brush Background
        {
            get => background;
            set => _set(ref background, value);
        }
    }
}
