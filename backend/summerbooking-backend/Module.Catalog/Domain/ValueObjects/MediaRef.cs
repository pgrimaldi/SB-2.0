using Module.Catalog.Domain.Enums;

namespace Module.Catalog.Domain.ValueObjects;

/// <summary>
/// Riferimento a un file (origin + path relativo). La composizione dell'URL completo è responsabilità
/// dell'Application (fuori scope in questa fase), mai del DB.
/// </summary>
public sealed record MediaRef(MediaOrigin Origin, string Path);
