namespace Shared.Dtos;

/// <summary>Formato di un'esportazione tabellare.</summary>
public enum TabularFormat { Csv = 0, Xlsx = 1 }

/// <summary>File prodotto: contenuto, tipo MIME e nome suggerito per il download.</summary>
public sealed record TabularFile(byte[] Content, string ContentType, string FileName);
