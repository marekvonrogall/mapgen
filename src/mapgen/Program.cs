using System.Net;
using System.Text.Json.Serialization;
using MapService.Classes;

var builder = WebApplication.CreateBuilder(args);

var environmentVariables = new EnvironmentVariables
{
    MapGenPort = Environment.GetEnvironmentVariable("MAPGEN_PORT") ?? "5001",
    ImgGenUrl = Environment.GetEnvironmentVariable("IMGGEN_URL") ?? "http://imggen:5000"
};

builder.WebHost.ConfigureKestrel((context, options) =>
{
    options.Listen(IPAddress.Any, int.Parse(environmentVariables.MapGenPort));
});

builder.Services.AddHttpClient();
builder.Services.AddSingleton(environmentVariables);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder => builder.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

//app.UseHttpsRedirection();
app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

app.Run();
