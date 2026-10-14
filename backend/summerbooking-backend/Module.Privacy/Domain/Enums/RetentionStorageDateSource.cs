namespace Module.Privacy.Domain.Enums;

/// <summary>
/// Da dove prende la data una policy che agisce su un <b>prefisso di storage</b>, cioe' su oggetti
/// che nessuna riga del database nomina (i QR delle prenotazioni, i file orfani).
/// </summary>
/// <remarks>
/// Un'etichetta sola, e di proposito: oggi l'unica data disponibile per un oggetto senza riga e' il
/// suo <c>LastModified</c> su S3. L'enum esiste comunque perche' la colonna deve dire da dove viene
/// la data invece di lasciarlo implicito nel codice del job, e perche' il giorno in cui si volesse
/// usare un tag dell'oggetto o un inventario S3 la forma e' già pronta.
/// </remarks>
public enum RetentionStorageDateSource
{
	/// <summary>Il <c>LastModified</c> dell'oggetto su S3, usato come data approssimata del fatto che lo ha prodotto.</summary>
	ObjectLastModified = 0
}
