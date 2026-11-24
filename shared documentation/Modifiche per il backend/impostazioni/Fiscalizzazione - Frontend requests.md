# Fiscalizzazione - Frontend requests

Le informazioni e le modifiche che servono dal backend perché la pagina **Fiscalizzazione** del gestionale funzioni come
la vuole il frontend. Il documento è pensato per chi le implementa (anche un agente): ogni sezione dice cosa serve e
quali scelte vanno chieste allo sviluppatore del backend prima di scrivere codice.

La pagina è `/settings/taxation`: il riquadro "Stampante" e il riquadro "Scontrino elettronico", con i termini e
condizioni e, dopo l'accettazione, il form "Configurazione account". Questo documento riguarda solo il form
"Configurazione account".

## Riepilogo

| # | Metodo | Rotta | Cosa serve |
|---|---|---|---|
| 1 | GET + POST | lettura da definire e `/api/taxation/save-electronic-receipt-account` | Informazioni: lettura dell'account dello scontrino elettronico e controllo di versione sul salvataggio |

## Regole comuni

- **Le API della pagina**: rotte, corpi e risposte sono quelli del contratto del frontend (`docs/API_CONTRACTS.md` del
  frontend e `shared documentation/API_CONTRACTS.docx`, sezione `/taxation`).
- **Stabilimento**: lo prende dal token, come le altre API del gestionale: nessuna richiesta lo contiene.
- **JSON** in camelCase, con i nomi esatti di questo documento.
- **Errori** nel formato Problem Details del backend (`Boot/Web/ProblemDetailsWriter.cs`), con `code`, `traceId` ed
  `errors[]` quando ci sono.

## 1. Lettura dell'account dello scontrino elettronico

### Il problema

Il form "Configurazione account" ha Nome azienda, Indirizzo azienda, Numero civico, Codice postale, Città, Nazione,
Codice fiscale, Codice Agenzia Entrate, Partita IVA, PIN e Password (`ElectronicReceiptAccount` nel frontend). Salva
chiama `POST /api/taxation/save-electronic-receipt-account` con tutti i campi, ma non esiste una lettura dell'account:
il form parte sempre vuoto e Salva scrive quello che c'è nei campi sopra l'account salvato, senza averlo letto.
Così l'utente non vede i dati salvati prima di cambiarli, e i cambiamenti salvati nel frattempo da un altro utente si
perdono.

In tutte le altre pagine del gestionale vale la regola opposta: Modifica e Salva restano spenti finché la lettura non è
riuscita, Salva rimanda tutto il modello letto e un controllo di versione blocca il salvataggio sopra dati cambiati
dopo la lettura (`Impostazioni magazzino - Frontend requests.md`, sezione 7).

### Cosa c'è oggi nel backend

- Il modulo `Module.Fiskaly` ha solo dominio e persistenza: l'entità `FiskalyAccount`
  (`Module.Fiskaly/Domain/Entities/FiskalyAccount.cs:12-56`) sulla tabella `fiskaly.accounts`
  (`Module.Fiskaly/Infrastructure/Configurations/FiskalyAccountConfiguration.cs:12`). Non ha servizi né endpoint: nel
  backend non c'è nessuna rotta `/api/taxation`, quindi mancano sia la lettura sia il salvataggio dell'account.
- Il vecchio sistema aveva `get-account` e `update-account` (`shared documentation/backend/analisi-pre/business-logic/
  stabilimento/11-configurazione.md`, sezione 9).
- PIN e password Fisconline non stanno nella tabella: `fisconline_credentials_secret_ref` punta a un segreto con
  `{ "pin", "password" }`, e `fisconline_updated_at` è la data dell'ultimo cambio
  (`shared documentation/backend/analisi-post/db/fiskaly/fiskaly_accounts.md`).

### Domande per lo sviluppatore

L'agente le chiede allo sviluppatore prima di scrivere codice; le risposte decidono rotte e campi.

1. **La lettura**: ci sarà una lettura dell'account, come `get-account` del vecchio sistema? Con quale rotta? Il
   frontend propone `GET /api/taxation/electronic-receipt-account`, senza parametri, che risponde con gli stessi campi
   del corpo di `save-electronic-receipt-account` tranne `pin` e `password`, che non arrivano mai al browser, e con un
   campo che dice se le credenziali Fisconline sono salvate (come `hasSmtpPassword` di Configurazione email). Un solo
   campo per PIN e password insieme, visto che stanno nello stesso segreto, o due?
2. **I campi**: come corrispondono i campi del form alle colonne di `fiskaly.accounts`? La proposta del frontend, da
   confermare:

   | Campo del form | Colonna |
   |---|---|
   | `companyName` | `taxpayer_legal_name` o `taxpayer_trade_name`? |
   | `companyAddress` | `taxpayer_address_street` |
   | `streetNumber` | `taxpayer_address_street_number` |
   | `postalCode` | `taxpayer_address_postal_code` |
   | `city` | `taxpayer_address_city` |
   | `country` | `taxpayer_address_country_code` (codice ISO: nel form oggi è un testo libero) |
   | `taxCode` | `taxpayer_fiscal_code` o `fisconline_holder_fiscal_code`? |
   | `revenueAgencyCode` | nessuna colonna evidente: dove sta? |
   | `vatNumber` | `taxpayer_vat_number` |
   | `pin`, `password` | il segreto di `fisconline_credentials_secret_ref` |

3. **I termini accettati**: la lettura può dire anche se i termini dello scontrino elettronico sono già stati accettati
   (`terms_accepted_at`)? Oggi la pagina lo ricorda solo per la visita in corso e a ogni apertura riparte dai termini.
4. **Il controllo di versione**: come nelle altre pagine, la lettura restituisce `version` (stringa opaca), il
   salvataggio la riceve nell'header `If-Match: "<version>"` e risponde con l'header `ETag` della nuova versione; se
   l'account è cambiato dopo la lettura risponde 409 `resource.version_conflict`, senza `errors[]` e senza salvare
   nulla. La versione copre i dati che la lettura restituisce, non PIN e password: il frontend non li legge mai e li
   manda solo quando l'utente ne scrive di nuovi, quindi un cambio delle sole credenziali non deve far fallire il
   salvataggio di un altro utente. Come calcolarla (contatore, hash dei dati, `xmin`...) lo decide lo sviluppatore.

### Cosa farà il frontend

Quando la lettura ci sarà, il form si comporterà come quelli delle altre pagine: all'apertura legge l'account, con i
campi bloccati e Modifica spento finché la lettura non è riuscita; Salva rimanda tutto il modello letto, con le
modifiche dell'utente, nell'header `If-Match` la versione letta; con 409 `resource.version_conflict` compare l'avviso
che un altro utente ha cambiato i dati e il form rilegge l'account. Fino alle risposte il frontend resta com'è.

## Verifica

1. `GET` della lettura per uno stabilimento con l'account configurato: 200 con i dati salvati e `version`, senza PIN e
   password.
2. `GET`, poi `POST /api/taxation/save-electronic-receipt-account` con `If-Match` uguale alla `version` letta: 204 con
   l'header `ETag` della nuova versione.
3. Due `POST` con la stessa `If-Match`: il primo 204, il secondo 409 `resource.version_conflict`, e l'account resta
   quello del primo.
4. `POST` che cambia solo PIN o password: 204 con `ETag` uguale alla versione letta.
