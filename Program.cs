using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient(); // Needed for sending messages

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new { status = "ok" }));
app.MapGet("/healthz", () => Results.Ok("Healthy"));

// ✅ Verify webhook with Meta

app.MapGet("/webhook", (HttpRequest request) =>
{
    Console.WriteLine("Received webhook verification request2");

    var mode = request.Query["hub.mode"].ToString();
    var token = request.Query["hub.verify_token"].ToString();
    var challenge = request.Query["hub.challenge"].ToString();

    var verifyToken =
        Environment.GetEnvironmentVariable("MY_API_SECRET_KEY")
        ?? "supersecret123";

    if (mode == "subscribe" && token == verifyToken)
    {
        Console.WriteLine("Webhook verified successfully!");
        return Results.Text(challenge, "text/plain");
    }

    return Results.StatusCode(403);
});


app.MapPost("/webhook", async (JsonElement body, IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var entry = body.GetProperty("entry")[0];
        var changes = entry.GetProperty("changes")[0];
        var value = changes.GetProperty("value");

        if (!value.TryGetProperty("messages", out var messages))
            return Results.Ok();

        var message = messages[0];
        var from = message.GetProperty("from").GetString();

        string text = null;
        if (message.TryGetProperty("text", out var textElement))
            text = textElement.GetProperty("body").GetString()?.Trim().ToLower();

        Console.WriteLine($"Received message from {from}: {text}");

        if (text == "hi")
        {
            Console.WriteLine($"Sending auto-reply to {from}");
            await SendWhatsAppMessageAsync(from, "Hello! This is an automated reply from your bot.", httpClientFactory);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error parsing webhook: {ex.Message}");
    }

    return Results.Ok();
});



app.Run();


// helper functions and records can go here
// ==============================
async Task SendWhatsAppMessageAsync(string to, string text, IHttpClientFactory httpClientFactory)
{
    var client = httpClientFactory.CreateClient();
    var token = Environment.GetEnvironmentVariable("WHATSAPP_TOKEN"); // Your WhatsApp token
    var phoneNumberId = Environment.GetEnvironmentVariable("WHATSAPP_PHONE_ID"); // Your test number ID

    var payload = new
    {
        messaging_product = "whatsapp",
        to = to,
        type = "text",
        text = new { body = text }
    };

    var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

    var response = await client.PostAsync($"https://graph.facebook.com/v17.0/{phoneNumberId}/messages", content);

    if (!response.IsSuccessStatusCode)
    {
        var respBody = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Failed to send WhatsApp message: {respBody}");
    }
}






















// using System.Text.Json;
// using System.Text;
// using Microsoft.AspNetCore.Http.Json;
// using WhatsAppFlowApi;
// using System.Net.Http.Headers;

// var builder = WebApplication.CreateBuilder(args);

// // ==============================
// // JSON OPTIONS
// // ==============================
// builder.Services.Configure<JsonOptions>(options =>
// {
//     options.SerializerOptions.PropertyNamingPolicy = null;
// });

// // HttpClient needed for WhatsApp + external API
// builder.Services.AddHttpClient();

// var app = builder.Build();

// // ==============================
// // PORT BINDING (Render compatible)
// // ==============================
// var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
// app.Urls.Add($"http://*:{port}");

// // ==============================
// // BASIC ENDPOINTS
// // ==============================
// app.MapGet("/", () => Results.Ok(new { status = "ok" }));
// app.MapGet("/healthz", () => Results.Ok("Healthy"));

// app.MapGet("/debug/env", () =>
// {
//     var hasPem = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PRIVATE_KEY_PEM"));
//     var hasB64 = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PRIVATE_KEY_PEM_B64"));
//     return Results.Ok(new { keyPresent = hasPem || hasB64, hasPem, hasB64 });
// });


// // =======================================================
// // 🔹 WHATSAPP WEBHOOK VERIFICATION
// // =======================================================
// app.MapGet("/webhook", (HttpRequest request) =>
// {
//     var mode = request.Query["hub.mode"];
//     var token = request.Query["hub.verify_token"];
//     var challenge = request.Query["hub.challenge"];

//     var verifyToken = Environment.GetEnvironmentVariable("MY_API_SECRET_KEY");

//     if (mode == "subscribe" && token == verifyToken)
//     {
//         return Results.Ok(challenge);
//     }

//     return Results.Unauthorized();
// });


// // =======================================================
// // 🔹 RECEIVE WHATSAPP MESSAGE (send Flow on "hi")
// // =======================================================
// app.MapPost("/webhook", async (
//     JsonElement body,
//     IHttpClientFactory httpClientFactory
// ) =>
// {
//     try
//     {
//         var message =
//             body.GetProperty("entry")[0]
//                 .GetProperty("changes")[0]
//                 .GetProperty("value")
//                 .GetProperty("messages")[0];

//         var from = message.GetProperty("from").GetString();
//         var text = message.GetProperty("text")
//                           .GetProperty("body")
//                           .GetString()
//                           ?.Trim()
//                           .ToLower();

//         if (text == "hi" && from != null)
//         {
//             await SendFlowAsync(from, httpClientFactory);
//         }
//     }
//     catch
//     {
//         // WhatsApp expects 200 always
//     }

//     return Results.Ok();
// });


// // =======================================================
// // 🔹 FLOW DATA API (DYNAMIC DROPDOWN)
// // =======================================================
// app.MapPost("/flow-data", async (
//     IHttpClientFactory httpClientFactory
// ) =>
// {
//     var client = httpClientFactory.CreateClient();

//     // 🔹 CALL YOUR EXTERNAL API
//     // Example expected response:
//     // [{ "code": "lekki", "name": "Lekki Phase 1" }]
//     var areas = await client.GetFromJsonAsync<List<AreaDto>>(
//         "https://api.mybusiness.com/delivery-areas"
//     );

