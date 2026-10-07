using LoanApp.Application.Contracts;
using LoanApp.Application.Domain;

namespace LoanApp.Application;

public sealed class NoOpLoanAssistant : ILoanAssistant
{
    public Task<string> ReplyAsync(LoanApplication application, string userMessage, CancellationToken ct)
    {
        _ = userMessage;
        return Task.FromResult(
            $"Assistant is not enabled. Application {application.Id} status is {application.Status}.");
    }
}
