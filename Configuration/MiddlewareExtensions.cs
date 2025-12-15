namespace TicketingASP.Configuration;

/// <summary>
/// Extension methods for configuring the application middleware pipeline
/// </summary>
public static class MiddlewareExtensions
{
    /// <summary>
    /// Configures the HTTP request pipeline with standard middleware
    /// </summary>
    public static WebApplication UseApplicationMiddleware(this WebApplication app)
    {
        // Configure the HTTP request pipeline based on environment
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        // Authentication must come before Authorization
        app.UseAuthentication();
        app.UseAuthorization();

        // Configure endpoints - default to Home page
        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}")
            .RequireAuthorization();
        
        app.MapRazorPages().RequireAuthorization();

        return app;
    }
}
