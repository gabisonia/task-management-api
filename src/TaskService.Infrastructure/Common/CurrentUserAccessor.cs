using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TaskService.Application.Abstractions;

namespace TaskService.Infrastructure.Common;

public sealed class CurrentUserAccessor(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public string Id => Principal?.FindFirst("sub")?.Value
                         ?? Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? Principal?.FindFirst("user_id")?.Value
                         ?? string.Empty;

    public string? Email => Principal?.FindFirst("email")?.Value;

    public string? DisplayName
    {
        get
        {
            var name = Principal?.FindFirst("name")?.Value ?? Principal?.FindFirst("display_name")?.Value;
            if (!string.IsNullOrEmpty(name)) return name;

            var userMetadataJson = Principal?.FindFirst("user_metadata")?.Value;
            if (!string.IsNullOrEmpty(userMetadataJson))
            {
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(userMetadataJson);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("display_name", out var dn) && dn.ValueKind == System.Text.Json.JsonValueKind.String)
                        return dn.GetString();
                    if (root.TryGetProperty("full_name", out var fn) && fn.ValueKind == System.Text.Json.JsonValueKind.String)
                        return fn.GetString();
                    if (root.TryGetProperty("name", out var n) && n.ValueKind == System.Text.Json.JsonValueKind.String)
                        return n.GetString();
                }
                catch { }
            }
            return null;
        }
    }

    public bool EmailVerified
    {
        get
        {
            if (Principal?.FindFirst("email_verified")?.Value == "true") return true;
            var userMetadataJson = Principal?.FindFirst("user_metadata")?.Value;
            if (!string.IsNullOrEmpty(userMetadataJson))
            {
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(userMetadataJson);
                    if (doc.RootElement.TryGetProperty("email_verified", out var ev) && ev.ValueKind == System.Text.Json.JsonValueKind.True)
                        return true;
                }
                catch { }
            }
            return false;
        }
    }

    public string[] Roles
    {
        get
        {
            var role = Principal?.FindFirst("role")?.Value;
            return role != null ? new[] { role } : Array.Empty<string>();
        }
    }
}

