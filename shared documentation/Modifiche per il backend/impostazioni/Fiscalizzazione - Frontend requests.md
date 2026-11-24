# Fiscalizzazione - Frontend requests

Le modifiche che il backend deve fare perché la pagina **Fiscalizzazione** del gestionale funzioni come la vuole il
frontend. Il documento è pensato per chi le implementa (anche un agente): ogni sezione dice cosa creare, con payload,
risposta, regole ed errori attesi, e quali scelte vanno chieste allo sviluppatore prima di scrivere codice.

La pagina è `/settings/taxation`: il riquadro "Stampante" con la tabella delle stampanti, e il riquadro "Scontrino
elettronico" con i termini e condizioni, la guida e, dopo l'accettazione, il form "Configurazione account".

## Riepilogo

| # | Metodo | Rotta | Cosa fa |
|---|---|---|---|
| 1 | POST | `/api/fiscalprint/printers/printer-list` | Elenco delle stampanti per la tabella, a pagine |
| 2 | POST | `/api/fiscalprint/printers/add-printer` | Aggiunge una stampante |
| 3 | POST | `/api/fiscalprint/printers/set-is-active` | Attiva o disattiva una stampante |
| 4 | POST | `/api/fiscalprint/printers/delete-printer` | Elimina una stampante |
| 5 | GET | `/api/fiskaly/electronic-receipt` | Il testo dei termini e condizioni |
| 6 | POST | `/api/fiskaly/electronic-receipt-activation` | Accetta i termini |
| 7 | POST | `/api/fiskaly/save-electronic-receipt-account` | Salva l'account; con le domande su lettura, campi e versione |
| 8 | POST | `/api/fiskaly/download-guide-electronic-receipt` | Scarica la guida |

Sono tutte API nuove.

## Regole comuni

- **Moduli**: `Module.FiscalPrint` (schema `fiscalprint`) per le stampanti e `Module.Fiskaly` (schema `fiskaly`) per lo
  scontrino elettronico. Oggi hanno dominio e persistenza, ma nessun endpoint (`Module.FiscalPrint/Module.cs`,
  `Module.Fiskaly/Module.cs`); Fiskaly ha anche `InactiveReservationReceipts`, che non cambia.
- **Rotte**: gruppi `/api/fiscalprint` e `/api/fiskaly`, con i `GroupName` che `Boot/Program.cs` ha già fra gli
  `ApiDocs` (`fiscalprint`, `fiskaly`), registrati accanto a `app.MapPaymentEndpoints()`.
- **Stabilimento**: lo prende dal token (`ICurrentUser.PropertyId`, come `PropertyOf` in
  `Module.Payment/Endpoints/Endpoints.cs`): nessuna richiesta lo contiene. Ogni API legge e cambia solo i dati di quello
  stabilimento. Token senza stabilimento: 403 `fiscalprint.property_required` o `fiskaly.property_required` (nuovi, come
  `payment.property_required`).
- **Permesso** di tutte: `catalog.settings` (`Permissions.CatalogSettings`), come le altre impostazioni dello stabilimento.
  Senza: 403 `auth.forbidden`. Senza token valido: 401 `auth.unauthenticated`.
- **JSON** in camelCase, con i nomi esatti di questo documento. Gli id sono UUID.
- **Errori** nel formato Problem Details del backend (`Boot/Web/ProblemDetailsWriter.cs`), con `code`, `traceId` ed
  `errors[]` quando ci sono.
- **Ordine dei controlli**: 401, 403 `auth.forbidden`, 400 `validation.failed`, 403 `…property_required`, poi gli errori della
  sezione.
- **Scontrino elettronico abilitato** (sezioni 5, 6 e 7): lo stabilimento deve avere un account non chiuso (`deleted_at` `null`) con
  `is_enabled` `true`, l'interruttore che gestisce il systemadmin (commento in `FiskalyAccountConfiguration`); altrimenti 403 `fiskaly.not_enabled`, come
  faceva il vecchio sistema con `fiskalyEnabled`. La guida (sezione 8) si scarica anche senza. Oggi nessuna API del backend
  cambia `is_enabled` (il vecchio sistema aveva `set-fiskaly-enabled`): finché non c'è, l'abilitano solo i dati portati dal
  vecchio sistema. Come si abilita uno stabilimento lo decide lo sviluppatore.
