using LoanApp.Application.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace LoanApp.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddLoanApplicationCore(this IServiceCollection services)
    {
        services.AddSingleton<ILoanCatalog, LoanCatalog>();
        services.AddScoped<LoanApplicationService>();
        services.AddScoped<IVerificationEngine, RulesVerificationEngine>();
        services.AddSingleton<ILoanAssistant, NoOpLoanAssistant>();
        services.AddScoped<ApplicationProcessingPipeline>();
        return services;
    }
}
