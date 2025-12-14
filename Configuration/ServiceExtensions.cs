namespace TicketingASP.Configuration;

/// <summary>
/// Extension methods for configuring application services
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// Adds core application services to the DI container
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Add any application-specific services here
        // Example: services.AddScoped<ITicketService, TicketService>();
        
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
}
