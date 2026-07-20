using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Stock.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<JwtTokenHandler>();

builder.Services.AddHttpClient(StockApiClient.HttpClientName, client =>
{
    var baseUrl = builder.Configuration["ApiBaseUrl"]
                  ?? throw new InvalidOperationException("Falta la configuración 'ApiBaseUrl'.");
    client.BaseAddress = new Uri(baseUrl);
})
.AddHttpMessageHandler<JwtTokenHandler>();
builder.Services.AddScoped<IStockApiClient, StockApiClient>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.SlidingExpiration = false;
    });

// RF-12: toda pantalla exige sesión salvo las marcadas con [AllowAnonymous].
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Dentro del contenedor sólo escuchamos HTTP: el redirect no tendría puerto destino
// y ensuciaría el log con un warning por request.
if (!app.Configuration.GetValue<bool>("DOTNET_RUNNING_IN_CONTAINER"))
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
