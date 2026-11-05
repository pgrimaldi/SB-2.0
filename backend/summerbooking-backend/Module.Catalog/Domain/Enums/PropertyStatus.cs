namespace Module.Catalog.Domain.Enums;

/// <summary>Stato del ciclo di vita amministrativo dello stabilimento (tenant).</summary>
public enum PropertyStatus
{
	Pending = 0,
	Approved = 1,
	Rejected = 2,
	Imported = 3,
	/// <summary>Nascosta: fuori dal sito e fuori dalla coda «in attesa», finché il systemadmin non decide.</summary>
	Hidden = 4,
}
