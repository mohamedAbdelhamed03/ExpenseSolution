using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Expense.Core.Application.Common.Interfaces
{
    public interface IFileUploader
    {
        Task<string> UploadFileAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default);
        Task DeleteFileAsync(string publicId, CancellationToken cancellationToken = default);
        Task<Stream> GetFileStreamAsync(string url, CancellationToken cancellationToken = default);
    }
}
