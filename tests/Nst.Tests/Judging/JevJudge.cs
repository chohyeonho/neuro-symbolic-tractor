using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;

namespace Nst.Tests.Judging;

// Jev (TypeSafe System One), the pinned judge. Its version is pinned in Model; see the README's reference environment.
// The key is given by the caller and is never printed.
public sealed class JevJudge(string key) : IJudge
{
    const string Url = "https://api.typesafe.ai/v1/systemone";

    public string Model => "jev-1.14.0";

    public async Task<IReadOnlyDictionary<string, double>> Ask(string text, IReadOnlyDictionary<string, string> questions)
    {
        var q = new JsonObject();
        foreach (var (name, instruction) in questions)
            q[name] = new JsonObject { ["type"] = "noul", ["instructions"] = instruction };
        var body = new JsonObject { ["model"] = Model, ["state"] = text, ["questions"] = q };

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        using var req = new HttpRequestMessage(HttpMethod.Post, Url)
        {
            Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        using var resp = await http.SendAsync(req);
        var reply = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException($"Jev answered {(int)resp.StatusCode}: {reply[..Math.Min(reply.Length, 300)]}");

        var answers = JsonNode.Parse(reply)?["answers"]?.AsObject()
            ?? throw new InvalidDataException("Jev's reply has no answers.");
        return questions.Keys.ToDictionary(
            name => name,
            name => answers[name]?["noul"]?.GetValue<double>()
                ?? throw new InvalidDataException($"Jev gave no answer for '{name}'."));
    }
}
