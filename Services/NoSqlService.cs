using System.Text.Json;
using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using testASP.NoSqlDb;
using testASP.NoSqlDb.Models;
using testASP.Models;

namespace testASP.Services;

/// <summary>
/// Внутренний NoSQL сервис для динамической работы с данными
/// Не доступен через API, используется только внутри кода
/// </summary>
public sealed class NoSqlService
{
    private readonly NoSqlDbContext _context;
    private readonly ILogger<NoSqlService> _logger;
    private readonly ConcurrentDictionary<string, CollectionSchema> _schemaCache = new();

    public NoSqlService(NoSqlDbContext context, ILogger<NoSqlService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Создание новой коллекции
    /// </summary>
    public async Task<bool> CreateCollectionAsync(string name, string displayName, List<FieldDefinition> fields, int createdBy)
    {
        try
        {
            // Проверяем существует ли коллекция
            var existingCollection = await _context.DynamicCollections
                .FirstOrDefaultAsync(c => c.Name == name);

            if (existingCollection != null)
            {
                _logger.LogWarning("Коллекция с именем {Name} уже существует", name);
                return false;
            }

            // Создаем коллекцию
            var collection = new DynamicCollection
            {
                Name = name,
                DisplayName = displayName,
                Schema = JsonSerializer.Serialize(fields, AppJsonContext.Default.ListFieldDefinition),
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsEnabled = true
            };

            _context.DynamicCollections.Add(collection);
            await _context.SaveChangesAsync();

            // Добавляем поля
            foreach (var fieldRequest in fields)
            {
                var field = new DynamicField
                {
                    CollectionId = collection.Id,
                    Name = fieldRequest.Name,
                    DisplayName = fieldRequest.DisplayName ?? fieldRequest.Name,
                    Type = fieldRequest.Type,
                    IsRequired = fieldRequest.IsRequired,
                    IsUnique = fieldRequest.IsUnique,
                    DefaultValue = fieldRequest.DefaultValue,
                    Configuration = JsonSerializer.Serialize(fieldRequest.Configuration ?? new Dictionary<string, object>(), AppJsonContext.Default.DictionaryStringObject),
                    Order = fieldRequest.Order,
                    CreatedAt = DateTime.UtcNow
                };

                _context.DynamicFields.Add(field);
            }

            await _context.SaveChangesAsync();

            // Обновляем кеш схемы
            var schema = new CollectionSchema
            {
                Id = collection.Id,
                Name = collection.Name,
                DisplayName = collection.DisplayName,
                Fields = fields
            };
            _schemaCache.TryAdd(collection.Name, schema);

            _logger.LogInformation("Создана коллекция: {CollectionName} с {FieldCount} полями", collection.Name, fields.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при создании коллекции: {CollectionName}", name);
            return false;
        }
    }

    /// <summary>
    /// Добавление документа в коллекцию
    /// </summary>
    public async Task<int?> AddDocumentAsync(string collectionName, Dictionary<string, object> data, int createdBy)
    {
        try
        {
            var schema = await GetCollectionSchemaAsync(collectionName);
            if (schema == null)
            {
                _logger.LogWarning("Коллекция не найдена: {CollectionName}", collectionName);
                return null;
            }

            // Валидация данных
            var validationResult = ValidateDocumentData(data, schema);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Ошибка валидации данных для коллекции {CollectionName}: {Errors}", collectionName, validationResult.ErrorMessage);
                return null;
            }

            // Добавляем системные поля
            var enrichedData = new Dictionary<string, object>(data)
            {
                ["CreatedAt"] = DateTime.UtcNow,
                ["CreatedBy"] = createdBy
            };

            var document = new DynamicDocument
            {
                CollectionId = schema.Id,
                Data = JsonSerializer.Serialize(enrichedData, AppJsonContext.Default.DictionaryStringObject),
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsEnabled = true
            };

            _context.DynamicDocuments.Add(document);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Добавлен документ в коллекцию {CollectionName}, ID: {DocumentId}", collectionName, document.Id);
            return document.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при добавлении документа в коллекцию: {CollectionName}", collectionName);
            return null;
        }
    }

    /// <summary>
    /// Получение документов из коллекции
    /// </summary>
    public async Task<List<Dictionary<string, object>>> GetDocumentsAsync(string collectionName, int limit = 100)
    {
        try
        {
            var schema = await GetCollectionSchemaAsync(collectionName);
            if (schema == null)
            {
                return new List<Dictionary<string, object>>();
            }

            var documents = await _context.DynamicDocuments
                .Where(d => d.CollectionId == schema.Id && d.IsEnabled)
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

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении документов из коллекции: {CollectionName}", collectionName);
            return new List<Dictionary<string, object>>();
        }
    }

    /// <summary>
    /// Поиск документов в коллекции
    /// </summary>
    public async Task<List<Dictionary<string, object>>> SearchDocumentsAsync(
        string collectionName, 
        Dictionary<string, object> filters, 
        int limit = 100)
    {
        try
        {
            var schema = await GetCollectionSchemaAsync(collectionName);
            if (schema == null)
            {
                return new List<Dictionary<string, object>>();
            }

            var documents = await _context.DynamicDocuments
                .Where(d => d.CollectionId == schema.Id && d.IsEnabled)
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
    /// Получение схемы коллекции
    /// </summary>
    private async Task<CollectionSchema?> GetCollectionSchemaAsync(string collectionName)
    {
        // Проверяем кеш
        if (_schemaCache.TryGetValue(collectionName, out var cachedSchema))
        {
            return cachedSchema;
        }

        try
        {
            var collection = await _context.DynamicCollections
                .Include(c => c.Fields)
                .FirstOrDefaultAsync(c => c.Name == collectionName && c.IsEnabled);

            if (collection == null)
            {
                return null;
            }

            var fields = collection.Fields
                .OrderBy(f => f.Order)
                .Select(f => new FieldDefinition
                {
                    Name = f.Name,
                    DisplayName = f.DisplayName,
                    Type = f.Type,
                    IsRequired = f.IsRequired,
                    IsUnique = f.IsUnique,
                    DefaultValue = f.DefaultValue,
                    Configuration = JsonSerializer.Deserialize<Dictionary<string, object>>(f.Configuration) ?? new Dictionary<string, object>(),
                    Order = f.Order
                })
                .ToList();

            var schema = new CollectionSchema
            {
                Id = collection.Id,
                Name = collection.Name,
                DisplayName = collection.DisplayName,
                Fields = fields
            };

            // Сохраняем в кеш
            _schemaCache.TryAdd(collection.Name, schema);

            return schema;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении схемы коллекции: {CollectionName}", collectionName);
            return null;
        }
    }

    /// <summary>
    /// Валидация данных документа
    /// </summary>
    private static FieldValidationResult ValidateDocumentData(Dictionary<string, object> data, CollectionSchema schema)
    {
        foreach (var field in schema.Fields.Where(f => f.IsRequired))
        {
            if (!data.ContainsKey(field.Name) || data[field.Name] == null)
            {
                return new FieldValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"Обязательное поле '{field.DisplayName}' отсутствует или пусто"
                };
            }
        }

        return new FieldValidationResult { IsValid = true };
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
    /// Инициализация системных коллекций (только если они не существуют)
    /// </summary>
    public async Task InitializeSystemCollectionsAsync()
    {
        try
        {
            var systemCollections = new[]
            {
                ("device_logs", "Логи устройств", new List<FieldDefinition>
                {
                    new() { Name = "deviceId", DisplayName = "ID устройства", Type = "string", IsRequired = true, Order = 1 },
                    new() { Name = "action", DisplayName = "Действие", Type = "string", IsRequired = true, Order = 2 },
                    new() { Name = "parameters", DisplayName = "Параметры", Type = "json", IsRequired = false, Order = 3 },
                    new() { Name = "timestamp", DisplayName = "Время", Type = "datetime", IsRequired = true, Order = 4 }
                }),
                ("security_events", "События безопасности", new List<FieldDefinition>
                {
                    new() { Name = "eventType", DisplayName = "Тип события", Type = "string", IsRequired = true, Order = 1 },
                    new() { Name = "ipAddress", DisplayName = "IP адрес", Type = "string", IsRequired = true, Order = 2 },
                    new() { Name = "userAgent", DisplayName = "User Agent", Type = "string", IsRequired = false, Order = 3 },
                    new() { Name = "details", DisplayName = "Детали", Type = "json", IsRequired = false, Order = 4 },
                    new() { Name = "timestamp", DisplayName = "Время", Type = "datetime", IsRequired = true, Order = 5 }
                })
            };

            var existingCollections = await _context.DynamicCollections
                .Select(c => c.Name)
                .ToListAsync();

            var createdCount = 0;
            foreach (var (name, displayName, fields) in systemCollections)
            {
                if (!existingCollections.Contains(name))
                {
                    _logger.LogInformation("Создание системной коллекции: {Name}", name);
                    await CreateCollectionAsync(name, displayName, fields, 0);
                    createdCount++;
                }
                else
                {
                    _logger.LogDebug("Системная коллекция уже существует: {Name}", name);
                }
            }

            if (createdCount > 0)
            {
                _logger.LogInformation("Создано {Count} новых системных коллекций", createdCount);
            }
            else
            {
                _logger.LogInformation("Все системные коллекции уже существуют");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при инициализации системных коллекций");
            throw;
        }
    }
}

/// <summary>
/// Схема коллекции
/// </summary>
internal sealed class CollectionSchema
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public List<FieldDefinition> Fields { get; set; } = new();
}

/// <summary>
/// Результат валидации полей
/// </summary>
internal sealed class FieldValidationResult
{
    public bool IsValid { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}