- **Registro**: le azioni nuove vanno in `AuditActionCodes` con categoria `Operator` (come le altre): i codici sono nelle
  sezioni. Una voce per modifica riuscita, nessuna dopo un errore. Il frontend le aggiungerà alle azioni del Log.

## 1. POST /api/fiscalprint/printers/printer-list

La tabella della pagina: il frontend la chiama all'apertura, al cambio di pagina o di lingua e dopo ogni azione.

### Richiesta

Il corpo è la richiesta standard delle tabelle del frontend. La tabella non ha ricerca, filtri né ordinamenti:

```json
{
  "page": 1,
  "pageSize": 5,
  "search": "",
  "filters": [],
  "sortField": "",
  "sortDirection": "Ascending"
}
```

| Campo | Tipo | Regola |
|---|---|---|
| `page` | number (intero) | Da 1 |
| `pageSize` | number (intero) | Sempre 5 |
| `search` | string | Sempre `""` |
| `filters` | array | Sempre `[]` |
| `sortField` | string | Sempre `""` |
| `sortDirection` | string | Sempre `Ascending` |

Altri valori: 400 `validation.failed` con `validation.invalid` sul campo. Il frontend non li manda.

### Risposta

200: `total` conta tutte le stampanti dello stabilimento, `rows` è la pagina richiesta.

```json
{
  "total": 2,
  "rows": [
    {
      "idPrinter": "0199a1b2-3c4d-7e5f-8a6b-7c8d9e0f1a2b",
      "printerName": "EPSON - 1",
      "printerIP": "192.0.2.10",
      "printerMode": "fiscal",
      "isEnabled": false
    },
    {
      "idPrinter": "0199a1b2-3c4d-7e5f-8a6b-7c8d9e0f1a2c",
      "printerName": "EPSON - 2",
      "printerIP": "192.0.2.11",
      "printerMode": "orders",
      "isEnabled": true
    }
  ]
}
```

| Campo | Tipo | Da `fiscalprint.printer_configs` (`PrinterConfig`) |
|---|---|---|
| `idPrinter` | string (UUID) | Id pubblico, nuovo (vedi sotto) |
| `printerName` | string | `name` |
| `printerIP` | string \| null | `host`; `null` per una stampante USB (`connection_kind` `usb`) |
| `printerMode` | string | `mode`: `fiscal`, `non_fiscal`, `orders`, `text` (sezione 2) |
| `isEnabled` | boolean | `is_active` |

- **Id pubblico**: `PrinterConfig` ha solo l'id interno `bigint`. Serve `public_id uuid NOT NULL` con indice unico, valorizzato
  (UUIDv7) anche per le righe esistenti, eliminate comprese, come `20261025193541_OnlineBookingOptions` per i limiti di
  Prenotazioni online. L'id interno non esce mai.
- **Ordine**: per nome, con i numeri confrontati come numeri ("EPSON - 2" prima di "EPSON - 10") e senza distinguere
  maiuscole e accenti; a parità, per id interno. Come ottenerlo lo decide lo sviluppatore (`Impostazioni magazzino - Frontend
  requests.md`, sezione 1).
- Le stampanti eliminate (`deleted_at`) non compaiono: lo fa già il filtro globale (`ApplySoftDeleteQueryFilter` nel
  `DbContext` del modulo).

## 2. POST /api/fiscalprint/printers/add-printer

"Salva" del popup "Aggiungi stampante".

### Richiesta

```json
{
  "printerName": "EPSON - 3",
  "printerIP": "192.0.2.12",
  "printerPort": 9100,
  "printerModel": "fp_81",
  "printerMode": "fiscal",
  "isSsl": false,
  "departmentVatRates": [22, 10, null, null, null],
  "firstHeader": "Bagno Esempio",
  "secondHeader": "Via del Mare 1",
  "footer": "Grazie e arrivederci"
}
```

