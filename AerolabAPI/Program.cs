using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Swashbuckle.AspNetCore.Filters;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Any;
using AerolabAPI.Services;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System;
using System.Collections.Generic;
using System.Threading;

// ✅ List of WebSocket connections
var webSocketConnections = new List<WebSocket>();

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ApplicationName = typeof(Program).Assembly.FullName,
    ContentRootPath = AppContext.BaseDirectory
});

// ✅ Ensure API listens on port 5054
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5054);
});

// 🔹 Read Database Type from Configuration
var useSQLite = builder.Configuration.GetValue<bool>("UseSQLite");

if (useSQLite)
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
    Console.WriteLine("✅ Using SQLite as the primary database.");
}

// ✅ Always Initialize MongoDB for OpenAI, FAQs, and Visitors
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDbSettings"));

builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
    return new MongoClient(settings.ConnectionString);
});

builder.Services.AddSingleton<IMongoDatabase>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
    return new MongoClient(settings.ConnectionString).GetDatabase(settings.DatabaseName);
});

Console.WriteLine("✅ MongoDB Connected Successfully.");

// 🔹 Enable Controllers
builder.Services.AddControllers();

// 🔹 Configure JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("your-secret-key")),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

builder.Services.AddAuthorization();

// ✅ Register Required Services
builder.Services.AddSingleton<FaqService>();

// 🔹 CORS Policy
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(MyAllowSpecificOrigins, policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 🔹 Add Swagger with JWT Support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Aerolab API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' followed by your token"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

var app = builder.Build();

// ✅ Enable Swagger
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Aerolab API V1");
    c.RoutePrefix = "swagger";
    Console.WriteLine("🚀 Swagger UI is now enabled. Visit: http://localhost:5054/swagger");
});
app.UseCors(MyAllowSpecificOrigins);

// ✅ Ensure database migrations run at startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<AppDbContext>();
        dbContext.Database.Migrate();
        Console.WriteLine("✅ Database migration applied successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Database migration failed: {ex.Message}");
    }
}

// ✅ Correct Middleware Order
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ✅ Enable WebSockets
app.UseWebSockets();
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/ws")
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            webSocketConnections.Add(webSocket);
            Console.WriteLine("🔗 WebSocket Client Connected!");

            await HandleWebSocket(webSocket);
        }
        else
        {
            context.Response.StatusCode = 400;
        }
    }
    else
    {
        await next();
    }
});

// ✅ Start Wake Word Detection in Background

// ✅ Function to Send Wake Word Events to WebSockets
async Task HandleWebSocket(WebSocket webSocket)
{
    var buffer = new byte[1024];

    while (webSocket.State == WebSocketState.Open)
    {
        var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

        if (result.MessageType == WebSocketMessageType.Close)
        {
            Console.WriteLine("❌ WebSocket Disconnected");
            webSocketConnections.Remove(webSocket);
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
        }
    }
}

// ✅ Function to Notify WebSocket Clients of Wake Word Detection
async Task NotifyWebSockets(string message)
{
    foreach (var ws in webSocketConnections)
    {
        if (ws.State == WebSocketState.Open)
        {
            var messageBytes = Encoding.UTF8.GetBytes(message);
            await ws.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}

// ✅ Print API URLs
Console.WriteLine("🚀 API started and listening on: http://0.0.0.0:5054");
Console.WriteLine("🔗 Swagger UI: http://0.0.0.0:5054/swagger");

app.Urls.Add("http://0.0.0.0:5054");
app.Urls.Add("http://localhost:5054");

// ✅ Run Application
app.Run();
