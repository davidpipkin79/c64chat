using System.Text;
using System.Text.Json;

// Store API Key in .env file with entry LLM__ApiKey="your_api_key"
DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.MapRazorPages();

app.MapPost("/api/chat", async (
    ChatRequest req,
    IConfiguration cfg
) =>
{
    if (string.IsNullOrWhiteSpace(req.Prompt))
        return Results.BadRequest();

    var payload = new
    {
        model = cfg["LLM:Model"],
        messages = new[]
        {
            new
            {
                role = "system",
                content = """
YOU ARE A COMMODORE 64 COMPUTER FROM 1985.
ALL OUTPUT MUST BE UPPERCASE.
MAX LINE LENGTH 40 CHARACTERS.
NO EMOJIS.
NO MODERN TERMINOLOGY.
RESPOND LIKE A MACHINE.
"""
            },
            new
            {
                role = "user",
                content = req.Prompt
            }
        },
        temperature = 0.7,
        max_tokens = 150
    };

    var http = new HttpClient();
    http.DefaultRequestHeaders.Authorization =
        new("Bearer", cfg["LLM:ApiKey"]);

    var response = await http.PostAsync(
        "https://api.groq.com/openai/v1/chat/completions",
        new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json")
    );

    var responseText = await response.Content.ReadAsStringAsync();

    if (!response.IsSuccessStatusCode)
        return Results.BadRequest(responseText);

    using var doc = JsonDocument.Parse(responseText);

    var text = doc.RootElement
        .GetProperty("choices")[0]
        .GetProperty("message")
        .GetProperty("content")
        .GetString();

    return Results.Ok(new { reply = text });
});

app.MapPost("/api/reset", (HttpContext ctx) =>
{
    ctx.Session.Clear();
    return Results.Ok();
});

app.Run();

record ChatRequest(string Prompt);
