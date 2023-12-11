using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using VirtoCommerce.AssetsModule.Core.Assets;
using VirtoCommerce.ExportModule.Core;
using VirtoCommerce.ExportModule.Core.Services;
using VirtoCommerce.Platform.Core;
using VirtoCommerce.Platform.Core.Exceptions;
using VirtoCommerce.Platform.Core.Extensions;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.ExportModule.Data.Services;

public class ExportFileStorage : IExportFileStorage
{
    private readonly PlatformOptions _platformOptions;
    private readonly ISettingsManager _settingsManager;
    private readonly IBlobStorageProvider _blobStorageProvider;

    public ExportFileStorage(
        IOptions<PlatformOptions> platformOptions,
        ISettingsManager settingsManager,
        IBlobStorageProvider blobStorageProvider)
    {
        _platformOptions = platformOptions.Value;
        _settingsManager = settingsManager;
        _blobStorageProvider = blobStorageProvider;
    }

    public string GenerateFileName(DateTime timestamp, string fileExtension)
    {
        var setting = ModuleConstants.Settings.General.ExportFileNameTemplate;
        var fileNameTemplate = _settingsManager.GetValue<string>(setting);

        if (string.IsNullOrEmpty(fileNameTemplate))
        {
            throw new PlatformException($"{setting.Name} is not set.");
        }

        var fileName = string.Format(fileNameTemplate, timestamp);

        if (!string.IsNullOrEmpty(fileExtension))
        {
            fileName = Path.ChangeExtension(fileName, fileExtension);
        }

        return fileName;
    }

    public Task<Stream> OpenWriteAsync(string fileName)
    {
        var url = GetExportFileUrl(fileName);

        return _blobStorageProvider.OpenWriteAsync(url);
    }

    public Task<Stream> OpenReadAsync(string fileName)
    {
        var url = GetExportFileUrl(fileName);

        return _blobStorageProvider.OpenReadAsync(url);
    }


    protected virtual string GetExportFileUrl(string fileName)
    {
        if (string.IsNullOrEmpty(_platformOptions.DefaultExportFolder))
        {
            throw new PlatformException($"{nameof(_platformOptions.DefaultExportFolder)} is not set.");
        }

        fileName = Path.GetFileName(fileName);

        return UrlHelperExtensions.Combine(_platformOptions.DefaultExportFolder, fileName);
    }
}