| Campo | Tipo | Dove va | Regola |
|---|---|---|---|
| `printerName` | string | `name` | Obbligatorio, al massimo 120 caratteri |
| `printerIP` | string | `host` | Obbligatorio, al massimo 255 caratteri |
| `printerPort` | number (intero) | `port` | Obbligatorio, da 1 a 65535 |
| `printerModel` | string | `model` | Obbligatorio: `fp_81`, `fp_90`, `rch`, `tm_t88`, `tm_t20`, `online` (`PrinterModel`) |
| `printerMode` | string | `mode` | Obbligatorio: `fiscal`, `non_fiscal`, `orders`, `text` (`PrinterMode`, con il valore nuovo) |
| `isSsl` | boolean | `use_tls` | |
| `departmentVatRates` | (number \| null)[] | `fiscalprint.printer_departments` | Sempre 5 elementi: la posizione (1-5) è `dept_number`, il valore `vat_rate`; `null` = reparto senza aliquota, nessuna riga |
| `firstHeader` | string \| null | `heading_line_1` | Al massimo 160 caratteri |
| `secondHeader` | string \| null | `heading_line_2` | Al massimo 160 caratteri |
| `footer` | string \| null | `footer_line` | Al massimo 160 caratteri |

- **Modalità "Testo"**: `PrinterMode` oggi ha `Fiscal`, `NonFiscal` e `Orders`. Serve il valore nuovo `Text` (codice `text`), anche
  nell'enum `fiscalprint.printer_mode` del database, con la sua migration. Cosa stampa una stampante in questa modalità non
  riguarda queste API: l'agente lo chiede allo sviluppatore.
- **Reparti**: solo con `printerMode` `fiscal`; con le altre modalità sono cinque `null`. Le aliquote che non sono `null` sono tutte
  diverse (`uq_fiscalprint_printer_departments_rate`). Il frontend offre 0, 4, 5, 10 e 22.
- **Connessione**: sempre di rete, `connection_kind` `tcp` (il popup non ha le stampanti USB).
- La stampante nasce **spenta** (`is_active` `false`), con `created_by_user_id` l'utente del token e un `public_id` nuovo. I testi si
  salvano senza spazi ai lati e le lunghezze si contano così; un testo vuoto vale `null`.
- Stampante e reparti in **una transazione sola**.
- **Risposta**: 204 senza corpo; la tabella si rilegge.
- **Registro**: `fiscalprint.printer_created`.

| Status | `code` | Quando |
|---|---|---|
| 400 | `validation.failed` | Uno o più campi non validi, in `errors[]` (vedi sotto) |
| 403 | `fiscalprint.property_required` | Token senza stabilimento |

In `errors[]`:

| `field` | `code` | Quando |
|---|---|---|
| `printerName`, `printerIP`, `printerPort`, `printerModel`, `printerMode` | `validation.required` | Assenti, `null` o vuoti |
| `printerName`, `printerIP`, `firstHeader`, `secondHeader`, `footer` | `validation.max_length` (`args.max`) | Troppo lunghi |
| `printerPort` | `validation.out_of_range` (`args.from` 1, `args.to` 65535) | Fuori dai limiti |
| `printerModel`, `printerMode` | `validation.invalid` | Valore sconosciuto |
| `departmentVatRates` | `validation.required` | Assente o `null` |
| `departmentVatRates` | `validation.invalid` | Non 5 elementi, un'aliquota ripetuta, sotto 0 o sopra 100, o un'aliquota con una modalità diversa da `fiscal` |

Il frontend controlla prima le stesse regole e mostra ogni errore sotto il suo campo.

## 3. POST /api/fiscalprint/printers/set-is-active

L'interruttore della riga.

```json
{
  "idPrinter": "0199a1b2-3c4d-7e5f-8a6b-7c8d9e0f1a2b",
  "isEnabled": true
}
```

| Campo | Tipo | Regola |
|---|---|---|
| `idPrinter` | string (UUID) | `idPrinter` dell'elenco |
| `isEnabled` | boolean | Il nuovo stato, in `is_active` |

- **Una sola attiva per modalità**: accendendo una stampante si spegne, nella stessa transazione, quella dello stabilimento attiva
  con la stessa modalità (`mode`), se c'è. Prima si spegne e poi si accende: `uq_fiscalprint_printer_configs_active_mode` non è
  differibile. Le stampanti delle altre modalità non cambiano. Spegnerne una non ne accende altre.
