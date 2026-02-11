using Expense.API.Controllers;
using Expense.Core.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Expense.UnitTests
{
    public class FilesControllerTests
    {
        private readonly Mock<IFileUploader> _mockFileUploader;
        private readonly FilesController _controller;

        public FilesControllerTests()
        {
            _mockFileUploader = new Mock<IFileUploader>();
            _controller = new FilesController(_mockFileUploader.Object);
        }

        [Fact]
        public async Task Upload_ShouldReturnOk_WhenFileIsValid()
        {
            // Arrange
            var content = "fake image content";
            var fileName = "test.png";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            var file = new FormFile(stream, 0, stream.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };

            var expectedUrl = "https://cloudinary.com/test.png";
            _mockFileUploader.Setup(x => x.UploadFileAsync(It.IsAny<Stream>(), fileName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedUrl);

            // Act
            var result = await _controller.Upload(file, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var value = okResult.Value;
            Assert.NotNull(value);
            
            // Use reflection or dynamic to access anonymous type property, or just check string representation if simple
            // Since we return new { url }, we can check the property
            var urlProperty = value.GetType().GetProperty("url");
            Assert.NotNull(urlProperty);
            Assert.Equal(expectedUrl, urlProperty.GetValue(value));
        }

        [Fact]
        public async Task Upload_ShouldReturnBadRequest_WhenFileIsEmpty()
        {
            // Arrange
            var file = new FormFile(Stream.Null, 0, 0, "file", "empty.png");

            // Act
            var result = await _controller.Upload(file, CancellationToken.None);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("File is empty", badRequestResult.Value);
        }

        [Fact]
        public async Task Upload_ShouldReturnBadRequest_WhenFileIsNotImage()
        {
            // Arrange
            var content = "fake text content";
            var fileName = "test.txt";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            var file = new FormFile(stream, 0, stream.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "text/plain"
            };

            // Act
            var result = await _controller.Upload(file, CancellationToken.None);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Only image files are allowed.", badRequestResult.Value);
        }

        [Fact]
        public async Task Download_ShouldReturnFile_WhenUrlIsValid()
        {
            // Arrange
            var url = "https://cloudinary.com/test.png";
            var content = "fake image content";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

            _mockFileUploader.Setup(x => x.GetFileStreamAsync(url, It.IsAny<CancellationToken>()))
                .ReturnsAsync(stream);

            // Act
            var result = await _controller.Download(url, CancellationToken.None);

            // Assert
            var fileResult = Assert.IsType<FileStreamResult>(result);
            Assert.Equal("test.png", fileResult.FileDownloadName);
            Assert.Equal("application/octet-stream", fileResult.ContentType);
        }

        [Fact]
        public async Task Preview_ShouldReturnFile_WhenUrlIsValid()
        {
            // Arrange
            var url = "https://cloudinary.com/test.png";
            var content = "fake image content";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

            _mockFileUploader.Setup(x => x.GetFileStreamAsync(url, It.IsAny<CancellationToken>()))
                .ReturnsAsync(stream);

            // Act
            var result = await _controller.Preview(url, CancellationToken.None);

            // Assert
            var fileResult = Assert.IsType<FileStreamResult>(result);
            Assert.Equal("image/png", fileResult.ContentType);
            Assert.True(string.IsNullOrEmpty(fileResult.FileDownloadName)); // Inline (null or empty)
        }
    }
}
