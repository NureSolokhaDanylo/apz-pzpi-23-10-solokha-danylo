using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

class HttpRequest
{
    public string Url { get; set; }
    public string Token { get; set; }
    public Dictionary<string, string> Parameters { get; set; }

    public HttpRequest(string url, string token)
    {
        Url = url;
        Token = token;
        Parameters = new Dictionary<string, string>();
    }

    // Deep copy clone
    public HttpRequest Clone()
    {
        var clone = new HttpRequest(Url, Token);
        clone.Parameters = new Dictionary<string, string>(Parameters);
        return clone;
    }

    // Методи для створення змінених копій
    public HttpRequest WithToken(string newToken)
    {
        var clone = Clone();
        clone.Token = newToken;
        return clone;
    }

    public HttpRequest AddParameter(string key, string value)
    {
        var clone = Clone();
        clone.Parameters[key] = value;
        return clone;
    }

    // Формування query string
    private string BuildQueryString()
    {
        return Parameters.Count == 0 ? "" : "?" + 
            string.Join("&", Parameters.Select(p => 
                $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
    }

    // Виконання GET-запиту
    public async Task<string> ExecuteAsync()
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {Token}");
        try
        {
            var response = await client.GetAsync(Url + BuildQueryString());
            return $"[{Token}] {response.StatusCode}";
        }
        catch (Exception ex)
        {
            return $"[{Token}] Error: {ex.Message}";
        }
    }
}

class Program
{
    static async Task Main()
    {
        // Базовий об'єкт з параметрами
        var baseRequest = new HttpRequest("https://api.github.com/users", "token_base");
        baseRequest.Parameters["per_page"] = "10";

        // Копії з різними токенами та параметрами
        var request1 = baseRequest.WithToken("token_user1").AddParameter("since", "1");
        var request2 = baseRequest.WithToken("token_user2").AddParameter("since", "100");
        var request3 = baseRequest.WithToken("token_user3").AddParameter("since", "200");

        // Паралельне виконання запитів
        var results = await Task.WhenAll(
            request1.ExecuteAsync(),
            request2.ExecuteAsync(),
            request3.ExecuteAsync()
        );

        foreach (var result in results)
            Console.WriteLine(result);
    }
}