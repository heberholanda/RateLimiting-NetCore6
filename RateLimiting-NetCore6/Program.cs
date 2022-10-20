using RateLimiting_NetCore6.ServiceCollection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Rate Limiting
builder.Services.AddRateLimitingServiceCollection(builder.Configuration);

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Rate Limiting
app.UseRateLimitingServiceCollection();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
