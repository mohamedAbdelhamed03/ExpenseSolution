using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Expense.Core.Application.Common.Interfaces;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using System.Net.Http;

namespace Expense.Infrastructure.Services.Cloudinary
{
    public class CloudinaryFileUploader : IFileUploader
    {
        private readonly CloudinaryDotNet.Cloudinary _cloudinary;
        private readonly HttpClient _httpClient;

        public CloudinaryFileUploader(IOptions<CloudinarySettings> options)
        {
            var account = new Account(
                options.Value.CloudName,
                options.Value.ApiKey,
                options.Value.ApiSecret
            );

            _cloudinary = new CloudinaryDotNet.Cloudinary(account);
            _httpClient = new HttpClient();
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default)
        {
            if (fileStream.Length > 0)
            {
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(fileName, fileStream),
                    Transformation = new Transformation().Quality("auto").FetchFormat("auto")
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams, cancellationToken);
                return uploadResult.SecureUrl.ToString();
            }

            return string.Empty;
        }

        public async Task DeleteFileAsync(string publicId, CancellationToken cancellationToken = default)
        {
            var deletionParams = new DeletionParams(publicId);
            await _cloudinary.DestroyAsync(deletionParams);
        }

        public async Task<Stream> GetFileStreamAsync(string url, CancellationToken cancellationToken = default)
        {
            // Security check: ensure the URL belongs to Cloudinary (basic check)
            if (string.IsNullOrEmpty(url) || !url.Contains("cloudinary.com"))
            {
                throw new ArgumentException("Invalid file URL");
            }

            return await _httpClient.GetStreamAsync(url, cancellationToken);
        }
    }
}
