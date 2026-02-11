using Expense.Core.Application.Common.Interfaces;
using Expense.Core.Common.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Expense.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FilesController : ControllerBase
    {
        private readonly IFileUploader _fileUploader;

        public FilesController(IFileUploader fileUploader)
        {
            _fileUploader = fileUploader;
        }

        [HttpPost("upload")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Upload(IFormFile file, CancellationToken cancellationToken)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("File is empty");
            }

            // Optional: Validate file type (e.g. only images)
            if (!file.ContentType.StartsWith("image/"))
            {
                 return BadRequest("Only image files are allowed.");
            }

            using var stream = file.OpenReadStream();
            var url = await _fileUploader.UploadFileAsync(stream, file.FileName, cancellationToken);

            return Ok(new { url });
        }

        [HttpGet("download")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Download([FromQuery] string url, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(url))
            {
                return BadRequest("URL is required");
            }

            try
            {
                var stream = await _fileUploader.GetFileStreamAsync(url, cancellationToken);
                var fileName = Path.GetFileName(new Uri(url).LocalPath);
                var contentType = "application/octet-stream"; // Or detect based on extension

                return File(stream, contentType, fileName);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return NotFound("File not found or could not be downloaded.");
            }
        }

        [HttpGet("preview")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Preview([FromQuery] string url, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(url))
            {
                return BadRequest("URL is required");
            }

            try
            {
                var stream = await _fileUploader.GetFileStreamAsync(url, cancellationToken);
                var fileName = Path.GetFileName(new Uri(url).LocalPath);
                
                // Simple content type detection based on extension
                var contentType = "application/octet-stream";
                var ext = Path.GetExtension(fileName).ToLowerInvariant();
                if (ext == ".png") contentType = "image/png";
                else if (ext == ".jpg" || ext == ".jpeg") contentType = "image/jpeg";
                else if (ext == ".gif") contentType = "image/gif";

                return File(stream, contentType); // Inline by default
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return NotFound("File not found or could not be previewed.");
            }
        }
    }
}
