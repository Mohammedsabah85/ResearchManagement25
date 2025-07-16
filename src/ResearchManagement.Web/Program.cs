using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ResearchManagement.Infrastructure.Data;
using ResearchManagement.Domain.Entities;
using ResearchManagement.Web.Extensions;
using ResearchManagement.Infrastructure.Middleware;
using Serilog;
using ResearchManagement.Application.Interfaces;
using ResearchManagement.Infrastructure.Repositories;
using ResearchManagement.Infrastructure.Services;
using FluentValidation;
using Microsoft.AspNetCore.RateLimiting;
using ResearchManagement.Application.Commands.Research;
using ResearchManagement.Application.Mappings;
using MediatR;
using ResearchManagement.Application.DTOs;
using ResearchManagement.Application.Queries.Research;
using ResearchManagement.Web.Mappings;
using Microsoft.Extensions.Options;


// ����� Serilog ������ ��� ����� ����
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/researchmanagement-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("��� ����� �������");

    //    var builder = WebApplication.CreateBuilder(args);

    //    // ������� Serilog
    //    builder.Host.UseSerilog();

    //    // Add services to the container.
    //    builder.Services.AddApplicationServices(builder.Configuration);
    //    builder.Services.AddRepositories();
    //    builder.Services.AddBusinessServices();
    //    builder.Services.AddMediatRServices();
    //    builder.Services.AddMappingServices();
    //    builder.Services.AddValidationServices();
    //    builder.Services.AddConfigurationServices(builder.Configuration);
    //    builder.Services.AddBackgroundServices();

    //    // ����� ����� MVC
    //    builder.Services.AddControllersWithViews()
    //        .AddJsonOptions(options =>
    //        {
    //            options.JsonSerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
    //        });

    //    // ����� �������
    //    builder.Services.AddSession(options =>
    //    {
    //        options.IdleTimeout = TimeSpan.FromMinutes(30);
    //        options.Cookie.HttpOnly = true;
    //        options.Cookie.IsEssential = true;
    //    });

    //    // ����� Cookie
    //    builder.Services.ConfigureApplicationCookie(options =>
    //    {
    //        options.LoginPath = "/Account/Login";
    //        options.LogoutPath = "/Account/Logout";
    //        options.AccessDeniedPath = "/Account/AccessDenied";
    //        options.ExpireTimeSpan = TimeSpan.FromDays(7);
    //        options.SlidingExpiration = true;
    //    });

    //    var app = builder.Build();

    //    // Configure the HTTP request pipeline.
    //    if (!app.Environment.IsDevelopment())
    //    {
    //        app.UseExceptionHandler("/Home/Error");
    //        app.UseHsts();
    //    }
    //    else
    //    {
    //        // �� ���� ������ѡ ���� ���� ������ �������
    //        app.UseDeveloperExceptionPage();
    //    }

    //    // ������� ����� ������� ������
    //    app.UseGlobalExceptionMiddleware();

    //    app.UseHttpsRedirection();
    //    app.UseStaticFiles();
    //    app.UseRouting();
    //    app.UseSession();

    //    app.UseAuthentication();
    //    app.UseAuthorization();

    //    // Serilog request logging
    //    app.UseSerilogRequestLogging();

    //    app.MapControllerRoute(
    //        name: "default",
    //        pattern: "{controller=Home}/{action=Index}/{id?}");

    //    // Database initialization and seeding
    //    using (var scope = app.Services.CreateScope())
    //    {
    //        var services = scope.ServiceProvider;
    //        try
    //        {
    //            var context = services.GetRequiredService<ApplicationDbContext>();
    //            var userManager = services.GetRequiredService<UserManager<User>>();
    //            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

    //            // ����� ��� migrations
    //            await context.Database.MigrateAsync();

    //            // ��� �������� �������
    //            await DatabaseSeeder.SeedAsync(context, userManager, roleManager);

    //            Log.Information("�� ����� ����� �������� �����");
    //        }
    //        catch (Exception ex)
    //        {
    //            Log.Fatal(ex, "��� �� ����� ����� ��������");
    //            throw;
    //        }
    //    }

    //    app.Run();
    //}
    //catch (Exception ex)
    //{
    //    Log.Fatal(ex, "��� ��� �������");
    //}
    //finally
    //{
    //    Log.CloseAndFlush();
    //}












    var builder = WebApplication.CreateBuilder(args);

// ����� Serilog ��� Logging �������
builder.Host.UseSerilog((context, configuration) =>
configuration
.ReadFrom.Configuration(context.Configuration)
.WriteTo.Console()
.WriteTo.File("logs/app-.txt", rollingInterval: RollingInterval.Day));



    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            b => b.MigrationsAssembly("ResearchManagement.Infrastructure")
        ));


    // ����� Identity �� ������� ������
    builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    // ������� ���� ������
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;

    // ������� ��������
    options.User.RequireUniqueEmail = true;

    // ������� �����
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // ������� ����� ������ ����������
    options.SignIn.RequireConfirmedEmail = false; // ���� ������ ��� true �� �������
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();
//builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddMappingServices(); // ���� �� ��� �����
// ����� Cookie ������
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
});


