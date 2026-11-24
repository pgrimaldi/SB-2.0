namespace Module.Catalog.Application.Dtos;

/// <summary>File ricevuto da un form multipart.</summary>
public sealed record UploadedFile(string FileName, string ContentType, long Length, Stream Content);
