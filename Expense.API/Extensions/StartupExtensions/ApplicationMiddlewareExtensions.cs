
using Expense.API.Middlewares;
using Expense.Infrastructure.Notifications;
using Serilog;
using Serilog.Events;

namespace Expense.API.Extensions.StartupExtensions;

public static class ApplicationMiddlewareExtensions
{
    /// <summary>
    /// Configures the HTTP request pipeline for the Expense.API application.
    /// </summary>
    /// <param name="app">The web application.</param>
    public static WebApplication ConfigureApplicationMiddleware(this WebApplication app)
    {
        app.UseRequestLocalization();
        app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

        app.UseSerilogRequestLogging(options =>
        {
            options.GetLevel = (httpContext, _, exception) =>
            {
                if (exception != null)
                {
                    return LogEventLevel.Error;
                }

                return httpContext.Response.StatusCode >= 500
                    ? LogEventLevel.Error
                    : LogEventLevel.Information;
            };

            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("TraceId", httpContext.TraceIdentifier);
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                diagnosticContext.Set("UserId", httpContext.User?.Identity?.IsAuthenticated == true
                    ? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                    : null);
            };
        });

        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Expense.API v1");
            c.EnablePersistAuthorization();
            c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
            c.EnableFilter();
            c.DisplayRequestDuration();
            c.EnableDeepLinking();
            c.DisplayOperationId();
            c.EnableTryItOutByDefault();
            c.DocumentTitle = "Expense.API";
            c.DefaultModelExpandDepth(1);
        });

        app.UseCors("FrontendPolicy");
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseAuthentication();
        app.UseWebSockets();
        app.UseMiddleware<WebSocketMiddleware>();
        app.UseAuthorization();

        app.MapControllers();
        return app;
    }
}
