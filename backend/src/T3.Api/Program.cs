using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using T3.Api.Authorization;
using T3.Api.Endpoints;
using T3.Api.Http;
using T3.Api.Identity;
using T3.Api.Middleware;
using T3.Api.RateLimiting;
using T3.Application;
using T3.Application.Common.Interfaces;
using T3.Domain.Identity;
using T3.Infrastructure;
using T3.Infrastructure.Identity;
using T3.Infrastructure.Persistence.Seed;

var builder = WebApplication.CreateBuilder(args);

// --- Yapılandırma ---------------------------------------------------------
// Sırlar ortam değişkeninden gelir; appsettings.json'a yazılmaz.
builder.Configuration.AddEnvironmentVariables("T3_");

// --- Servisler -----------------------------------------------------------
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();

// Denetim izinin "nereden" sorusu: IP ve istemci kimliği. Kimlikten ayrı
// arayüz — bkz. IClientContext.
builder.Services.AddScoped<IClientContext, HttpClientContext>();

// Hız sınırı reddi izine pencere başına tek satır düşsün diye
// (bkz. AuthRateLimit.OnRejected). Boyut sınırı bilinçli: anahtar denenen
// e-postadan türüyor, sınırsız sözlük saldırganın belleği şişirmesine
// açık kapı olurdu.
builder.Services.AddMemoryCache(options => options.SizeLimit = 10_000);

var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException(
        "Jwt:Secret tanımlı değil. .env dosyasında T3_Jwt__Secret ayarlayın.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "t3-ekosistem",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "t3-ekosistem-api",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

// Varsayılan güvenli: yetkilendirme üst verisi taşımayan her uç kimlik ister.
// Herkese açık uçlar (sağlık, giriş) bunu .AllowAnonymous() ile devre dışı bırakır.
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.AddPolicy(Policies.ManageStartups, policy => policy.RequireRole(
        nameof(UserRole.SuperAdmin), nameof(UserRole.ProgramManager)));

    options.AddPolicy(Policies.ManagePrograms, policy => policy.RequireRole(
        nameof(UserRole.SuperAdmin)));

    options.AddPolicy(Policies.ManageProgramTerms, policy => policy.RequireRole(
        nameof(UserRole.SuperAdmin), nameof(UserRole.ProgramManager)));

    options.AddPolicy(Policies.ReviewApprovals, policy => policy.RequireRole(
        nameof(UserRole.SuperAdmin), nameof(UserRole.ProgramManager)));

    options.AddPolicy(Policies.ManageUsers, policy => policy.RequireRole(
        nameof(UserRole.SuperAdmin)));

    // Denetim izi kullanıcı yönetiminden ayrı politika: ikisi bugün aynı role
    // açık olsa da farklı gerekçelerle açık, tek sabiti paylaşmaları birini
    // gevşetirken diğerini sessizce gevşetmek olurdu.
    options.AddPolicy(Policies.ViewAuditLogs, policy => policy.RequireRole(
        nameof(UserRole.SuperAdmin)));
});

// Enum'lar sayı değil ad olarak taşınır: arayüz tipleri okunabilir kalır ve
// enum'a yeni değer eklemek mevcut istemcilerin anlamını kaydırmaz.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

const string FrontendCors = "frontend";
builder.Services.AddCors(options =>
    options.AddPolicy(FrontendCors, policy => policy
        .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
                     ?? ["http://localhost:5173"])
        .AllowAnyHeader()
        .AllowAnyMethod()));

// Login ve AI uçlarını kaba kuvvete karşı sınırlar. Kovalar bölümlendirilmiş:
// bir hesabın kilidi başka hesabı, bir kullanıcının AI kotası başkasını etkilemez.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = AuthRateLimit.OnRejected;
    options.AddPolicy(AuthRateLimit.AuthPolicy, AuthRateLimit.PartitionAuth);
    options.AddPolicy(AuthRateLimit.AiPolicy, AuthRateLimit.PartitionAi);
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "T3 Girişim Ekosistemi API",
        Version = "v1",
        Description = "T3 girişimcilik ekosisteminin tek kurumsal hafızası ve karar destek platformu."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT erişim jetonu"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        }] = []
    });
});

var app = builder.Build();

// --- Ardışık düzen -------------------------------------------------------
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Ardışık düzenin ürettiği gövdesiz hataları (401/403 politika reddi, 429 hız
// sınırı) handler hatalarıyla aynı JSON şekline sokar. Gövdesi olan yanıtlara
// dokunmaz, dolayısıyla handler'ın kendi mesajını ezmez.
app.UseStatusCodePages(async context =>
{
    var response = context.HttpContext.Response;

    if (!string.IsNullOrEmpty(response.ContentType))
        return;

    response.ContentType = "application/json";
    await response.WriteAsJsonAsync(new ApiErrorBody(response.StatusCode, response.StatusCode switch
    {
        StatusCodes.Status401Unauthorized => "Oturum açmanız gerekiyor.",
        StatusCodes.Status403Forbidden => "Bu işlem için yetkiniz yok.",
        StatusCodes.Status404NotFound => "İstenen kaynak bulunamadı.",
        StatusCodes.Status429TooManyRequests => "Çok fazla istek gönderdiniz, lütfen bekleyin.",
        _ => "İstek işlenemedi."
    }));
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.DocumentTitle = "T3 Ekosistem API");
}

app.UseCors(FrontendCors);

// Hız sınırı bölüm anahtarı giriş gövdesindeki e-postayı kullanıyor; gövde
// sınırlayıcıdan önce tamponlanmak zorunda.
app.Use(AuthRateLimit.CaptureLoginEmail);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthEndpoints();
app.MapAuthEndpoints();
app.MapStartupEndpoints();
app.MapAchievementEndpoints();
app.MapDocumentEndpoints();
app.MapProgramEndpoints();
app.MapApprovalEndpoints();
app.MapAuditEndpoints();
app.MapUserEndpoints();
app.MapReportEndpoints();
app.MapAssistantEndpoints();
app.MapMcpEndpoints();

// --- Açılış teşhisi ------------------------------------------------------
// Sessiz yedek mekanizma bir daha kimseyi yanıltmasın: anahtar okunmadığında
// asistan hata vermeden yerel plana düşüyor, bu da "AI çalışıyor" sanılıyordu.
var chatModel = app.Services.GetRequiredService<IChatModel>();

if (chatModel.IsAvailable)
    app.Logger.LogInformation("Dil modeli hazır: {Model}", chatModel.Name);
else
    app.Logger.LogWarning(
        "Ai:ApiKey boş — asistan yerel plana düşecek. .env içinde T3_Ai__ApiKey ayarlayın.");

// --- Demo verisi ---------------------------------------------------------
// Üretimde hiçbir koşulda çalışmaz: tohum kullanıcıların şifresi bilinen bir
// değer olduğu için ortam kontrolü burada bir güvenlik sınırıdır.
if (app.Environment.IsDevelopment())
{
    using var seedScope = app.Services.CreateScope();
    var seedOptions = seedScope.ServiceProvider
        .GetRequiredService<IOptions<SeedOptions>>().Value;

    if (seedOptions.Enabled)
        await seedScope.ServiceProvider
            .GetRequiredService<DevDataSeeder>()
            .SeedAsync(seedOptions.Password);
}

app.Run();
