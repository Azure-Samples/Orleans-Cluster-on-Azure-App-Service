// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License.

namespace Orleans.ShoppingCart.Silo.Authorization;

internal static class AuthorizationPolicies
{
    internal const string ProductManagement = nameof(ProductManagement);
    internal const string ProductAdministratorRole = "ProductAdministrator";
}