// ����� Repositories
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IResearchRepository, ResearchRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IResearchStatusHistoryRepository, ResearchStatusHistoryRepository>();
    builder.Services.AddScoped<ITrackManagerRepository, TrackManagerRepository>();

    builder.Services.AddLogging();

    // ����� Services
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IFileService, FileService>();
    builder.Services.AddScoped<INotificationService, NotificationService>();

    // 4. ����� ������� ������ ��������

    builder.Services.AddScoped<IResearchRepository, ResearchRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<IResearchStatusHistoryRepository, ResearchStatusHistoryRepository>();




// ����� MediatR
builder.Services.AddMediatR(cfg =>
cfg.RegisterServicesFromAssembly(typeof(ResearchManagement.Application.Commands.Research.CreateResearchCommand).Assembly));
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(CreateResearchCommandHandler).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(UpdateResearchCommandHandler).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(DeleteResearchCommandHandler).Assembly);
    builder.Services.AddScoped<IRequestHandler<UpdateResearchStatusCommand, bool>, UpdateResearchStatusCommandHandler>();

});

// 3. ����� Query Handlers
builder.Services.AddScoped<IRequestHandler<GetResearchByIdQuery, ResearchDto?>, GetResearchByIdQueryHandler>();
builder.Services.AddScoped<IRequestHandler<GetResearchFileByIdQuery, ResearchFileDto?>, GetResearchFileByIdQueryHandler>();
builder.Services.AddScoped<IRequestHandler<GetResearchListQuery, PagedResult<ResearchDto>>, GetResearchListQueryHandler>();

 //builder.Services.AddScoped<INotificationService, NotificationService>();


    builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

    // 6. ����� AutoMapper
    builder.Services.AddAutoMapper(typeof(ApplicationMappingProfile), typeof(WebMappingProfile));

    // ����� AutoMapper
    builder.Services.AddAutoMapper(typeof(ResearchManagement.Application.Mappings.ApplicationMappingProfile));

// ����� FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(ResearchManagement.Application.Validators.CreateResearchDtoValidator).Assembly);

// ����� Configuration Settings
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.Configure<FileUploadSettings>(builder.Configuration.GetSection("FileUploadSettings"));

// ����� Controllers �� Views
builder.Services.AddControllersWithViews(options =>
{
    // ����� ����� ���� ��� ��� �����
})
.AddRazorRuntimeCompilation(); // ������� ���

// ����� Background Services
builder.Services.AddHostedService<EmailBackgroundService>();
builder.Services.AddHostedService<DeadlineReminderService>();

// ����� ����� ������ ������ �������
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});

// ����� Rate Limiting ������� �� �������
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("general", cfg =>
    {
        cfg.PermitLimit = 100;
        cfg.Window = TimeSpan.FromMinutes(1);
        cfg.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
        cfg.QueueLimit = 10;
    });
});

// ����� HTTP Client Factory ������� ��������
builder.Services.AddHttpClient();

var app = builder.Build();

// ����� HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    // ������� ���� ��� ����� �� �������
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    // ��� ������ ������� �� �������
    app.UseDeveloperExceptionPage();
}

// ����� Middleware �������� ������ �������
app.UseGlobalExceptionMiddleware();

// ����� Serilog request logging
app.UseSerilogRequestLogging();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// ����� Rate Limiting
app.UseRateLimiter();

// ����� Authentication � Authorization
app.UseAuthentication();
app.UseAuthorization();
await SeedDatabase(app);
// ����� ��������
app.MapControllerRoute(
name: "default",
pattern: "{controller=Home}/{action=Index}/{id?}")
.RequireRateLimiting("general");

// ����� Database Seeding ���� ���
async Task SeedDatabase(WebApplication app)
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();

        try
        {
            logger.LogInformation("��� ����� ������� ������ ����� ��������...");

            var context = services.GetRequiredService<ApplicationDbContext>();
            var userManager = services.GetRequiredService<UserManager<User>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

                // ����� ����� �������� ��� �� ��� ������
                await context.Database.EnsureCreatedAsync();
                //await context.Database.MigrateAsync();
                // ����� Database Seeding
                //await DatabaseSeeder.SeedAsync(context, userManager, roleManager);
            await EnhancedDatabaseSeeder.SeedAsync(context, userManager, roleManager);
            logger.LogInformation("�� ����� ������� �����");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "��� ��� ����� ��� ������� �� ����� ����� ��������");

            // �� ���� ������ѡ ���� ����� �������
            if (app.Environment.IsDevelopment())
            {
                throw;
            }

            // �� ������̡ ��� ����� ������
            logger.LogWarning("-");
        }
    }
}
// ����� ������� ������ �� ���� �������
app.Lifetime.ApplicationStarted.Register(() =>
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("-");
});

app.Lifetime.ApplicationStopping.Register(() =>
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("-");
});

app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "��� ��� �������");
}
finally
{
    Log.CloseAndFlush();
}
