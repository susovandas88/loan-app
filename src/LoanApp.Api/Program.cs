using System.Text.Json.Serialization;
using LoanApp.Api.Auth;
using LoanApp.Api.Middleware;
using LoanApp.Application.Contracts;
using LoanApp.Infrastructure;
using LoanApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

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
            "Development uploads receives the actual file bytes for local/Docker storage (PUT to the URL returned by upload-url). " +
            "With development auth, requests succeed without a JWT; optional header X-User-Id selects the applicant."
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

var useDevAuth = builder.Configuration.GetValue("Auth:UseDevelopmentAuth", builder.Environment.IsDevelopment());
if (useDevAuth)
{
    builder.Services.AddAuthentication(DevelopmentAuthHandler.SchemeName)
        .AddScheme<DevelopmentAuthOptions, DevelopmentAuthHandler>(DevelopmentAuthHandler.SchemeName, _ => { });
}
else
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = builder.Configuration["Auth:Authority"];
            options.Audience = builder.Configuration["Auth:Audience"];
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                NameClaimType = "oid"
            };
        });
}

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LoanDbContext>();
    await db.Database.EnsureCreatedAsync();
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
