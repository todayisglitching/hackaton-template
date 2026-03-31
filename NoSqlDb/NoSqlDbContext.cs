using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using testASP.Models;
using testASP.NoSqlDb.Models;
using System.Text.Json;

namespace testASP.NoSqlDb;

/// <summary>
/// NoSQL Database Context для динамической работы с данными
/// Поддерживает создание коллекций на лету и динамические схемы
/// </summary>
public sealed class NoSqlDbContext : DbContext
{
    private readonly ILogger<NoSqlDbContext> _logger;
    
    public NoSqlDbContext(DbContextOptions<NoSqlDbContext> options, ILogger<NoSqlDbContext> logger) 
        : base(options)
    {
        _logger = logger;
    }

    // Статические таблицы для пользователей и устройств
    public DbSet<User> Users { get; set; }
    public DbSet<Device> Devices { get; set; }
    
    // Динамические коллекции для NoSQL функциональности
    public DbSet<DynamicCollection> DynamicCollections { get; set; }
    public DbSet<DynamicDocument> DynamicDocuments { get; set; }
    public DbSet<DynamicField> DynamicFields { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Конфигурация пользователей
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.PasswordHash).IsRequired();
        });

        // Конфигурация устройств
        modelBuilder.Entity<Device>(entity =>
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
        modelBuilder.Entity<DynamicCollection>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Schema).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<DynamicField>(entity =>
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

        modelBuilder.Entity<DynamicDocument>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Data).IsRequired();
            entity.HasOne(e => e.Collection)
                  .WithMany(c => c.Documents)
                  .HasForeignKey(e => e.CollectionId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.CollectionId);
        });

        _logger.LogInformation("NoSQL Database Context сконфигурирован");
    }

    /// <summary>
    /// Получение документов из динамической коллекции
    /// </summary>
    public async Task<List<Dictionary<string, object>>> GetDynamicDocumentsAsync(string collectionName, int limit = 100)
    {
        try
        {
            var collection = await DynamicCollections
                .Include(c => c.Fields)
                .FirstOrDefaultAsync(c => c.Name == collectionName);

            if (collection == null)
            {
                return new List<Dictionary<string, object>>();
            }

            var documents = await DynamicDocuments
                .Where(d => d.CollectionId == collection.Id && d.IsEnabled)
                .OrderByDescending(d => d.CreatedAt)
                .Take(limit)
                .ToListAsync();

            var result = new List<Dictionary<string, object>>();

            foreach (var document in documents)
            {
                var data = JsonSerializer.Deserialize(document.Data, AppJsonContext.Default.DictionaryStringObject);
                if (data != null)
                {
                    data["Id"] = document.Id;
                    data["CreatedAt"] = document.CreatedAt;
                    data["UpdatedAt"] = document.UpdatedAt;
                    result.Add(data);
                }
            }

            _logger.LogDebug("Получено {Count} документов из коллекции {CollectionName}", result.Count, collectionName);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении документов из коллекции: {CollectionName}", collectionName);
            return new List<Dictionary<string, object>>();
        }
    }

    /// <summary>
    /// Поиск документов в динамической коллекции
    /// </summary>
    public async Task<List<Dictionary<string, object>>> SearchDynamicDocumentsAsync(
        string collectionName, 
        Dictionary<string, object> filters, 
        int limit = 100)
    {
        try
        {
            var collection = await DynamicCollections
                .Include(c => c.Fields)
                .FirstOrDefaultAsync(c => c.Name == collectionName);

            if (collection == null)
            {
                return new List<Dictionary<string, object>>();
            }

            var documents = await DynamicDocuments
                .Where(d => d.CollectionId == collection.Id && d.IsEnabled)
                .OrderByDescending(d => d.CreatedAt)
                .Take(limit)
                .ToListAsync();

            var result = new List<Dictionary<string, object>>();

            foreach (var document in documents)
            {
                var data = JsonSerializer.Deserialize(document.Data, AppJsonContext.Default.DictionaryStringObject);
                if (data != null && MatchesFilters(data, filters))
                {
                    data["Id"] = document.Id;
                    data["CreatedAt"] = document.CreatedAt;
                    data["UpdatedAt"] = document.UpdatedAt;
                    result.Add(data);
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при поиске документов в коллекции: {CollectionName}", collectionName);
            return new List<Dictionary<string, object>>();
        }
    }

    /// <summary>
    /// Добавление документа в динамическую коллекцию
    /// </summary>
    public async Task<bool> AddDynamicDocumentAsync(string collectionName, Dictionary<string, object> data, int createdBy)
    {
        try
        {
            var collection = await DynamicCollections
                .FirstOrDefaultAsync(c => c.Name == collectionName);

            if (collection == null)
            {
                _logger.LogWarning("Коллекция не найдена: {CollectionName}", collectionName);
                return false;
            }

            // Добавляем системные поля
            var enrichedData = new Dictionary<string, object>(data)
            {
                ["CreatedAt"] = DateTime.UtcNow,
                ["CreatedBy"] = createdBy
            };

            var document = new DynamicDocument
            {
                CollectionId = collection.Id,
                Data = JsonSerializer.Serialize(enrichedData, AppJsonContext.Default.DictionaryStringObject),
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsEnabled = true
            };

            DynamicDocuments.Add(document);
            await SaveChangesAsync();

            _logger.LogInformation("Добавлен документ в коллекцию {CollectionName}, ID: {DocumentId}", collectionName, document.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при добавлении документа в коллекцию: {CollectionName}", collectionName);
            return false;
        }
    }

    /// <summary>
    /// Проверка соответствует ли документ фильтрам
    /// </summary>
    private static bool MatchesFilters(Dictionary<string, object> data, Dictionary<string, object> filters)
    {
        foreach (var filter in filters)
        {
            if (data.TryGetValue(filter.Key, out var value))
            {
                var valueStr = value?.ToString()?.ToLower();
                var filterStr = filter.Value?.ToString()?.ToLower();

                if (valueStr == null || filterStr == null || !valueStr.Contains(filterStr))
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Получение списка коллекций
    /// </summary>
    public async Task<List<DynamicCollection>> GetCollectionsAsync()
    {
        try
        {
            return await DynamicCollections
                .Where(c => c.IsEnabled)
                .OrderBy(c => c.DisplayName)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении списка коллекций");
            return new List<DynamicCollection>();
        }
    }

    /// <summary>
    /// Создание динамической коллекции
    /// </summary>
    public async Task<bool> CreateDynamicCollectionAsync(string name, string displayName, List<DynamicField> fields, int createdBy)
    {
        try
        {
            var collection = new DynamicCollection
            {
                Name = name,
                DisplayName = displayName,
                Schema = JsonSerializer.Serialize(fields, AppJsonContext.Default.ListDynamicField),
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsEnabled = true
            };

            DynamicCollections.Add(collection);
            await SaveChangesAsync();

            // Добавляем поля
            foreach (var field in fields)
            {
                field.CollectionId = collection.Id;
                DynamicFields.Add(field);
            }
            await SaveChangesAsync();

            _logger.LogInformation("Создана динамическая коллекция: {CollectionName}", name);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при создании динамической коллекции: {CollectionName}", name);
            return false;
        }
    }
}
