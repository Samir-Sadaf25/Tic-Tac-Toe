using Cortex.Mediator;
using Cortex.Mediator.DependencyInjection;
using Serilog;
using TicTacToe.Application;
using TicTacToe.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

// Register Cortex.Mediator scanning Application assembly
builder.Services.AddCortexMediator(
    new[] { typeof(AssemblyReference) },
    options => options.AddDefaultBehaviors());

// Register Infrastructure services
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Lobby}/{action=Index}/{id?}");

app.Run();
