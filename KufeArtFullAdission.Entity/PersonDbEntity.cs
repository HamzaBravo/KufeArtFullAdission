﻿using KufeArtFullAdission.Enums;

namespace KufeArtFullAdission.Entity;

public sealed class PersonDbEntity:BaseDbEntity
{
    public string Username { get; set; }
    public string FullName { get; set; }
    public string Password { get; set; }
    public AccessType AccessType { get; set; }
    public bool IsActive { get; set; }
    public string ProfileImagePath { get; set; } // Profil resmi yolu
    public bool IsDeleted { get; set; } = false;

    public bool? IsSuperAdmin { get; set; } = null;
}
