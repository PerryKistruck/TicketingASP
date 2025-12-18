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
        // Check for ShowDetailedErrors setting to enable debugging in Azure without changing environment
        var showDetailedErrors = app.Configuration.GetValue<bool>("ShowDetailedErrors", false);
        
        if (app.Environment.IsDevelopment() || showDetailedErrors)
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        // Add security headers
        app.Use(async (context, next) =>
        {
            // Baseline security headers
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Append("X-Frame-Options", "DENY");
            context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
            context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

            // Provide a conservative default CSP when none is set by upstream
            if (!context.Response.Headers.ContainsKey("Content-Security-Policy"))
            {
                context.Response.Headers.Append(
                    "Content-Security-Policy",
                    "default-src 'self'; style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; img-src 'self' data:; font-src 'self' data: https://cdn.jsdelivr.net; connect-src 'self'; frame-ancestors 'none'"
                );
            }

            // Remove identifying headers as late as possible to catch server-added values
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;
                headers.Remove("Server");
                headers.Remove("X-Powered-By");
                headers.Remove("X-AspNet-Version");
                headers.Remove("X-AspNetMvc-Version");
                return Task.CompletedTask;
            });

            await next();
        });

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
