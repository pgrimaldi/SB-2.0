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

## Prima di tutto

- **Enum nelle opzioni EF** (`PROMEMORIA-85-8` di `85-promemoria.md`, da chiudere prima della prima API di ogni modulo): in
  `Module.FiscalPrint` e `Module.Fiskaly` `MapEnum` sta solo sul data source (`ModulePersistenceRegistration.cs`,
  `DesignTimeDbContextFactory.cs`), non in `UseNpgsql`. EF tratta gli enum come interi (`Property<int>("Mode")` nello snapshot)
  e ogni lettura o scrittura di `PrinterConfig` e `FiskalyAccount` darebbe 500. Servono `FiscalPrintEnumMappings` e
  `FiskalyEnumMappings` come `Module.Payment/Infrastructure/Infras/PaymentEnumMappings.cs`, usati nel data source e in
  `UseNpgsql`, a runtime e a design-time. Non ne nasce nessuna migration.
- **Progetti**: `Module.FiscalPrint.csproj` e `Module.Fiskaly.csproj` non hanno `FrameworkReference Microsoft.AspNetCore.App`,
  FluentValidation 12.1.1 e il riferimento a `BuildingBlocks.Application`, come li ha `Module.Catalog`. Valgono le regole di
  `ArchitectureTests`: endpoint senza Infrastructure né entità, `DbContext` solo in Infrastructure, porte in
  `Application/Abstractions`, repository in `Infrastructure/Repositories`, errori in `FiscalPrintErrors` e `FiskalyErrors`.
- **Vecchi dati**: se l'importazione dal vecchio sistema va aggiornata (per il `public_id` della sezione 1) l'agente lo chiede
  allo sviluppatore: non è in questo repository.

## Regole comuni

- **Moduli**: `Module.FiscalPrint` (schema `fiscalprint`) per le stampanti e `Module.Fiskaly` (schema `fiskaly`) per lo
  scontrino elettronico. Oggi hanno dominio e persistenza, ma nessun endpoint (`Module.FiscalPrint/Module.cs`,
  `Module.Fiskaly/Module.cs`, con i commenti da aggiornare); Fiskaly ha anche `InactiveReservationReceipts`, che non cambia.
- **Rotte**: gruppi `/api/fiscalprint` e `/api/fiskaly`, con i `GroupName` che `Boot/Program.cs` ha già fra gli
  `ApiDocs` (`fiscalprint`, `fiskaly`), registrati con `MapFiscalPrintEndpoints` e `MapFiskalyEndpoints` accanto a
  `app.MapPaymentEndpoints()`.
- **Stabilimento**: lo prende dal token (`ICurrentUser.PropertyId`): nessuna richiesta lo contiene. Ogni API legge e cambia solo
  i dati di quello stabilimento. Token senza stabilimento: 403 `fiscalprint.property_required` o `fiskaly.property_required`
  (nuovi, come `payment.property_required`). Che lo stabilimento esista ancora non si controlla, come in `Module.Payment`:
  nessun 404.
- **Permesso** di tutte, anche delle letture: `catalog.settings` (`Permissions.CatalogSettings`: superadmin, admin, user1).
  Senza: 403 `auth.forbidden`. Senza token valido: 401 `auth.unauthenticated`.
- **JSON** in camelCase, con i nomi esatti di questo documento. Gli id sono UUID.
- **Errori** nel formato Problem Details del backend (`Boot/Web/ProblemDetailsWriter.cs`), con `code`, `traceId` ed
  `errors[]` quando ci sono.
- **Ordine dei controlli**: 401, 403 `auth.forbidden`, 400 sulle sole regole del corpo, 403 `…property_required`, poi gli errori
  della sezione nell'ordine della sua tabella. Si valida prima di leggere lo stabilimento, come `InventoryService.ListAsync`
  (`EnsureValid` e poi `IdAsync`): di `PropertyOf` (`Module.Payment/Endpoints/Endpoints.cs`) si copia solo il 403.
- **Valori che non si leggono**: un id che non è un UUID, un numero che non entra in un intero a 32 bit o un tipo sbagliato non
  arrivano ai validatori: 400 `validation.failed` senza `errors[]` (`BadHttpRequestException` in `ProblemDetailsWriter.cs`). Nei
  DTO i campi sono nullable (`Guid?`, `bool?`, `int?`, `string?`), così assente e `null` danno `validation.required`. Il frontend
  non manda questi valori.
