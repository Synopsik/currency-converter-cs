using currency_converter_cs.Components;
using currency_converter_cs.Components.Clients;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register the IHttpClientFactory service and configure a named client
builder.Services.AddHttpClient("ExchangeApi", client =>
{
    // Attach global attribute BaseAddress to the client
    client.BaseAddress = new Uri("https://cdn.jsdelivr.net/npm/@fawazahmed0/");
});

// Register our services
builder.Services.AddScoped<ExchangeRateService>();
builder.Services.AddScoped<FavoritesService>();
builder.Services.AddScoped<CacheService>();

// Build the application
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();