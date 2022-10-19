using AspNetCoreRateLimit;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddInMemoryRateLimiting();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
/*
builder.Services.Configure<IpRateLimitOptions>(options =>
{
    options.EnableEndpointRateLimiting = true;
    options.StackBlockedRequests = false;
    options.HttpStatusCode = (int)HttpStatusCode.TooManyRequests;
    options.RealIpHeader = "X-Real-IP";
    options.QuotaExceededMessage = "Maximum request limit exceeded!";
    options.QuotaExceededResponse = new QuotaExceededResponse()
    {
        Content = "{{ \"message\": \"Whoa! Calm down, cowboy!\", \"details\": \"Quota exceeded. Maximum allowed: {0} per {1}. Please try again in {2} second(s).\" }}",
        ContentType = "application/json",
        StatusCode = (int)HttpStatusCode.TooManyRequests
};
    options.EndpointWhitelist = new List<string>() { "get:/api/license", "*:/api/status" };
    //options.IpWhitelist = new List<string>() { "127.0.0.1", "::1/10", "192.168.0.0/24" };
    options.GeneralRules = new List<RateLimitRule>
        {
            new RateLimitRule
            {
                Endpoint = "*",
                Period = "20s",
                Limit = 2
            }
        };
});
*/

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
app.UseIpRateLimiting();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