- **Id che non si trovano**: `idPrinter` assente, `null`, inesistente, eliminato o di un altro stabilimento dà sempre 404
  `fiscalprint.printer_not_found`, mai un errore in `errors[]`.
- **Sessione impersonata** dal systemadmin: le sezioni 6 e 7 rispondono 403 `auth.forbidden` (`ICurrentUser.ImpersonatorUserId`, come
  `ForbidWhenImpersonated` in `MeService`): non accetta i termini e non salva le credenziali Fisconline. Nelle altre
  `created_by_user_id` e `updated_by_user_id` sono il titolare, e il registro porta l'impersonatore.
- **Scontrino elettronico abilitato** (sezioni 5, 6 e 7): lo stabilimento deve avere un account non chiuso (`deleted_at` `null`)
  con `is_enabled` `true`, altrimenti 403 `fiskaly.not_enabled`, come faceva il vecchio sistema con `fiskalyEnabled`. La guida
  (sezione 8) si scarica anche senza. `is_enabled` lo gestisce il systemadmin (`FiskalyAccountConfiguration.cs:60-61`; il
  commento della colonna, riga 52, dice "SuperAdmin", che nel backend è il titolare: va corretto). Il commento di `deleted_at`
  (riga 246) dice che un salvataggio riapre l'account: qui non vale, l'account chiuso dà 403, e il commento va corretto. Oggi
  nessuna API cambia `is_enabled`, e aggiungerla non fa parte di questo documento: finché non c'è valgono solo gli stabilimenti
  abilitati nel vecchio sistema. Come si abilita uno stabilimento lo decide lo sviluppatore: l'agente glielo chiede.
- **Codici nuovi**: `fiscalprint.property_required`, `fiscalprint.printer_not_found`, `fiskaly.property_required`,
  `fiskaly.not_enabled`, `fiskaly.terms_not_accepted`, `fiskaly.unavailable` (503: `new DomainException(code, 503)`),
  `fiskaly.guide_not_found`. `resource.version_conflict` è il codice comune chiesto anche dalle altre pagine (in
  `CommonErrorCodes`), non `booking.version_conflict`.
- **Registro**: classi `AuditActionCodes.FiscalPrint` (`Module` `fiscalprint`, `Targets.Printer` `printer`) e
  `AuditActionCodes.Fiskaly` (`Module` `fiskaly`, `Targets.Account` `account`), con le loro voci in `All` scritte come
  `new(codice, AuditCategory.Operator, <Classe>.Module)`: l'helper `Operator(...)` mette il modulo `identity`, e una voce con un
  modulo non previsto da `SourceModule` (`fiscalprint`, non `fiscal_print`) si scarta (`AuditEntryRecordedHandler`). Servono i
  testi `Action_…` nei 7 `AuditExport*.resx`. Le voci si scrivono con `IAuditTrail` dopo il salvataggio, come
  `ReservationPayments`, con `TargetId` uguale al `public_id`. Una voce per modifica riuscita, nessuna dopo un errore. Sono
  codici in più rispetto a `Gestione utenti - Frontend requests.md`, sezione 9 (`cash.*`); il frontend li aggiungerà alle
  azioni del Log.

| Azione | `code` | Metadati |
|---|---|---|
| Sezione 2 | `fiscalprint.printer_created` | `name`, `mode` |
| Sezione 3 | `fiscalprint.printer_updated` | `name`, `mode`, `isActive` |
| Sezione 4 | `fiscalprint.printer_deleted` | `name`, `mode` |
| Sezione 6 | `fiskaly.terms_accepted` | nessuno |
| Sezione 7 | `fiskaly.account_updated` | `credentialsChanged`; mai dati del titolare, PIN o password |

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

Altri valori: 400 `validation.failed` con `validation.invalid` sul campo, con un validatore proprio (5 non è fra i `PageSizes` di
`CatalogQueryRules`). Il frontend non li manda.

### Risposta

