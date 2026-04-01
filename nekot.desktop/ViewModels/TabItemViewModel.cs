using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia.Controls;
using NekoT.Desktop.Resources;

namespace NekoT.Desktop.ViewModels;

public class TabItemViewModel : ViewModelBase
{
    private bool _isSelected;
    private string _title = Strings.Tab_NewTab;
    private UserControl? _content;
    private bool _canClose = true;

    public event EventHandler? Closed;
    public event EventHandler? Selected;

    public string Title
    {
        get => _title;
        set => SetField(ref _title, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetField(ref _isSelected, value) && value)
            {
                Selected?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public void SetIsSelectedSilent(bool value)
    {
        if (_isSelected != value)
        {
            _isSelected = value;
            OnPropertyChanged(nameof(IsSelected));
        }
    }

    public UserControl? Content
    {
        get => _content;
        set => SetField(ref _content, value);
    }

    public bool CanClose
    {
        get => _canClose;
        set => SetField(ref _canClose, value);
    }

    public string TabType { get; set; } = "browser";

    public ICommand Close { get; }
    public ICommand Select { get; }

    public TabItemViewModel()
    {
        Close = new RelayCommand(_ => OnClose());
        Select = new RelayCommand(_ => OnSelect());
    }

    private void OnClose()
    {
        Closed?.Invoke(this, EventArgs.Empty);
    }

    private void OnSelect()
    {
        IsSelected = true;
    }
}