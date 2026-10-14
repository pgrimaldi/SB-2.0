namespace Module.ZTest.Infrastructure.Configurations;

/// <summary>
/// Esempio di configurazione della persistenza per l'entita Reservation.
/// Qui e espressa come metadati puri per non introdurre dipendenze da EF Core.
/// In un modulo reale sarebbe una classe che implementa
/// IEntityTypeConfiguration&lt;Reservation&gt; e nel metodo Configure(builder) mapperebbe:
/// tabella e schema, chiave primaria, colonne, e i Value Object (es. DateRange come OwnsOne).
/// </summary>
public static class ReservationConfiguration
{
	public const string TableName = "Reservations";
	public const string Schema = "ztest";

	public const int StatusMaxLength = 20;
	public const int PricePrecision = 18;
	public const int PriceScale = 2;
}
