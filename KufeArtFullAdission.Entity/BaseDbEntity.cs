﻿namespace KufeArtFullAdission.Entity;

public abstract class BaseDbEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