200: `total` conta tutte le stampanti dello stabilimento, `rows` è la pagina richiesta. Oltre l'ultima pagina `rows` è vuoto e
`total` resta quello vero: la tabella torna da sola all'ultima pagina (succede eliminando l'ultima riga). È un DTO con `total` e
`rows`, non `PagedResult`.

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
| `printerIP` | string \| null | `host`; `null` per una stampante USB (`connection_kind` `usb`), anche con `host` valorizzato |
| `printerMode` | string | `mode`: `fiscal`, `non_fiscal`, `orders`, `text` (sezione 2) |
| `isEnabled` | boolean | `is_active` |

- **Id pubblico**: `PrinterConfig` ha solo l'id interno `bigint`, che non deve uscire (`pre/identificatori-e-uuid.md`). Serve
  `public_id uuid NOT NULL` con indice unico `uq_fiscalprint_printer_configs_public_id`, `ValueGeneratedNever()` e
  `Guid.CreateVersion7()` nella factory, come `OnlineStayLimitConfiguration`. La migration lo valorizza per tutte le righe,
  eliminate comprese, prima di creare l'indice (`20261025193541_OnlineBookingOptions` parte da `Guid.Empty`). La scheda
  `fiscalprint_printer_configs.md`, che dice "Nessun `public_id`", è del collega: l'agente glielo segnala.
- **Ordine**: per nome, con i numeri confrontati come numeri ("EPSON - 2" prima di "EPSON - 10") e senza distinguere
  maiuscole e accenti; a parità il nome e poi l'id interno. Come ottenerlo lo decide lo sviluppatore, l'agente glielo chiede
  (`Impostazioni magazzino - Frontend requests.md`, sezione 1).
- Le stampanti eliminate (`deleted_at`) non compaiono: lo fa già il filtro globale (`ApplySoftDeleteQueryFilter` nel
  `DbContext` del modulo).

| Status | `code` | Quando |
|---|---|---|
| 400 | `validation.failed` | Un valore diverso da quelli della tabella |
| 403 | `fiscalprint.property_required` | Token senza stabilimento |

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
| `isSsl` | boolean | `use_tls` | Assente vale `false` |
| `departmentVatRates` | (number \| null)[] | `fiscalprint.printer_departments` | Sempre 5 elementi: la posizione (1-5) è `dept_number`, il valore `vat_rate`; `null` = reparto senza aliquota, nessuna riga |
| `firstHeader` | string \| null | `heading_line_1` | Al massimo 160 caratteri |
| `secondHeader` | string \| null | `heading_line_2` | Al massimo 160 caratteri |
| `footer` | string \| null | `footer_line` | Al massimo 160 caratteri |

- **Modalità "Testo"**: `PrinterMode` oggi ha `Fiscal`, `NonFiscal` e `Orders`. Serve il valore nuovo `Text = 3` (codice `text`),
  anche nell'enum `fiscalprint.printer_mode` del database, con una migration sua, separata da quella di `public_id`, generata da
  `dotnet ef` e senza righe che usano `text` (`03-gestione-dati.md`: `ALTER TYPE … ADD VALUE` va tenuto isolato; precedente
  `20261106065440_LedgerSourceReservationSplit`). Si aggiornano il summary di `PrinterMode` e il commento della colonna `mode`.
  Cosa stampa una stampante in questa modalità non riguarda queste API.
- **Reparti**: solo con `printerMode` `fiscal`; con le altre modalità sono cinque `null`. Le aliquote che non sono `null` sono tutte
  diverse (`uq_fiscalprint_printer_departments_rate`) e hanno al massimo due decimali (`numeric(5,2)`: 22,001 e 22,004 diventano
  uguali). L'errore è sempre su `departmentVatRates`, mai su un elemento. Il frontend offre 0, 4, 5, 10 e 22.
- **Nomi e indirizzi**: nomi uguali ammessi; il formato dell'indirizzo non si controlla (può essere un nome di rete).
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
| `departmentVatRates` | `validation.invalid` | Non 5 elementi, un'aliquota ripetuta, sotto 0, sopra 100 o con più di due decimali, o un'aliquota con una modalità valida diversa da `fiscal` |

Il frontend controlla prima le stesse regole, manda i testi senza spazi ai lati e mostra ogni errore sotto il suo campo (quello
di `departmentVatRates` sopra Salva quando i reparti non si vedono).

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
  differibile. Le stampanti delle altre modalità non cambiano. Spegnerne una non ne accende altre. `updated_by_user_id` è
  l'utente del token, anche su quella che si spegne.
