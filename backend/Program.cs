using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using BusinessOps.Backend.Auth;
using BusinessOps.Backend.Data;
using BusinessOps.Backend.Permissions;
using BusinessOps.Backend.Repositories.Implementations;
using BusinessOps.Backend.Repositories.Interfaces;
using BusinessOps.Backend.Seed;
using BusinessOps.Backend.Services;
using BusinessOps.Backend.Services.Implementations;
using BusinessOps.Backend.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection("Auth"));
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<PasswordHasherService>();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<PermissionService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IModuleRepository, ModuleRepository>();
builder.Services.AddScoped<IEntryRepository, EntryRepository>();
builder.Services.AddScoped<IPermissionRepository, PermissionRepository>();
builder.Services.AddScoped<IVisualizationRepository, VisualizationRepository>();
builder.Services.AddScoped<IDashboardWidgetRepository, DashboardWidgetRepository>();
builder.Services.AddScoped<IAnalyticsWidgetRepository, AnalyticsWidgetRepository>();
builder.Services.AddScoped<ISeedRepository, SeedRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IModuleService, ModuleService>();
builder.Services.AddScoped<IEntryService, EntryService>();
builder.Services.AddScoped<IPermissionManagementService, PermissionManagementService>();
builder.Services.AddScoped<IVisualizationService, VisualizationService>();
builder.Services.AddScoped<IDashboardWidgetService, DashboardWidgetService>();
builder.Services.AddScoped<IAnalyticsWidgetService, AnalyticsWidgetService>();

builder.Services
    .AddAuthentication("Bearer")
    .AddScheme<AuthenticationSchemeOptions, SimpleJwtAuthenticationHandler>("Bearer", _ => { });

builder.Services.AddAuthorization();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var useInMemoryFallback = string.IsNullOrWhiteSpace(connectionString);

if (!useInMemoryFallback)
{
    try
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
    }
    catch
    {
        useInMemoryFallback = true;
    }
}

if (useInMemoryFallback)
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseInMemoryDatabase("business_ops_dev_db"));

    Console.WriteLine("PostgreSQL unavailable. Using in-memory database fallback.");
}
else
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connectionString));
}

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var passwordHasherService = scope.ServiceProvider.GetRequiredService<PasswordHasherService>();
    if (dbContext.Database.IsRelational())
    {
        await dbContext.Database.MigrateAsync();
    }
    else
    {
        await dbContext.Database.EnsureCreatedAsync();
    }
    await ModuleSeedService.EnsureDefaultModulesAsync(dbContext);
    await ModuleSeedService.EnsureSecurityDefaultsAsync(dbContext, builder.Configuration, passwordHasherService);
}

app.UseHttpsRedirection();
app.UseCors("DevCors");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
