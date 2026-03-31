# NoSQL Database System Documentation

## Overview

Smart Home API использует кастомную NoSQL систему построенную на Entity Framework Core с SQLite. Система поддерживает динамические коллекции, гибкие схемы данных и оптимизирована для IoT сценариев.

## Architecture

### Core Components

```
NoSqlDb/
├── Models/
│   ├── DynamicCollection.cs    # Метаданные коллекции
│   ├── DynamicDocument.cs     # Документ коллекции
│   ├── DynamicField.cs        # Поля коллекции
│   └── NoSqlModels.cs         # Дополнительные модели
├── NoSqlDbContext.cs          # Entity Framework контекст
├── NoSqlDbContextModelBuilder.cs  # Конфигурация моделей
└── NoSqlService.cs            # Business логика
```

### Database Schema

```sql
-- Коллекции (метаданные)
CREATE TABLE "DynamicCollections" (
    "Id" INTEGER PRIMARY KEY,
    "Name" TEXT NOT NULL UNIQUE,
    "DisplayName" TEXT NOT NULL,
    "Description" TEXT,
    "Schema" TEXT NOT NULL,          -- JSON schema
    "IsEnabled" INTEGER NOT NULL,
    "CreatedBy" INTEGER NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NOT NULL
);

-- Поля коллекций
CREATE TABLE "DynamicFields" (
    "Id" INTEGER PRIMARY KEY,
    "CollectionId" INTEGER NOT NULL,
    "Name" TEXT NOT NULL,
    "DisplayName" TEXT NOT NULL,
    "Type" TEXT NOT NULL,             -- string, number, boolean, date, object, array
    "IsRequired" INTEGER NOT NULL,
    "IsUnique" INTEGER NOT NULL,
    "DefaultValue" TEXT,
    "Configuration" TEXT NOT NULL,    -- JSON конфигурация поля
    "Order" INTEGER NOT NULL,
    "CreatedAt" TEXT NOT NULL
);

-- Документы (данные)
CREATE TABLE "DynamicDocuments" (
    "Id" INTEGER PRIMARY KEY,
    "CollectionId" INTEGER NOT NULL,
    "Data" TEXT NOT NULL,             -- JSON данные
    "CreatedBy" INTEGER NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NOT NULL,
    "IsEnabled" INTEGER NOT NULL
);
```

## Data Models

### DynamicCollection
```csharp
public class DynamicCollection
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Schema { get; set; } = string.Empty;        // JSON Schema
    public bool IsEnabled { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public ICollection<DynamicField> Fields { get; set; } = new List<DynamicField>();
    public ICollection<DynamicDocument> Documents { get; set; } = new List<DynamicDocument>();
}
```

### DynamicField
```csharp
public class DynamicField
{
    public int Id { get; set; }
    public int CollectionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;         // string, number, boolean, date, object, array
    public bool IsRequired { get; set; }
    public bool IsUnique { get; set; }
    public string? DefaultValue { get; set; }
    public string Configuration { get; set; } = string.Empty; // JSON конфигурация
    public int Order { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Navigation property
    public DynamicCollection Collection { get; set; } = null!;
}
```

### DynamicDocument
```csharp
public class DynamicDocument
{
    public int Id { get; set; }
    public int CollectionId { get; set; }
    public string Data { get; set; } = string.Empty;         // JSON данные
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsEnabled { get; set; }
    
    // Navigation property
    public DynamicCollection Collection { get; set; } = null!;
}
```

## NoSqlService

### Core Operations

#### Create Collection
```csharp
public async Task<DynamicCollection> CreateCollectionAsync(string name, string displayName, 
    string description, List<DynamicField> fields, int createdBy)
{
    var collection = new DynamicCollection
    {
        Name = name,
        DisplayName = displayName,
        Description = description,
        Schema = GenerateSchema(fields),
        CreatedBy = createdBy,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        IsEnabled = true
    };

    _context.Collections.Add(collection);
    await _context.SaveChangesAsync();
    
    return collection;
}
```

