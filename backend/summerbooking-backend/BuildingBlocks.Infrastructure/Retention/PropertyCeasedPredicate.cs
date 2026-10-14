namespace BuildingBlocks.Infrastructure.Retention;

/// <summary>
/// Costruisce il predicato SQL «lo stabilimento a cui questa riga appartiene ha <b>cessato</b> prima
/// del taglio», che è la decorrenza delle policy con <c>reference_event = 'property_ceased'</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Perché serve un pezzo di codice e non una colonna.</b> «Cessazione» non corrisponde a nessuno
/// stato a database: <c>catalog.property_status</c> è <c>pending | approved | rejected | imported</c>
/// e non dice niente sull'abbonamento. La decisione del titolare (2026-10-02) è di
/// <b>derivarla</b> invece di aggiungere uno stato che qualcuno deve ricordarsi di scrivere — una
/// colonna che si dimentica di aggiornare fa scadere i dati di uno stabilimento ancora attivo.
/// </para>
/// <para>
/// <b>La definizione, in due metà che servono entrambe.</b>
/// </para>
/// <list type="number">
/// <item><b>Ha chiuso</b>: esiste un abbonamento in stato <c>cancelled</c> oppure <c>expired</c>, e
/// la data dell'ultimo periodo chiuso (<c>max(closed_at)</c>) è anteriore al taglio;</item>
/// <item><b>E non è ripartito</b>: <i>nessun</i> abbonamento è <c>active</c> o <c>suspended</c>.
/// ⚠️ Questa metà è quella che conta. Senza, uno stabilimento che ha disdetto il piano vecchio e ne
/// ha sottoscritto uno nuovo risulterebbe cessato, e la retention gli cancellerebbe le foto mentre
/// sta lavorando. Un abbonamento <b>sospeso</b> non è una cessazione: è l'obiezione che aveva
/// bloccato la decisione, e sta scritta qui.</item>
/// </list>
/// <para>
/// <c>account_subscription_periods</c> porta <c>property_id</c> per conto proprio, quindi la data non
/// richiede nessuna giunzione con <c>account_subscriptions</c>.
/// </para>
/// <para>
/// ⚠️ <b>Lo stabilimento si identifica con lo <c>public_id</c> (uuid), non con l'<c>id</c>.</b> In
/// questo database convivono due riferimenti con lo stesso nome: <c>catalog.media_assets.property_id</c>
/// e <c>identity.account_subscription_periods.property_id</c> sono <c>uuid</c> e puntano a
/// <c>catalog.properties.public_id</c>, mentre le 17 tabelle <i>dentro</i> <c>catalog</c> usano
/// <c>int8</c> verso <c>catalog.properties.id</c>, con una vera chiave esterna.
/// </para>
/// <para>
/// ✅ Passare l'uno per l'altro <b>non è un errore silenzioso</b>: PostgreSQL rifiuta
/// <c>uuid = bigint</c> con «l'operatore non esiste», perché fra i due tipi non c'è nessuna
/// conversione implicita. La differenza di tipo <b>protegge</b>. Resta utile il controllo in
/// <c>RetentionEngine</c> — vedi lì il perché.
/// </para>
/// </remarks>
public static class PropertyCeasedPredicate
{
	/// <summary>Il nome dell'evento, come sta scritto nella colonna <c>reference_event</c>.</summary>
	public const string Event = "property_ceased";

	/// <summary>
	/// La condizione, <b>nuda</b>: nessun <c>AND</c> davanti, perché chi la usa a volte la mette in
	/// testa a un <c>WHERE</c> e a volte in mezzo. Un <c>AND</c> incluso costringeva il chiamante a
	/// ritagliarlo via contando i caratteri, che è il genere di codice che si rompe in silenzio.
	/// </summary>
	/// <param name="propertyPublicIdExpression">
	/// Espressione SQL che vale lo <b>public_id (uuid)</b> dello stabilimento della riga corrente —
	/// per esempio <c>t."property_id"</c>, oppure <c>p."public_id"</c> se si è passati dalla tabella
	/// degli stabilimenti. ⚠️ Non un <c>id</c> numerico: non troverebbe niente, in silenzio.
	/// </param>
	/// <param name="cutoffParameter">Nome del parametro che porta la data di taglio.</param>
	public static string For(string propertyPublicIdExpression, string cutoffParameter = "@cutoff")
	{
		var p = propertyPublicIdExpression;

		return $"""
			(EXISTS (
			       SELECT 1
			         FROM identity.account_subscriptions s
			        WHERE s.property_id = {p}
			          AND s.deleted_at IS NULL
			          AND s.status::text IN ('cancelled', 'expired')
			          AND (SELECT max(pp.closed_at)
			                 FROM identity.account_subscription_periods pp
			                WHERE pp.property_id = {p}
			                  AND pp.deleted_at IS NULL) < {cutoffParameter})
			 AND NOT EXISTS (
			       SELECT 1
			         FROM identity.account_subscriptions s2
			        WHERE s2.property_id = {p}
			          AND s2.deleted_at IS NULL
			          AND s2.status::text IN ('active', 'suspended')))
			""".Replace("\r\n", "\n").Replace("\n", " ");
	}
}
