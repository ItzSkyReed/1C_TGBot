using System.Net.Http.Json;
using System.Text.Json;
using Application.Interfaces;
using Domain.Entities;

namespace Infrastructure.Services;

public class OneCService(HttpClient httpClient) : IOneCService
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    public async Task<List<Component>> GetComponentCategoriesAsync(CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<List<Component>>("component_categories", Options, ct) ?? [];
    }

    public async Task<List<LeftoversEntity>> GetLeftoversByCategoryIdAsync(string categoryId, CancellationToken ct = default)
    {
        var url = $"components/leftovers/{Uri.EscapeDataString(categoryId)}";

        return await httpClient.GetFromJsonAsync<List<LeftoversEntity>>(url, Options, ct) ?? [];
    }

    public async Task<List<Component>> GetSimilarComponentName(string componentName, CancellationToken ct = default)
    {
        var url = $"components/similar?комплектующая={Uri.EscapeDataString(componentName)}";

        using var response = await httpClient.GetAsync(url, ct);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new HttpRequestException($"Комплектующие по запросу '{componentName}' не найдены.", null, response.StatusCode);
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<List<Component>>(Options, ct) ?? [];
    }

    public async Task<List<Supplier>> GetSimilarSupplierName(string supplierName, CancellationToken ct = default)
    {
        var url = $"supplier/similar?поставщик={Uri.EscapeDataString(supplierName)}";

        using var response = await httpClient.GetAsync(url, ct);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new HttpRequestException($"Поставщики по запросу '{supplierName}' не найдены.", null, response.StatusCode);
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<List<Supplier>>(Options, ct) ?? [];
    }

    public async Task<bool> SendClaimAsync(ClaimData data, CancellationToken ct = default)
    {
        var payload = new
        {
            data.ComponentId,
            data.SupplierId,
            data.Description,
            data.PhotoBase64
        };

        var response = await httpClient.PostAsJsonAsync("claim/create", payload, ct);

        response.EnsureSuccessStatusCode();

        return true;
    }

    public async Task<bool> AuthorizeUserAsync(UserAuthDto user, CancellationToken ct)
    {
        var response = await httpClient.PostAsJsonAsync("users/auth", user, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {

            return false;
        }

        response.EnsureSuccessStatusCode();

        return true;
    }
}