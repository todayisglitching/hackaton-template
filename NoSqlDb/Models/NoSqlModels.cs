namespace testASP.NoSqlDb.Models;

/// <summary>
/// Определение поля для динамической коллекции
/// </summary>
public sealed class FieldDefinition
{
    /// <summary>
    /// Имя поля
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Отображаемое имя поля
    /// </summary>
    public string? DisplayName { get; set; }
    
    /// <summary>
    /// Тип поля
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
    /// Дополнительная конфигурация
    /// </summary>
    public Dictionary<string, object>? Configuration { get; set; }
    
    /// <summary>
    /// Порядок отображения
    /// </summary>
    public int Order { get; set; }
}

/// <summary>
/// Типы полей для NoSQL коллекций
/// </summary>
public static class FieldTypes
{
    public const string String = "string";
    public const string Text = "text";
    public const string Number = "number";
    public const string Boolean = "boolean";
    public const string Date = "date";
    public const string DateTime = "datetime";
    public const string Email = "email";
    public const string Url = "url";
    public const string Json = "json";
    public const string Select = "select";
    public const string MultiSelect = "multiselect";
    public const string File = "file";
    public const string Image = "image";
}

/// <summary>
/// Предустановленные конфигурации полей
/// </summary>
public static class FieldConfigurations
{
    /// <summary>
    /// Текстовое поле с максимальной длиной
    /// </summary>
    public static Dictionary<string, object> TextField(int maxLength = 255) => new()
    {
        ["maxLength"] = maxLength,
        ["placeholder"] = "Введите текст..."
    };
    
    /// <summary>
    /// Числовое поле с ограничениями
    /// </summary>
    public static Dictionary<string, object> NumberField(decimal? min = null, decimal? max = null, int decimals = 0) => new()
    {
        ["min"] = min,
        ["max"] = max,
        ["decimals"] = decimals
    };
    
    /// <summary>
    /// Поле выбора из списка
    /// </summary>
    public static Dictionary<string, object> SelectField(params string[] options) => new()
    {
        ["options"] = options,
        ["allowMultiple"] = false
    };
    
    /// <summary>
    /// Множественный выбор
    /// </summary>
    public static Dictionary<string, object> MultiSelectField(params string[] options) => new()
    {
        ["options"] = options,
        ["allowMultiple"] = true
    };
    
    /// <summary>
    /// Поле для загрузки файла
    /// </summary>
    public static Dictionary<string, object> FileField(string[] allowedTypes, long maxSize = 10485760) => new()
    {
        ["allowedTypes"] = allowedTypes,
        ["maxSize"] = maxSize
    };
}
