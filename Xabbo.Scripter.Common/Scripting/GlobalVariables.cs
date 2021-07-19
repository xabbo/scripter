using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;

namespace Xabbo.Scripter.Scripting
{
    /// <summary>
    /// The global variables container for the scripter.
    /// </summary>
    public class GlobalVariables : DynamicObject
    {
        private readonly ConcurrentDictionary<string, dynamic> _variables;

        public dynamic? this[string variableName]
        {
            get => _variables.TryGetValue(variableName, out dynamic? value) ? value : default;
            set => Update(variableName, value);
        }

        event EventHandler<PropertyChangedEventArgs>? VariableAdded;
        event EventHandler<PropertyChangedEventArgs>? VariableRemoved;
        event EventHandler<PropertyChangedEventArgs>? VariableUpdated;

        public IEnumerable<KeyValuePair<string, dynamic>> Variables => _variables;

        public GlobalVariables()
        {
            _variables = new ConcurrentDictionary<string, dynamic>();
        }

        public bool Init(string variableName, dynamic value)
        {
            if (_variables.TryAdd(variableName, value))
            {
                VariableAdded?.Invoke(this, new PropertyChangedEventArgs(variableName));
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool Init(string variableName, Func<dynamic> valueFactory)
        {
            while (true)
            {
                if (_variables.ContainsKey(variableName))
                {
                    return false;
                }
                else if (_variables.TryAdd(variableName, valueFactory()))
                {
                    VariableAdded?.Invoke(this, new PropertyChangedEventArgs(variableName));
                    return true;
                }
            }
        }

        /// <summary>
        /// Attempts to update the global variable and returns whether the value was updated or not.
        /// </summary>
        public bool Update(string variableName, dynamic? newValue)
        {
            bool updated;

            if (newValue is null)
            {
                updated = _variables.TryRemove(variableName, out _);

                if (updated)
                    VariableRemoved?.Invoke(this, new PropertyChangedEventArgs(variableName));
            }
            else
            {
                while (true)
                {
                    if (_variables.TryGetValue(variableName, out dynamic? currentValue) &&
                        _variables.TryUpdate(variableName, newValue, currentValue))
                    {
                        updated = newValue != currentValue;
                        if (updated)
                            VariableUpdated?.Invoke(this, new PropertyChangedEventArgs(variableName));
                        break;
                    }
                    else if (_variables.TryAdd(variableName, newValue))
                    {
                        updated = true;
                        VariableAdded?.Invoke(this, new PropertyChangedEventArgs(variableName));
                        break;
                    }
                }
            }

            return updated;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object? result)
        {
            _variables.TryGetValue(binder.Name, out result);
            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object? value)
        {
            Update(binder.Name, value);
            return true;
        }
    }
}
