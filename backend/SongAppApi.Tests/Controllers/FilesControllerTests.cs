using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SongAppApi.Controllers;
using SongAppApi.Models.Files;
using SongAppApi.Services;
using SongAppApi.Tests.Common;
using Xunit;
using EntityFile = SongAppApi.Entities.File;

namespace SongAppApi.Tests.Controllers
{
    public class FilesControllerTests
    {
        private readonly Mock<IFileService> _fileService = new();
        private readonly FilesController _controller;

        public FilesControllerTests()
        {
            _controller = new FilesController(_fileService.Object);
            ControllerContextHelper.Attach(_controller, account: null);
        }

        [Fact]
        public void Post_HappyPath_ReturnsOk()
        {
            _fileService.Setup(s => s.Create(It.IsAny<FileModel>())).Returns("file-id");

            var result = _controller.Post(new FileModel());

            result.Result.Should().BeOfType<OkObjectResult>()
                .Which.Value.Should().Be("file-id");
        }

        [Fact]
        public void Post_ServiceThrows_Returns500()
        {
            _fileService.Setup(s => s.Create(It.IsAny<FileModel>()))
                .Throws(new Exception("boom"));

            var result = _controller.Post(new FileModel());

            var status = result.Result.Should().BeOfType<StatusCodeResult>().Subject;
            status.StatusCode.Should().Be(500);
        }

        [Fact]
        public void Get_FileMissing_Returns404()
        {
            _fileService.Setup(s => s.GetFileById(It.IsAny<string>()))
                .Returns((EntityFile?)null!);

            var result = _controller.Get("some-id");

            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public void Get_ServiceThrows_Returns500()
        {
            _fileService.Setup(s => s.GetFileById(It.IsAny<string>()))
                .Throws(new Exception("boom"));

            var result = _controller.Get("some-id");

            var status = result.Should().BeOfType<StatusCodeResult>().Subject;
            status.StatusCode.Should().Be(500);
        }
    }
}
