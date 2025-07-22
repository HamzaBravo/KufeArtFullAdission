using System;
using System.Security.Claims;

namespace KufeArtFullAdission.Mvc.Extensions;

public static class UserExtensions
{
    /// <summary>
    /// Giriş yapmış kullanıcının ID'sini alır
    /// </summary>
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    /// <summary>
    /// Giriş yapmış kullanıcının tam adını alır
    /// </summary>
    public static string GetFullName(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Name)?.Value ?? "Bilinmeyen Kullanıcı";
    }

    /// <summary>
    /// Giriş yapmış kullanıcının kullanıcı adını alır
    /// </summary>
    public static string GetUsername(this ClaimsPrincipal user)
    {
        return user.FindFirst("Username")?.Value ?? "";
    }

    /// <summary>
    /// Giriş yapmış kullanıcının yetkisini alır
    /// </summary>
    public static string GetRole(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Role)?.Value ?? "";
    }

    /// <summary>
    /// Kullanıcının admin olup olmadığını kontrol eder
    /// </summary>
    public static bool IsAdmin(this ClaimsPrincipal user)
    {
        return user.GetRole() == "Admin";
    }

    /// <summary>
    /// Giriş yapmış kullanıcının profil resmini alır
    /// </summary>
    public static string GetProfileImage(this ClaimsPrincipal user)
    {
        return user.FindFirst("ProfileImage")?.Value ?? "";
    }
}