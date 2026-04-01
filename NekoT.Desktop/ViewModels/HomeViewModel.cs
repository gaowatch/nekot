using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using NekoT.Desktop.Services;

namespace NekoT.Desktop.ViewModels;

public class HomeViewModel : ViewModelBase
{
    private string _searchQuery = "";

    public event EventHandler<string>? NavigateRequested;

    public string SearchQuery
    {
        get => _searchQuery;
        set => SetField(ref _searchQuery, value);
    }

    public ICommand NavigateCommand => new RelayCommand(_ =>
    {
        var url = GetNavigateUrl(SearchQuery);
        NavigateRequested?.Invoke(this, url);
    });

    public string GetNavigateUrl(string query)
    {
        query = query?.Trim() ?? "";
        System.Diagnostics.Debug.WriteLine($"[GetNavigateUrl] Query: '{query}'");

        if (string.IsNullOrEmpty(query))
        {
            var homePage = UserSettingsService.Instance.HomePage;
            System.Diagnostics.Debug.WriteLine($"[GetNavigateUrl] Empty query, HomePage from settings: '{homePage}'");
            
            if (!string.IsNullOrEmpty(homePage) && homePage != "about:blank")
            {
                System.Diagnostics.Debug.WriteLine($"[GetNavigateUrl] Returning custom home page: {homePage}");
                return homePage;
            }
            System.Diagnostics.Debug.WriteLine($"[GetNavigateUrl] Returning default: https://www.google.com");
            return "https://www.google.com";
        }

        if (query.StartsWith("http://") || query.StartsWith("https://"))
        {
            System.Diagnostics.Debug.WriteLine($"[GetNavigateUrl] Returning URL: {query}");
            return query;
        }

        if (query.Contains(".") && !query.Contains(" "))
        {
            var url = $"https://{query}";
            System.Diagnostics.Debug.WriteLine($"[GetNavigateUrl] Returning domain: {url}");
            return url;
        }

        var searchUrl = $"https://www.google.com/search?q={Uri.EscapeDataString(query)}";
        System.Diagnostics.Debug.WriteLine($"[GetNavigateUrl] Returning search: {searchUrl}");
        return searchUrl;
    }
}

public class HomeViewModel : ViewModelBase
{
    private string _searchQuery = "";

    public event EventHandler<string>? NavigateRequested;

    public string SearchQuery
    {
        get => _searchQuery;
        set => SetField(ref _searchQuery, value);
    }

    public ICommand NavigateCommand => new RelayCommand(_ =>
    {
        var url = GetNavigateUrl(SearchQuery);
        NavigateRequested?.Invoke(this, url);
    });

    public string GetNavigateUrl(string query)
    {
        query = query?.Trim() ?? "";
        if (string.IsNullOrEmpty(query))
        {
            var homePage = UserSettingsService.Instance.HomePage;
            if (!string.IsNullOrEmpty(homePage) && homePage != "about:blank") return homePage;
            return "https://www.google.com";
        }

        if (query.StartsWith("http://") || query.StartsWith("https://")) return query;
        if (query.Contains(".") && !query.Contains(" ")) return $"https://{query}";
        return $"https://www.google.com/search?q={Uri.EscapeDataString(query)}";
    }
}