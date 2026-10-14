# Tabella `payment_wallet_transaction_items`

> **Dominio**: C3 — `payment` · **sottoarea**: `wallet` · **schema**: `payment` · **nome flat**:
> `payment_wallet_transaction_items` · **forma canonica**: `payment.wallet_transaction_items`.
>
> Figlia di [`payment_wallet_transactions`](payment_wallet_transactions.md). Sostituisce il JSON
> [`card_transactions.debit_details`](../../../analisi-pre/db/card_transactions.md) (oggetto, valorizzato
> sulle sole righe `transaction_type = 2`). Fonti:
> [`analisi-pre/db/card_transactions.md`](../../../analisi-pre/db/card_transactions.md),
> [`docs/03-gestione-dati.md` §2.5, §3.7](../../../progettazione/03-gestione-dati.md).

---

## Descrizione

**Una riga = un articolo consumato** pagato con un movimento di **debito** del borsellino (es. una
consumazione al bar). È lo scontrino interno del consumo: descrizione, quantità, prezzo unitario,
eventuali varianti, totale — **congelati** al momento del movimento. Duplica volutamente ciò che C1
registra sulla riga servizio (in C1 resta solo `is_paid_with_wallet` + il riferimento al wallet), qui
è la prova del *cosa* è stato addebitato sulla tessera.

Correzioni rispetto al legacy:

- **Blob JSON `debit_details`** con dati sporchi (`price` `"55.656595"`, `serviceCount: 0` con
  `totalPrice: null`, `serviceCount: 22`) → colonne tipizzate + bonifica
  ([`docs/03 §2.5, §3.7`](../../../progettazione/03-gestione-dati.md)).
- **`selectedVariantData` array di `{variantnames, variantvalues}`** → `variants_description` (testo
  leggibile congelato) + `variants_surcharge` numerico. La struttura per-variante ha valore quasi nullo
  (~50 movimenti storici, sola visualizzazione); se servisse, diventa una terza figlia
  ([D-35](../_domande-committente.md) analogo).
- **`price` come stringa** → `numeric(12,2)`.


