using LiteInvestFront.Services;
using LiteInvestFront.Components;
using LiteInvestServerDll;
using LiteInvest.Entity.PlazaEntity;
using Microsoft.AspNetCore.Components.Server;

using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Formatters;
using Newtonsoft.Json.Converters;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore.Internal;
using static Microsoft.IO.RecyclableMemoryStreamManager;

AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;



var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.Configure<CircuitOptions>(options =>
{
    options.DetailedErrors = true; // Enable detailed error messages
});

builder.Services.AddHttpContextAccessor();

////////////// MS SQL connection
builder.Services.AddDbContextFactory<AppDBContext>(options =>
{
    string connectionString = "";
    string locationSQLServer = builder.Configuration.GetConnectionString("ConnectionType");

    if (locationSQLServer == "DBLocal")
    {
        connectionString = builder.Configuration.GetConnectionString("ConnectionDBLocal");
    }
    else if (locationSQLServer == "DBTestServer")
    {
        connectionString = builder.Configuration.GetConnectionString("ConnectionDBTestServer");
    }
    else if (locationSQLServer == "DBProdServer")
    {
        connectionString = builder.Configuration.GetConnectionString("ConnectionDBProdServer");
    }
    options.UseSqlServer(connectionString);
});


builder.Services.AddScoped<UiLogicService>();
builder.Services.AddScoped<JsInteropService>();

builder.Services.AddScoped<DbContext>();

builder.Services.AddSingleton<DBManager>(provider =>
{
    var dbContextFactory = provider.GetRequiredService<IDbContextFactory<AppDBContext>>();
    return new DBManager(dbContextFactory);
}); ;

builder.Services.AddSingleton<ServerService>(provider =>
{
    var dbContextFactory = provider.GetRequiredService<IDbContextFactory<AppDBContext>>();
    return new ServerService(dbContextFactory);
});
builder.Services.AddSingleton<ServiceDB>(provider =>
{
    var dbContextFactory = provider.GetRequiredService<IDbContextFactory<AppDBContext>>();
    return new ServiceDB(dbContextFactory);
});

builder.Services.AddSingleton<AccountManager>();
builder.Services.AddSingleton<TransactionManager>();
builder.Services.AddSingleton<CommissionManager>();



builder.Services.AddControllers(o =>
{
    o.RespectBrowserAcceptHeader = true;
    
})
    
    .AddNewtonsoftJson(opt =>{opt.SerializerSettings.Converters.Add(new StringEnumConverter());})
    .AddJsonOptions(opt => { opt.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());  });

builder.Services.AddEndpointsApiExplorer();
builder.AddSwaggerServiceLite();
builder.AddJWTAutorizationServiceLite();
builder.Services.AddAutentificationServiceLite();

builder.Services.AddTelerikBlazor();

//////////////////////////////////
// Localization
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// Register cultures
var supportedCultures = new[] { "ru", "en" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures[0])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

//////////////////////////////////

var app = builder.Build();

//////////////////////////////////
// Localization
app.UseRequestLocalization(localizationOptions);
//////////////////////////////////

PlazaOptions? plazasimulation = builder.Configuration.GetSection("PlazaOptions").Get<PlazaOptions>();
ServiceDB? serviceDB = app.Services.GetRequiredService<ServiceDB>();
ServerService? server = app.Services.GetRequiredService<ServerService>();
AccountManager? accountManager = app.Services.GetRequiredService<AccountManager>();
TransactionManager? transactionManager = app.Services.GetRequiredService<TransactionManager>();
TransactionManager.Open(server, serviceDB);
CommissionManager commissionManager = app.Services.GetService<CommissionManager>();
CommissionManager.Open(serviceDB);
app.Services.GetRequiredService<DBManager>();

server.Start(plazasimulation, serviceDB, accountManager);


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseAuthentication();   // добавление middleware аутентификации 
app.UseAuthorization();   // добавление middleware авторизации 

app.UseAntiforgery();

app.UseCors(_ => _.SetIsOriginAllowed(_ => true)
                  .AllowCredentials()
                  .AllowAnyHeader()
                  .AllowAnyMethod());

app.UseHttpsRedirection();



app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapSwaggerAppLite();

app.MapControllers();

app.Run();


Console.WriteLine("END SERVER");


void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
{
    try
    {
        string message =sender?.ToString()+ "\n" + e?.ExceptionObject?.ToString()??"Unknown Error";
        message = System.Reflection.Assembly.GetExecutingAssembly() + "\n" + message;
        Console.WriteLine(message);

        using (StreamWriter writer = new StreamWriter($"UnhandledExceptionError{DateTime.Now.Date}.txt", true))
        {
            writer.WriteLine(message);
        }

    }

    catch
    {

    }
}