#### Create Document
```csharp
public async Task<DynamicDocument> CreateDocumentAsync(string collectionName, 
    object data, int createdBy)
{
    var collection = await GetCollectionByNameAsync(collectionName);
    if (collection == null)
        throw new InvalidOperationException($"Collection '{collectionName}' not found");

    var document = new DynamicDocument
    {
        CollectionId = collection.Id,
        Data = JsonSerializer.Serialize(data),
        CreatedBy = createdBy,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        IsEnabled = true
    };

    _context.Documents.Add(document);
    await _context.SaveChangesAsync();
    
    return document;
}
```

#### Query Documents
```csharp
public async Task<IEnumerable<T>> QueryDocumentsAsync<T>(string collectionName, 
    Func<JsonElement, bool>? filter = null)
{
    var collection = await GetCollectionByNameAsync(collectionName);
    if (collection == null)
        throw new InvalidOperationException($"Collection '{collectionName}' not found");

    var documents = _context.Documents
        .Where(d => d.CollectionId == collection.Id && d.IsEnabled)
        .AsEnumerable();

    if (filter != null)
    {
        documents = documents.Where(d => 
        {
            var element = JsonSerializer.Deserialize<JsonElement>(d.Data);
            return filter(element);
        });
    }

    return documents.Select(d => JsonSerializer.Deserialize<T>(d.Data)!);
}
```

## System Collections

### Device Logs Collection
```json
{
  "name": "device_logs",
  "displayName": "Device Logs",
  "description": "Logs from smart home devices",
  "fields": [
    {
      "name": "deviceId",
      "displayName": "Device ID",
      "type": "string",
      "isRequired": true,
      "isUnique": false
    },
    {
      "name": "timestamp",
      "displayName": "Timestamp",
      "type": "date",
      "isRequired": true,
      "isUnique": false
    },
    {
      "name": "eventType",
      "displayName": "Event Type",
      "type": "string",
      "isRequired": true,
      "isUnique": false
    },
    {
      "name": "data",
      "displayName": "Event Data",
      "type": "object",
      "isRequired": false,
      "isUnique": false
    }
  ]
}
```

**Sample Document:**
```json
{
  "deviceId": "device-123",
  "timestamp": "2024-01-01T12:00:00Z",
  "eventType": "status_change",
  "data": {
    "oldStatus": "offline",
    "newStatus": "online",
    "reason": "power_restored"
  }
}
```

### Security Events Collection
```json
{
  "name": "security_events",
  "displayName": "Security Events",
  "description": "Security-related events and logs",
  "fields": [
    {
      "name": "userId",
      "displayName": "User ID",
      "type": "number",
      "isRequired": true,
      "isUnique": false
    },
    {
      "name": "eventType",
      "displayName": "Event Type",
      "type": "string",
      "isRequired": true,
      "isUnique": false
    },
    {
      "name": "ipAddress",
      "displayName": "IP Address",
      "type": "string",
      "isRequired": true,
      "isUnique": false
    },
    {
      "name": "userAgent",
      "displayName": "User Agent",
      "type": "string",
      "isRequired": false,
      "isUnique": false
    },
    {
      "name": "details",
      "displayName": "Event Details",
      "type": "object",
      "isRequired": false,
      "isUnique": false
    }
  ]
}
```

**Sample Document:**
```json
{
  "userId": 42,
  "eventType": "login_success",
  "ipAddress": "192.168.1.100",
  "userAgent": "Mozilla/5.0...",
  "details": {
    "loginMethod": "password",
    "sessionId": "sess-abc123",
    "location": "Moscow, Russia"
  }
}
```

## Usage Examples

### Creating Custom Collection
```csharp
// Создание коллекции для температурных сенсоров
var fields = new List<DynamicField>
{
    new() { Name = "sensorId", DisplayName = "Sensor ID", Type = "string", IsRequired = true, Order = 1 },
    new() { Name = "temperature", DisplayName = "Temperature", Type = "number", IsRequired = true, Order = 2 },
    new() { Name = "humidity", DisplayName = "Humidity", Type = "number", IsRequired = false, Order = 3 },
    new() { Name = "location", DisplayName = "Location", Type = "string", IsRequired = true, Order = 4 }
};

var collection = await noSqlService.CreateCollectionAsync(
    "temperature_readings",
    "Temperature Readings",
    "Temperature and humidity sensor data",
    fields,
    userId
);
```

