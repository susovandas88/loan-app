using LoanApp.Application.Contracts;
using LoanApp.Application.Domain;

namespace LoanApp.Application;

public sealed class LoanCatalog : ILoanCatalog
{
    private static readonly IReadOnlyList<LoanProductDto> Products =
    [
        new(LoanProductCode.Personal, "Personal loan",
            [DocumentType.IdProof, DocumentType.AddressProof, DocumentType.IncomeProof]),
        new(LoanProductCode.Home, "Home loan",
            [DocumentType.IdProof, DocumentType.AddressProof, DocumentType.IncomeProof, DocumentType.BankStatement]),
        new(LoanProductCode.Auto, "Auto loan",
            [DocumentType.IdProof, DocumentType.AddressProof, DocumentType.IncomeProof])
    ];

    public IReadOnlyList<LoanProductDto> GetProducts() => Products;

    public LoanProductDto GetRequired(LoanProductCode code) =>
        Products.FirstOrDefault(p => p.Code == code)
        ?? throw new AppException("invalid_product", "Unknown loan product.");
}
