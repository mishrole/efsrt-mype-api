using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using Mype.Application.MovementEvidences.Common;
using Mype.Application.MovementEvidences.Interfaces;
using Mype.Application.MovementEvidences.Models;
using Mype.Shared.Constants;

namespace Mype.Tests.Application.MovementEvidences.Common
{
    public sealed class EvidenceFileValidatorTests
    {
        private readonly EvidenceFileValidator _validator = new(new Settings());

        public static TheoryData<string, string, byte[], string, string> ValidFiles =>
            new()
            {
                {
                    "photo.jpg",
                    "image/jpeg",
                    new byte[] { 0xFF, 0xD8, 0xFF, 0x01 },
                    "image/jpeg",
                    ".jpg"
                },
                {
                    "photo.jpeg",
                    "image/jpeg",
                    new byte[] { 0xFF, 0xD8, 0xFF, 0x01 },
                    "image/jpeg",
                    ".jpg"
                },
                {
                    "image.png",
                    "image/png",
                    new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A },
                    "image/png",
                    ".png"
                },
                {
                    "document.pdf",
                    "application/pdf",
                    new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D },
                    "application/pdf",
                    ".pdf"
                },
            };

        [Theory]
        [MemberData(nameof(ValidFiles))]
        public void Validate_Should_Accept_Supported_File(
            string name,
            string declaredMime,
            byte[] bytes,
            string expectedMime,
            string expectedExtension
        )
        {
            using var stream = new MemoryStream(bytes);

            var result = _validator.Validate(
                new EvidenceFile(name, declaredMime, bytes.Length, stream)
            );

            result.IsSuccess.Should().BeTrue();
            result.Value.MimeType.Should().Be(expectedMime);
            result.Value.Extension.Should().Be(expectedExtension);
            stream.Position.Should().Be(0);
        }

        [Fact]
        public void Validate_Should_Preserve_Original_Stream_Position()
        {
            var bytes = new byte[] { 0x00, 0x00, 0x25, 0x50, 0x44, 0x46, 0x2D };
            using var stream = new MemoryStream(bytes) { Position = 2 };

            var result = _validator.Validate(
                new EvidenceFile("document.pdf", "application/pdf", 5, stream)
            );

            result.IsSuccess.Should().BeTrue();
            stream.Position.Should().Be(2);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(11)]
        public void Validate_Should_Reject_Invalid_Size(long length)
        {
            using var stream = new MemoryStream(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D });

            var result = _validator.Validate(
                new EvidenceFile("document.pdf", "application/pdf", length, stream)
            );

            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(ErrorCodes.InvalidEvidenceSize);
        }

        [Theory]
        [InlineData("document.exe", "application/pdf")]
        [InlineData("document.pdf", "image/png")]
        [InlineData("document.png", "application/pdf")]
        public void Validate_Should_Reject_Inconsistent_Extension_Mime_Or_Signature(
            string name,
            string mime
        )
        {
            using var stream = new MemoryStream(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D });

            var result = _validator.Validate(new EvidenceFile(name, mime, stream.Length, stream));

            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(ErrorCodes.UnsupportedEvidenceFormat);
        }

        [Fact]
        public void Validate_Should_Reject_Unknown_Signature()
        {
            using var stream = new MemoryStream(new byte[] { 0x01, 0x02, 0x03, 0x04 });

            var result = _validator.Validate(
                new EvidenceFile("file.pdf", "application/pdf", stream.Length, stream)
            );

            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(ErrorCodes.UnsupportedEvidenceFormat);
        }

        [Fact]
        public void Validate_Should_Normalize_Unsafe_File_Name()
        {
            using var stream = new MemoryStream(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D });

            var result = _validator.Validate(
                new EvidenceFile(
                    "../  inva\u0000lid<>name.PDF",
                    "application/pdf",
                    stream.Length,
                    stream
                )
            );

            result.IsSuccess.Should().BeTrue();
            result.Value.OriginalFileName.Should().Be("invalidname.pdf");
        }

        [Fact]
        public void Validate_Should_Fallback_To_Evidence_When_Name_Becomes_Empty()
        {
            using var stream = new MemoryStream(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D });

            var result = _validator.Validate(
                new EvidenceFile("<>.pdf", "application/pdf", stream.Length, stream)
            );

            result.IsSuccess.Should().BeTrue();
            result.Value.OriginalFileName.Should().Be("evidence.pdf");
        }

        [Fact]
        public void Validate_Should_Truncate_Normalized_Name_To_255_Characters()
        {
            var name = new string('a', 300) + ".pdf";
            using var stream = new MemoryStream(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D });

            var result = _validator.Validate(
                new EvidenceFile(name, "application/pdf", stream.Length, stream)
            );

            result.IsSuccess.Should().BeTrue();
            result.Value.OriginalFileName.Should().HaveLength(255);
            result.Value.OriginalFileName.Should().EndWith(".pdf");
        }

        [Fact]
        public void Validate_Should_Not_Modify_Image_Bytes()
        {
            var bytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x7A, 0x7B };
            using var stream = new MemoryStream(bytes.ToArray());

            var result = _validator.Validate(
                new EvidenceFile("image.png", "image/png", bytes.Length, stream)
            );

            result.IsSuccess.Should().BeTrue();
            stream.ToArray().Should().Equal(bytes);
        }

        private sealed class Settings : IEvidenceStorageSettings
        {
            public string Provider => "AzureBlob";
            public string ContainerName => "evidences";
            public long MaxSizeBytes => 10;
            public int ReadUriMinutes => 5;
            public int PurgeRetentionDays => 30;
        }
    }
}
