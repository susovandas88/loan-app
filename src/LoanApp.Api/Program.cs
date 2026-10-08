using System.Text;
using System.Text.Json.Serialization;
using LoanApp.Api.Auth;
using LoanApp.Api.Middleware;
using LoanApp.Application.Contracts;
using LoanApp.Infrastructure;
using LoanApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Loan Application API",
        Version = "v1",
        Description =
            "Customer loan apply-and-upload API. " +
            "Use Applications to create drafts, request upload URLs, complete uploads, submit, and poll status. " +
            "Loan products lists required document types. " +
            "Sign in with POST /api/v1/auth/login and send Authorization: Bearer {accessToken}. " +
            "Development uploads receives the actual file bytes for local/Docker storage (PUT to the URL returned by upload-url)."
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT returned by POST /api/v1/auth/login."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddLoanInfrastructure(builder.Configuration, enableInProcessWorker: true);

var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:5173"];
builder.Services.AddCors(options =>
{
    options.AddPolicy("spa", policy =>
        policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var useDevAuth = builder.Configuration.GetValue("Auth:UseDevelopmentAuth", false);
var authMode = builder.Configuration["Auth:Mode"] ?? "Local";
if (useDevAuth)
{
    builder.Services.AddAuthentication(DevelopmentAuthHandler.SchemeName)
        .AddScheme<DevelopmentAuthOptions, DevelopmentAuthHandler>(DevelopmentAuthHandler.SchemeName, _ => { });
}
else if (string.Equals(authMode, "External", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = builder.Configuration["Auth:Authority"];
            options.Audience = builder.Configuration["Auth:Audience"];
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                NameClaimType = "sub",
                RoleClaimType = "role"
            };
        });
}
else
{
    var issuer = builder.Configuration["Auth:Issuer"] ?? "loan-app";
    var audience = builder.Configuration["Auth:Audience"] ?? "loan-app-api";
    var signingKey = builder.Configuration["Auth:SigningKey"] ?? "";
    const string wellKnownDevKey = "dev-only-loan-app-signing-key-32!";
    if (Encoding.UTF8.GetByteCount(signingKey) < 32)
    {
        throw new InvalidOperationException("Auth:SigningKey must be at least 32 bytes.");
    }

    if (!builder.Environment.IsDevelopment() && signingKey == wellKnownDevKey)
    {
        throw new InvalidOperationException("Replace Auth:SigningKey outside Development.");
    }

    builder.Services.AddSingleton(new JwtIssuer(
        issuer,
        audience,
        signingKey,
        builder.Configuration.GetValue("Auth:TokenMinutes", 60)));
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                ClockSkew = TimeSpan.FromMinutes(1),
                NameClaimType = "sub",
                RoleClaimType = "role"
            };
        });
}

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LoanDbContext>();
    await db.Database.EnsureCreatedAsync();
    await UserSeeder.SeedAsync(db);
}

app.UseMiddleware<ExceptionMappingMiddleware>();
app.UseCors("spa");
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Loan Application API v1");
        options.RoutePrefix = "swagger";
    });
    app.MapOpenApi();
}
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