### Adding Documents
```csharp
// Добавление показаний сенсора
var reading = new
{
    sensorId = "temp-sensor-001",
    temperature = 23.5,
    humidity = 45.2,
    location = "Living Room",
    timestamp = DateTime.UtcNow
};

var document = await noSqlService.CreateDocumentAsync("temperature_readings", reading, userId);
```

### Querying Data
```csharp
// Получение показаний за последний час
var readings = await noSqlService.QueryDocumentsAsync<TemperatureReading>(
    "temperature_readings",
    element => element.GetProperty("timestamp").GetDateTime() > DateTime.UtcNow.AddHours(-1)
);

// Получение показаний конкретного сенсора
var sensorReadings = await noSqlService.QueryDocumentsAsync<TemperatureReading>(
    "temperature_readings",
    element => element.GetProperty("sensorId").GetString() == "temp-sensor-001"
);
```

## Performance Optimizations

### Indexing Strategy
```csharp
// Автоматические индексы для системных коллекций
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Индексы для быстрого поиска
    modelBuilder.Entity<DynamicDocument>()
        .HasIndex(d => new { d.CollectionId, d.CreatedAt });
    
    modelBuilder.Entity<DynamicDocument>()
        .HasIndex(d => new { d.CollectionId, d.IsEnabled });
    
    // Уникальные индексы
    modelBuilder.Entity<DynamicCollection>()
        .HasIndex(c => c.Name)
        .IsUnique();
}
```

### Query Optimization
```csharp
// Использование compiled queries для частых операций
private static readonly Func<NoSqlDbContext, string, Task<DynamicCollection?>> 
    GetCollectionByNameQuery = 
    EF.CompileAsyncQuery((NoSqlDbContext ctx, string name) =>
        ctx.Collections.FirstOrDefault(c => c.Name == name));

public async Task<DynamicCollection?> GetCollectionByNameAsync(string name)
{
    return await GetCollectionByNameQuery(_context, name);
}
```

### Connection Pooling
```csharp
// Оптимизация SQLite для concurrent access
services.AddDbContext<NoSqlDbContext>(options =>
{
    options.UseSqlite(connectionString, sqlite =>
    {
        sqlite.CommandTimeout(30);
    });
    
    // Connection pooling для SQLite
    options.EnableSensitiveDataLogging(false);
    options.EnableDetailedErrors(false);
});
```

## Data Validation

### Schema Validation
```csharp
public class DocumentValidator
{
    public ValidationResult ValidateDocument(DynamicCollection collection, JsonElement data)
    {
        var schema = JsonSerializer.Deserialize<JsonSchema>(collection.Schema);
        var errors = new List<string>();
        
        foreach (var field in collection.Fields)
        {
            if (!data.TryGetProperty(field.Name, out var property))
            {
                if (field.IsRequired)
                    errors.Add($"Field '{field.Name}' is required");
                continue;
            }
            
            if (!ValidateFieldType(property, field.Type))
            {
                errors.Add($"Field '{field.Name}' must be of type '{field.Type}'");
            }
        }
        
        return new ValidationResult
        {
            IsValid = !errors.Any(),
            Errors = errors
        };
    }
}
```

### Type Validation
```csharp
private static bool ValidateFieldType(JsonElement element, string expectedType)
{
    return expectedType.ToLower() switch
    {
        "string" => element.ValueKind == JsonValueKind.String,
        "number" => element.ValueKind == JsonValueKind.Number,
        "boolean" => element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False,
        "date" => element.TryGetDateTime(out _),
        "object" => element.ValueKind == JsonValueKind.Object,
        "array" => element.ValueKind == JsonValueKind.Array,
        _ => false
    };
}
```

## Migration and Backup

