using System;
using System.IO;
using System.Threading.Tasks;

namespace VirtoCommerce.ExportModule.Core.Services;

public interface IExportFileStorage
{
    string GenerateFileName(DateTime timestamp, string fileExtension);
    Task<Stream> OpenWriteAsync(string fileName);
    Task<Stream> OpenReadAsync(string fileName);
}
