
using Expense.API.Middlewares;

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

        app.UseCors();
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

        app.MapControllers();
        return app;
    }
}