- **Risposta**: 204 senza corpo, anche se la stampante è già in quello stato (allora niente voce nel registro). Riuscita o no,
  la tabella si rilegge e mostra lo stato di tutte.
- **Registro**: `fiscalprint.printer_updated`, con il nuovo stato fra i metadati; una voce anche per la stampante che si è spenta.
- Due chiamate insieme, anche per due stampanti della stessa modalità, non danno mai un 500: alla fine è attiva una sola.

| Status | `code` | Quando |
|---|---|---|
| 400 | `validation.failed` | `idPrinter` o `isEnabled` assenti o `null` (`validation.required`) |
| 403 | `fiscalprint.property_required` | Token senza stabilimento |
| 404 | `fiscalprint.printer_not_found` | Stampante che non esiste, eliminata o di un altro stabilimento |

## 4. POST /api/fiscalprint/printers/delete-printer

Il cestino della riga, dopo la conferma.

```json
{
  "idPrinter": "0199a1b2-3c4d-7e5f-8a6b-7c8d9e0f1a2b"
}
```

- **Eliminazione logica**: `deleted_at` valorizzato e `is_active` `false` (lo dice il commento della colonna in
  `PrinterConfigConfiguration`). Anche i suoi reparti hanno `deleted_at`: `SoftDeleteInterceptor` segna solo le entità caricate
  nel contesto, quindi vanno caricati con la stampante. Scontrini e numeratori (`fiscalprint.receipts`,
  `fiscalprint.serial_numbers`) restano.
- **Risposta**: 204 senza corpo; la tabella si rilegge.
- **Registro**: `fiscalprint.printer_deleted`, con il nome della stampante.

| Status | `code` | Quando |
|---|---|---|
| 400 | `validation.failed` | `idPrinter` assente o `null` (`validation.required`) |
| 403 | `fiscalprint.property_required` | Token senza stabilimento |
| 404 | `fiscalprint.printer_not_found` | Stampante che non esiste, già eliminata o di un altro stabilimento: niente cambia |

## 5. GET /api/fiskaly/electronic-receipt

I termini e condizioni dello scontrino elettronico, letti all'apertura della pagina e al cambio di lingua.

- **Richiesta**: nessun corpo e nessun parametro.
- **Risposta**: 200 con il solo testo HTML come stringa JSON (non un oggetto), `Content-Type: application/json`, nella lingua di
  `Accept-Language` (`IRequestLanguage`): `it`, `en`, `fr`, `es`, `de`, `zh`, `ar`. `null` se il testo non c'è: con `null` il
  frontend accetta sia il corpo `null` sia un corpo vuoto.

```json
"<h4>Generale</h4><p>Testo dei termini e condizioni…</p>"
```

- **Elementi**: titoli (`h4`), paragrafi, elenchi e link `https` o `mailto:`. Il frontend lo mostra ripulito da script, eventi e
  link `javascript:`.
- **Da dove viene**: nel database c'è solo la data dell'accettazione (`fiskaly.accounts.terms_accepted_at`), e anche il vecchio
  `get-terms` restituiva solo se erano accettati (`shared documentation/backend/analisi-pre/db/fiskaly_account.md`,
  `accept_terms`); il nuovo disegno (`analisi-pre/azioni/stabilimento/13-gestionale-impostazioni.md`, sezione 9) prevede anche
  il testo. Dove tenerlo, chi lo fornisce e in quali lingue lo decide lo sviluppatore: l'agente glielo chiede prima di scrivere
  codice. In una lingua che manca si risponde in italiano.
- **Scontrino non abilitato**: 403 `fiskaly.not_enabled` (vedi "Scontrino elettronico abilitato" nelle regole comuni). Al posto dei
  termini la pagina mostra il messaggio, senza le caselle né "Invia".
- Non scrive niente e non chiama Fiskaly. Se i termini sono già accettati lo dice la lettura dell'account (sezione 7, domanda 3).

