using Microsoft.Extensions.FileProviders;

namespace testASP.Configuration;

/// <summary>
/// Интерфейс для конфигурации SPA (Single Page Application)
/// </summary>
public interface ISpaConfiguration
{
    /// <summary>
    /// Готово ли SPA приложение к работе
    /// </summary>
    bool IsSpaReady { get; }
    
    /// <summary>
    /// Провайдер файлов для статических ресурсов SPA
    /// </summary>
    PhysicalFileProvider? FileProvider { get; }
    
    /// <summary>
    /// Путь к директории со сборкой SPA
    /// </summary>
    string SpaDistPath { get; }
}

/// <summary>
/// Реализация конфигурации SPA
/// </summary>
public sealed class SpaConfiguration : ISpaConfiguration
{
    public bool IsSpaReady { get; }
    public PhysicalFileProvider? FileProvider { get; }
    public string SpaDistPath { get; }

    public SpaConfiguration(string contentRootPath, string baseDirectory)
    {
        // Определяем возможные пути к директории со сборкой Vite
        var contentDist = Path.Combine(contentRootPath, "Vite", "dist");
        var binDist = Path.Combine(baseDirectory, "Vite", "dist");
        
        // Выбираем существующую директорию
        SpaDistPath = Directory.Exists(contentDist) ? contentDist : binDist;
        
        // Проверяем наличие index.html
        var indexFile = Path.Combine(SpaDistPath, "index.html");
        IsSpaReady = Directory.Exists(SpaDistPath) && File.Exists(indexFile);
        
        // Создаем провайдер файлов если SPA готово
        FileProvider = IsSpaReady ? new PhysicalFileProvider(SpaDistPath) : null;
    }
}
