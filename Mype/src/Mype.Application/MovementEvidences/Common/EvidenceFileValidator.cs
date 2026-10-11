using System;
using System.IO;
using Mype.Application.Common;
using Mype.Application.MovementEvidences.Interfaces;
using Mype.Application.MovementEvidences.Models;
using Mype.Domain.MovementEvidences.Constraints;

namespace Mype.Application.MovementEvidences.Common
{
    public sealed class EvidenceFileValidator : IEvidenceFileValidator
    {
        private readonly IEvidenceStorageSettings _options;

        public EvidenceFileValidator(IEvidenceStorageSettings options)
        {
            _options = options;
        }

        public Result<ValidatedEvidenceFile> Validate(EvidenceFile file)
        {
            if (file == null || file.Content == null)
                return Result<ValidatedEvidenceFile>.Failure(MovementEvidenceErrors.InvalidFile);
            if (file.Length <= 0 || file.Length > _options.MaxSizeBytes)
                return Result<ValidatedEvidenceFile>.Failure(MovementEvidenceErrors.InvalidSize);
            if (!file.Content.CanRead || !file.Content.CanSeek)
                return Result<ValidatedEvidenceFile>.Failure(MovementEvidenceErrors.InvalidFile);

            var originalPosition = file.Content.Position;
            Span<byte> header = stackalloc byte[8];
            var read = file.Content.Read(header);
            file.Content.Position = originalPosition;

            var detected = Detect(header[..read]);
            if (detected == null)
                return Result<ValidatedEvidenceFile>.Failure(
                    MovementEvidenceErrors.UnsupportedFormat
                );

            var extension = Path.GetExtension(file.OriginalFileName)?.ToLowerInvariant();
            if (!Matches(detected.Value.MimeType, extension, file.DeclaredContentType))
                return Result<ValidatedEvidenceFile>.Failure(
                    MovementEvidenceErrors.UnsupportedFormat
                );

            var safeName = NormalizeName(file.OriginalFileName, detected.Value.Extension);
            return Result<ValidatedEvidenceFile>.Success(
                new(safeName, detected.Value.MimeType, detected.Value.Extension, file.Length)
            );
        }

        private static (string MimeType, string Extension)? Detect(ReadOnlySpan<byte> value)
        {
            if (value.Length >= 3 && value[0] == 0xFF && value[1] == 0xD8 && value[2] == 0xFF)
                return ("image/jpeg", ".jpg");
            if (
                value.Length >= 8
                && value.SequenceEqual(
                    new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }
                )
            )
                return ("image/png", ".png");
            if (
                value.Length >= 5
                && value[0] == 0x25
                && value[1] == 0x50
                && value[2] == 0x44
                && value[3] == 0x46
                && value[4] == 0x2D
            )
                return ("application/pdf", ".pdf");
            return null;
        }

        private static bool Matches(string mime, string extension, string declared)
        {
            if (!string.Equals(mime, declared, StringComparison.OrdinalIgnoreCase))
                return false;

            return mime switch
            {
                "image/jpeg" => extension is ".jpg" or ".jpeg",
                "image/png" => extension == ".png",
                "application/pdf" => extension == ".pdf",
                _ => false,
            };
        }

        private static string NormalizeName(string value, string extension)
        {
            var name = Path.GetFileNameWithoutExtension(value ?? string.Empty).Trim();
            Span<char> buffer = stackalloc char[name.Length];
            var length = 0;
            foreach (var character in name)
                if (
                    !char.IsControl(character)
                    && (char.IsLetterOrDigit(character) || character is ' ' or '-' or '_' or '.')
                )
                    buffer[length++] = character;
            var normalized = new string(buffer[..length]).Trim();
            if (string.IsNullOrWhiteSpace(normalized))
                normalized = "evidence";
            var max = MovementEvidenceConstraints.OriginalFileNameMaxLength - extension.Length;
            if (normalized.Length > max)
                normalized = normalized[..max];
            return normalized + extension;
        }
    }
}
