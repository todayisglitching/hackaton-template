namespace testASP.NoSqlDb.Models;

/// <summary>
/// Модель динамической коллекции для NoSQL функциональности
/// </summary>
public sealed class DynamicCollection
{
    public int Id { get; set; }
    
    /// <summary>
    /// Уникальное имя коллекции (для внутреннего использования)
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Отображаемое имя коллекции
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// Описание коллекции
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// JSON схема коллекции (конфигурация полей)
    /// </summary>
    public string Schema { get; set; } = string.Empty;
    
    /// <summary>
    /// Включена ли коллекция
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>
    /// ID создателя коллекции
    /// </summary>
    public int CreatedBy { get; set; }
    
    /// <summary>
    /// Дата создания
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Дата обновления
    /// </summary>
    public DateTime UpdatedAt { get; set; }
    
    // Навигационные свойства
    public List<DynamicField> Fields { get; set; } = new();
    public List<DynamicDocument> Documents { get; set; } = new();
}

/// <summary>
/// Модель поля динамической коллекции
/// </summary>
public sealed class DynamicField
{
    public int Id { get; set; }
    
    /// <summary>
    /// ID коллекции
    /// </summary>
    public int CollectionId { get; set; }
    
    /// <summary>
    /// Уникальное имя поля
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Отображаемое имя поля
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// Тип поля (string, number, boolean, date, json, etc.)
    /// </summary>
    public string Type { get; set; } = string.Empty;
    
    /// <summary>
    /// Обязательно ли поле
    /// </summary>
    public bool IsRequired { get; set; }
    
    /// <summary>
    /// Уникальное ли поле
    /// </summary>
    public bool IsUnique { get; set; }
    
    /// <summary>
    /// Значение по умолчанию
    /// </summary>
    public string? DefaultValue { get; set; } = null;
    
    /// <summary>
    /// Дополнительная конфигурация поля (JSON)
    /// </summary>
    public string Configuration { get; set; } = string.Empty;
    
    /// <summary>
    /// Порядок отображения
    /// </summary>
    public int Order { get; set; }
    
    /// <summary>
    /// Дата создания
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Навигационные свойства
    public DynamicCollection Collection { get; set; } = null!;
}

/// <summary>
/// Модель документа в динамической коллекции
/// </summary>
public sealed class DynamicDocument
{
    public int Id { get; set; }
    
    /// <summary>
    /// ID коллекции
    /// </summary>
    public int CollectionId { get; set; }
    
    /// <summary>
    /// Данные документа в формате JSON
    /// </summary>
    public string Data { get; set; } = string.Empty;
    
    /// <summary>
    /// ID создателя документа
    /// </summary>
    public int CreatedBy { get; set; }
    
    /// <summary>
    /// Дата создания
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Дата обновления
    /// </summary>
    public DateTime UpdatedAt { get; set; }
    
    /// <summary>
    /// Включен ли документ
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    
    // Навигационные свойства
    public DynamicCollection Collection { get; set; } = null!;
}
