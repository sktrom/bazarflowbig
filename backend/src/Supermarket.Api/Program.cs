using Supermarket.Api.Middleware;
using Supermarket.Api.Services;
using Supermarket.Application;
using Supermarket.Application.Auth.Interfaces;
using Supermarket.Application.Common.Interfaces;
using Supermarket.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
const string CorsPolicyName = "BazarFlowCorsPolicy";

builder.Services.AddControllers();

// Layer DI
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Scoped session context — one instance per HTTP request
builder.Services.AddScoped<SessionContextAccessor>();
builder.Services.AddScoped<ISessionContextAccessor>(sp => sp.GetRequiredService<SessionContextAccessor>());
builder.Services.AddScoped<ISessionContext>(sp => sp.GetRequiredService<SessionContextAccessor>().Current);
builder.Services.AddSingleton<IAuthPolicy, AuthPolicy>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var allowedOrigins = builder.Environment.IsDevelopment()
    ? new[] { "http://localhost:4200", "https://localhost:4200" }
    : builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()?
        .Where(origin => !string.IsNullOrWhiteSpace(origin))
        .Select(origin => origin.Trim())
        .ToArray() ?? Array.Empty<string>();

if (!builder.Environment.IsDevelopment())
{
    if (allowedOrigins.Length == 0)
    {
        throw new InvalidOperationException("Cors:AllowedOrigins must be configured in non-development environments.");
    }

    if (allowedOrigins.Any(origin => origin == "*" || origin.Contains('*')))
    {
        throw new InvalidOperationException("Cors:AllowedOrigins must not contain wildcards in non-development environments.");
    }
}

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseCors(CorsPolicyName);

// Passive session middleware — populates ISessionContext, never short-circuits
app.UseMiddleware<SessionMiddleware>();

app.UseAuthorization();
app.MapControllers();
app.Run();
