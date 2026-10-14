# Documentazione del backend

Documentazione del nuovo backend .NET di SummerBooking (riscrittura del gestionale legacy Node.js).
Il codice sta in [`backend/summerbooking-backend/`](../../backend/summerbooking-backend/).

Le cartelle seguono l'ordine del lavoro: prima si è studiato il sistema legacy (`analisi-pre`), poi si
è disegnato il nuovo (`analisi-post`); `progettazione` raccoglie le scelte tecniche trasversali.

| Cartella | Contenuto |
|---|---|
| [`progettazione/`](progettazione/) | Decisioni di architettura e di implementazione del nuovo backend |
| [`analisi-pre/`](analisi-pre/) | Analisi del sistema legacy (AS-IS): tabelle, logica di business, azioni dal frontend |
| [`analisi-post/`](analisi-post/) | Il nuovo sistema (TO-BE): schema del database per modulo e logica di business nuova |

## progettazione/

Documenti numerati, uno per tema. Il numero raggruppa per area, non indica un ordine di lettura.

| Numeri | Area | Documenti principali |
|---|---|---|
| 00–03 | Impostazione | criticità del legacy e indicazioni ([01](progettazione/01-criticita-e-indicazioni-backend.md)), strategia di test ([02](progettazione/02-strategia-di-test.md)), gestione dati ([03](progettazione/03-gestione-dati.md)) |
| 05–07 | Database | ottimizzazioni ([05](progettazione/05-ottimizzazioni-db.md), [07](progettazione/07-ottimizzazioni-db-2.md)), partizioni e manutenzione ([06](progettazione/06-partizioni-manutenzione.md)) |
| 11–35 | Componenti trasversali | privacy, lingue e date, log, Redis, segreti, frontend offline, job, chatbot, MCP, observability, multitenant, integrazioni esterne |
| 40–54 | Operatività e codice | backup, migrazione dal legacy ([42](progettazione/42-migration.md), [43](progettazione/43-migration-procedure.md)), standard di codifica ([50](progettazione/50-standard-di-codifica.md)), prestazioni, database di test |
| 80–83 | Retention | regole ([80](progettazione/80-retention.md)), motore ([81](progettazione/81-retention-motore.md)), dati validati dal titolare ([82](progettazione/82-retention-data.md)), file ([83](progettazione/83-retention-file.md)) |
| 84–88 | Riferimento e API | dizionario dei termini ([84](progettazione/84-dizionario.md)), promemoria `PROMEMORIA-85-n` ([85](progettazione/85-promemoria.md)), modulo Audit ([86](progettazione/86-modulo-audit.md)), API Identity ([87](progettazione/87-api-identity.md)), API Audit ([88](progettazione/88-api-audit.md)) |
| 92 | Sicurezza | copie di sicurezza ([92](progettazione/92-copie-sicurezza.md)) |

- [`pre/`](progettazione/pre/): documenti di partenza: piano di refactoring, linee guida implementative
  .NET, autenticazione e autorizzazione, identificatori e UUID, evoluzione verso i microservizi.
- I `.docx` sono i documenti scambiati con il titolare (retention, dizionario, verifica licenze).

## analisi-pre/

Il sistema legacy com'è oggi, ricostruito dal codice e dai dati di produzione.

| Sottocartella | Contenuto |
|---|---|
| [`db/`](analisi-pre/db/) | Una scheda per tabella del database legacy (MySQL), con i dati reali di produzione; [`_feature-tabelle.md`](analisi-pre/db/_feature-tabelle.md) le raggruppa per funzionalità. `old/` contiene le versioni precedenti |
| [`business-logic/`](analisi-pre/business-logic/_indice.md) | La logica di business del legacy, per tipo di utente e area: calcoli, regole, eventi |
| [`azioni/`](analisi-pre/azioni/_indice.md) | Le azioni dell'utente dal frontend con input e output delle API, per definire le nuove firme |
| [`db-no-copied/`](analisi-pre/db-no-copied/_indice.md) | Le righe che la migrazione non ha copiato nel nuovo database e perché; i report sono generati dalla utility di migrazione |

## analisi-post/

Il nuovo sistema, per come è stato deciso.

| Sottocartella | Contenuto |
|---|---|
| [`db/`](analisi-post/db/) | Una scheda per tabella del nuovo schema PostgreSQL, divise per modulo (`audit`, `booking`, `catalog`, `chatbot`, `fiscalprint`, `fiskaly`, `identity`, `infra`, `notification`, `payment`, `privacy`, `switchboard`). Da leggere prima: [`_feature-tabelle.md`](analisi-post/db/_feature-tabelle.md) (convenzioni e domini), [`_dotnet-template.md`](analisi-post/db/_dotnet-template.md) (come una scheda diventa entità EF Core), [`_domande-committente.md`](analisi-post/db/_domande-committente.md) (decisioni D-nn) |
| [`bl/`](analisi-post/bl/) | Logica di business nuova, senza controparte nel legacy |
| [`docs/`](analisi-post/docs/) | Documenti per il frontend (`.docx`, `.xlsx`): contratti API, traduzioni |

## Link verso cartelle esterne al repo

Molti documenti, soprattutto in `analisi-pre`, citano il codice del sistema legacy (`back-pre`,
`front-pre` e gli altri frontend), i mockup e le utility di migrazione. Quelle cartelle non fanno
parte di questo repository: i link funzionano solo nella copia di lavoro locale, dove il repo si trova in
`repo/SB-2.0/` accanto a esse.
