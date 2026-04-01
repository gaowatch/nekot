namespace NekoT.Core.Browser;

public interface IBrowserEngine { Task<bool> NavigateAsync(string url); string CurrentUrl { get; } bool CanGoBack { get; } bool CanGoForward { get; } Task GoBackAsync(); Task GoForwardAsync(); Task ReloadAsync(); }

public class BrowserTab { public string Id { get; set; } = Guid.NewGuid().ToString(); public string Url { get; set; } = string.Empty; public string Title { get; set; } = string.Empty; public bool IsLoading { get; set; } }

public class BrowserTabManager
{
    private readonly Dictionary<string, BrowserTab> _tabs = new(); private string? _activeTabId;
    public int TabCount => _tabs.Count; public string? ActiveTabId => _activeTabId;
    public BrowserTab CreateTab(string url) { var tab = new BrowserTab { Url = url, Title = url }; _tabs[tab.Id] = tab; _activeTabId = tab.Id; return tab; }
    public void CloseTab(string id) { if (_tabs.Remove(id) && _activeTabId == id) _activeTabId = _tabs.Keys.FirstOrDefault(); }
    public void SetActiveTab(string id) { if (_tabs.ContainsKey(id)) _activeTabId = id; }
    public BrowserTab? GetTab(string id) => _tabs.TryGetValue(id, out var tab) ? tab : null;
}

public class BrowserUrlValidator
{
    private static readonly HashSet<string> DomainBlacklist = new(StringComparer.OrdinalIgnoreCase) { "coinbase.com", "binance.com", "kraken.com", ".onion" };
    private static readonly HashSet<string> KeywordBlacklist = new(StringComparer.OrdinalIgnoreCase) { "cryptocurrency", "crypto.com", "miningpool", "wallet" };
    public bool IsAllowed(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        if (url.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase)) return false;
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            var host = uri.Host;
            foreach (var blocked in DomainBlacklist) { if (host.Equals(blocked, StringComparison.OrdinalIgnoreCase) || host.EndsWith("." + blocked, StringComparison.OrdinalIgnoreCase)) return false; }
            foreach (var keyword in KeywordBlacklist) { if (url.Contains(keyword, StringComparison.OrdinalIgnoreCase)) return false; }
        }
        else { foreach (var keyword in KeywordBlacklist) { if (url.Contains(keyword, StringComparison.OrdinalIgnoreCase)) return false; } }
        return true;
    }
}