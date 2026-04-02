using Domain.Entities;

namespace Application.Interfaces;

public interface IOneCService
{
    public Task<List<Component>> GetComponentCategoriesAsync(CancellationToken ct);
    public Task<List<LeftoversEntity>> GetLeftoversByCategoryIdAsync(string categoryId, CancellationToken ct);
    public Task<List<Component>> GetSimilarComponentName(string componentName, CancellationToken ct);
    public Task<List<Supplier>> GetSimilarSupplierName(string supplierName, CancellationToken ct);
    public Task<bool> SendClaimAsync(ClaimData data, CancellationToken ct);
    Task<bool> AuthorizeUserAsync(UserAuthDto user, CancellationToken ct);

}