| Status | `code` | Quando |
|---|---|---|
| 403 | `fiskaly.property_required` | Token senza stabilimento |
| 403 | `fiskaly.not_enabled` | Scontrino elettronico non abilitato per lo stabilimento |

## 6. POST /api/fiskaly/electronic-receipt-activation

"Invia" con "Accetto" spuntato.

```json
{
  "isElectronicReceiptAccepted": true
}
```

- Il frontend manda sempre `true` (con "Non accetto" il bottone è spento): `false`, `null` o assente danno 400
  `validation.failed` con `validation.invalid` su `isElectronicReceiptAccepted`.
- **Cosa fa**: scrive `terms_accepted_at` (e `updated_by_user_id`) sull'account dello stabilimento. Termini già accettati: 204,
  la data resta la prima e non c'è una voce nel registro.
- **Scontrino non abilitato**: 403 `fiskaly.not_enabled` e niente cambia. Non si crea nessun account (lo faceva il vecchio
  `updateTerms`): lo crea chi abilita lo scontrino.
- Non cambia `is_enabled` e non chiama Fiskaly. Due chiamate insieme non danno mai un 500 e la data è una sola.
- **Risposta**: 204 senza corpo; il frontend mostra il form "Configurazione account".
- **Registro**: `fiskaly.terms_accepted`.

| Status | `code` | Quando |
|---|---|---|
| 400 | `validation.failed` | Vedi sopra |
| 403 | `fiskaly.property_required` | Token senza stabilimento |
| 403 | `fiskaly.not_enabled` | Scontrino elettronico non abilitato per lo stabilimento |

## 7. POST /api/fiskaly/save-electronic-receipt-account

"Salva" del form "Configurazione account".

### Richiesta

```json
{
  "companyName": "Bagno Esempio Srl",
  "companyAddress": "Via del Mare",
  "streetNumber": "1",
  "postalCode": "00100",
  "city": "Roma",
  "country": "Italia",
  "taxCode": "XXXXXXXXXXXXXXXX",
  "revenueAgencyCode": "AE00000000",
  "vatNumber": "00000000000",
  "pin": "00000",
  "password": "password-digitata"
}
```

Tutti i campi sono `string | null`; le colonne sono nella domanda 2.

- Il frontend manda **sempre tutti i campi** e li vuole tutti compilati: assente, `null` o vuoto danno `validation.required`.
- **`pin` e `password`**: `null` mantiene il valore salvato, uno per uno (si può cambiare il PIN tenendo la password).
  `validation.required` solo se non ce n'è uno salvato. Stanno nel segreto di `fisconline_credentials_secret_ref`, mai nelle
  tabelle, nelle risposte, nei log o nel registro. Oggi `ISecretResolver` legge soltanto
  (`ConfigurationSecretResolver`): come scrivere il segreto lo decide lo sviluppatore. `fisconline_updated_at` cambia solo se
  cambia uno dei due.
