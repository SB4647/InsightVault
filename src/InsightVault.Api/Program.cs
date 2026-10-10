using InsightVault.Api.Auth;
using InsightVault.Api.Observability;
using InsightVault.Api.RateLimiting;
using InsightVault.Api.ProcessingQueue;
using InsightVault.Application.Features.Chat;
using InsightVault.Application.Features.Documents;
using InsightVault.Application.Features.Documents.Processing;
using InsightVault.Application.Features.Search;
using InsightVault.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IDocumentChunkingService, DocumentChunkingService>();
builder.Services.AddScoped<IDocumentProcessingService, DocumentProcessingService>();
var rateLimitOptions = builder.Configuration.GetSection(ApiRateLimitOptions.SectionName).Get<ApiRateLimitOptions>() ?? new ApiRateLimitOptions();
rateLimitOptions.Validate();
builder.Services.AddSingleton(rateLimitOptions);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("authentication", context => CreateFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        rateLimitOptions.AuthenticationPermitLimit,
        TimeSpan.FromMinutes(1)));
    options.AddPolicy("upload", context => CreateFixedWindowLimiter(
        GetRateLimitUserKey(context),
        rateLimitOptions.UploadPermitLimit,
        TimeSpan.FromHours(1)));
    options.AddPolicy("interactive", context => CreateFixedWindowLimiter(
        GetRateLimitUserKey(context),
        rateLimitOptions.InteractivePermitLimit,
        TimeSpan.FromMinutes(1)));
});
var uploadQuotaOptions = builder.Configuration.GetSection(UploadQuotaOptions.SectionName).Get<UploadQuotaOptions>() ?? new UploadQuotaOptions();
uploadQuotaOptions.Validate();
builder.Services.AddSingleton(uploadQuotaOptions);
var retrievalOptions = builder.Configuration.GetSection(RetrievalOptions.SectionName).Get<RetrievalOptions>() ?? new RetrievalOptions();
retrievalOptions.Validate();
builder.Services.AddSingleton(retrievalOptions);
builder.Services.AddSingleton<HybridRetrievalService>();
builder.Services.AddScoped<ISemanticSearchService, SemanticSearchService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddSingleton<IQuestionSafetyService, QuestionSafetyService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddInfrastructure(builder.Configuration);
if (!string.Equals(builder.Configuration["Queue:Provider"], "Disabled", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddHostedService<DocumentProcessingOutboxHostedService>();
}
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey))
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("ClientApp", policy =>
    {
        policy
            .WithOrigins(GetAllowedCorsOrigins(builder.Configuration))
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (builder.Configuration.GetValue("HttpsRedirection:Enabled", true))
{
app.UseHttpsRedirection();
}

app.UseRouting();
app.UseCors("ClientApp");
app.UseAuthentication();
app.UseRateLimiter();
app.UseMiddleware<SafeRequestLoggingMiddleware>();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

static string[] GetAllowedCorsOrigins(IConfiguration configuration)
{
    var configuredOrigins = configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? [];

    if (configuredOrigins.Length == 0)
    {
        configuredOrigins = (configuration["Cors:AllowedOrigins"] ?? string.Empty)
            .Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    if (configuredOrigins.Length == 0)
    {
        throw new InvalidOperationException("Cors:AllowedOrigins must contain at least one origin.");
    }

    return configuredOrigins;
}

static RateLimitPartition<string> CreateFixedWindowLimiter(string partitionKey, int permitLimit, TimeSpan window)
{
    return RateLimitPartition.GetFixedWindowLimiter(
        partitionKey,
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = window,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0,
            AutoReplenishment = true
        });
}

static string GetRateLimitUserKey(HttpContext context)
{
    return context.User.Identity?.IsAuthenticated == true
        ? context.User.GetRequiredUserId()
        : context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
