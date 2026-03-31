namespace testASP.Models;

/// <summary>
/// Модель устройства для Smart Home системы
/// </summary>
public sealed class Device
{
    public int Id { get; set; }
    
    /// <summary>
    /// Уникальный идентификатор устройства
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;
    
    /// <summary>
    /// Название устройства
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Тип устройства (light, thermostat, camera, etc.)
    /// </summary>
    public string Type { get; set; } = string.Empty;
    
    /// <summary>
    /// Статус устройства (online, offline, error)
    /// </summary>
    public string Status { get; set; } = "offline";
    
    /// <summary>
    /// Свойства устройства в формате JSON
    /// </summary>
    public string Properties { get; set; } = "{}";
    
    /// <summary>
    /// ID владельца устройства
    /// </summary>
    public int UserId { get; set; }
    
    /// <summary>
    /// Расположение устройства
    /// </summary>
    public string? Location { get; set; }
    
    /// <summary>
    /// Производитель
    /// </summary>
    public string? Manufacturer { get; set; }
    
    /// <summary>
    /// Модель устройства
    /// </summary>
    public string? Model { get; set; }
    
    /// <summary>
    /// Версия прошивки
    /// </summary>
    public string? FirmwareVersion { get; set; }
    
    /// <summary>
    /// Дата создания записи
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Дата последнего обновления
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Включено ли устройство
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}
