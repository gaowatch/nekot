using System.Text;

namespace NekoT.Desktop.Services.WebView2;

public class StealthScriptBuilder
{
    public bool IncludeTrackingProtection { get; set; }
    public bool IncludeAdBlocking { get; set; }

    public string Build()
    {
        var sb = new StringBuilder(16384);
        sb.AppendLine("(function() {");
        sb.AppendLine("    'use strict';");
        sb.AppendLine();
        sb.AppendLine("    // ========== Stealth Mode ==========");
        sb.Append(GetStealthScriptContent());
        sb.AppendLine();

        if (IncludeTrackingProtection)
        {
            sb.AppendLine("    // ========== Tracking Protection ==========");
            sb.Append(GetTrackingProtectionContent());
            sb.AppendLine();
        }

        if (IncludeAdBlocking)
        {
            sb.AppendLine("    // ========== Ad Blocking ==========");
            sb.Append(GetAdBlockContent());
            sb.AppendLine();
        }

        sb.AppendLine("})();");
        return sb.ToString();
    }

    public int GetEstimatedSize()
    {
        int size = GetStealthScriptContent().Length + 200;
        if (IncludeTrackingProtection) size += GetTrackingProtectionContent().Length + 100;
        if (IncludeAdBlocking) size += GetAdBlockContent().Length + 100;
        return size;
    }

    private static string GetStealthScriptContent() => @"
        try {
            if (navigator.webdriver !== undefined) {
                Object.defineProperty(navigator, 'webdriver', {
                    get: function() { return undefined; }, configurable: true
                });
            }
            Object.defineProperty(navigator, 'languages', {
                get: function() { return ['zh-CN', 'zh', 'en-US', 'en']; }
            });
            Object.defineProperty(navigator, 'platform', {
                get: function() { return 'Win32'; }
            });
            Object.defineProperty(navigator, 'hardwareConcurrency', {
                get: function() { return 8; }
            });
            Object.defineProperty(navigator, 'deviceMemory', {
                get: function() { return 8; }
            });
            window.chrome = { app: { isInstalled: false }, webstore: {}, runtime: {} };
            var getParameter = WebGLRenderingContext.prototype.getParameter;
            WebGLRenderingContext.prototype.getParameter = function(parameter) {
                if (parameter === 37445) return 'Intel Inc.';
                if (parameter === 37446) return 'Intel Iris OpenGL Engine';
                return getParameter.call(this, parameter);
            };
            console.log('[NekoT] Stealth mode initialized');
        } catch (e) {
            console.error('[NekoT] Stealth mode error:', e);
        }
    ";

    private static string GetTrackingProtectionContent() => @"
        try {
            var blockedDomains = ['google-analytics.com', 'googletagmanager.com', 'analytics.google.com',
                'facebook.com/tr', 'connect.facebook.net', 'ads.twitter.com', 'analytics.twitter.com',
                'scorecardresearch.com', 'quantserve.com', 'newrelic.com', 'hotjar.com',
                'fullstory.com', 'mixpanel.com', 'amplitude.com', 'segment.com'];
            var originalCreateElement = document.createElement;
            document.createElement = function(tag) {
                var element = originalCreateElement.call(document, tag);
                if (tag.toLowerCase() === 'script') {
                    var originalSetAttribute = element.setAttribute;
                    element.setAttribute = function(name, value) {
                        if (name === 'src' && typeof value === 'string') {
                            for (var i = 0; i < blockedDomains.length; i++) {
                                if (value.indexOf(blockedDomains[i]) !== -1) { return; }
                            }
                        }
                        return originalSetAttribute.call(this, name, value);
                    };
                }
                return element;
            };
            console.log('[NekoT] Tracking protection enabled');
        } catch (e) {
            console.error('[NekoT] Tracking protection error:', e);
        }
    ";

    private static string GetAdBlockContent() => @"
        try {
            var adDomains = ['googleads.g.doubleclick.net', 'pagead2.googlesyndication.com',
                'adservice.google.com', 'ads.google.com', 'doubleclick.net', 'googlesyndication.com',
                'facebook.com/tr', 'facebook.net/en_US/fbevents.js', 'ads.twitter.com',
                'ads.yahoo.com', 'amazon-adsystem.com', 'ads.youtube.com'];
            var hideAdsCss = `[class*='ad-'], [id*='ad-'], [class*='ads-'], [id*='ads-'],
                [class*='advert'], [id*='advert'], [class*='banner'], [class*='sponsor'],
                [data-ad], [data-ads], iframe[src*='doubleclick'], ins.adsbygoogle { display: none !important; }`;
            var style = document.createElement('style');
            style.textContent = hideAdsCss;
            (document.head || document.documentElement).appendChild(style);
            console.log('[NekoT] Ad blocking enabled');
        } catch (e) {
            console.error('[NekoT] Ad blocking error:', e);
        }
    ";
}