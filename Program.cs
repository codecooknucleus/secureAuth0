using Auth0.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Cookie configuration for HTTP to support cookies with SameSite=None
// builder.Services.ConfigureSameSiteNoneCookies();

// ✅ Configure cookie policy manually
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.None;
    options.HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.Always;
    options.Secure = CookieSecurePolicy.Always; // Microsoft.AspNetCore.CookiePolicy.CookieSecurePolicy.Always;

    options.OnAppendCookie = context =>
    {
        context.CookieOptions.SameSite = SameSiteMode.None;
    };
    options.OnDeleteCookie = context =>
    {
        context.CookieOptions.SameSite = SameSiteMode.None;
    };
});

// Cookie configuration for HTTPS
//  builder.Services.Configure<CookiePolicyOptions>(options =>
//  {
//     options.MinimumSameSitePolicy = SameSiteMode.None;
//  });







builder.Services.AddAuth0WebAppAuthentication(options =>
{
    options.Domain = builder.Configuration["Auth0:Domain"];
    options.ClientId = builder.Configuration["Auth0:ClientId"];
    options.Scope = "openid profile email scope1 scope2";
});


// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
