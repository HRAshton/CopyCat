using CopyCat.Application.DependencyInjection;
using CopyCat.Infrastructure.Data;
using CopyCat.Infrastructure.DependencyInjection;
using CopyCat.Telegram.DependencyInjection;
using CopyCat.Worker.Services;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddCopyCatApplication();
builder.Services.AddCopyCatInfrastructure(builder.Configuration);
builder.Services.AddCopyCatTelegram();
builder.Services.AddHostedService<ChannelDiscoveryWorker>();
builder.Services.AddHostedService<TelegramControlOperationWorker>();
builder.Services.AddHostedService<LiveMessageIngestWorker>();
builder.Services.AddHostedService<FilteringWorker>();
builder.Services.AddHostedService<ForwardingWorker>();

IHost host = builder.Build();
using (IServiceScope scope = host.Services.CreateScope())
{
    CopyCatDbContext dbContext = scope.ServiceProvider.GetRequiredService<CopyCatDbContext>();
    await DatabaseMigrator.ApplyMigrationsAsync(dbContext);
}

host.Run();
