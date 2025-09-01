using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Starter.Api.Auth;

public class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private const string Prefix = "Perm:";

    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallback = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
        => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
        => _fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
        {
            var perm = policyName.Substring(Prefix.Length);

            // >>> Troque para "perm" (claim unitário por permissão)
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .RequireClaim("perm", perm)
                .Build();

            return Task.FromResult(policy);
        }
        return _fallback.GetPolicyAsync(policyName);
    }
}
