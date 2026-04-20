using HBOperations.Application;
using HBOperations.Infrastructure;
using HBOperations.Infrastructure.Data.Seed;
using HBOperations.Web.Components;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services));

    // Add layers
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddCascadingAuthenticationState();

    var app = builder.Build();

    // Seed database
    await AppDbContextSeed.SeedAsync(app.Services);

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        app.UseHsts();
    }

    app.UseSerilogRequestLogging();
    app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
    app.UseHttpsRedirection();

    app.UseAuthentication();
    app.UseAuthorization();

    app.UseAntiforgery();

    // Logout endpoint
    app.MapPost("/logout", async (HttpContext ctx, Microsoft.AspNetCore.Identity.SignInManager<HBOperations.Infrastructure.Identity.ApplicationUser> signInManager) =>
    {
        await signInManager.SignOutAsync();
        ctx.Response.Redirect("/login");
    }).DisableAntiforgery();

    app.MapStaticAssets();
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
