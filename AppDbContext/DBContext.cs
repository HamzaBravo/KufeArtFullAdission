using KufeArtFullAdission.Entity;
using Microsoft.EntityFrameworkCore;

namespace AppDbContext;

public class DBContext(DbContextOptions<DBContext> options) : DbContext(options)
{
    public DbSet<AddtionHistoryDbEntity> AddtionHistories { get; set; }
    public DbSet<PersonDbEntity> Persons { get; set; }
    public DbSet<ProductDbEntity> Products { get; set; }
    public DbSet<ProductImagesDbEntity> ProductImages { get; set; }
    public DbSet<TableDbEntity> Tables { get; set; }
    public DbSet<QrMenuVisibleDbEntity> QrMenuVisibles { get; set; }

}
