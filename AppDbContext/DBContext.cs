﻿using KufeArtFullAdission.Entity;
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
    public DbSet<CampaignDbEntity> Campaigns { get; set; }
    public DbSet<CustomerPurchaseHistoryDbEntity> CustomerPurchaseHistories { get; set; }
    public DbSet<CampaignProgressDbEntity> CampaignProgresses { get; set; }
    public DbSet<PaymentDbEntity> Payments { get; set; }
    public DbSet<KufePointTransactionDbEntity> KufePointTransactions { get; set; }
    public DbSet<CustomerPointsDbEntity> CustomerPoints { get; set; }
    public DbSet<CustomerDbEntity> Customers { get; set; }
    public DbSet<OrderBatchStatusDbEntity> OrderBatchStatuses { get; set; }
    public DbSet<PaymentItemDbEntity> PaymentItems { get; set; }
}
