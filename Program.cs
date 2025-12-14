using TicketingASP.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Configure services using extension methods for modularity
builder.Services.AddApplicationServices();
builder.Services.AddMvcServices();

var app = builder.Build();

// Configure the HTTP request pipeline using extension methods
app.UseApplicationMiddleware();

app.Run();
