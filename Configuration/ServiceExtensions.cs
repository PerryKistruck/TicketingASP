using Microsoft.EntityFrameworkCore;
using TicketingASP.Data;
using TicketingASP.Repositories;
using TicketingASP.Services;

namespace TicketingASP.Configuration;

/// <summary>
/// Extension methods for configuring application services
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// Adds core application services to the DI container
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Get connection string - check multiple locations:
        // 1. Azure Connection String (prefixed by type): POSTGRESQLCONNSTR_AZURE_POSTGRESQL_CONNECTIONSTRING
        // 2. Azure App Setting: AZURE_POSTGRESQL_CONNECTIONSTRING
        // 3. Azure Connection String via GetConnectionString
        // 4. Local User Secrets: ConnectionStrings:DefaultConnection
        var connectionString = configuration["POSTGRESQLCONNSTR_AZURE_POSTGRESQL_CONNECTIONSTRING"]
            ?? configuration["AZURE_POSTGRESQL_CONNECTIONSTRING"] 
            ?? configuration.GetConnectionString("AZURE_POSTGRESQL_CONNECTIONSTRING")
            ?? configuration.GetConnectionString("DefaultConnection");
        
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException(
                "Database connection string not found. " +
                "Please set AZURE_POSTGRESQL_CONNECTIONSTRING in Azure App Settings or Connection Strings, " +
                "or configure ConnectionStrings:DefaultConnection locally.");
        }
        
        // Add DbContext with PostgreSQL
        services.AddDbContext<TicketingDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Add database connection factory for Dapper
        services.AddScoped<IDbConnectionFactory>(sp => 
            new PostgresConnectionFactory(connectionString!));

        // Add repositories
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITeamRepository, TeamRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<ILookupRepository, LookupRepository>();

        // Add services
        services.AddScoped<ITicketService, TicketService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ITeamService, TeamService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<ILookupService, LookupService>();

        return services;
    }

    /// <summary>
    /// Adds MVC and Razor Pages services
    /// </summary>
    public static IServiceCollection AddMvcServices(this IServiceCollection services)
    {
        services.AddControllersWithViews();
        services.AddRazorPages();

        return services;
    }

    /// <summary>
    /// Adds authentication services
    /// </summary>
    public static IServiceCollection AddAuthenticationServices(this IServiceCollection services)
    {
        services.AddAuthentication("Cookies")
            .AddCookie("Cookies", options =>
            {
                options.LoginPath = "/Account/Login";
                options.LogoutPath = "/Account/Logout";
                options.AccessDeniedPath = "/Account/AccessDenied";
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;
            });

        services.AddAuthorization(options =>
        {
            // Require authentication by default for all endpoints
            options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
            
            options.AddPolicy("AdminOnly", policy => 
                policy.RequireRole("Administrator"));
            options.AddPolicy("ManagerOrAdmin", policy => 
                policy.RequireRole("Administrator", "Manager"));
            options.AddPolicy("AgentOrAbove", policy => 
                policy.RequireRole("Administrator", "Manager", "Agent"));
        });

        return services;
    }
}
