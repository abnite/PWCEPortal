using System.Globalization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PWCEPortal.ApplicationClass;
using PWCEPortal.Data;
using PWCEPortal.Interfaces;
using PWCEPortal.Services;

var builder = WebApplication.CreateBuilder(args);

// Configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json")
    .Build();

builder.Configuration.AddJsonFile("appsettings.json");

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<PortalDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("PortalDefault")), ServiceLifetime.Scoped);


builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(o =>
{
    o.Password.RequireDigit = false;
    o.Password.RequireNonAlphanumeric = false;
    o.Password.RequireUppercase = false;
    o.Password.RequireLowercase = false;
    o.Lockout.MaxFailedAccessAttempts = 6;
    o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
}).AddDefaultTokenProviders().AddEntityFrameworkStores<PortalDbContext>();

builder.Services.Configure<CookiePolicyOptions>(o =>
{
    o.CheckConsentNeeded = context => true;
    o.MinimumSameSitePolicy = SameSiteMode.None;
});
builder.Services.ConfigureApplicationCookie(a =>
{
    a.AccessDeniedPath = "/Error/403";
    a.Cookie.Name = "PWCEPortalApp";
    a.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    a.LoginPath = "/Account/Login";
    a.LogoutPath = "/Account/Logout";
    a.ReturnUrlParameter = CookieAuthenticationDefaults.ReturnUrlParameter;
    a.SlidingExpiration = true;
});
builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    options.TokenLifespan = TimeSpan.FromHours(24); // Adjust the duration as needed
});

builder.Services.AddScoped<seed>();
builder.Services.AddScoped<IUserCreation, UserCreationService>();
builder.Services.AddScoped<IEmailSender, EmailSenderService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<IAcademicService, AcademicService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<ExcelExportService>();
builder.Services.AddTransient<DataHelper>();

var cultureInfo = new CultureInfo("en-GH");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<PortalDbContext>();
    var seed = services.GetRequiredService<seed>();
    seed.InsertDB();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
   // app.UseExceptionHandler("/Home/Error");
    app.UseExceptionHandler("/Error");
    app.UseStatusCodePagesWithReExecute("/Error/{0}");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.Use(async (context, next) =>
{
    await next();
    if (context.Response.StatusCode == 404 && !context.Response.HasStarted)
    {
        context.Request.Path = "/Error/404";
        await next();
    }
});

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();