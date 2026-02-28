using System.Globalization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
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
    options.TokenLifespan = TimeSpan.FromHours(24);
});

// ── Permission model: handler + named policies for every permission ──────────
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddAuthorization(options =>
{
    foreach (var permission in Permissions.GetAll())
    {
        options.AddPolicy(permission, policy =>
            policy.Requirements.Add(new PermissionRequirement(permission)));
    }
});

// ── Application services ─────────────────────────────────────────────────────
builder.Services.AddScoped<seed>();
builder.Services.AddScoped<IUserCreation, UserCreationService>();
builder.Services.AddScoped<IEmailSender, EmailSenderService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<IAcademicService, AcademicService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<ExcelExportService>();
builder.Services.AddTransient<DataHelper>();

// Phase 2 services
builder.Services.AddScoped<ICourseLecturerService, CourseLecturerService>();
builder.Services.AddScoped<IAssessmentStructureService, AssessmentStructureService>();
builder.Services.AddScoped<IMarksEntryService, MarksEntryService>();
builder.Services.AddScoped<IAssessmentApprovalService, AssessmentApprovalService>();
builder.Services.AddScoped<IGPAService, GPAService>();
builder.Services.AddScoped<ITranscriptService, TranscriptService>();
builder.Services.AddScoped<ITeachingAppraisalService, TeachingAppraisalService>();
builder.Services.AddScoped<INonTeachingStaffService, NonTeachingStaffService>();

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
