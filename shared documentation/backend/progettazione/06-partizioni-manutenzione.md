# Partizioni — manutenzione e scadenza

> Cosa va fatto, e **entro quando**, perché le tabelle partizionate introdotte il 2026-09-21 continuino
> a funzionare. Nasce da una scadenza concreta: le partizioni create arrivano a **fine 2030**.
>
> Presuppone [`05-ottimizzazioni-db.md` §2 e §14.1](05-ottimizzazioni-db.md) (perché si è partizionato e
> cosa significa "gestire manualmente") e [`80-retention.md` §6](80-retention.md) (i job che potano i
> dati vecchi). Qui si parla solo della **manutenzione della struttura**, non della politica di
> conservazione.

---

## 0. In una riga

**Entro il 2030 qualcuno deve creare le partizioni del 2031**, oppure i dati di quell'anno finiranno
tutti nella partizione di default — non si perde niente, ma si perde il beneficio per cui il
partizionamento esiste.

---

## 1. Cosa è partizionato, oggi

| Tabella | Chiave di partizione | Granularità | Partizioni esistenti |
|---|---|---|---|
| `booking.resource_consumptions` | `consumption_date` | anno solare | `_y2021` … `_y2035` + `_default` |
| `booking.resource_availability_daily` | `consumption_date` | anno solare | `_y2021` … `_y2035` + `_default` |

Create oggi dalla `InitialCreate` di `Module.Booking` (helper `PartizioniAnnuali`), che dal
2026-10-06 consolida le due migration che le avevano introdotte: `PartitionResourceTablesByYear`
(2021-2030) e `ExtendPartitionsAndResaleOfferExclude` (2031-2035, aggiunte il 2026-09-30). Vedi
[`42-migration.md` §10](42-migration.md).

**Perché anno solare e non stagione**: le stagioni sono definite per stabilimento
([`catalog_pricing_seasons`](../analisi-post/db/catalog/catalog_pricing_seasons.md)), quindi non esiste
un confine unico su cui tagliare. L'anno solare è l'unico criterio condiviso da tutte le property.

**Perché fino al 2030 e non oltre**: dieci anni è un compromesso fra due errori opposti. Poche
partizioni scadono presto e obbligano a intervenire sotto pressione; troppe le si crea a vuoto — ogni
partizione è una tabella vera, con il suo costo in cataloghi di sistema e nel tempo di pianificazione
delle query che le attraversano tutte. Dieci copre l'orizzonte in cui il progetto verrà comunque
rivisto, e le prime (2021-2024, insieme meno di 15.000 righe) si potranno staccare molto prima.

---

## 2. La scadenza del 2030 — cosa succede davvero

Il punto delicato è che **non succede un errore**. È il motivo per cui va scritto qui: un problema che
fallisce rumorosamente si scopre da solo, uno che degrada in silenzio no.

Il §14.1 di `05-ottimizzazioni-db.md` avvertiva che *"se nessuno crea la partizione del 2029 prima che
arrivi il 2029, il primo `INSERT` di quell'anno fallisce"*. Nella migration applicata **non è così**,
perché è stata aggiunta una **partizione di default**:

```sql
CREATE TABLE booking.resource_consumptions_default
  PARTITION OF booking.resource_consumptions DEFAULT;
```

Con la default presente, un `INSERT` con `consumption_date` nel 2031 non fallisce: la riga finisce lì.
È stata una scelta deliberata — in produzione una prenotazione rifiutata perché *nessuno ha creato una
partizione* sarebbe un danno molto peggiore di una query lenta — ma ha un costo che va capito:

| Senza partizione di default | Con partizione di default (scelta adottata) |
|---|---|
| Il 1° gennaio 2031 le scritture **falliscono** | Le scritture **continuano**, i dati finiscono nella default |
| Ci si accorge subito, nel modo peggiore | Non se ne accorge nessuno |
| Nessuna perdita di prestazioni, perché non si scrive | La default cresce senza limite e **non beneficia del pruning** |