- **Termini**: senza `terms_accepted_at` 409 `fiskaly.terms_not_accepted`, come il vecchio `updateAccount` ("Terms not
  accepted"): niente cambia.
- **Fiskaly**: i passi verso SIGN IT (organizzazione, titolare, sede, credenziali) sono del backend. Se falliscono la risposta è
  un errore, mai un 204: cosa resta salvato e come si riprende lo decide lo sviluppatore.
- **Risposta**: 204 senza corpo, con l'header `ETag` quando ci sarà il controllo di versione (domanda 4).
- **Registro**: `fiskaly.account_updated`, senza PIN e password.

| Status | `code` | Quando |
|---|---|---|
| 400 | `validation.failed` | Uno o più campi non validi, in `errors[]` con `field` uguale al nome nel corpo (`validation.required`, `validation.max_length` con la lunghezza della colonna) |
| 403 | `fiskaly.property_required` | Token senza stabilimento |
| 403 | `fiskaly.not_enabled` | Scontrino elettronico non abilitato per lo stabilimento: niente cambia |
| 409 | `fiskaly.terms_not_accepted` | Termini non accettati |
| 409 | `resource.version_conflict` | Account cambiato dopo la lettura (domanda 4) |
| 503 | `fiskaly.unavailable` | Fiskaly o l'archivio dei segreti non rispondono |

Gli altri errori di Fiskaly: domanda 5.

### La lettura che manca

Il form "Configurazione account" ha Nome azienda, Indirizzo azienda, Numero civico, Codice postale, Città, Nazione,
Codice fiscale, Codice Agenzia Entrate, Partita IVA, PIN e Password (`ElectronicReceiptAccount` nel frontend). Salva
chiama `POST /api/fiskaly/save-electronic-receipt-account` con tutti i campi, ma non esiste una lettura dell'account:
il form parte sempre vuoto e Salva scrive quello che c'è nei campi sopra l'account salvato, senza averlo letto.
Così l'utente non vede i dati salvati prima di cambiarli, e i cambiamenti salvati nel frattempo da un altro utente si
perdono.

In tutte le altre pagine del gestionale vale la regola opposta: Modifica e Salva restano spenti finché la lettura non è
riuscita, Salva rimanda tutto il modello letto e un controllo di versione blocca il salvataggio sopra dati cambiati
dopo la lettura (`Impostazioni magazzino - Frontend requests.md`, sezione 7).

### Cosa c'è oggi nel backend

- Il modulo `Module.Fiskaly` ha solo dominio e persistenza: l'entità `FiskalyAccount`
  (`Module.Fiskaly/Domain/Entities/FiskalyAccount.cs:12-56`) sulla tabella `fiskaly.accounts`
  (`Module.Fiskaly/Infrastructure/Configurations/FiskalyAccountConfiguration.cs:12`). Non ha endpoint, quindi mancano sia la
  lettura sia il salvataggio dell'account.
- Il vecchio sistema aveva `get-account` e `update-account` (`shared documentation/backend/analisi-pre/business-logic/
  stabilimento/11-configurazione.md`, sezione 9).
- PIN e password Fisconline non stanno nella tabella: `fisconline_credentials_secret_ref` punta a un segreto con
  `{ "pin", "password" }`, e `fisconline_updated_at` è la data dell'ultimo cambio
  (`shared documentation/backend/analisi-post/db/fiskaly/fiskaly_accounts.md`).

### Domande per lo sviluppatore

L'agente le chiede allo sviluppatore prima di scrivere codice; le risposte decidono rotte e campi.

1. **La lettura**: ci sarà una lettura dell'account, come `get-account` del vecchio sistema? Con quale rotta? Il
   frontend propone `GET /api/fiskaly/electronic-receipt-account`, senza parametri, che risponde con gli stessi campi
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

5. **Gli errori di Fiskaly e di Fisconline**: quali errori del servizio esterno arrivano al gestionale (dati del titolare
   rifiutati, PIN o password sbagliati, servizio che non risponde)? L'agente li scrive qui con status, `code` e, se
   riguardano un campo, `errors[]`, perché il frontend li mostri.

### Cosa farà il frontend

Quando la lettura ci sarà, il form si comporterà come quelli delle altre pagine: all'apertura legge l'account, con i
campi bloccati e Modifica spento finché la lettura non è riuscita; Salva rimanda tutto il modello letto, con le
modifiche dell'utente, nell'header `If-Match` la versione letta; con 409 `resource.version_conflict` compare l'avviso
che un altro utente ha cambiato i dati e il form rilegge l'account. Fino alle risposte il frontend resta com'è.

## 8. POST /api/fiskaly/download-guide-electronic-receipt

"Scarica la guida", nell'intestazione del riquadro "Scontrino elettronico".

- **Richiesta**: nessun corpo.
- **Risposta**: 200 con il file (binario, non JSON), `Content-Type` del file e il nome in `Content-Disposition`:

  ```
  Content-Type: application/pdf
  Content-Disposition: attachment; filename="guida-scontrino-elettronico.pdf"
  ```

  Se il backend sta su un altro dominio serve `Access-Control-Expose-Headers: Content-Disposition`.
- **Quale file**: la guida per chi configura lo scontrino elettronico, uguale per tutti gli stabilimenti. Nei documenti e nel
  codice del backend non ce n'è traccia: chi la fornisce, in che formato, in quali lingue (`Accept-Language`) e dove sta lo decide
  lo sviluppatore. Il frontend salva il file così com'è. Nessuna voce nel registro.

| Status | `code` | Quando |
|---|---|---|
| 403 | `fiskaly.property_required` | Token senza stabilimento |

## Verifica

Con un token di un utente dello stabilimento che può cambiare le impostazioni.

1. Ogni API con un token senza il permesso: 403 `auth.forbidden`; senza token: 401; con un token senza stabilimento: 403
   `fiscalprint.property_required` o `fiskaly.property_required`.
2. `printer-list` con 6 stampanti: `total` 6, 5 righe nella pagina 1 e 1 nella pagina 2, "EPSON - 2" prima di "EPSON - 10", id
   UUID, ognuna con la sua `printerMode`; nessuna stampante eliminata o di un altro stabilimento. Una stampante USB ha `printerIP` `null`. Stabilimento senza
   stampanti: `total` 0 e `rows` vuoto.
3. Dopo la migration ogni riga, eliminata compresa, ha un `public_id` diverso.
4. `set-is-active` spegne una stampante attiva: 204 e l'elenco ha `isEnabled` `false`. La stessa chiamata di nuovo: 204. Con l'id di
   un altro stabilimento, eliminato o inesistente: 404 `fiscalprint.printer_not_found` e niente cambia. Con due stampanti
   fiscali, una attiva, e una di comande attiva, accendere l'altra fiscale: 204, la prima fiscale è spenta e quella di comande
   resta accesa. Due accensioni insieme di due stampanti fiscali: nessun 500 e una sola attiva.
5. `delete-printer`: 204; la stampante non è più nell'elenco, `is_active` è `false`, i suoi reparti hanno `deleted_at`, i suoi
   scontrini restano. Di nuovo: 404 `fiscalprint.printer_not_found`.
6. `add-printer` con tutti i campi validi e `departmentVatRates` `[22, 10, null, null, null]`: 204, la stampante è nell'elenco spenta,
   con due reparti (1 → 22, 2 → 10). Con `printerMode` `text` e cinque `null`: 204. 400 con il suo `errors[]`: ogni campo
   obbligatorio vuoto, nome di 121 caratteri, indirizzo di 256, una riga di 161, porta 0 e 65536, modello e modalità
   sconosciuti, quattro elementi nei reparti, un'aliquota ripetuta, un'aliquota con `printerMode` `orders`; in ognuno niente
   cambia.
7. `GET electronic-receipt` con `Accept-Language` `it` e `en`: 200 con una stringa JSON. Con una lingua che non c'è: il testo
   italiano.
8. Senza account, con l'account chiuso e con `is_enabled` `false`: `electronic-receipt`, `electronic-receipt-activation` e
   `save-electronic-receipt-account` rispondono 403 `fiskaly.not_enabled` e niente cambia; `download-guide-electronic-receipt`
   risponde 200.
9. `electronic-receipt-activation` con `true`: 204 e `terms_accepted_at` valorizzato; di nuovo: 204 e la stessa data. Con `false` e
   senza il campo: 400 `validation.invalid`. Due chiamate insieme: nessun 500 e una sola data.
10. `save-electronic-receipt-account` senza termini accettati: 409 `fiskaly.terms_not_accepted`. Con un campo vuoto: 400
   `validation.required` su quel campo. Con `pin` `null` e credenziali salvate: 204 e il PIN resta quello di prima; senza
   credenziali salvate: 400 su `pin`. Nei log e nel registro non compaiono PIN e password.
11. Lettura e versione dell'account (domande 1 e 4): la `GET` risponde 200 con i dati salvati e `version`, senza PIN e password;
   `POST` con `If-Match` uguale a quella `version`: 204 con l'header `ETag` della nuova; due `POST` con la stessa `If-Match`: il
   primo 204, il secondo 409 `resource.version_conflict` e l'account resta quello del primo; un `POST` che cambia solo PIN o
   password: 204 con `ETag` uguale alla versione letta.
12. `download-guide-electronic-receipt`: 200 con il file, `Content-Type` e `Content-Disposition`.
13. Il registro ha una voce per ogni modifica riuscita e nessuna dopo un errore.
