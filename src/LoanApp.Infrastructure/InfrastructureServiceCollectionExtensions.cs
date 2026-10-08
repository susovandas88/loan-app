using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using LoanApp.Application;
using LoanApp.Application.Contracts;
using LoanApp.Infrastructure.Banking;
using LoanApp.Infrastructure.Intelligence;
using LoanApp.Infrastructure.Messaging;
using LoanApp.Infrastructure.Persistence;
using LoanApp.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LoanApp.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddLoanInfrastructure(this IServiceCollection services, IConfiguration configuration, bool enableInProcessWorker)
    {
        var provider = configuration["Database:Provider"] ?? "Sqlite";
        var connectionString = configuration.GetConnectionString("LoanDb") ?? "Data Source=loanapp.db";

        services.AddDbContext<LoanDbContext>(options =>
        {
            if (string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                options.UseSqlServer(connectionString);
            }
            else
            {
                options.UseSqlite(connectionString);
            }
        });

        services.AddScoped<IApplicationRepository, ApplicationRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IFeatureFlags>(new FeatureFlags
        {
            AiAssistant = configuration.GetValue("FeatureFlags:AiAssistant", false),
            AiVerificationAssist = configuration.GetValue("FeatureFlags:AiVerificationAssist", false)
        });
        services.AddSingleton<IMalwareScan, FileNameMalwareScan>();
        services.AddSingleton<IDocumentIntelligenceClient, StubDocumentIntelligenceClient>();

        var blobMode = configuration["Storage:Mode"] ?? "Local";
        if (string.Equals(blobMode, "Azure", StringComparison.OrdinalIgnoreCase))
        {
            var blobUri = configuration["Storage:BlobServiceUri"];
            BlobServiceClient client;
            if (!string.IsNullOrWhiteSpace(blobUri))
            {
                client = new BlobServiceClient(new Uri(blobUri), new DefaultAzureCredential());
            }
            else if (!string.IsNullOrWhiteSpace(configuration["Storage:ConnectionString"]))
            {
                client = new BlobServiceClient(configuration["Storage:ConnectionString"]);
            }
            else
            {
                throw new InvalidOperationException("Configure Storage:BlobServiceUri or Storage:ConnectionString.");
            }

            services.AddSingleton(client);
            services.AddSingleton<IBlobStorageService>(sp =>
                new AzureBlobStorageService(sp.GetRequiredService<BlobServiceClient>(), configuration["Storage:Container"] ?? "loan-documents"));
        }
        else
        {
            services.AddSingleton<LocalUploadTicketStore>();
            services.AddSingleton<IBlobStorageService>(sp =>
                new LocalBlobStorageService(
                    configuration["Storage:LocalRoot"] ?? Path.Combine(AppContext.BaseDirectory, "blobs"),
                    configuration["PublicApiBaseUrl"] ?? "http://localhost:5088",
                    sp.GetRequiredService<LocalUploadTicketStore>(),
                    sp.GetRequiredService<IClock>()));
        }

        var busMode = configuration["ServiceBus:Mode"] ?? "InMemory";
        if (string.Equals(busMode, "Azure", StringComparison.OrdinalIgnoreCase))
        {
            var namespaceFqdn = configuration["ServiceBus:FullyQualifiedNamespace"];
            ServiceBusClient sbClient = !string.IsNullOrWhiteSpace(namespaceFqdn)
                ? new ServiceBusClient(namespaceFqdn, new DefaultAzureCredential())
                : new ServiceBusClient(configuration["ServiceBus:ConnectionString"] ?? throw new InvalidOperationException("Service Bus connection is missing."));
            services.AddSingleton(sbClient);
            services.AddSingleton<IMessageBus>(sp =>
                new AzureServiceBusMessageBus(sp.GetRequiredService<ServiceBusClient>(), configuration["ServiceBus:Queue"] ?? "loan-applications"));
        }
        else
        {
            services.AddSingleton<InMemoryMessageBus>();
            services.AddSingleton<IMessageBus>(sp => sp.GetRequiredService<InMemoryMessageBus>());
            if (enableInProcessWorker)
            {
                services.AddHostedService<InProcessApplicationWorker>();
            }
        }

        var bankBase = configuration["BankCore:BaseUrl"] ?? "http://localhost:5090/";
        services.AddHttpClient<IBankCoreClient, HttpBankCoreClient>(client =>
        {
            client.BaseAddress = new Uri(bankBase);
            var apiKey = configuration["BankCore:ApiKey"];
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation("X-Api-Key", apiKey);
            }
        });

        services.AddLoanApplicationCore();
        return services;
    }
}
