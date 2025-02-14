using LiteInvestFront.Services;
using LiteInvestFront.Components;
using LiteInvestServerDll;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<UiLogicService>();
builder.Services.AddScoped<JsInteropService>();

builder.Services.AddSingleton<ServerService>();


builder.Services.AddTelerikBlazor();

var app = builder.Build();
var server = app.Services.GetRequiredService<ServerService>();
server.Start(true);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
