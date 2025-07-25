// KufeArtFullAdission.GarsonMvc/Extensions/UserExtensions.cs
using System.Security.Claims;

namespace KufeArtFullAdission.GarsonMvc.Extensions;

public static class UserExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    public static string GetFullName(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Name)?.Value ?? "Bilinmeyen Kullanıcı";
    }
}