> ### ✅ Applicato nel backend (2026-09-21)
>
> Aggiunta la colonna **`property_id`** denormalizzata dalla testata ([`docs/05-ottimizzazioni-db.md`](../../../progettazione/05-ottimizzazioni-db.md) §10),
> migration `AddDbOptimizationsPropertyIdAndGeneratedColumns`, con l'indice che la accompagna.
>
> Priorità bassa nel documento (volume minimo), applicata comunque per coerenza con le altre due figlie del modulo: una regola uguale ovunque costa meno di un'eccezione. Senza, ogni riga richiede un hop sulla testata per sapere a quale stabilimento appartiene — e le
> query che aggregano *per riga e per stabilimento* pagano quel join su tutto il volume.
>
> Riferimento **testuale** al `public_id` di `catalog.properties`, nessuna FK cross-schema: stesso
> criterio già applicato a `fiskaly_receipt_lines` e alle altre figlie denormalizzate.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_payment_wallet_transaction_items` | `id` | PK | — |
| `ix_..._transaction` | `(transaction_id)` | btree | righe di un movimento di consumo |
| `ix_..._sale_item` | `(sale_item_public_id) WHERE sale_item_public_id IS NOT NULL` | btree parziale | consumi per articolo di catalogo (report) |
| `ix_..._property` | `(property_id)` | btree | consumi per stabilimento senza join sulla testata |


> ### ✅ Applicato nel backend (2026-09-21)
>
> La colonna **`line_total`** è ora `GENERATED ALWAYS AS (quantity * unit_price + variants_surcharge) STORED` ([`docs/05-ottimizzazioni-db.md`](../../../progettazione/05-ottimizzazioni-db.md) §5.1),
> migration `AddDbOptimizationsPropertyIdAndGeneratedColumns`.
>
> **È un cambio di contratto, non solo di schema**: PostgreSQL *rifiuta* un `INSERT`/`UPDATE` che
> valorizzi esplicitamente la colonna. Il codice applicativo che oggi la scrive va rimosso, non
> adattato. È la conversione meglio giustificata della lista: la scheda segnalava un bug storico del legacy in cui il totale arrivava spesso `null` o incoerente con le sue componenti. Reso impossibile per costruzione.

## Campi

> ### ✅ Corretta il 2026-10-06 — la FK è su `transaction_id`
>
> Fino al 2026-10-06 la tabella aveva una seconda colonna **`wallet_transaction_id`**, *shadow property* creata da EF
> per la relazione owned (`OwnsMany`) perché la configurazione non diceva che la chiave esterna era
> `transaction_id`. Era lei a portare la FK reale verso la testata; `transaction_id`, la colonna usata da codice ed ETL,
> non aveva vincolo. Stesso difetto già corretto il 2026-09-15 per
> [`payment_ledger_entry_lines`](payment_ledger_entry_lines.md), non esteso allora alle altre figlie owned.
>
> Migration `FixOwnedItemsForeignKeys` di `Module.Payment`: configurazione con
> `WithOwner().HasForeignKey(...)`, `wallet_transaction_id` eliminata, FK `ON DELETE CASCADE` spostata su `transaction_id`. La
> migration verifica prima che le due colonne coincidano su ogni riga (si ferma altrimenti); il `Down`
> ripristina la colonna travasando i valori. Aggiornato anche il transform ETL, che scriveva entrambe.


### id
- **Legacy**: elemento del JSON `debit_details` (nessun id)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### transaction_id
- **Legacy**: `card_transactions.id` (la riga JSON stava sulla transazione)
- **Descrizione**: movimento di debito a cui l'articolo appartiene.
- **Tipo**: `bigint`, `NOT NULL`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK interna** → [`payment_wallet_transactions.id`](payment_wallet_transactions.md);
  `ON DELETE CASCADE`; ammesso solo su movimenti `movement_type = debit`.

### property_id
- **Legacy**: derivato dalla testata
- **Descrizione**: stabilimento, denormalizzato dalla testata per aggregare per riga e per stabilimento
  senza join ([`docs/05-ottimizzazioni-db.md`](../../../progettazione/05-ottimizzazioni-db.md) §10).
- **Tipo**: `uuid` (= `public_id` di `catalog_properties`).
- **Default**: nessuno. (Fino al 2026-10-06 c'era `DEFAULT '00000000-…'::uuid`, il segnaposto che EF mette
  aggiungendo una colonna `NOT NULL` a una tabella piena: faceva passare in silenzio una riga senza
  stabilimento. Tolto dal consolidamento delle migration — [`docs/42-migration.md` §10](../../../progettazione/42-migration.md).)
- **Nullabile**: No.
- **Relazioni**: **`public_id` (uuid) copiato** da C2 [`catalog_properties`](../catalog/catalog_properties.md),
  nessuna FK cross-schema; deve coincidere con quello della testata (garanzia applicativa).

### sale_item_public_id
- **Legacy**: nessuno (`debit_details.name` era testo libero da `selectedItemForCard.ServiceName`)
- **Descrizione**: articolo di catalogo consumato, quando risolvibile.
- **Tipo**: `uuid` (= `public_id` di `catalog_service_sale_items`).
- **Default**: `NULL`.
- **Nullabile**: Sì (articolo non a catalogo / non risolto).
- **Relazioni**: **`public_id` (uuid) copiato** da C2 [`catalog_service_sale_items`](../catalog/catalog_service_sale_items.md).
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 34 righe storiche: **nessuna mapping spec tocca questa colonna**. Per lo storico migrato resta vuota e la popola il nuovo backend; se la riga «Legacy» qui sopra indica una fonte, vale la pena verificare perché non sia stata mappata. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### description_snapshot
- **Legacy**: `debit_details.name` (`string`, es. `"Espresso"`, `"Lettino"`)
- **Descrizione**: nome dell'articolo, **congelato** al momento del consumo — regola `_snapshot`: nome
  copiato da un'entità mutabile (`catalog_service_sale_items`) con la FK viva `sale_item_public_id`
  accanto ([`_feature-tabelle.md` → C1-7](../_feature-tabelle.md)).
- **Tipo**: `varchar(160)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: snapshot; se `sale_item_public_id` è valorizzato, il nome "vivo" si legge da C2
  [`catalog_service_sale_items`](../catalog/catalog_service_sale_items.md).

