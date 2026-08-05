// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License.

namespace Orleans.ShoppingCart.Silo.Services;

public sealed class ProductService : BaseClusterService
{
    private readonly IAuthorizationService _authorizationService;

    public ProductService(
        IHttpContextAccessor httpContextAccessor,
        IClusterClient client,
        IAuthorizationService authorizationService) :
        base(httpContextAccessor, client)
    {
        _authorizationService = authorizationService;
    }

    public async Task CreateOrUpdateProductAsync(
        ProductDetails product,
        ClaimsPrincipal user)
    {
        var authorizationResult = await _authorizationService.AuthorizeAsync(
            user,
            AuthorizationPolicies.ProductManagement);
        if (!authorizationResult.Succeeded)
        {
            throw new UnauthorizedAccessException("Product management authorization is required.");
        }

        await _client.GetGrain<IProductGrain>(product.Id).CreateOrUpdateProductAsync(product);
    }

    public Task<(bool IsAvailable, ProductDetails? ProductDetails)> TryTakeProductAsync(
        string productId, int quantity) =>
        TryUseGrain<IProductGrain, Task<(bool IsAvailable, ProductDetails? ProductDetails)>>(
            products => products.TryTakeProductAsync(quantity),
            productId,
            () => Task.FromResult<(bool IsAvailable, ProductDetails? ProductDetails)>(
                (false, null)));

    public Task ReturnProductAsync(string productId, int quantity) =>
        TryUseGrain<IProductGrain, Task>(
            products => products.ReturnProductAsync(quantity),
            productId,
            () => Task.CompletedTask);

    public Task<int> GetProductAvailability(string productId) =>
        TryUseGrain<IProductGrain, Task<int>>(
            products => products.GetProductAvailabilityAsync(),
            productId,
            () => Task.FromResult(0));
}
