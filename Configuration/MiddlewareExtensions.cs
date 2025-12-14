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

        app.UseAuthorization();

        // Configure endpoints
        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");
        
        app.MapRazorPages();

        return app;
    }
}
