using TableBackend.Hubs;
using TableBackend.service;
using TableBackend.Service;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSingleton<ITableStorage, TableStorage>();
builder.Services.AddControllers();


builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.MaximumReceiveMessageSize = 102400000; // 100 MB
});



builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});


var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors("AllowReact");

app.MapControllers(); 

app.MapHub<EmailHub>("/emailHub");

app.Run();
