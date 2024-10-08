﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License.

namespace Orleans.ShoppingCart.Abstractions;

[GenerateSerializer]
public sealed record class ProductDetails
{
    [Id(0)]
    public string? Id { get; set; }
    [Id(1)]
    public string? Name { get; set; }
    [Id(2)]
    public string? Description { get; set; }
    [Id(3)]
    public ProductCategory Category { get; set; }
    [Id(4)]
    public int Quantity { get; set; }
    [Id(5)]
    public decimal UnitPrice { get; set; }
    [Id(6)]
    public string? DetailsUrl { get; set; }
    [Id(7)]
    public string? ImageUrl { get; set; }

    [JsonIgnore]
    public decimal TotalPrice =>
        Math.Round(Quantity * UnitPrice, 2);
}
