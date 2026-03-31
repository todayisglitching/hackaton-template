using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using testASP.Models;
using testASP.NoSqlDb.Models;

namespace testASP.NoSqlDb;

/// <summary>
/// Скомпилированная модель для EF Core AOT
/// </summary>
public sealed class NoSqlDbContextModelBuilder : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime)
    {
        // Создаем модель для runtime (не design time)
        if (!designTime)
        {
            var builder = new ModelBuilder(new ConventionSet());

            // Конфигурация пользователей
            builder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.PasswordHash).IsRequired();
            });

            // Конфигурация устройств
            builder.Entity<Device>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.DeviceId).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Type).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Status).HasMaxLength(50);
                entity.Property(e => e.Properties).IsRequired();
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.Location).HasMaxLength(255);
                entity.Property(e => e.Manufacturer).HasMaxLength(255);
                entity.Property(e => e.Model).HasMaxLength(255);
                entity.Property(e => e.FirmwareVersion).HasMaxLength(100);
            });

            // Конфигурация динамических коллекций
            builder.Entity<DynamicCollection>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Schema).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.HasIndex(e => e.Name).IsUnique();
            });

            builder.Entity<DynamicField>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Configuration).IsRequired();
                entity.Property(e => e.DefaultValue).HasMaxLength(500);
                entity.HasOne(e => e.Collection)
                      .WithMany(c => c.Fields)
                      .HasForeignKey(e => e.CollectionId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => new { e.CollectionId, e.Name }).IsUnique();
            });

            builder.Entity<DynamicDocument>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Data).IsRequired();
                entity.HasOne(e => e.Collection)
                      .WithMany(c => c.Documents)
                      .HasForeignKey(e => e.CollectionId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => e.CollectionId);
            });

            return builder.Model;
        }

        // Для design time возвращаем null (будет использоваться стандартный механизм)
        return null!;
    }
}
