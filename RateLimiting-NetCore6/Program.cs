using RateLimiting_NetCore6.ServiceCollection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the DI (Dependency Injection) container

// Rate Limiting configuration - limits the number of requests per IP or per Client
// Rules are defined in appsettings.json under IpRateLimiting and ClientRateLimiting sections
builder.Services.AddRateLimitingServiceCollection(builder.Configuration);

builder.Services.AddControllers();

// Swagger/OpenAPI configuration for API documentation
// Learn more: https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline

// Enable Swagger only in development environment
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Rate Limiting middleware - MUST come before UseAuthorization and UseEndpoints
// Evaluates and limits requests based on configured policies
app.UseRateLimitingServiceCollection();

// Redirects HTTP requests to HTTPS
app.UseHttpsRedirection();

// Enables authorization for protected endpoints
app.UseAuthorization();

// Maps controllers to API endpoints
app.MapControllers();

app.Run();