//     var dropdown = areas!.Select(a => new
//     {
//         id = a.Code,
//         title = a.Name
//     });

//     return Results.Ok(new
//     {
//         delivery_areas = dropdown
//     });
// });


// // =======================================================
// // 🔹 ENCRYPTED FLOW ENDPOINT (UNCHANGED)
// // =======================================================
// app.MapPost("/flows/endpoint", async (FlowEncryptedRequest req) =>
// {
//     try
//     {
//         var privateKeyPem = Environment.GetEnvironmentVariable("PRIVATE_KEY_PEM");

//         if (string.IsNullOrEmpty(privateKeyPem))
//         {
//             var privateKeyPemB64 = Environment.GetEnvironmentVariable("PRIVATE_KEY_PEM_B64");
//             if (!string.IsNullOrEmpty(privateKeyPemB64))
//             {
//                 privateKeyPem = Encoding.UTF8.GetString(
//                     Convert.FromBase64String(privateKeyPemB64));
//             }
//         }

//         if (string.IsNullOrEmpty(privateKeyPem))
//         {
//             return Results.BadRequest(new { error = "PRIVATE_KEY_PEM not set" });
//         }

//         var rsa = FlowEncryptStatic.LoadRsaFromPem(privateKeyPem);

//         // 1️⃣ Decrypt
//         var decryptedJson = FlowEncryptStatic.DecryptFlowRequest(
//             req, rsa, out var aesKey, out var iv);

//         using var doc = JsonDocument.Parse(decryptedJson);
//         var action = doc.RootElement.GetProperty("action").GetString();

//         // 2️⃣ Ping handler
//         if (action == "ping")
//         {
//             var responseObj = new
//             {
//                 version = "3.0",
//                 data = new { status = "active" }
//             };

//             var encrypted = FlowEncryptStatic.EncryptFlowResponse(
//                 responseObj, aesKey, iv);

//             return Results.Text(encrypted, "application/json");
//         }

//         // 3️⃣ Default response
//         var fallback = FlowEncryptStatic.EncryptFlowResponse(
//             new { version = "3.0", data = new { status = "active" } },
//             aesKey, iv);

//         return Results.Text(fallback, "application/json");
//     }
//     catch (Exception ex)
//     {
//         Console.Error.WriteLine(ex);
//         return Results.StatusCode(500);
//     }
// });

// app.Run();


// =======================================================
// 🔹 HELPERS
// =======================================================
// static async Task SendFlowAsync(
//     string to,
//     IHttpClientFactory httpClientFactory)
// {
//     var phoneNumberId = Environment.GetEnvironmentVariable("WHATSAPP_PHONE_NUMBER_ID");
//     var accessToken = Environment.GetEnvironmentVariable("WHATSAPP_ACCESS_TOKEN");
//     var flowId = Environment.GetEnvironmentVariable("WHATSAPP_FLOW_ID");

//     var url =
//         $"https://graph.facebook.com/v19.0/{phoneNumberId}/messages";

//     var payload = new
//     {
//         messaging_product = "whatsapp",
//         to,
//         type = "interactive",
//         interactive = new
//         {
//             type = "flow",
//             flow = new
//             {
//                 name = "ADDRESS_FLOW",
//                 flow_id = flowId,
//                 flow_cta = "Order Now"
//             }
//         }
//     };

//     var client = httpClientFactory.CreateClient();

//     var request = new HttpRequestMessage(HttpMethod.Post, url)
//     {
//         Content = JsonContent.Create(payload)
//     };

//     request.Headers.Authorization =
//         new AuthenticationHeaderValue("Bearer", accessToken);

//     await client.SendAsync(request);
// }

// record AreaDto(string Code, string Name);






















































// app.MapPost("/flow", async (HttpRequest request) =>
// {
//     using var reader = new StreamReader(request.Body);
//     var body = await reader.ReadToEndAsync();

//     var json = System.Text.Json.JsonDocument.Parse(body);

//     // 1️⃣ Handle verification challenge
//     if (json.RootElement.TryGetProperty("challenge", out var challenge))
//     {
//         return Results.Ok(new
//         {
//             challenge = challenge.GetString()
//         });
//     }

//     // 2️⃣ Handle real flow submission later
//     Console.WriteLine("FLOW DATA:");
//     Console.WriteLine(body);

//     return Results.Ok(new
//     {
//         status = "success"
//     });
// });



// Flow POST endpoint
// app.MapPost("/flow", async (HttpRequest request) =>
// {
//     using var reader = new StreamReader(request.Body);
//     var body = await reader.ReadToEndAsync();

//     Console.WriteLine("FLOW DATA RECEIVED:");
//     Console.WriteLine(body);

//     // Required response
//     return Results.Ok(new
//     {
//         status = "success"
//     });
// });


// app.Run();





// var builder = WebApplication.CreateBuilder(args);

// // Add services to the container.
// // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
// builder.Services.AddOpenApi();

// var app = builder.Build();

// // Configure the HTTP request pipeline.
// if (app.Environment.IsDevelopment())
// {
//     app.MapOpenApi();
// }

// app.UseHttpsRedirection();

// var summaries = new[]
// {
//     "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
// };

// app.MapGet("/weatherforecast", () =>
// {
//     var forecast =  Enumerable.Range(1, 5).Select(index =>
//         new WeatherForecast
//         (
//             DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
//             Random.Shared.Next(-20, 55),
//             summaries[Random.Shared.Next(summaries.Length)]
//         ))
//         .ToArray();
//     return forecast;
// })
// .WithName("GetWeatherForecast");

// app.Run();

// record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
// {
//     public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
// }
