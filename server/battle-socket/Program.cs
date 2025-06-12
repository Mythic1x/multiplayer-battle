
using System.Collections.Concurrent;
using System.Diagnostics.Eventing.Reader;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options => {
    options.SerializerOptions.IncludeFields = true;
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});
builder.WebHost.UseUrls("http://*:5050");
builder.Services.AddCors(options => {
    options.AddDefaultPolicy(policy => {
        policy.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173", "http://73.87.125.145:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => {
        options.Events.OnRedirectToLogin = context => {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options => {
    options.AddFixedWindowLimiter("login", opt => {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromSeconds(60);
    });
});
var app = builder.Build();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
//Battle rooms
var battleSessions = new ConcurrentDictionary<string, GameSession>();
//Users connected
var sessionConnections = new ConcurrentDictionary<string, SessionCache>();

var webSocketOptions = new WebSocketOptions {
    KeepAliveInterval = TimeSpan.FromMinutes(1),
    KeepAliveTimeout = TimeSpan.FromMinutes(2),
};

app.MapPost("/register", async (RegisterData data) => {
    if (data.username.Length > 24) {
        return Results.BadRequest("Username cannot be longer than 24 characters");
    }
    if (data.username.Contains('/') || data.username.Contains('\\')) {
        return Results.BadRequest("invalid characters in username");
    }
    bool successful = await Database.RegisterUser(data.username, data.password, data.starterFighter);
    if (!successful) {
        return Results.InternalServerError("Error registering");
    } else {
        return Results.Ok("Registration successful");
    }
});

app.MapPost("/login", async (LoginData data, HttpContext context) => {
    var user = await Database.GetUser(data.username);
    if (user is null || !PasswordHasher.VerifyPassword(user.hashedPassword, data.password)) {
        return Results.BadRequest("User not found or incorrect password");
    }

    var claims = new List<Claim> {
        new(ClaimTypes.Name, user.username),
        new(ClaimTypes.NameIdentifier, user.id.ToString()),
        new("Permissions", user.permissions.ToString())
    };

    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

    var authProperties = new AuthenticationProperties {
        IsPersistent = true,
        AllowRefresh = true
    };
    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
    var userInfo = new SafeUser(user.username);
    return Results.Ok(userInfo);
}).RequireRateLimiting("login");

app.MapGet("/logout", async (HttpContext httpContext) => {
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Ok("User logged out.");
});

app.MapGet("/player", async (HttpContext context) => {
    var userIdString = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userIdString is null || !long.TryParse(userIdString, out var userId)) {
        return Results.BadRequest("Invalid user ID");
    }
    var player = await Database.GetPlayerById(userId);
    if (player is null) {
        return Results.InternalServerError("Error getting player data");
    }
    return Results.Ok(player);
}).RequireAuthorization();

app.MapGet("/user", async (HttpContext context) => {
    var userIdString = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userIdString is null || !long.TryParse(userIdString, out var userId)) {
        return Results.BadRequest("Invalid user ID");
    }
    var user = await Database.GetUserById(userId);
    if (user is null) {
        return Results.InternalServerError("Error getting player data");
    }
    var safeUser = new SafeUser(user.username);
    return Results.Ok(safeUser);
});

app.MapGet("/get-fighters", () => {
    Results.Ok(Database.Fighters);
});

app.MapPost("get-fighter", (FighterRequestData data) => {
    var fighter = Database.GetFighterByName(data.name);
    if (fighter is not null) {
        Results.Ok(fighter);
    } else {
        Results.NotFound("Fighter not found");
    }
});

app.MapGet("createroom", (HttpContext context) => {
    var userIdString = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userIdString is null) {
        return Results.BadRequest();
    }
    if (sessionConnections.TryGetValue(userIdString, out SessionCache? session)) {
        return Results.Ok(session.id);
    }
    Guid id = Guid.NewGuid();
    battleSessions.TryAdd(id.ToString(), new GameSession());
    return Results.Ok(id.ToString());
}).RequireAuthorization();

app.MapGet("/battle/{roomId}", (HttpContext context, string roomId) => {
    var userIdString = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userIdString is null) {
        return Results.BadRequest();
    }
    if (sessionConnections.TryGetValue(userIdString, out SessionCache? session)) {
        return Results.Redirect($"/battle/{session.id}");
    }
    if (battleSessions.ContainsKey(roomId)) {
        return Results.Ok();
    } else {
        return Results.NotFound();
    }
}).RequireAuthorization();

app.UseWebSockets(webSocketOptions);

app.Use(async (context, next) => {
    if (context.Request.Path == "/ws" && context.WebSockets.IsWebSocketRequest) {
        if (context.User?.Identity is not null && context.User.Identity.IsAuthenticated) {
            var cookieId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (cookieId is null) {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("User is authenticated but missing a user ID claim.");
                return;
            }
            using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            if (sessionConnections.TryGetValue(cookieId, out SessionCache? session)) {
                if (session.session.battle is not null) {
                    await BattleWebSocket.handleMessages(webSocket, context, true, session, battleSessions, sessionConnections, cookieId);
                }
            } else {
                await BattleWebSocket.handleMessages(webSocket, context, null, null, battleSessions, sessionConnections, cookieId);
            }
        } else {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        }
    } else {
        await next(context);
    }
});
//initialize tables and fighter/skill data
await Database.InitializeDatabase();


await app.RunAsync();
record RegisterData(string username, string password, string starterFighter);
record LoginData(string username, string password);
record FighterRequestData(string name);
record SafeUser(string username);