### quantity
- **Legacy**: `debit_details.serviceCount` (`number`; nei dati anche `0` e `22`)
- **Descrizione**: quantità consumata.
- **Tipo**: `integer`.
- **Default**: `1`.
- **Nullabile**: No.
- **Relazioni**: `CHECK (quantity > 0)` — le righe legacy con `0` si scartano/normalizzano.

### unit_price
- **Legacy**: `debit_details.price` (`string`, decimali eccessivi)
- **Descrizione**: prezzo unitario dell'articolo al momento del consumo.
- **Tipo**: `numeric(12,2)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: arrotondato a 2 decimali in migrazione.

### variants_description
- **Legacy**: `debit_details.selectedVariantData[].variantnames`
- **Descrizione**: varianti scelte in forma leggibile congelata (es. `"Macchiato"`). `NULL` se nessuna
  variante. **Niente suffisso `_snapshot`**: non c'è un'entità "variante" modellata né una FK viva
  accanto (la regola C1-7 lega `_snapshot` a un nome copiato da un'entità mutabile con FK viva) — qui è
  testo congelato proprio della riga-scontrino, come `stripe_status` su una transazione.
- **Tipo**: `varchar(255)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —; non filtrato per campo.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 34 righe storiche: **nessuna mapping spec tocca questa colonna**. Per lo storico migrato resta vuota e la popola il nuovo backend; se la riga «Legacy» qui sopra indica una fonte, vale la pena verificare perché non sia stata mappata. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### variants_surcharge
- **Legacy**: `debit_details.selectedVariantData[].variantvalues` (es. `"0.30"`)
- **Descrizione**: somma dei sovrapprezzi delle varianti su questa riga.
- **Tipo**: `numeric(12,2)`.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: incluso in `line_total`.

### line_total
- **Legacy**: `debit_details.totalPrice` (`number`: `count * price + Σ(varianti)`)
- **Descrizione**: totale della riga.
- **Tipo**: `numeric(12,2)`, `GENERATED ALWAYS AS (quantity * unit_price + variants_surcharge) STORED`
  (riquadro in «Indici»).
- **Default**: — (colonna generata).
- **Nullabile**: No.
- **Relazioni**: `Σ(line_total)` = `payment_wallet_transactions.amount` del movimento; discrepanze
  legacy annotate in migrazione.

### created_at / updated_at / deleted_at
- **Legacy**: da `card_transactions.createdAt` / `updatedAt` / —
- **Descrizione**: creazione / modifica. `deleted_at` per uniformità: append-only con il movimento.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF su `deleted_at` (sempre `NULL`).

## Valutazione JSON

`debit_details` (oggetto) → questa tabella. `selectedVariantData` (array annidato) → 2 colonne
(`variants_description` testo congelato + `variants_surcharge` numerico): la forma per-variante non ha
consumatori che la interroghino, quindi non giustifica una terza figlia.

## Suggerimenti per le prestazioni

- Volume minimo (~50 movimenti di consumo storici, poche righe ciascuno). Solo indice su
  `transaction_id`.
- `sale_item_public_id` indicizzato parziale per l'eventuale report "consumi tessera per articolo".

## Implementazione .NET

`WalletTransactionItem` è un **owned type** (collezione owned `Items`) dell'entità
`WalletTransaction` ([`payment_wallet_transactions`](payment_wallet_transactions.md)), area `Wallet` in
`Module.Payment`, schema `payment`: nessuna identità pubblica, nessun repository proprio, ammessa solo su
movimenti `MovementType = Debit`. Nessun enum proprio.

### `WalletTransactionItemConfiguration`

Applicata dentro `WalletTransactionConfiguration.OwnsMany`:
`builder.OwnsMany(x => x.Items, items => WalletTransactionItemConfiguration.Configure(items))`, con
`builder.WithOwner().HasForeignKey(x => x.TransactionId)` nella configurazione della figlia (senza, EF crea
una colonna FK propria: riquadro in «Campi»).

