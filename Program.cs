using FleetBharat.TMSService.Application.Interfaces;
using FleetBharat.TMSService.Application.Services;
using FleetBharat.TMSService.Infrastructure.ConnectionFactory;
using FleetBharat.TMSService.Infrastructure.Repository.Implementation;
using FleetBharat.TMSService.Infrastructure.Repository.Interfaces;
using Hangfire;
using Hangfire.PostgreSql;
using Infrastructure.ExternalServices;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ✅ Add Hangfire
builder.Services.AddHangfire(config =>
    config.UsePostgreSqlStorage(builder.Configuration.GetConnectionString("Default"))
);

builder.Services.AddHangfireServer();


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FleetBharat TMS Service API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {your JWT token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            Array.Empty<string>()
        }
    });
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<AuthHeaderHandler>();

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("api", config =>
    {
        config.Window = TimeSpan.FromMinutes(1);
        config.PermitLimit = 100;
        config.QueueLimit = 0;
    });
});

builder.Services.AddHttpClient<IdentityApiClient>(client =>
{
    var identityBaseUrl = builder.Configuration["Services:IdentityUrl"];
    if (!string.IsNullOrWhiteSpace(identityBaseUrl))
        client.BaseAddress = new Uri(identityBaseUrl);
})
.AddHttpMessageHandler<AuthHeaderHandler>();

// Register CommonApiClient and use base URL from configuration (Services:CommonApiUrl).
// Falls back to Services:IdentityUrl when CommonApiUrl is not configured.
var commonBaseUrl = builder.Configuration["Services:CommonApiUrl"] ?? builder.Configuration["Services:IdentityUrl"];
builder.Services.AddHttpClient<CommonApiClient>(client =>
{
    if (!string.IsNullOrWhiteSpace(commonBaseUrl))
        client.BaseAddress = new Uri(commonBaseUrl);
})
.AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddScoped<IDbConnectionFactory, DbConnectionFactory>();
builder.Services.AddScoped<GetServices>();
builder.Services.AddScoped<DbLogger>();
builder.Services.AddScoped<IRouteService, RouteService>();
builder.Services.AddScoped<ITripService, TripService>();
builder.Services.AddScoped<ITripPlanRepository, TripPlanRepository>();
builder.Services.AddScoped<ICreateTripService, CreateTripService>();
builder.Services.AddScoped<ICreateTripRepository, CreateTripRepository>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration.GetConnectionString("Default");

builder.Services.AddDbContext<AppDbContext>(opt =>
{
    opt.UseNpgsql(connectionString);

    if (builder.Environment.IsDevelopment())
    {
        opt.EnableDetailedErrors();
        opt.EnableSensitiveDataLogging();
    }
});

// ✅ Hardened JWT
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException("Jwt:Key is not configured.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = true;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.Zero,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey =
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AppCors", policy =>
    {
        if (allowedOrigins is { Length: > 0 })
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
            return;
        }

        if (builder.Environment.IsDevelopment())
        {
            policy.SetIsOriginAllowed(_ => true)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
            return;
        }

        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

var scope = app.Services.CreateScope();
var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

recurringJobManager.AddOrUpdate<ICreateTripService>(
    "create-recurring-trips",
    service => service.CreateTripAsync(),
    "15 0 * * *",
    new RecurringJobOptions
    {
        TimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time")
    });
// ✅ Hangfire Dashboard
app.UseHangfireDashboard("/hangfire");


// ================= MIDDLEWARE =================

// Global exception handler (wrap all)
app.UseMiddleware<ExceptionMiddleware>();

//Security headers early
//app.UseMiddleware<SecurityHeadersMiddleware>();

if (app.Environment.IsDevelopment() || app.Environment.IsProduction() || app.Environment.IsStaging())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "FleetBharat TMS API v1");
        options.RoutePrefix = string.Empty;
    });
}

//app.UseHttpsRedirection();

app.UseCors("AppCors");

//Rate limiter line refernce:50 
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers()
   .RequireAuthorization()
   .RequireRateLimiting("api");

app.Run();