Il rischio si sposta quindi dalla disponibilità alle prestazioni, e va sorvegliato attivamente (§4).

### Il vincolo che rende urgente accorgersene

C'è una seconda conseguenza, meno ovvia: **finché la partizione di default contiene righe di un certo
anno, non si può più creare la partizione di quell'anno.** PostgreSQL rifiuta:

```
ERROR: updated partition constraint for default partition would be violated by some row
```

Rimediare richiede di svuotare la default (spostare le righe fuori, creare la partizione, rimetterle
dentro) — un'operazione con lock su una tabella grande, cioè esattamente ciò che non si vuole fare di
fretta a gennaio. **Creare le partizioni in anticipo costa niente; recuperare dopo costa una finestra di
manutenzione.**

---

## 3. Come aggiungere le partizioni dell'anno

Operazione veloce e non bloccante: `CREATE TABLE ... PARTITION OF` su un intervallo futuro non tocca
dati esistenti.

```sql
-- da eseguire una volta l'anno, con qualche anno di margine
CREATE TABLE booking.resource_consumptions_y2031
  PARTITION OF booking.resource_consumptions
  FOR VALUES FROM ('2031-01-01') TO ('2032-01-01');

CREATE TABLE booking.resource_availability_daily_y2031
  PARTITION OF booking.resource_availability_daily
  FOR VALUES FROM ('2031-01-01') TO ('2032-01-01');
```

Gli indici e la chiave primaria **non** vanno ricreati: sono dichiarati sulla tabella padre e PostgreSQL
li propaga automaticamente a ogni nuova partizione.

Va fatto come **migration EF Core** (`migrationBuilder.Sql`), non a mano sul database: altrimenti gli
ambienti divergono e il prossimo che ricrea il DB da zero non ha la partizione. Vedi
[`43-migration-procedure.md`](43-migration-procedure.md).

### Dove va questa responsabilità, a regime

Tre strade, in ordine di costo crescente:

1. **Una migration ogni pochi anni.** È lo stato attuale. Costa zero finché qualcuno se ne ricorda — ed
   è precisamente il punto debole: il promemoria è questo documento e il controllo del §4.
2. **Un job annuale** (`EnsureFuturePartitions`, dicembre) che crea le partizioni mancanti per tutte le
   tabelle partizionate. Si affianca ai job di [`20-jobs.md` §1.5](20-jobs.md) e toglie la dipendenza
   dalla memoria di una persona. È la strada giusta quando le tabelle partizionate saranno più di due.
3. **`pg_partman`**, che automatizza creazione e potatura. Ha senso solo con molte tabelle partizionate:
   per due tabelle è un'estensione in più da installare, aggiornare e capire in emergenza.

**Oggi: strada 1.** Si passa alla 2 quando si partiziona una terza tabella.

> **Terza tabella partizionata (2026-10-07): `audit.trail_entries`**, per **mese** su `occurred_at`, partizioni
> da ottobre 2026 a dicembre 2028 più la default (migration `InitialCreate` di `Module.Audit`,
> [86-modulo-audit.md](86-modulo-audit.md)). La soglia della strada 2 è raggiunta: il job
> `EnsureFuturePartitions` va scritto insieme all'host dei job, e con partizioni mensili il margine è di
> anni, non di decenni. Nuove partizioni: stesso schema di nome `trail_entries_y<anno>m<mese>`.

---

## 4. Il controllo che fa scattare l'allarme

Da eseguire periodicamente (idealmente come health check o come parte del job notturno). Risponde a due
domande: *quanti anni di margine restano* e *la default si sta popolando*.

```sql
SELECT parent.relname                                        AS tabella,
       max(substring(child.relname FROM '_y(\d{4})$'))::int  AS ultimo_anno_coperto,
       max(substring(child.relname FROM '_y(\d{4})$'))::int
         - extract(year FROM CURRENT_DATE)::int              AS anni_di_margine
FROM pg_inherits i
JOIN pg_class child  ON child.oid  = i.inhrelid
JOIN pg_class parent ON parent.oid = i.inhparent
WHERE child.relname ~ '_y\d{4}$'
GROUP BY parent.relname
ORDER BY 3;
```

