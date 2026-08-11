using System.Web;

namespace IntegrationTests;

public static class HttpClientExtensions
{
    public static string AddQueryString(this string uri, Dictionary<string, string> parameters)
    {
        if (parameters == null || parameters.Count == 0)
        {
            return uri;
        }

        var paramList = new List<string>();
        foreach (var kvp in parameters)
        {
            paramList.Add($"{kvp.Key}={HttpUtility.UrlEncode(kvp.Value)}");
        }

        var queryString = string.Join("&", paramList);

        var separator = uri.Contains('?') ? '&' : '?';

        return $"{uri}{separator}{queryString}";
    }
}