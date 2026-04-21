namespace HBOperations.Application.Common.Interfaces;

public interface IFileValidationService
{
    FileValidationResult Validate(Stream stream, string fileName, string contentType, long fileSize);
}

public record FileValidationResult(bool IsValid, string? ErrorMessage = null);
