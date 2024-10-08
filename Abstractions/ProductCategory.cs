﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License.

namespace Orleans.ShoppingCart.Abstractions;

[GenerateSerializer]
public enum ProductCategory
{
    Accessories,
    Hardware,
    Software,
    Books,
    Movies,
    Music,
    Games,
    Other
}
