using System.Text;

namespace NekoT.Desktop.Services.WebView2;

public class StealthScriptBuilder
{
    public bool IncludeTrackingProtection { get; set; } = true;
    public bool IncludeAdBlocking { get; set; } = false;
    public bool ModifyNavigator { get; set; } = true;
    public bool BlockWebRTC { get; set; } = false;
    public bool SpoofTimezone { get; set; } = false;

    public string Build()
    {
        var sb = new StringBuilder();
        sb.AppendLine("(function(){");
        if (ModifyNavigator) BuildNavigatorSpoofing(sb);
        if (BlockWebRTC) BuildWebRTCBlocking(sb);
        if (IncludeTrackingProtection) BuildTrackingProtection(sb);
        if (IncludeAdBlocking) BuildAdBlocking(sb);
        if (SpoofTimezone) BuildTimezoneSpoofing(sb);
        sb.AppendLine("})();");
        return sb.ToString();
    }

    private void BuildNavigatorSpoofing(StringBuilder sb)
    {
        sb.AppendLine("(function(){");
        sb.AppendLine("  Object.defineProperty(navigator, 'webdriver', { get: () => undefined });");
        sb.AppendLine("  Object.defineProperty(navigator, 'plugins', { get: () => [1, 2, 3, 4, 5] });");
        sb.AppendLine("  Object.defineProperty(navigator, 'languages', { get: () => ['zh-CN', 'zh', 'en-US', 'en'] });");
        sb.AppendLine("  window.chrome = { runtime: {} };");
        sb.AppendLine("})();");
    }

    private void BuildWebRTCBlocking(StringBuilder sb)
    {
        sb.AppendLine("  window.RTCPeerConnection = function() { return null; };");
    }

    private void BuildTrackingProtection(StringBuilder sb)
    {
        sb.AppendLine("  const trackingPatterns = [/google-analytics\\.com/, /googletagmanager\\.com/, /facebook\\.net/, /doubleclick\\.net/];");
        sb.AppendLine("  window.fetch = new Proxy(window.fetch, {");
        sb.AppendLine("    apply: function(target, thisArg, args) {");
        sb.AppendLine("      const url = args[0]?.url || args[0];");
        sb.AppendLine("      if (trackingPatterns.some(p => p.test(url))) return Promise.reject('Blocked');");
        sb.AppendLine("      return target.apply(thisArg, args);");
        sb.AppendLine("    }");
        sb.AppendLine("  });");
    }

    private void BuildAdBlocking(StringBuilder sb)
    {
        sb.AppendLine("  const adSelectors = ['[class*=\"ad\"]', '[id*=\"ad\"]', '.advertisement', '.sponsored'];");
        sb.AppendLine("  const style = document.createElement('style');");
        sb.AppendLine("  style.textContent = adSelectors.join(',') + '{display:none !important;}';");
        sb.AppendLine("  document.head.appendChild(style);");
    }

    private void BuildTimezoneSpoofing(StringBuilder sb)
    {
        sb.AppendLine("  Intl.DateTimeFormat = new Proxy(Intl.DateTimeFormat, {");
        sb.AppendLine("    construct: function(target, args) { return new target(args[0], {...args[1], timeZone: 'Asia/Shanghai'}); }");
        sb.AppendLine("  });");
    }

    public int GetEstimatedSize() => Build().Length;
}