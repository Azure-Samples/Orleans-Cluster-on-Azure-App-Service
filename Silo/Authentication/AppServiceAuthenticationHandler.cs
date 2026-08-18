// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License.

using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Orleans.ShoppingCart.Silo.Authentication;

internal sealed class AppServiceAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    private const string ClientPrincipalHeader = "X-MS-CLIENT-PRINCIPAL";
    private const string MicrosoftEntraIdentityProvider = "aad";
    private const int MaximumHeaderLength = 16 * 1024;

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ClientPrincipalHeader, out var values))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (values.Count != 1 || string.IsNullOrWhiteSpace(values[0]) ||
            values[0]!.Length > MaximumHeaderLength)
        {
            return Task.FromResult(AuthenticateResult.Fail("The App Service principal header is invalid."));
        }

        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(values[0]!));
            var principal = JsonSerializer.Deserialize<ClientPrincipal>(json);

            if (principal is null ||
                !string.Equals(
                    principal.IdentityProvider,
                    MicrosoftEntraIdentityProvider,
                    StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(principal.NameClaimType) ||
                string.IsNullOrWhiteSpace(principal.RoleClaimType) ||
                principal.Claims is null)
            {
                return Task.FromResult(
                    AuthenticateResult.Fail("The App Service principal is incomplete."));
            }

            var claims = principal.Claims
                .Where(claim => !string.IsNullOrWhiteSpace(claim.Type) &&
                                !string.IsNullOrWhiteSpace(claim.Value))
                .Select(claim => new Claim(claim.Type!, claim.Value!));
            var identity = new ClaimsIdentity(
                claims,
                Scheme.Name,
                principal.NameClaimType,
                principal.RoleClaimType);

            return Task.FromResult(
                AuthenticateResult.Success(
                    new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
        }
        catch (FormatException)
        {
            return Task.FromResult(
                AuthenticateResult.Fail("The App Service principal header is not valid Base64."));
        }
        catch (JsonException)
        {
            return Task.FromResult(
                AuthenticateResult.Fail("The App Service principal header is not valid JSON."));
        }
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        var returnUrl = $"{Request.PathBase}{Request.Path}{Request.QueryString}";
        Response.Redirect(
            $"/.auth/login/aad?post_login_redirect_uri={Uri.EscapeDataString(returnUrl)}");
        return Task.CompletedTask;
    }

    private sealed class ClientPrincipal
    {
        [JsonPropertyName("auth_typ")]
        public string? IdentityProvider { get; init; }

        [JsonPropertyName("name_typ")]
        public string? NameClaimType { get; init; }

        [JsonPropertyName("role_typ")]
        public string? RoleClaimType { get; init; }

        [JsonPropertyName("claims")]
        public IReadOnlyList<ClientPrincipalClaim>? Claims { get; init; }
    }

    private sealed class ClientPrincipalClaim
    {
        [JsonPropertyName("typ")]
        public string? Type { get; init; }

        [JsonPropertyName("val")]
        public string? Value { get; init; }
    }
}
