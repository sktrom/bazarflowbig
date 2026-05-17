using Supermarket.Api.Middleware;
using Supermarket.Api.Services;
using Supermarket.Application;
using Supermarket.Application.Common.Interfaces;
using Supermarket.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Layer DI
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Scoped session context — one instance per HTTP request
builder.Services.AddScoped<SessionContextAccessor>();
builder.Services.AddScoped<ISessionContextAccessor>(sp => sp.GetRequiredService<SessionContextAccessor>());
builder.Services.AddScoped<ISessionContext>(sp => sp.GetRequiredService<SessionContextAccessor>().Current);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalDevPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("LocalDevPolicy");

// Passive session middleware — populates ISessionContext, never short-circuits
app.UseMiddleware<SessionMiddleware>();

app.UseAuthorization();
app.MapControllers();
app.Run();
