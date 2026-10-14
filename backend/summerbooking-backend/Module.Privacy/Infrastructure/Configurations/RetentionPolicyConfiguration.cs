using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Privacy.Domain.Entities;

namespace Module.Privacy.Infrastructure.Configurations;

public sealed class RetentionPolicyConfiguration : IEntityTypeConfiguration<RetentionPolicy>
{
	public void Configure(EntityTypeBuilder<RetentionPolicy> builder)
	{
		builder.ToTable("privacy_retention_policies", "privacy", t =>
		{
			t.HasComment(
				"Configurazione dichiarativa di retention: su schema.tabella (campo/i X) oppure su un " +
				"prefisso di storage, calcolato dalla data Y, oltre la durata Z, esegui l'azione W. Letta " +
				"dal job del modulo proprietario, mai eseguita da qui. Ambito piattaforma, valida per " +
				"tutti gli stabilimenti.");

			// Una policy ha O una tabella O un prefisso di storage, mai entrambi, mai nessuno dei due.
			// table_name usa la stringa vuota come "assente" (la colonna e' NOT NULL dall'origine), e la
			// stessa convenzione vale per reference_date_column sulle policy su prefisso.
			t.HasCheckConstraint("ck_privacy_retention_policies_target_xor",
				"(table_name <> '' AND storage_prefix IS NULL) " +
				"OR (storage_prefix IS NOT NULL AND storage_prefix <> '' AND table_name = '')");

			// Su un prefisso si puo' solo agire sull'oggetto: non c'e' nessuna riga da cancellare.
			t.HasCheckConstraint("ck_privacy_retention_policies_prefix_action",
				"storage_prefix IS NULL OR action IN ('delete_object', 'archive_object')");

			// Una policy su prefisso deve dire da dove prende la data, e una policy su tabella non deve.
			t.HasCheckConstraint("ck_privacy_retention_policies_prefix_date",
				"(storage_prefix IS NULL) = (storage_date_source IS NULL)");

			// Ogni policy deve avere UNA decorrenza, da qualche parte: una colonna data sulla riga, un
			// evento, oppure la data dell'oggetto su storage. reference_date_column e' NOT NULL
			// dall'origine e usa la stringa vuota come "nessuna colonna locale" - la stessa convenzione
			// di table_name sulle policy su prefisso. Senza questo vincolo si potrebbe inserire una
			// regola che non scade mai, e sarebbe indistinguibile da una regola scritta male.
			t.HasCheckConstraint("ck_privacy_retention_policies_has_decorrenza",
				"reference_date_column <> '' OR reference_event IS NOT NULL OR storage_date_source IS NOT NULL");

			// Il perimetro e' un oggetto JSON con una forma chiusa: o dichiara "tutte le righe", o dice
			// su quale colonna e con quale operatore filtrare. Il vincolo prende gli errori di battitura
			// al momento del seed, invece di farli scoprire al motore la notte in cui gira.
			t.HasCheckConstraint("ck_privacy_retention_policies_target_filter_shape",
				"target_filter IS NULL OR (jsonb_typeof(target_filter) = 'object' AND " +
				"(target_filter ? 'scope' OR (target_filter ? 'column' AND target_filter ? 'op')))");

			// Una policy su PREFISSO non ha righe da filtrare: il perimetro, la', e' il prefisso stesso.
			t.HasCheckConstraint("ck_privacy_retention_policies_target_filter_table",
				"target_filter IS NULL OR table_name <> ''");

			// Un file che segue le righe ha senso solo quando le righe vengono ARCHIVIATE. Su una
			// cancellazione non servirebbe (la policy su file agisce per conto suo) e su un
			// azzeramento non vorrebbe dire niente.
			t.HasCheckConstraint("ck_privacy_retention_policies_linked_asset_action",
				"linked_asset IS NULL OR action = 'archive_cold'");

			// Forma chiusa, come target_filter: gli errori di battitura si prendono al seed.
			t.HasCheckConstraint("ck_privacy_retention_policies_linked_asset_shape",
				"linked_asset IS NULL OR (jsonb_typeof(linked_asset) = 'object' AND " +
				"linked_asset ? 'column' AND linked_asset ? 'table' AND linked_asset ? 'action')");
		});

		builder.HasKey(x => x.Id).HasName("pk_privacy_retention_policies");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.HasComment("Id pubblico per il pannello di revisione SystemAdmin/DPO.");

		builder.Property(x => x.SchemaName)
			.HasColumnName("schema_name")
			.HasMaxLength(63)
			.IsRequired()
			.HasComment("Schema PostgreSQL del modulo proprietario della tabella target — riferimento " +
				"testuale, non FK (punta fuori da privacy per definizione), validato contro module_code.");

		builder.Property(x => x.TableName)
			.HasColumnName("table_name")
			.HasMaxLength(63)
			.IsRequired()
			.HasComment("Tabella target nella forma canonica relativa allo schema (senza prefisso modulo).");

		builder.Property(x => x.FieldNames)
			.HasColumnName("field_names")
			.HasColumnType("varchar(120)[]")
			.HasDefaultValueSql("'{}'")
			.IsRequired()
			.HasComment("Campo/i a cui si applica l'azione; array vuoto = l'intera riga (rilevante per " +
				"delete_row/archive_cold).");
		// string[] (non List<string>): implementa IStructuralComparable, richiesto perche' il campo
		// entra nell'indice unico composito uq_privacy_retention_policies_target qui sotto.

		builder.Property(x => x.ReferenceDateColumn)
			.HasColumnName("reference_date_column")
			.HasMaxLength(63)
			.IsRequired()
			.HasComment("Colonna sorgente della data di riferimento da cui decorre la durata. Stringa " +
				"vuota = nessuna colonna locale la porta: la decorrenza la danno reference_event (un fatto " +
				"accaduto altrove, come la cessazione dello stabilimento) oppure storage_date_source (la " +
				"data dell'oggetto su storage, per le policy su prefisso).");

		builder.Property(x => x.DurationValue)
			.HasColumnName("duration_value")
			.HasComment("Durata numerica della conservazione, validata positiva in applicazione.");

		builder.Property(x => x.DurationUnit)
			.HasColumnName("duration_unit")
			.HasColumnType("privacy.retention_duration_unit")
			.HasComment("Unità della durata: days, months, years o seasons (stagioni, risolte dal job " +
				"annuale sui confini di catalog.pricing_seasons — non equivalenti a 12 mesi).");

		builder.Property(x => x.Action)
			.HasColumnName("action")
			.HasColumnType("privacy.retention_action")
			.HasComment("Azione da applicare al superamento della durata: delete_row, null_fields, " +
				"anonymize, archive_cold, delete_object (rimuove l'oggetto dallo storage) o " +
				"archive_object (lo sposta su classe fredda).");

		builder.Property(x => x.OnlyIfUnreferenced)
			.HasColumnName("only_if_unreferenced")
			.HasDefaultValue(false)
			.HasComment("Se true, l'azione su storage si esegue solo quando nessun'altra riga nomina " +
				"quel file: la stessa foto puo' stare su due vetrine e lo stesso allegato su due " +
				"campagne, e la scadenza della prima non lo rende inutile. Ignorato dalle azioni su riga.");

		builder.Property(x => x.ReferenceEvent)
			.HasColumnName("reference_event")
			.HasColumnType("privacy.retention_reference_event")
			.HasComment("Evento da cui decorre la durata, in alternativa alla colonna data: " +
				"property_ceased (derivato da identity.account_subscriptions, a database quello stato " +
				"non esiste), asset_unreferenced o asset_replaced. Fa parte della chiave naturale, " +
				"perche' due policy sulla stessa tabella con la stessa azione si distinguono per l'evento " +
				"che le fa scattare.");

		builder.Property(x => x.StoragePrefix)
			.HasColumnName("storage_prefix")
			.HasMaxLength(255)
			.HasComment("Prefisso di storage su cui agisce la policy quando nessuna riga nomina i file " +
				"(QR delle prenotazioni, file orfani). ATTENZIONE: non e' il prefisso a decidere, e' " +
				"l'assenza di riferimento; il prefisso limita solo DOVE guardare. Il job deve accettare " +
				"solo prefissi da elenco chiuso e rifiutare qualunque prefisso antenato di un altro noto.");

		builder.Property(x => x.StorageDateSource)
			.HasColumnName("storage_date_source")
			.HasColumnType("privacy.retention_storage_date_source")
			.HasComment("Da dove prende la data una policy su prefisso: oggi solo object_last_modified, " +
				"il LastModified dell'oggetto su S3 usato come data approssimata del fatto che lo ha " +
				"prodotto. Valorizzato se e solo se storage_prefix lo e'.");

		builder.Property(x => x.TargetFilter)
			.HasColumnName("target_filter")
			.HasColumnType("jsonb")
			.HasComment("QUALI righe della tabella riguarda la policy, quando non sono tutte. NULL = non " +
				"dichiarato. Due forme: {\"scope\": \"whole_table\"} per dire DELIBERATAMENTE tutte, " +
				"oppure {\"column\": \"kind\", \"op\": \"=\", \"value\": \"rendered_pdf\"} per un " +
				"filtro (op ammessi: = <> in 'not in' 'is null' 'is not null'). ⚠️ Su un'azione su storage " +
				"questa colonna o only_if_unreferenced DEVONO essere valorizzate, altrimenti il motore " +
				"salta la policy: su un file un perimetro indovinato non si annulla. I valori sono " +
				"limitati a token identificativi, vedi RetentionEngine.BuildTargetFilter.");

		builder.Property(x => x.LinkedAsset)
			.HasColumnName("linked_asset")
			.HasColumnType("jsonb")
			.HasComment("Il file che deve SEGUIRE le righe archiviate da questa policy, nella forma " +
				"{\"column\": \"pdf_asset_id\", \"table\": \"media_assets\", \"action\": " +
				"\"archive_object\"}. Riguarda le sole policy archive_cold. ⚠️ Serve perche' archiviare " +
				"una riga e lasciare il suo file dov'era lascia il file sullo storage senza nessuna riga " +
				"calda che lo nomini, e con la data da cui scade uscita dal database insieme alla riga: " +
				"non resta niente che possa decidere quando cancellarlo. Il file viene anche " +
				"inventariato in privacy_archive_manifests con object_kind = 'linked_asset', cosi' la " +
				"policy decennale su quell'inventario lo cancella insieme all'archivio che accompagnava.");

		builder.Property(x => x.LegalBasis)
			.HasColumnName("legal_basis")
			.HasColumnType("text")
			.IsRequired()
			.HasComment("Riferimento normativo o motivazione di policy aziendale del termine scelto.");

		builder.Property(x => x.BasisSource)
			.HasColumnName("basis_source")
			.HasColumnType("privacy.retention_basis_source")
			.HasComment("Qualifica la fonte del termine: law (obbligo verificato), recommended_policy " +
				"(proposta da approvare) o disputed (qualificazione non risolta, non attivare).");

		builder.Property(x => x.IsDryRunOnly)
			.HasColumnName("is_dry_run_only")
			.HasDefaultValue(true)
			.HasComment("Se true, il job produce solo un report senza eseguire l'azione — obbligatorio " +
				"prima di abilitare l'esecuzione reale.");

		builder.Property(x => x.IsActive)
			.HasColumnName("is_active")
			.HasDefaultValue(true)
			.HasComment("Policy attiva/sospesa: permette di sospendere una regola senza cancellarla.");

		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.HasDefaultValueSql("now()")
			.HasComment("Creazione della regola.");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.HasDefaultValueSql("now()")
			.HasComment("Ultima modifica della regola.");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Soft-delete della policy (regola ritirata) — Global Query Filter EF standard.");

		builder.HasIndex(x => x.PublicId)
			.IsUnique()
			.HasDatabaseName("uq_privacy_retention_policies_public_id");

		// uq_privacy_retention_policies_target NON e' dichiarato qui: e' creato in SQL dalle migration
		// perche' ha bisogno di NULLS NOT DISTINCT, che la Fluent API non sa esprimere (eccezione
		// prevista: SQL raw nella migration).
		//
		// Forma: UNIQUE (schema_name, table_name, field_names, action, reference_event, storage_prefix,
		//        target_filter) NULLS NOT DISTINCT WHERE deleted_at IS NULL
		//
		// Perche' ogni colonna e' nella chiave naturale:
		//  - action: la politica validata dal titolare e' spesso a DUE FASI sullo stesso target (es.
		//    booking.reservations archiviata oltre N-2 e cancellata a 10 anni; idem payment.pos_charges,
		//    payment.ledger_entries, fiskaly.receipts). Senza action la seconda fase violerebbe l'indice;
		//  - reference_event: due policy sulla stessa tabella con la stessa azione si distinguono per
		//    l'evento che le fa scattare (le foto scadono sia quando nessuno le usa piu' sia alla
		//    cessazione dello stabilimento, e sono due regole diverse);
		//  - storage_prefix: le policy su prefisso hanno table_name vuoto e collidereb­bero tutte fra loro.
		//  - target_filter: la stessa tabella puo' tenere righe con durate diverse (le tre categorie di
		//    audit.trail_entries); jsonb si confronta per valore, quindi l'ordine delle chiavi non conta.
		//
		// Perche' NULLS NOT DISTINCT e non colonne nullable semplici: in PostgreSQL i NULL sono distinti
		// in un indice unico, quindi due righe identiche con reference_event e storage_prefix entrambi
		// NULL - il caso delle 44 policy preesistenti - NON collidereb­bero, e la protezione che l'indice
		// esiste per dare sparirebbe. Chi aggiunge un indice a questa tabella lo dichiari qui sopra; chi
		// tocca QUESTO indice lo faccia in SQL, consapevolmente.

		builder.HasIndex(x => new { x.SchemaName, x.TableName })
			.HasFilter("is_active AND deleted_at IS NULL")
			.HasDatabaseName("ix_privacy_retention_policies_lookup");
	}
}
