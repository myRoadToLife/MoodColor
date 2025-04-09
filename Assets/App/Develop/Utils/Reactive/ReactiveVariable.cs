using System;

namespace App.Develop.Utils.Reactive
{
    public class ReactiveVariable<T> : IReadOnlyVariable<T> where T : IEquatable<T>
    {
        public event Action<T, T> Changed;

        private T _value;

        public ReactiveVariable() => _value = default;

        public ReactiveVariable(T value) => _value = value;

        public T Value
        {
            get => _value;
            set
            {
                if ((_value == null && value != null) || 
                    (_value != null && !_value.Equals(value)))
                {
                    T oldValue = _value;
                    _value = value;
                    Changed?.Invoke(oldValue, value);
                }
            }
        }
    }
}
