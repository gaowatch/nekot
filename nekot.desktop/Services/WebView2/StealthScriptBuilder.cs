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

        if (IncludeTrackingProtection)
        {
            size += GetTrackingProtectionContent().Length + 100;
        }

        if (IncludeAdBlocking)
        {
            size += GetAdBlockContent().Length + 100;
        }

        return size;
    }

    private static string GetStealthScriptContent()
    {
        return @"
        try {
            if (navigator.webdriver !== undefined) {
                Object.defineProperty(navigator, 'webdriver', {
                    get: function() { return undefined; },
                    configurable: true
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
            
            window.chrome = {
                app: { isInstalled: false },
                webstore: {},
                runtime: {}
            };
            
            var getParameter = WebGLRenderingContext.prototype.getParameter;
            WebGLRenderingContext.prototype.getParameter = function(parameter) {
                if (parameter === 37445) return 'Intel Inc.';
                if (parameter === 37446) return 'Intel Iris OpenGL Engine';
                return getParameter.call(this, parameter);
            };
            
            var originalToDataURL = HTMLCanvasElement.prototype.toDataURL;
            HTMLCanvasElement.prototype.toDataURL = function() {
                if (this.width === 220 && this.height === 30) {
                    var context = this.getContext('2d');
                    if (context) {
                        var imageData = context.getImageData(0, 0, this.width, this.height);
                        for (var i = 0; i < imageData.data.length; i += 4) {
                            imageData.data[i] ^= (Math.random() * 2) | 0;
                        }
                        context.putImageData(imageData, 0, 0);
                    }
                }
                return originalToDataURL.apply(this, arguments);
            };
            
            var originalGetTimezoneOffset = Date.prototype.getTimezoneOffset;
            Date.prototype.getTimezoneOffset = function() {
                return -480;
            };
            
            console.log('[NekoT] Stealth mode initialized');
        } catch (e) {
            console.error('[NekoT] Stealth mode error:', e);
        }
    ";
    }

    private static string GetTrackingProtectionContent()
    {
        return @"
        try {
            var blockedDomains = [
                'google-analytics.com',
                'googletagmanager.com',
                'analytics.google.com',
                'facebook.com/tr',
                'connect.facebook.net',
                'ads.twitter.com',
                'analytics.twitter.com',
                'scorecardresearch.com',
                'quantserve.com',
                'newrelic.com',
                'hotjar.com',
                'fullstory.com',
                'mixpanel.com',
                'amplitude.com',
                'segment.com'
            ];
            
            var originalCreateElement = document.createElement;
            document.createElement = function(tag) {
                var element = originalCreateElement.call(document, tag);
                if (tag.toLowerCase() === 'script') {
                    var originalSetAttribute = element.setAttribute;
                    element.setAttribute = function(name, value) {
                        if (name === 'src' && typeof value === 'string') {
                            for (var i = 0; i < blockedDomains.length; i++) {
                                if (value.indexOf(blockedDomains[i]) !== -1) {
                                    console.log('[NekoT] Blocked tracking script:', value);
                                    return;
                                }
                            }
                        }
                        return originalSetAttribute.call(this, name, value);
                    };
                }
                return element;
            };
            
            var originalAppendChild = Element.prototype.appendChild;
            Element.prototype.appendChild = function(child) {
                if (child && child.tagName === 'SCRIPT' && child.src) {
                    for (var i = 0; i < blockedDomains.length; i++) {
                        if (child.src.indexOf(blockedDomains[i]) !== -1) {
                            console.log('[NekoT] Blocked tracking script append:', child.src);
                            return child;
                        }
                    }
                }
                return originalAppendChild.call(this, child);
            };
            
            var imgProto = HTMLImageElement.prototype;
            var originalSrcSetter = Object.getOwnPropertyDescriptor(imgProto, 'src').set;
            Object.defineProperty(imgProto, 'src', {
                set: function(value) {
                    if (typeof value === 'string') {
                        for (var i = 0; i < blockedDomains.length; i++) {
                            if (value.indexOf(blockedDomains[i]) !== -1) {
                                console.log('[NekoT] Blocked tracking pixel:', value);
                                return;
                            }
                        }
                    }
                    return originalSrcSetter.call(this, value);
                }
            });
            
            var originalXHROpen = XMLHttpRequest.prototype.open;
            XMLHttpRequest.prototype.open = function(method, url) {
                for (var i = 0; i < blockedDomains.length; i++) {
                    if (url.indexOf(blockedDomains[i]) !== -1) {
                        console.log('[NekoT] Blocked tracking XHR:', url);
                        this._blocked = true;
                        return;
                    }
                }
                return originalXHROpen.apply(this, arguments);
            };
            
            var originalXHRSend = XMLHttpRequest.prototype.send;
            XMLHttpRequest.prototype.send = function() {
                if (this._blocked) {
                    return;
                }
                return originalXHRSend.apply(this, arguments);
            };
            
            var originalFetch = window.fetch;
            window.fetch = function(url) {
                var urlStr = typeof url === 'string' ? url : url.url;
                for (var i = 0; i < blockedDomains.length; i++) {
                    if (urlStr && urlStr.indexOf(blockedDomains[i]) !== -1) {
                        console.log('[NekoT] Blocked tracking fetch:', urlStr);
                        return Promise.resolve(new Response('', { status: 200 }));
                    }
                }
                return originalFetch.apply(this, arguments);
            };
            
            console.log('[NekoT] Tracking protection enabled');
        } catch (e) {
            console.error('[NekoT] Tracking protection error:', e);
        }
    ";
    }

    private static string GetAdBlockContent()
    {
        return @"
        try {
            var adDomains = [
                'googleads.g.doubleclick.net',
                'pagead2.googlesyndication.com',
                'adservice.google.com',
                'ads.google.com',
                'doubleclick.net',
                'googlesyndication.com',
                'facebook.com/tr',
                'facebook.net/en_US/fbevents.js',
                'ads.twitter.com',
                'ads.yahoo.com',
                'amazon-adsystem.com',
                'ads.youtube.com'
            ];
            
            var hideAdsCss = `
                [class*='ad-'], [id*='ad-'],
                [class*='ads-'], [id*='ads-'],
                [class*='advert'], [id*='advert'],
                [class*='banner'], [class*='sponsor'],
                [class*='promo'], [class*='commercial'],
                [data-ad], [data-ads],
                iframe[src*='doubleclick'],
                iframe[src*='googlesyndication'],
                iframe[src*='googleads'],
                ins.adsbygoogle,
                .adsbygoogle,
                div[class*='google-ad'],
                div[id*='google-ad'],
                [aria-label*='advertisement'],
                [aria-label*='Ad'],
                a[href*='click'] img,
                a[href*='track'] img
                { display: none !important; visibility: hidden !important; height: 0 !important; width: 0 !important; }
            `;
            
            var style = document.createElement('style');
            style.textContent = hideAdsCss;
            (document.head || document.documentElement).appendChild(style);
            
            var originalCreateElement = document.createElement;
            document.createElement = function(tag) {
                var element = originalCreateElement.call(document, tag);
                if (tag.toLowerCase() === 'script' || tag.toLowerCase() === 'iframe') {
                    var originalSetAttribute = element.setAttribute;
                    element.setAttribute = function(name, value) {
                        if (name === 'src' && typeof value === 'string') {
                            for (var i = 0; i < adDomains.length; i++) {
                                if (value.indexOf(adDomains[i]) !== -1) {
                                    console.log('[NekoT] Blocked ad:', value);
                                    return;
                                }
                            }
                        }
                        return originalSetAttribute.call(this, name, value);
                    };
                }
                return element;
            };
            
            var originalAppendChild = Element.prototype.appendChild;
            Element.prototype.appendChild = function(child) {
                if (child && (child.tagName === 'SCRIPT' || child.tagName === 'IFRAME') && child.src) {
                    for (var i = 0; i < adDomains.length; i++) {
                        if (child.src.indexOf(adDomains[i]) !== -1) {
                            console.log('[NekoT] Blocked ad element:', child.src);
                            return child;
                        }
                    }
                }
                return originalAppendChild.call(this, child);
            };
            
            console.log('[NekoT] Ad blocking enabled');
        } catch (e) {
            console.error('[NekoT] Ad blocking error:', e);
        }
    ";
    }
}