using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Routing;

namespace LeaveManagement.Api.Infrastructure;

/// <summary>
/// Transforms route token values from PascalCase to kebab-case so that
/// controllers such as <c>LeaveRequestsController</c> are reachable at
/// <c>/api/leave-requests</c> instead of <c>/api/LeaveRequests</c>.
/// </summary>
public class SlugifyParameterTransformer : IOutboundParameterTransformer
{
    public string? TransformOutbound(object? value)
    {
        if (value is null)
        {
            return null;
        }

        return Regex.Replace(
            value.ToString()!,
            "([a-z0-9])([A-Z])",
            "$1-$2",
            RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100)).ToLowerInvariant();
    }
}