### Data Export
```csharp
public async Task ExportCollectionAsync(string collectionName, Stream outputStream)
{
    var collection = await GetCollectionByNameAsync(collectionName);
    var documents = await QueryDocumentsAsync<JsonElement>(collectionName);
    
    var exportData = new
    {
        Collection = collection,
        Documents = documents,
        ExportedAt = DateTime.UtcNow
    };
    
    await JsonSerializer.SerializeAsync(outputStream, exportData);
}
```

### Data Import
```csharp
public async Task ImportCollectionAsync(Stream inputStream)
{
    var importData = await JsonSerializer.Deserialize<ImportData>(inputStream);
    
    // Создание коллекции
    var collection = importData!.Collection;
    _context.Collections.Add(collection);
    
    // Импорт документов
    foreach (var doc in importData.Documents)
    {
        var document = new DynamicDocument
        {
            CollectionId = collection.Id,
            Data = JsonSerializer.Serialize(doc),
            CreatedBy = doc.CreatedBy,
            CreatedAt = doc.CreatedAt,
            UpdatedAt = doc.UpdatedAt,
            IsEnabled = doc.IsEnabled
        };
        _context.Documents.Add(document);
    }
    
    await _context.SaveChangesAsync();
}
```

## Monitoring and Maintenance

### Collection Statistics
```csharp
public async Task<CollectionStats> GetCollectionStatsAsync(string collectionName)
{
    var collection = await GetCollectionByNameAsync(collectionName);
    
    var stats = new CollectionStats
    {
        Name = collectionName,
        DocumentCount = await _context.Documents
            .CountAsync(d => d.CollectionId == collection.Id),
        EnabledDocumentCount = await _context.Documents
            .CountAsync(d => d.CollectionId == collection.Id && d.IsEnabled),
        FieldCount = collection.Fields.Count,
        CreatedAt = collection.CreatedAt,
        LastUpdated = collection.UpdatedAt
    };
    
    return stats;
}
```

### Cleanup Operations
```csharp
public async Task CleanupOldDocumentsAsync(string collectionName, TimeSpan maxAge)
{
    var collection = await GetCollectionByNameAsync(collectionName);
    var cutoffDate = DateTime.UtcNow - maxAge;
    
    var oldDocuments = await _context.Documents
        .Where(d => d.CollectionId == collection.Id && d.CreatedAt < cutoffDate)
        .ToListAsync();
    
    _context.Documents.RemoveRange(oldDocuments);
    await _context.SaveChangesAsync();
    
    _logger.LogInformation("Cleaned up {Count} old documents from collection {CollectionName}", 
        oldDocuments.Count, collectionName);
}
```

## Best Practices

### Collection Design
- **Meaningful Names**: Используйте понятные имена коллекций
- **Consistent Schema**: Следуйте единой схеме данных
- **Proper Indexing**: Индексируйте часто используемые поля
- **Validation**: Всегда валидируйте входные данные

### Performance
- **Batch Operations**: Используйте массовые операции
- **Async Operations**: Всегда используйте async/await
- **Connection Management**: Оптимизируйте соединения с БД
- **Memory Usage': Избегайте загрузки больших объемов данных

### Security
- **Input Validation**: Проверяйте все входные данные
- **Access Control**: Ограничивайте доступ к коллекциям
- **Audit Logging**: Логируйте все операции
- **Data Encryption**: Шифруйте чувствительные данные

## Future Enhancements

### Planned Features
- **Full-text Search**: Поиск по документам
- **Aggregation Pipeline**: Агрегационные операции
- **Change Streams**: Real-time уведомления об изменениях
- **Sharding**: Горизонтальное масштабирование

### Performance Improvements
- **Read Replicas**: Для read-heavy операций
- **Caching Layer**: Redis для горячих данных
- **Compression**: Сжатие JSON данных
- **Bulk Operations**: Оптимизация массовых операций

### Advanced Features
- **GraphQL Integration**: Flexible API
- **Time Series**: Специализированные операции для временных рядов
- **Geospatial**: Геолокационные запросы
- **Machine Learning**: Интеграция с ML моделями
