using Ecommerce.Infrastructure.CloudImageStorage;
using Ecommerce.Infrastructure.Payment;
using Ecommerce.Infrastructure.Persistence.Identity;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Ecommerce.Api.IntegrationTests.Startup;

internal class EcommerceProgram : WebApplicationFactory<Program>
{
    public EcommerceDbContext CreateApplicationDbContext()
    {
        var db = Services.GetRequiredService<IDbContextFactory<EcommerceDbContext>>().CreateDbContext();

        db.Database.EnsureCreated();

        return db;
    }
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(configurationBuilder =>
        {
            var integrationConfig = new ConfigurationBuilder()
                .AddJsonFile("appsettings.test.json")
                .AddEnvironmentVariables()
                .Build();

            configurationBuilder.AddConfiguration(integrationConfig);
        });

        builder.UseEnvironment("testing");

        builder.ConfigureServices((builder, services) => {
            services.Remove<DbContextOptions<EcommerceDbContext>>()
                    .AddDbContextFactory<EcommerceDbContext>((_, options) => {
                        options.UseSqlServer(builder.Configuration.GetConnectionString("TestConnection"),
                        builder => builder.MigrationsAssembly(typeof(Ecommerce.Infrastructure.AssemblyReference).Assembly.FullName));
                    });

            services.Remove<IStripeService>()
                    .AddScoped<IStripeService, StripeServiceMock>();

            services.Remove<ICloudinaryService>()
                    .AddScoped<ICloudinaryService, CloudinaryServiceMock>();
        });

        return base.CreateHost(builder);
    }
}

file static class ServiceCollectionExtensions
{
    public static IServiceCollection Remove<TService>(this IServiceCollection service)
    {
        var serviceDescriptor = service.FirstOrDefault(d => d.ServiceType == typeof(TService));

        if (serviceDescriptor is not null)
        {
            service.Remove(serviceDescriptor);
        }

        return service;
    }
}