```sql
-- la partizione di default deve restare VUOTA: una riga qui e' un campanello
SELECT count(*) AS righe_in_default FROM booking.resource_consumptions_default;
SELECT count(*) AS righe_in_default FROM booking.resource_availability_daily_default;
```

**Soglie proposte:**

| Condizione | Livello | Azione |
|---|---|---|
| `anni_di_margine >= 3` | ok | nessuna |
| `anni_di_margine < 3` | attenzione | pianificare la migration che aggiunge gli anni mancanti |
| `anni_di_margine < 1` | allarme | aggiungere le partizioni **subito**, prima di gennaio |
| righe nella partizione di default `> 0` | allarme | una partizione manca già, o esistono date anomale: indagare **prima** di creare la partizione mancante (potrebbe non essere più creabile, §2) |

> Righe nella default non significano automaticamente "partizione mancante": possono essere date
> palesemente sbagliate (un `consumption_date` nel 1970 o nel 2099 da un import). In quel caso la
> correzione è sul dato, non sulla struttura — ed è un'altra ragione per guardarci dentro invece di
> creare la partizione e basta.

---

## 5. L'altra metà: staccare le partizioni vecchie

Il partizionamento non serve solo a rendere veloci le letture: rende **istantanea la potatura**. È il
punto di [`05-ottimizzazioni-db.md` §4](05-ottimizzazioni-db.md) (`DELETE` di massa vs `DROP PARTITION`).

Oggi il job `SeasonalArchive` ([`80-retention.md` §6](80-retention.md)) pota i contatori di consumo
oltre N-1 con una `DELETE`. Su una tabella partizionata per anno c'è una strada molto più economica:

```sql
-- niente DELETE riga per riga, niente bloat, niente VACUUM: la tabella smette di esistere
ALTER TABLE booking.resource_consumptions DETACH PARTITION booking.resource_consumptions_y2021;
DROP TABLE booking.resource_consumptions_y2021;
```

`DETACH` + `DROP` costa quanto cancellare un file; una `DELETE` equivalente riscrive ogni riga, gonfia la
tabella e lascia lavoro al `VACUUM`.

**Attenzione al disallineamento fra le due granularità.** La retention è espressa in **stagioni**
(N-1), le partizioni in **anni solari**: una partizione si può staccare solo quando *tutta* la stagione
che contiene è fuori finestra. In pratica si stacca l'anno `X` quando la stagione `X` è interamente
scaduta, cioè un giro dopo rispetto a una `DELETE` per data. È il prezzo dell'anno solare, ed è la
ragione per cui il job deve continuare a saper fare *anche* la `DELETE` per i casi al confine — non è
un'alternativa secca.

Le partizioni 2021-2024 (insieme meno di 15.000 righe) sono già oggi candidate al distacco: non
rallentano niente, ma sono il caso ideale per collaudare la procedura prima di applicarla a una
partizione da mezzo milione di righe.

---

## 6. Riepilogo delle regole

1. **Le partizioni arrivano a fine 2030**; il margine va controllato, non ricordato a memoria.
2. La **partizione di default** è una rete di sicurezza per la disponibilità, non un posto dove i dati
   debbano stare: una riga lì dentro è un allarme.
3. Le partizioni nuove si creano **con una migration**, mai a mano sul database.
4. Indici e PK **non** si ricreano per partizione: sono sul padre e si propagano.
5. La PK delle tabelle partizionate è `(id, consumption_date)` — requisito PostgreSQL, non una scelta di
   modellazione. Chi aggiunge una FK verso queste tabelle deve saperlo.
6. Potare significa **`DETACH` + `DROP`**, non `DELETE`, quando l'intervallo coincide con una partizione.
7. Alla terza tabella partizionata, si scrive il job annuale invece di continuare a mano.