- **Risposta**: 204 senza corpo, anche se la stampante è già in quello stato (allora niente voce nel registro). Riuscita o no,
  la tabella si rilegge e mostra lo stato di tutte.
- **Registro**: `fiscalprint.printer_updated`, con il nuovo stato fra i metadati; una voce anche per la stampante che si è spenta.
- **Chiamate insieme**, mai un 500: due accensioni di due stampanti della stessa modalità rispondono 204 tutte e due e resta
  attiva l'ultima; con un'eliminazione insieme 204 o 404, e una stampante eliminata non è mai attiva. Come serializzarle lo
  decide lo sviluppatore: l'agente glielo chiede.

| Status | `code` | Quando |
|---|---|---|
| 400 | `validation.failed` | `isEnabled` assente o `null` (`validation.required`) |
| 403 | `fiscalprint.property_required` | Token senza stabilimento |
| 404 | `fiscalprint.printer_not_found` | Stampante che non esiste, eliminata o di un altro stabilimento |

## 4. POST /api/fiscalprint/printers/delete-printer

Il cestino della riga, dopo la conferma.

```json
{
  "idPrinter": "0199a1b2-3c4d-7e5f-8a6b-7c8d9e0f1a2b"
}
```

- **Eliminazione logica**: `deleted_at` valorizzato, `updated_by_user_id` l'utente del token e `is_active` `false` (lo dice il
  commento della colonna in `PrinterConfigConfiguration`, ma oggi nessuno lo fa: lo azzera il metodo dell'entità). Anche i suoi
  reparti hanno `deleted_at`: `SoftDeleteInterceptor` segna solo le entità caricate
  nel contesto, quindi vanno caricati con la stampante. Scontrini e numeratori (`fiscalprint.receipts`,
  `fiscalprint.serial_numbers`) restano, gli scontrini fino alla loro conservazione (12 mesi).
- Due eliminazioni insieme: una 204 e una 404, una sola voce nel registro.
- **Risposta**: 204 senza corpo; la tabella si rilegge.
- **Registro**: `fiscalprint.printer_deleted`, con il nome della stampante.

| Status | `code` | Quando |
|---|---|---|
| 403 | `fiscalprint.property_required` | Token senza stabilimento |
| 404 | `fiscalprint.printer_not_found` | Stampante che non esiste, già eliminata o di un altro stabilimento: niente cambia |

## 5. GET /api/fiskaly/electronic-receipt

I termini e condizioni dello scontrino elettronico, letti all'apertura della pagina e al cambio di lingua.

- **Richiesta**: nessun corpo e nessun parametro.
- **Risposta**: 200 con il solo testo HTML come stringa JSON (non un oggetto), `Content-Type: application/json` (con
  `Results.Ok(testo)`: una stringa restituita da sola sarebbe `text/plain`), nella lingua di `Accept-Language` (`IRequestLanguage`, non
  `fiskaly.language_code`, che ha solo due lingue): `it`, `en`, `fr`, `es`, `de`, `zh`, `ar`. `null` se il testo non c'è: il
  frontend accetta sia il corpo `null` sia un corpo vuoto, dice che i termini non sono ancora disponibili e non li fa accettare.

```json
"<h4>Generale</h4><p>Testo dei termini e condizioni…</p>"
```

- **Elementi**: titoli (`h4`), paragrafi, elenchi e link `https` o `mailto:`. Il frontend lo mostra ripulito da script, eventi e
  link `javascript:`, e apre i link in una nuova scheda.
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
  `validation.failed` con `validation.must_accept` (`ValidationErrorCodes.MustAccept`) su `isElectronicReceiptAccepted`.
- **Cosa fa**: scrive `terms_accepted_at` (e `updated_by_user_id`) sull'account dello stabilimento. Termini già accettati: 204,
  la data resta la prima e non c'è una voce nel registro.
- **Scontrino non abilitato**: 403 `fiskaly.not_enabled` e niente cambia. Non si crea nessun account (lo faceva il vecchio
  `updateTerms`): lo crea chi abilita lo scontrino.
- Non cambia `is_enabled` e non chiama Fiskaly. Due chiamate insieme non danno mai un 500 e la data è una sola.
- **Risposta**: 204 senza corpo; il frontend mostra il form "Configurazione account".
- **Registro**: `fiskaly.terms_accepted`.