```csharp
namespace Module.Payment.Infrastructure.Configurations;

public sealed class WalletTransactionItemConfiguration : IEntityTypeConfiguration<WalletTransactionItem>
{
	public void Configure(EntityTypeBuilder<WalletTransactionItem> builder)
	{
		builder.ToTable("wallet_transaction_items", "payment", t => t.HasComment(
			"Articolo consumato pagato con un movimento di debito del borsellino (es. una consumazione al bar), congelato al momento del consumo."));

		builder.HasKey(x => x.Id).HasName("pk_payment_wallet_transaction_items");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.TransactionId)
			.HasColumnName("transaction_id")
			.IsRequired()
			.HasComment("Movimento di debito a cui l'articolo appartiene.");

		builder.Property(x => x.SaleItemPublicId)
			.HasColumnName("sale_item_public_id")
			.HasComment("public_id di catalog.service_sale_items, copiato — nessuna FK cross-schema; articolo di catalogo consumato, quando risolvibile.");

		builder.Property(x => x.DescriptionSnapshot)
			.HasColumnName("description_snapshot")
			.HasMaxLength(160)
			.IsRequired()
			.HasComment("Nome dell'articolo congelato al momento del consumo; il nome vivo si legge da C2 quando sale_item_public_id è valorizzato.");

		builder.Property(x => x.Quantity)
			.HasColumnName("quantity")
			.HasDefaultValue(1)
			.IsRequired()
			.HasComment("Quantità consumata.");

		builder.Property(x => x.UnitPrice)
			.HasColumnName("unit_price")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComment("Prezzo unitario dell'articolo al momento del consumo.");

		builder.Property(x => x.VariantsDescription)
			.HasColumnName("variants_description")
			.HasMaxLength(255)
			.HasComment("Varianti scelte in forma leggibile congelata; testo libero della riga-scontrino, non uno snapshot di un'entità modellata.");

		builder.Property(x => x.VariantsSurcharge)
			.HasColumnName("variants_surcharge")
			.HasColumnType("numeric(12,2)")
			.HasDefaultValue(0m)
			.IsRequired()
			.HasComment("Somma dei sovrapprezzi delle varianti su questa riga, inclusa in line_total.");

		builder.Property(x => x.LineTotal)
			.HasColumnName("line_total")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComment("Totale della riga (quantity * unit_price + variants_surcharge).");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Esposto per uniformità: append-only con il movimento, sempre NULL.");

		builder.HasIndex(x => x.TransactionId)
			.HasDatabaseName("ix_payment_wallet_transaction_items_transaction");

		builder.HasIndex(x => x.SaleItemPublicId)
			.HasFilter("sale_item_public_id IS NOT NULL")
			.HasDatabaseName("ix_payment_wallet_transaction_items_sale_item");
	}
}
```

### Relazioni

`transaction_id` è la chiave dell'owned collection verso `WalletTransaction` (stesso schema `payment`,
gestita da EF tramite `OwnsMany`). `sale_item_public_id` è un `uuid` copiato da C2
[`catalog_service_sale_items`](../catalog/catalog_service_sale_items.md): solo `.HasComment`, nessuna FK
cross-schema.

Nessun `ISoftDeletable`/`IAuditable` dedicato: owned type, segue il ciclo di vita del movimento;
`deleted_at` resta per uniformità di schema ma sempre `NULL`.

### Note di migrazione

- `JSON.parse(debit_details)` → una riga per articolo; bonifica `serviceCount = 0` (scartare/normalizzare
  a `quantity = 1` con nota), `price` arrotondato a 2 decimali, `totalPrice = null` ricalcolato
  (`quantity * unit_price + variants_surcharge`).
- `selectedVariantData[]` → `variants_description` (join leggibile dei `variantnames`) +
  `variants_surcharge` (somma dei `variantvalues`); la struttura per-variante (~50 movimenti storici) non
  giustifica una terza figlia — se emergesse un requisito di analisi per-variante, si introduce
  `WalletTransactionItemVariant` in un secondo momento.
