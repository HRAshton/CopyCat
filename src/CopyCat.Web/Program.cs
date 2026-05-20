using CopyCat.Application.DependencyInjection;
using CopyCat.Infrastructure.Data;
using CopyCat.Infrastructure.DependencyInjection;
using CopyCat.Telegram.DependencyInjection;
using CopyCat.Web.Components;
using CopyCat.Web.Services;

using MudBlazor.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddMudServices();
builder.Services.AddCopyCatApplication();
builder.Services.AddCopyCatInfrastructure(builder.Configuration);
builder.Services.AddCopyCatTelegram();
builder.Services.AddSingleton<ForwardingJobUpdateNotifier>();
builder.Services.AddHostedService<ForwardingJobNotificationListenerService>();

WebApplication app = builder.Build();

using (IServiceScope scope = app.Services.CreateScope())
{
    CopyCatDbContext dbContext = scope.ServiceProvider.GetRequiredService<CopyCatDbContext>();
    await DatabaseMigrator.ApplyMigrationsAsync(dbContext);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found");
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
