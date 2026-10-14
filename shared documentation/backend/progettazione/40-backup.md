# Backup e disaster recovery

> Cosa si copia, con che meccanismo, con che frequenza e per quanto si conserva nel nuovo backend
> **.NET 10** su AWS. Copre database, storage oggetti, secret, infrastruttura come codice.
>
> Complemento a [../infra/infrastruttura-aws.md](../../../../../infra/infrastruttura-aws.md) (RDS PostgreSQL
> Multi-AZ, S3, regione `eu-south-1`), [16-redis.md](16-redis.md) (perché Redis non si backuppa),
> [54-database-di-test.md](54-database-di-test.md) (la pipeline di sanificazione riusa gli snapshot),
> [11-modulo-privacy.md §6](11-modulo-privacy.md) (retention: un backup non aggira gli obblighi di
> cancellazione) e [80-retention.md](80-retention.md).
>
> Per le copie che devono restare leggibili **fuori** da questo account AWS — altra AZ/altro account,
> altro server in Europa, server locale ad hoc — e per il rapporto fra copia e retention, vedi
> [92-copie-sicurezza.md](92-copie-sicurezza.md).
>
> **Documento vivo**: da aggiornare quando si definiscono RTO/RPO contrattuali o si aggiunge una
> regione di DR.

---

## 0. Principio guida

- **Un backup mai testato non è un backup.** Il restore drill periodico (§3) è parte della policy, non
  un extra.
- **Difesa su più assi**: il backup automatico gestito da AWS protegge dal guasto hardware; una copia
  logica indipendente e in un account separato protegge da errore umano e da compromissione
  dell'account.
- **Il backup non è retention.** Conservare un dato in uno snapshot non soddisfa un obbligo di
  conservazione applicativo né aggira un obbligo di cancellazione: sono piani distinti
  ([11-modulo-privacy.md §6](11-modulo-privacy.md), [80-retention.md](80-retention.md)).
- **Obiettivi indicativi** (da confermare come target formali): **RPO ≤ 5 min** (grazie al PITR),
  **RTO ≤ 2-4 h** per un ripristino completo in regione, più lungo per un DR cross-region.

---

## 1. Tabella dei backup

| Cosa | Meccanismo | Frequenza | Retention | Note |
|---|---|---|---|---|
| **Database RDS (PostgreSQL)** | Snapshot automatici + backup continuo dei log delle transazioni (**PITR**) | Snapshot giornaliero, PITR continuo | 7-35 giorni (configurabile) | Restore a un secondo preciso, non solo all'ultimo snapshot |
| **RDS — pre-migrazione** | Snapshot **manuale** | Prima di ogni migrazione EF Core rischiosa in produzione | Fino a conferma che la migrazione è stabile (es. 7 giorni), poi eliminabile | Rete di sicurezza indipendente dal ciclo automatico |
| **DB — export logico** | `pg_dump` schedulato | Settimanale | 30-90 giorni | Copia indipendente dal motore RDS; utile anche per rigenerare un ambiente di test ([54-database-di-test.md](54-database-di-test.md)). Conservare in **bucket/account separato** dal primario |
| **DB — copia cross-region** | Copia dello snapshot automatico in una **regione secondaria** | Settimanale (o a ogni snapshot, secondo budget) | Allineata alla retention degli snapshot | DR in caso di problema sull'intera regione `eu-south-1` |
| **S3** (media, upload, PDF scontrini/fatture) | **Versioning** del bucket + lifecycle policy (Glacier per oggetti vecchi) | Continuo (versioning), non a intervalli | Secondo la retention del dato (fiscale: anni; media utente: minore) | Protegge da overwrite/delete accidentale. **Non** dall'eliminazione volontaria dell'intero bucket → serve anche **MFA Delete** e, dove critico, replica cross-account |
| **Secrets Manager / SSM** | Versioning nativo del servizio | Automatico a ogni scrittura | Le versioni precedenti restano recuperabili finché non eliminate esplicitamente | Nessuna azione manuale ([17-secrets.md](17-secrets.md)) |
| **Redis** (se introdotto) | **Nessun backup** | — | — | È sempre una cache ricostruibile dalla fonte di verità; un backup di Redis è complessità inutile ([16-redis.md](16-redis.md)) |
| **Infrastruttura (IaC)** | Versionamento Git di `infra/summerbooking-aws.yaml` | A ogni modifica (commit) | Storia Git completa | È la "copia di sicurezza" dell'ambiente: ricostruzione da zero possibile in caso di disastro totale dell'account |
| **Prova di restore** | Restore drill in ambiente isolato | **Trimestrale** | — | Verifica che snapshot/export siano realmente ripristinabili e che l'RTO sia accettabile |

