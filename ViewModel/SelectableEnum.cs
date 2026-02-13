using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.ViewModel
{
    /// <summary>
    /// Wrapper class to display Enum values with a CheckBox state.
    /// </summary>
    public class SelectableEnum<T> : INotifyPropertyChanged where T : Enum
    {
        public T Value { get; }
        public string Name => Value.ToString();

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public SelectableEnum(T value, bool initialList = true)
        {
            Value = value;
            _isSelected = initialList;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}