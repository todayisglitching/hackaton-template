using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using testASP.Infrastructure;
using testASP.Models;
using testASP.Services;
using testASP.Services.Interfaces;

AppContext.SetSwitch("Microsoft.AspNetCore.Mvc.ApiExplorer.IsEnhancedModelMetadataSupported", true);

var builder = WebApplication.CreateBuilder(args);

// -------------------------------
// Configuration
// -------------------------------
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? "Rostelecom_SmartHome_2026_Ultra_Secret"; // TODO: move to user secrets or env in prod

var jwtKey = Encoding.ASCII.GetBytes(jwtSecret);
var contentDist = Path.Combine(builder.Environment.ContentRootPath, "Vite", "dist");
var binDist = Path.Combine(AppContext.BaseDirectory, "Vite", "dist");
var webRoot = Directory.Exists(contentDist) ? contentDist : binDist;
var indexFile = Path.Combine(webRoot, "index.html");
var spaReady = Directory.Exists(webRoot) && File.Exists(indexFile);
PhysicalFileProvider? webRootProvider = spaReady ? new PhysicalFileProvider(webRoot) : null;

builder.Services.AddControllers();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(jwtKey),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
{
    options.AddPolicy("VitePolicy", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonContext.Default));

builder.Services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
{
    options.JsonSerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonContext.Default);
});

builder.Services.AddSingleton(new JwtTokenService(jwtSecret));
builder.Services.AddSingleton<UserStore>();
builder.Services.AddSingleton<DeviceStore>();
builder.Services.AddSingleton<RefreshTokenStore>();
builder.Services.AddSingleton<WsConnectionManager>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDeviceService, DeviceService>();

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5000);
    options.ListenAnyIP(5001, listenOptions => listenOptions.UseHttps());
});

var app = builder.Build();

if (spaReady && webRootProvider != null)
{
    app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = webRootProvider });
    app.UseStaticFiles(new StaticFileOptions { FileProvider = webRootProvider });
}

app.UseCors("VitePolicy");
app.UseAuthentication();
app.UseAuthorization();
app.UseWebSockets();
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        app.Logger.LogInformation("API {Method} {Path}", context.Request.Method, context.Request.Path);
    }
    await next();
});

app.MapControllers();

// Все остальные запросы в /api/* -> 404
app.Map("/api/{**rest}", () => Results.NotFound());

// -------------------------------
// SPA fallback (non-API)
// -------------------------------
if (spaReady)
{
    app.MapFallbackToFile("index.html", new StaticFileOptions { FileProvider = webRootProvider! });
}
else
{
    app.MapFallback(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        await context.Response.WriteAsync("SPA build not found. Run Vite build to generate /Vite/dist.");
    });
}

app.Run();