| Status | `code` | Quando |
|---|---|---|
| 403 | `auth.forbidden` | Utente senza il permesso, o sessione impersonata |
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
  "pin": "0000000000",
  "password": "password-digitata"
}
```

Tutti i campi sono `string | null`; le colonne sono nella domanda 2.

- Il frontend manda **sempre tutti i campi**. Tutti tranne `pin` e `password` sono obbligatori: assente, `null` o vuoto danno
  `validation.required`.
- **Testi** senza spazi ai lati (il frontend li toglie prima di mandarli). `pin` e `password` si tengono così come arrivano; vuoti o
  di soli spazi valgono `null`.
- **Lunghezze**: `companyName`, `companyAddress` e `city` 128, `streetNumber` 16, `postalCode` 5, `taxCode` 16, `vatNumber` 13, come
  le colonne; il frontend non lascia scrivere di più. `country`, `revenueAgencyCode`, `pin` e `password` non hanno un limite finché
  non c'è la risposta alla domanda 2: `taxpayer_address_country_code` è `char(2)`, e nel form la nazione è un testo libero come
  nell'esempio.
- **`pin` e `password`**: `null` mantiene il valore salvato, uno per uno (si può cambiare il PIN tenendo la password).
  `validation.required` solo se non ce n'è uno salvato. Stanno nel segreto di `fisconline_credentials_secret_ref`, mai nelle
  tabelle, nelle risposte, nei log o nel registro. Il valore scritto deve essere **rileggibile subito e da tutte le istanze**: oggi
  `ISecretResolver` legge soltanto, dalla configurazione (`ConfigurationSecretResolver`), che si aggiorna solo con un nuovo
  avvio (17-secrets.md, 3.2). Come scriverlo e rileggerlo lo decide lo sviluppatore: l'agente glielo chiede. Segreto che non si
  riesce a leggere: 503 `fiskaly.unavailable`. `fisconline_updated_at` cambia quando ne arriva almeno uno non `null`.
- **Termini**: senza `terms_accepted_at` 409 `fiskaly.terms_not_accepted`, come il vecchio `updateAccount` ("Terms not
  accepted"): niente cambia. La pagina riapre i termini e, accettati, torna al form con i dati scritti.
- **Scontrino non abilitato**: 403 `fiskaly.not_enabled`; la pagina mostra il messaggio al posto del form.
- **Fiskaly**: i passi verso SIGN IT (organizzazione, titolare, sede, credenziali) sono del backend, con le regole di
  `35-integrazioni-esterne.md` (contratto in `Application/Abstractions`, client in `Infrastructure/ExternalApi`, resilienza,
  `idempotencyKey`, provider mai chiamato dai test, checklist della sezione 7). Se falliscono la risposta è un errore, mai un
  204. L'agente chiede allo sviluppatore: cosa resta salvato e come si riprende, anche dopo un tempo scaduto; cosa succede con un
  account già in esercizio quando cambiano indirizzo o dati fiscali (nel vecchio sistema sede e titolare si dismettevano e si
  ricreavano); la cassa software e il ricalcolo di `fiscal_setup_level`, che non si scrive a mano.
- **Due salvataggi insieme**: mai un 500 e mai due entità su Fiskaly.
- **Versione**: finché non c'è la lettura (domande 1 e 4) il salvataggio ignora `If-Match` e non risponde `ETag`; il frontend oggi
  non lo manda. Dopo, senza `If-Match` o con uno vecchio risponde 409 `resource.version_conflict`, come nelle altre pagine.
- **Risposta**: 204 senza corpo, con l'header `ETag` quando ci sarà il controllo di versione. `updated_by_user_id` è l'utente del
  token.
- **Registro**: `fiskaly.account_updated`.

Errori, nell'ordine in cui si controllano:

| Status | `code` | Quando |
|---|---|---|
| 403 | `auth.forbidden` | Utente senza il permesso, o sessione impersonata |
| 400 | `validation.failed` | Campi obbligatori e lunghezze, in `errors[]` con `field` uguale al nome nel corpo (`validation.required`, `validation.max_length`) |
| 403 | `fiskaly.property_required` | Token senza stabilimento |
| 403 | `fiskaly.not_enabled` | Scontrino elettronico non abilitato per lo stabilimento: niente cambia |
| 409 | `fiskaly.terms_not_accepted` | Termini non accettati: niente cambia |
| 409 | `resource.version_conflict` | Account cambiato dopo la lettura (dalla domanda 4): niente cambia |
| 400 | `validation.failed` | `validation.required` su `pin` o `password` quando non ce n'è uno salvato |
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
   campo per PIN e password insieme, visto che stanno nello stesso segreto, o due? Con `catalog.settings`, e con 403
   `fiskaly.not_enabled` come la sezione 5? Cosa risponde con l'account abilitato ma mai configurato (tutti i campi `null`,
   anche la nazione, che nel database parte da `IT`), con i termini non accettati, con le credenziali mai scritte?
2. **I campi**: come corrispondono i campi del form alle colonne di `fiskaly.accounts`? La proposta del frontend, da
   confermare:

   | Campo del form | Colonna |
   |---|---|
   | `companyName` | `taxpayer_legal_name` (nel vecchio sistema era obbligatorio, e `taxpayer_trade_name` partiva uguale)? |
   | `companyAddress` | `taxpayer_address_street` |
   | `streetNumber` | `taxpayer_address_street_number` |
   | `postalCode` | `taxpayer_address_postal_code` |
   | `city` | `taxpayer_address_city` |
   | `country` | `taxpayer_address_country_code` (codice ISO: nel form oggi è un testo libero) |
   | `taxCode` | `taxpayer_fiscal_code` (codice fiscale o partita IVA del soggetto)? |
   | nessun campo | `fisconline_holder_fiscal_code`: il vecchio sistema lo voleva sempre, è il codice fiscale della persona abilitata a Fisconline. Da dove arriva? |
   | `revenueAgencyCode` | nessuna colonna evidente: dove sta? |
   | `vatNumber` | `taxpayer_vat_number` |
   | `pin`, `password` | il segreto di `fisconline_credentials_secret_ref` |

   Se servono campi nuovi (il codice fiscale della persona Fisconline) o la nazione come codice, li cambia il frontend.

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
6. **Formato dei campi**: il vecchio sistema controllava CAP (5 cifre), nazione, codice fiscale o partita IVA, partita IVA e PIN
   (10 cifre). Il backend li controlla ancora? La proposta è 400 con `validation.invalid` sul campo, che il frontend mostra
   sotto quel campo.

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

  con `Results.File(contenuto, tipo, nome)` come `PropertyAuditEndpoints`. Frontend e API stanno sulla stessa origine (`/api`): il
  browser legge `Content-Disposition` (e l'`ETag` della sezione 7) senza modifiche al CORS.
- **Quale file**: la guida per chi configura lo scontrino elettronico, uguale per tutti gli stabilimenti. Nei documenti e nel
  codice del backend non ce n'è traccia: chi la fornisce, in che formato, in quali lingue (`Accept-Language`) e dove sta lo decide
  lo sviluppatore: l'agente glielo chiede. Il frontend salva il file così com'è. Nessuna voce nel registro.

| Status | `code` | Quando |
|---|---|---|
| 403 | `fiskaly.property_required` | Token senza stabilimento |
| 404 | `fiskaly.guide_not_found` | La guida non c'è (ancora) |

## Verifica

Con un token di un utente dello stabilimento che può cambiare le impostazioni. Account abilitati o chiusi, credenziali salvate e
stampanti USB si preparano nel database di prova; Fiskaly è sempre un client finto.

1. Ogni API con un token senza il permesso: 403 `auth.forbidden`; senza token: 401; con un token senza stabilimento: 403
   `fiscalprint.property_required` o `fiskaly.property_required`. `ArchitectureTests` passano.
2. `printer-list` con 6 stampanti: `total` 6, 5 righe nella pagina 1, 1 nella pagina 2, nessuna nella 3 con `total` 6; "EPSON - 2"
   prima di "EPSON - 10", id UUID, ognuna con la sua `printerMode`; nessuna stampante eliminata o di un altro stabilimento. Una
   stampante USB ha `printerIP` `null`. Stabilimento senza stampanti: `total` 0 e `rows` vuoto. `page` 0, `pageSize` 10 e un
   filtro: 400 `validation.invalid` sul campo.
3. Dopo le migration ogni riga, eliminata compresa, ha un `public_id` diverso, e l'enum ha `text`.
4. `set-is-active` spegne una stampante attiva: 204 e l'elenco ha `isEnabled` `false`. La stessa chiamata di nuovo: 204. Senza
   `idPrinter`, con l'id di un altro stabilimento, eliminato o inesistente: 404 `fiscalprint.printer_not_found`; con un id che non
   è un UUID: 400 senza `errors[]`; senza `isEnabled`: 400. In ognuno niente cambia. Con due stampanti fiscali, una attiva, e
   una di comande attiva, accendere l'altra fiscale: 204, la prima fiscale è spenta e quella di comande resta accesa, con
   `updated_by_user_id` su tutte e due le fiscali. Due accensioni insieme di due stampanti fiscali: due 204 e una sola attiva.
5. `delete-printer`: 204; la stampante non è più nell'elenco, `is_active` è `false`, i suoi reparti hanno `deleted_at`, i suoi
   scontrini restano. Di nuovo: 404 `fiscalprint.printer_not_found`. Due eliminazioni insieme: una 204 e una 404. Insieme a
   un'accensione: nessun 500 e nessuna stampante eliminata attiva.
6. `add-printer` con tutti i campi validi e `departmentVatRates` `[22, 10, null, null, null]`: 204, la stampante è nell'elenco spenta,
   con due reparti (1 → 22, 2 → 10) e i testi senza spazi ai lati. Con `printerMode` `text` e cinque `null`: 204. Nome uguale a
   un'altra stampante: 204. 400 con il suo `errors[]`: ogni campo obbligatorio vuoto, nome di 121 caratteri, indirizzo di 256, una
   riga di 161, porta 0 e 65536, modello e modalità sconosciuti, reparti assenti, quattro elementi, un'aliquota ripetuta, -1,
   101 o 22,001, un'aliquota con `non_fiscal`, `orders` e `text`; in ognuno niente cambia.
7. `GET electronic-receipt` con `Accept-Language` `it` e `en`: 200 con una stringa JSON. Con una lingua che non c'è: il testo
   italiano. Senza testo: `null`.
8. Senza account, con l'account chiuso e con `is_enabled` `false`: `electronic-receipt`, `electronic-receipt-activation` e
   `save-electronic-receipt-account` rispondono 403 `fiskaly.not_enabled` e niente cambia; `download-guide-electronic-receipt`
   risponde 200.
9. `electronic-receipt-activation` con `true`: 204 e `terms_accepted_at` valorizzato; di nuovo: 204 e la stessa data. Con `false` e
   senza il campo: 400 `validation.must_accept`. Sessione impersonata: 403 `auth.forbidden`. Due chiamate insieme: nessun 500 e
   una sola data.
10. `save-electronic-receipt-account` senza termini accettati: 409 `fiskaly.terms_not_accepted`. Con un campo vuoto: 400
    `validation.required` su quel campo; con un CAP di 6 caratteri: 400 `validation.max_length`. Con `pin` `null` e credenziali salvate:
    204 e il PIN resta quello di prima; senza credenziali salvate: 400 su `pin`. Dopo un 204 il PIN nuovo si rilegge subito, anche da
    un'altra istanza, e `fisconline_updated_at` è cambiato. Fiskaly che non risponde: 503 `fiskaly.unavailable`, mai 204. Due
    salvataggi insieme: nessun 500. Sessione impersonata: 403. Nei log e nel registro non compaiono PIN, password e dati del
    titolare.
11. Lettura e versione dell'account (domande 1 e 4): la `GET` risponde 200 con i dati salvati e `version`, senza PIN e password;
    `POST` con `If-Match` uguale a quella `version`: 204 con l'header `ETag` della nuova; due `POST` che cambiano i dati con la stessa
    `If-Match`: il primo 204, il secondo 409 `resource.version_conflict` e l'account resta quello del primo; senza `If-Match`: 409; un
    `POST` che cambia solo PIN o password: 204 con `ETag` uguale alla versione letta.
12. `download-guide-electronic-receipt`: 200 con il file, `Content-Type` e `Content-Disposition`. Senza
    la guida: 404 `fiskaly.guide_not_found`.
13. `GET /api/audit/entries` ha una voce per ogni modifica riuscita (due per l'accensione che spegne un'altra stampante), con il
    modulo, il `targetType` e i metadati della tabella, tradotta nell'esportazione; nessuna voce dopo un errore.
