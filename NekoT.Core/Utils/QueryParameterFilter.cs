using System;
using System.Collections.Generic;
using System.Text;

namespace NekoT.Core.Utils;

public static class QueryParameterFilter
{
    private static readonly HashSet<string> SensitiveParamNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "api_key", "api-key", "apikey", "key", "app_key", "appkey",
        "private_key", "privatekey",
        "token", "access_token", "access-token",
        "refresh_token", "refresh-token",
        "auth_token", "auth-token",
        "bearer_token", "bearer-token",
        "secret", "client_secret", "client-secret",
        "app_secret", "app-secret",
        "secret_key", "secret-key",
        "authorization", "auth",
        "credentials", "credential",
        "password", "passwd", "pwd",
        "session_id", "session-id", "sessionid",
        "session_token", "session-token",
        "signature", "sign",
        "api_secret", "api-secret"
    };

    public static string FilterSensitiveParams(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return string.Empty;

        query = query.Trim();

        if (query == "?" || query.Length == 0)
            return string.Empty;

        bool hasQuestionMark = query.StartsWith("?", StringComparison.Ordinal);
        var queryContent = hasQuestionMark ? query.Substring(1) : query;

        if (string.IsNullOrEmpty(queryContent))
            return string.Empty;

        var parameters = queryContent.Split('&');
        var filteredParams = new List<string>();

        foreach (var param in parameters)
        {
            if (string.IsNullOrEmpty(param))
                continue;

            var eqIndex = param.IndexOf('=');

            if (eqIndex <= 0)
            {
                if (!IsSensitiveParam(param))
                {
                    filteredParams.Add(param);
                }
            }
            else
            {
                var paramName = param.Substring(0, eqIndex);

                if (!IsSensitiveParam(paramName))
                {
                    filteredParams.Add(param);
                }
            }
        }

        if (filteredParams.Count == 0)
            return string.Empty;

        var result = new StringBuilder();
        if (hasQuestionMark)
            result.Append('?');

        result.Append(string.Join("&", filteredParams));
        return result.ToString();
    }

    private static bool IsSensitiveParam(string paramName)
    {
        if (string.IsNullOrEmpty(paramName))
            return false;

        string decodedName;
        try
        {
            decodedName = Uri.UnescapeDataString(paramName);
        }
        catch
        {
            decodedName = paramName;
        }

        if (SensitiveParamNames.Contains(decodedName))
            return true;

        var lowerName = decodedName.ToLowerInvariant();

        var sensitivePrefixes = new[]
        {
            "api_key_", "api-key-", "apikey_",
            "token_", "access_token_", "refresh_token_", "auth_token_",
            "secret_", "client_secret_", "app_secret_",
            "auth_", "authorization_",
            "password_", "passwd_", "pwd_",
            "session_", "session_id_", "session_token_",
            "signature_", "sign_"
        };

        foreach (var prefix in sensitivePrefixes)
        {
            if (lowerName.StartsWith(prefix))
                return true;
        }

        return false;
    }

    public static IReadOnlyCollection<string> GetSensitiveParamNames()
    {
        return SensitiveParamNames;
    }
}