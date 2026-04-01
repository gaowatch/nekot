using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using Avalonia;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;
using NekoT.Desktop.Resources;

namespace NekoT.Desktop.MarkupExtensions;

public class DynamicLocalizationExtension : MarkupExtension, INotifyPropertyChanged, IDisposable
{
    private static readonly object _lock = new();
    private static readonly List<WeakReference<DynamicLocalizationExtension>> _instances = new();
    private static readonly ConcurrentDictionary<string, PropertyInfo?> _propertyCache = new();
    
    private string _key = string.Empty;
    private string? _cachedValue;
    private bool _disposed;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Key
    {
        get => _key;
        set
        {
            if (_key != value)
            {
                _key = value;
                _cachedValue = null;
                OnPropertyChanged(nameof(Value));
            }
        }
    }

    public string Value
    {
        get
        {
            if (string.IsNullOrEmpty(_key))
                return string.Empty;
                
            if (_cachedValue == null)
            {
                _cachedValue = GetLocalizedString(_key);
            }
            return _cachedValue ?? _key;
        }
    }

    static DynamicLocalizationExtension()
    {
        Strings.StaticPropertyChanged += OnStringsStaticPropertyChanged;
    }

    public DynamicLocalizationExtension()
    {
        RegisterInstance(this);
    }

    public DynamicLocalizationExtension(string key) : this()
    {
        _key = key;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        try
        {
            return CreateBinding(serviceProvider);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DynamicLocalization] Error in ProvideValue: {ex.Message}");
            return Value;
        }
    }

    private object CreateBinding(IServiceProvider serviceProvider)
    {
        var binding = new ReflectionBindingExtension
        {
            Path = nameof(Value),
            Mode = BindingMode.OneWay,
            Source = this
        };
        return binding.ProvideValue(serviceProvider);
    }

    private static void RegisterInstance(DynamicLocalizationExtension instance)
    {
        ExecuteWithLock(() =>
        {
            CleanupDeadReferences();
            _instances.Add(new WeakReference<DynamicLocalizationExtension>(instance));
        });
    }

    private static void CleanupDeadReferences()
    {
        _instances.RemoveAll(wr => !wr.TryGetTarget(out _));
    }

    private static void ExecuteWithLock(Action action)
    {
        lock (_lock)
        {
            action();
        }
    }

    private static List<DynamicLocalizationExtension> GetAliveInstances()
    {
        lock (_lock)
        {
            CleanupDeadReferences();
            var aliveInstances = new List<DynamicLocalizationExtension>(_instances.Count);
            
            foreach (var wr in _instances)
            {
                if (wr.TryGetTarget(out var instance) && !instance._disposed)
                {
                    aliveInstances.Add(instance);
                }
            }
            
            return aliveInstances;
        }
    }

    private static void OnStringsStaticPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        var toRefresh = GetAliveInstances();
        
        foreach (var instance in toRefresh)
        {
            instance.RefreshValue();
        }
    }

    private void RefreshValue()
    {
        _cachedValue = null;
        OnPropertyChanged(nameof(Value));
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private static string? GetLocalizedString(string key)
    {
        try
        {
            var property = _propertyCache.GetOrAdd(key, k =>
            {
                var type = typeof(Strings);
                return type.GetProperty(k, BindingFlags.Public | BindingFlags.Static);
            });
            
            if (property != null)
            {
                var value = property.GetValue(null);
                return value?.ToString();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DynamicLocalization] Failed to get string for key '{key}': {ex.Message}");
        }
        return key;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            ExecuteWithLock(() => RemoveInstanceFromList());
        }
    }
    
    private void RemoveInstanceFromList()
    {
        _instances.RemoveAll(wr => 
        {
            if (wr.TryGetTarget(out var instance))
            {
                return instance == this;
            }
            return true;
        });
    }
}