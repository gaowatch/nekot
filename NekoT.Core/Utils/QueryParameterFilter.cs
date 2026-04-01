using System;
using System.Collections.Generic;
using System.Text;

namespace NekoT.Core.Utils;

public static class QueryParameterFilter
{
    private static readonly string[] SensitiveParamNames = new[]
    {
        "api_key", "apikey", "key", "app_key", "private_key",
        "token", "access_token", "refresh_token", "auth_token", "bearer_token",
        "secret", "client_secret", "app_secret", "secret_key",
        "authorization", "auth", "credentials", "credential",
        "password", "passwd", "pwd",
        "session", "session_id", "session_token"
    };

    public static string FilterSensitiveParams(string? query)
    {
        if (string.IsNullOrEmpty(query))
            return string.Empty;

        var hasQuestionMark = query.Contains('?');
        var queryContent = hasQuestionMark ? query.Substring(query.IndexOf('?') + 1) : query;

        if (string.IsNullOrEmpty(queryContent))
            return string.Empty;

        var parameters = queryContent.Split('&');
        var filteredParams = new List<string>();

        foreach (var param in parameters)
        {
            var eqIndex = param.IndexOf('=');
            if (eqIndex <= 0) continue;

            var paramName = param.Substring(0, eqIndex);
            if (!IsSensitiveParam(paramName))
            {
                filteredParams.Add(param);
            }
        }

        return string.Join("&", filteredParams);
    }

    private static bool IsSensitiveParam(string paramName)
    {
        var lowerName = paramName.ToLowerInvariant();
        foreach (var prefix in SensitiveParamNames)
        {
            if (lowerName.StartsWith(prefix))
                return true;
        }
        return false;
    }

    public static IReadOnlyCollection<string> GetSensitiveParamNames() => SensitiveParamNames;
}