---

## 2. Storage oggetti (S3) — dettaglio

- **Versioning attivo** su tutti i bucket con dati non rigenerabili (media utente, PDF fiscali).
- **Lifecycle**: transizione a storage class più economiche (Standard-IA → Glacier) in base all'età;
  **expiration** delle *versioni non correnti* dopo N giorni per non pagare all'infinito la cronologia.
- **MFA Delete** sui bucket con documenti fiscali: impedisce la cancellazione di versioni/bucket senza
  secondo fattore.
- **Block Public Access** a livello di account e di bucket.
- Per i documenti fiscali (retention pluriennale, [11-modulo-privacy.md §6](11-modulo-privacy.md)):
  valutare **Object Lock** in modalità compliance per la durata dell'obbligo legale.

---

## 3. Restore drill (trimestrale)

1. Ripristino di uno snapshot RDS **recente** su un'istanza isolata (subnet dedicata, nessun accesso
   applicativo).
2. Ripristino di un **`pg_dump`** settimanale su una seconda istanza — verifica che anche il percorso
   indipendente dal motore RDS funzioni.
3. Smoke test: migrazioni EF `Database.Migrate` applicabili, query di conteggio sulle tabelle
   principali, avvio dell'app puntata all'istanza ripristinata.
4. **Misura dei tempi** (estrazione + restore + smoke) → confronto con l'RTO obiettivo; se sfora, si
   agisce (istanza più grande per il restore, snapshot più frequenti, ecc.).
5. Esito del drill loggato/archiviato (data, snapshot usato, tempi, problemi) — è la prova che il piano
   di continuità è vivo.

---

## 4. Disaster recovery cross-region

- **Scenario**: indisponibilità prolungata dell'intera regione `eu-south-1`.
- **Predisposizione minima oggi**: copia settimanale degli snapshot RDS e degli export `pg_dump` in una
  regione secondaria + IaC parametrico sulla regione (il `CloudFormation`/template si applica altrove
  senza modifiche manuali).
- **Non** un ambiente passivo sempre acceso (costo non giustificato a questa scala): il DR è "ricostruisci
  da IaC + ripristina l'ultimo snapshot cross-region", con RTO nell'ordine delle ore.
- Rivalutare un warm standby solo se emerge un requisito di continuità con RTO < 1 h.

---

## 5. Cosa NON si backuppa (e perché)

| Elemento | Perché no |
|---|---|
| Redis / cache in-memory | Ricostruibile dalla fonte di verità ([16-redis.md](16-redis.md)) |
| Log applicativi oltre l'archiviazione S3 già prevista | Sono già su CloudWatch → S3 ([15-logs.md](15-logs.md), [31-observability.md](31-observability.md)); non è "backup" ma archiviazione |
| Artefatti di build / immagini container | Rigenerabili dalla pipeline CI dal codice sorgente versionato |
| Working set offline del gestore (IndexedDB) | Copia locale volatile per lavoro offline, la fonte di verità è il server ([19-frontend-offline.md](19-frontend-offline.md)) |

---

## 6. Responsabilità e automazione

- Tutti i job di backup non gestiti nativamente da RDS/S3 (export `pg_dump`, copia cross-region,
  cleanup versioni) sono schedulati con lo stesso pattern dei job disaccoppiati dal web
  (EventBridge Scheduler → `ECS RunTask`, [../infra/infrastruttura-aws.md §3.7](../../../../../infra/infrastruttura-aws.md),
  [20-jobs.md](20-jobs.md)).
- Ogni esecuzione emette una metrica di esito e alimenta un alert "backup non eseguito nella finestra
  attesa" ([31-observability.md §6.2](31-observability.md)).
- Il template IaC è l'unico artefatto che descrive l'ambiente: va tenuto allineato alla realtà (nessuna
  modifica manuale in console che non venga riportata nel template).
