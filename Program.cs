using System.Text;
using System.Text.Json;

// Store HuggingFace Key in .env file with entry HuggingFace__ApiKey="your_api_key"
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

app.MapPost("/api/chat", async (ChatRequest req, HttpContext ctx, IConfiguration cfg) =>
{
    if (string.IsNullOrWhiteSpace(req.Prompt))
        return Results.BadRequest("EMPTY PROMPT");

    var history = ctx.Session.GetString("history") ?? "";

    var messages = new List<object>
    {
        new
        {
            role = "system",
            content = """
YOU ARE A COMMODORE 64 COMPUTER FROM 1985.
ALL OUTPUT MUST BE UPPERCASE.
MAX LINE LENGTH 40 CHARACTERS.
NO MODERN TERMS.
NO EMOJIS.
RESPOND LIKE A MACHINE.
"""
        }
    };

    if (!string.IsNullOrWhiteSpace(history))
    {
        messages.Add(new { role = "assistant", content = history });
    }

    messages.Add(new { role = "user", content = req.Prompt });

    var payload = new
    {
        model = cfg["HuggingFace:Model"],
        messages
    };

    var http = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(120)
    };

    http.DefaultRequestHeaders.Authorization =
        new("Bearer", cfg["HuggingFace:ApiKey"]);

    var json = JsonSerializer.Serialize(payload);

    var response = await http.PostAsync(
        "https://router.huggingface.co/v1/chat/completions",
        new StringContent(json, Encoding.UTF8, "application/json")
    );

    var responseText = await response.Content.ReadAsStringAsync();

    if (!response.IsSuccessStatusCode)
        return Results.BadRequest();

    var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    var text = doc.RootElement
        .GetProperty("choices")[0]
        .GetProperty("message")
        .GetProperty("content")
        .GetString() ?? "";

    history += "\n" + req.Prompt + "\n" + text;
    ctx.Session.SetString("history", history);

    return Results.Ok(new { reply = text });
});

app.MapPost("/api/reset", (HttpContext ctx) =>
{
    ctx.Session.Clear();
    return Results.Ok();
});

app.Run();

record ChatRequest(string Prompt);
