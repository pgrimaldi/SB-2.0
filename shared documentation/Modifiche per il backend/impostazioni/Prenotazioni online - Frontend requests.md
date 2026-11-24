# Prenotazioni online - Frontend requests

Le modifiche che il backend deve fare perché la pagina **Prenotazioni online** del gestionale funzioni come la vuole
il frontend. Il documento è pensato per chi le implementa (anche un agente): ogni sezione dice cosa creare o cambiare,
con payload, risposta, regole ed errori attesi.

La pagina è `/settings/online-booking-configuration`: il collegamento a Stripe, i comandi per le opzioni della
prenotazione online e il codice iframe. Il pannello "Opzioni prenotazioni online" e il popup "Opzioni prenotazioni online
servizi" leggono le loro opzioni a ogni apertura e Salva le rimanda tutte.

## Riepilogo

| # | Metodo | Rotta | Cosa fa |
|---|---|---|---|
| 1 | GET + PUT | `/api/catalog/online-settings/services` | API esistenti: controllo di versione (`version` nella lettura, `If-Match` ed `ETag` nel salvataggio, 409 `resource.version_conflict`) e `sectionTitle` fino a 2.000 caratteri |
| 2 | GET | `/api/payment/get-stripe-information` | API nuova: l'id del conto Stripe collegato allo stabilimento |
| 3 | GET + PUT | `/api/catalog/online-settings` | API esistenti: anche i limiti di booking e la legenda, il blocco delle prenotazioni online in ore e il controllo di versione |
| 4 | POST | `/api/payment/connect-stripe` | API nuova: collega il conto Stripe con il `code` del ritorno da Stripe |

La pagina usa anche `POST /api/catalog/sectors/sector-combobox` (chiesta in `Servizi - Frontend requests.md`, sezione 25)
e, così come sono, `GET /api/catalog/map/icon-catalog` (le icone della legenda) e `GET /api/catalog/property-info` (solo
`slug`, per il codice iframe).

## Regole comuni

- **Stabilimento**: lo prende dal token, come le altre API del gestionale: nessuna richiesta lo contiene.
- **JSON** in camelCase, con i nomi esatti di questo documento.
- **Errori** nel formato Problem Details del backend (`Boot/Web/ProblemDetailsWriter.cs`), con `code`, `traceId` ed
  `errors[]` quando ci sono.

## 1. Opzioni dei servizi: controllo di versione e titolo

Il frontend usa le due API che il backend ha già (`Endpoints/OnlineSettingsEndpoints.cs:28-35`,
`OnlineSettingsService.GetServicesAsync` e `SaveServicesAsync`), con i loro nomi: rotte, campi, permessi, limiti, codici
e voce del registro **restano come sono**, a parte quello che chiede questa sezione.

### Come sono oggi

- **Dati**: `catalog.property_service_booking_settings` (`ServiceBookingSettings`), una riga per stabilimento;
  `ServiceOnlineSettingsDto` nella risposta, `SaveServiceOnlineSettingsRequest` nel corpo, con gli stessi campi.
- **Permessi**: `catalog.read` per leggere, `catalog.settings` per salvare. Senza: 403 `auth.forbidden`. Token senza
  stabilimento: 403 `catalog.property_required`; stabilimento che non esiste più: 404 `catalog.property_not_found`.
  Senza token valido: 401 `auth.unauthenticated`.
- **Riga mancante**: la lettura dà i valori di partenza (tutto spento e vuoto) senza creare la riga; il salvataggio la
  crea.
- **Registro**: una voce `catalog.service_online_settings_updated` per salvataggio.

```json
{
  "isEnabled": true,
  "surchargeType": "percentage",
  "surchargeValue": 10,
  "requiresManualConfirmation": false,
  "sectionTitle": "<p>Prenota i tuoi servizi.</p>",
  "usefulInfo": "<p>Il <strong>parcheggio</strong> è gratuito per i clienti.</p>",
  "paymentMethods": ["pay_now", "pay_at_property", "pay_at_property_prepaid"]
}
```

| Campo | Nel popup | Regola (`SaveServiceOnlineSettingsRequestValidator`) |
|---|---|---|
| `isEnabled` | "Abilita prenotazioni online dei servizi" | |
| `surchargeType` | "Tipo di supplemento" | `disable`, `percentage`, `fixed`; un altro valore è un errore su `surchargeValue` |
| `surchargeValue` | "Valore supplemento" | Da 0 a 100 con `percentage`, da 0 a 10.000 con `fixed` |
| `requiresManualConfirmation` | "Richiedi conferma prenotazione" | |
| `sectionTitle` | "Titolo del pannello prenotazione servizi" (HTML) | Oggi al massimo 255 caratteri, HTML compreso: diventano **2.000** (vedi sotto) |
| `usefulInfo` | "Informazioni utili" (HTML) | Al massimo 10.000 caratteri, HTML compreso |
| `paymentMethods` | "Metodi di pagamento accettati", una casella per codice | `pay_now`, `pay_at_property`, `pay_at_property_no_card`, `deposit_half`, `pay_at_property_prepaid` |

- Il frontend manda **sempre tutti i campi**, anche quelli non cambiati: senza supplemento `surchargeValue` torna com'era
  (0 se l'utente lo ha appena tolto); i codici di `paymentMethods` che il popup non conosce tornano come sono arrivati.
- Il frontend controlla gli stessi limiti prima di salvare e mostra sotto i campi gli errori del backend (400
  `validation.failed` con `errors[]`: `validation.invalid` su `surchargeValue` e `paymentMethods`, `validation.max_length`
  su `sectionTitle` e `usefulInfo`); ogni altro errore sopra i bottoni.

### Titolo fino a 2.000 caratteri

`sectionTitle` è un testo formattato (paragrafi, grassetto, corsivo, sottolineato, elenchi, link) e il limite conta anche
i tag: con 255 caratteri un titolo con un link può non starci. Il limite diventa **2.000** caratteri:

- colonna `catalog.property_service_booking_settings.section_title` da `varchar(255)` a `varchar(2000)`
  (`ServiceBookingSettingsConfiguration`), con la migration; i valori già salvati non cambiano;
- `SaveServiceOnlineSettingsRequestValidator`: `MaximumLength(2000)`, cioè `validation.max_length` con `args.max` 2000.

### Controllo di versione

Oggi, se due utenti cambiano le opzioni dei servizi, vince chi salva per ultimo: il popup rimanda tutte le opzioni lette
all'apertura e cancella, senza che nessuno se ne accorga, quello che un altro utente ha salvato nel frattempo.
`ServiceBookingSettings` non ha un token di concorrenza e la `PUT` non guarda `If-Match`. Il frontend chiede lo stesso
schema di **concorrenza ottimistica** degli altri documenti, con un **codice di errore unico per tutte le API**:
`resource.version_conflict`.

**Non è quello che Booking ha già per le prenotazioni** (`Module.Booking/Endpoints/Endpoints.cs:253-255`,
`ReservationService.cs:114-115`): lì l'`If-Match` è un numero, se manca o non si legge non si controlla niente, e il
conflitto è 412 o 409 `booking.version_conflict`. Qui valgono le regole sotto: con un altro stato o un altro codice il
popup non avviserebbe l'utente.

- **Versione nella lettura**: la `GET` restituisce, insieme alle opzioni, il campo `version` (stringa opaca), allo stesso
  livello degli altri campi, anche senza riga salvata. Cambia ogni volta che un salvataggio cambia le opzioni, e solo
  allora: due letture senza modifiche in mezzo danno la stessa versione. Ogni stabilimento ha la sua.
- **`If-Match` nel salvataggio**: il frontend manda la versione letta nell'header `If-Match: "<version>"`; il corpo della
  `PUT` non la contiene. Se non è più quella attuale, il backend non salva niente e risponde **409
  `resource.version_conflict`**, senza `errors[]`. Con un `If-Match` assente o illeggibile risponde allo stesso modo: il
  frontend lo manda sempre.
- **`ETag` nella risposta**: il salvataggio riuscito risponde 200 con le opzioni salvate, come oggi, e con la nuova
  versione nell'header `ETag: "<version>"`. Frontend e API stanno sulla stessa origine (`/api`): se non fosse così, il
  CORS dovrebbe esporre l'header `ETag`.
- **Opzioni mai salvate**: il primo salvataggio con la versione letta riesce. Se un altro utente salva per primo, chi
  salva dopo con la stessa versione riceve il 409.
- **Riga eliminata** (`deleted_at` valorizzato, per esempio da un'importazione: nessun codice la elimina oggi): vale come
  opzioni mai salvate. La lettura dà i valori di partenza e una `version`, e il salvataggio riesce. Oggi darebbe un 500 a
  ogni salvataggio: il filtro globale la nasconde (`ServiceBookingSettings` è `ISoftDeletable`) ma l'indice unico
  `uq_catalog_property_service_booking_settings_property` non ha il filtro
  (`ServiceBookingSettingsConfiguration.cs:72-74`).
- **Controllo e salvataggio sono una cosa sola**: di due salvataggi nello stesso momento con lo stesso `If-Match`, che
  cambiano le opzioni in modo diverso, uno risponde 200 e l'altro 409 `resource.version_conflict`; mai i dati di tutti e
  due mescolati, mai un 500. Vale anche per il primo in assoluto: oggi la seconda riga viola lo stesso indice unico e
  l'errore non è gestito.
- **Salvataggio che non cambia niente**: con la versione attuale risponde 200; la versione può restare la stessa.
- Il 409 si controlla dopo la validazione del corpo (400) e dopo i 403 e 404 dello stabilimento.
- **Come farlo lo sceglie lo sviluppatore del backend** (il token `xmin` di Npgsql, un hash delle opzioni, un
  contatore...): l'agente glielo chiede prima di scrivere codice. Il frontend tratta la versione come una stringa opaca e
  non la confronta mai.

### Cosa fa il frontend

Con 409 `resource.version_conflict` il popup si chiude e compare l'avviso "Nel frattempo un altro utente ha modificato
questi dati: li abbiamo ricaricati. Rifai le tue modifiche.". Come dopo un salvataggio riuscito, alla prossima apertura
il popup rilegge opzioni e versione con la `GET`.

## 2. GET `/api/payment/get-stripe-information`

Il conto Stripe collegato allo stabilimento. La pagina la chiama all'apertura per sapere se Stripe è già collegato.

- **Dove**: nel gruppo `/api/payment` di `Module.Payment/Endpoints/Endpoints.cs`. I dati sono in
  `payment.gateway_connected_accounts` (`ConnectedAccount`), al massimo una riga per stabilimento; oggi nessun codice la
  legge (solo entità e configurazione).
- **Richiesta**: nessun corpo e nessun parametro.
- **Risposta**: 200 con il solo id del conto, `StripeAccountId`, come stringa JSON (non un oggetto), per esempio
  `"acct_1QmDemo4Fz8Lw2Xc"`. `null` se lo stabilimento non ha un conto collegato: nessuna riga, riga eliminata
  (`deleted_at`) o collegamento revocato (`RevokedAt` valorizzato). Lo stato dell'onboarding non conta. Con `null` il
  frontend accetta sia il corpo `null` sia un corpo vuoto.
- **Non restituisce altro**: nessun dato del conto, nessuna chiave. Non scrive niente e non chiama Stripe.
- **Permesso**: `catalog.settings` (`Permissions.CatalogSettings`), come il salvataggio delle opzioni della pagina.

| Status | `code` | Quando |
|---|---|---|
| 403 | `payment.property_required` | Token senza stabilimento (`PropertyOf`, come le altre API di Payment) |
| 403 | `auth.forbidden` | Utente senza il permesso |
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido |

Se la chiamata fallisce la pagina resta com'è, senza messaggi.

## 3. GET + PUT `/api/catalog/online-settings`: tutte le opzioni del pannello

Il pannello "Opzioni prenotazioni online" legge tutto con **una `GET`** quando si apre e salva tutto con **una `PUT`**.
Oggi il backend ha tre gruppi di API (`Endpoints/OnlineSettingsEndpoints.cs:19-68`): le opzioni, i limiti uno per uno
(`/stay-limits`) e la legenda (`/legend`). Salvare con tante chiamate può fermarsi a metà. Le API `/stay-limits` e
`/legend` **restano come sono**: il frontend non le usa.

### Cosa si aggiunge a `OnlineSettingsDto` e a `SaveOnlineSettingsRequest`

Tutti i campi di oggi restano, con gli stessi nomi e le stesse regole. In più:

| Campo | Lettura | Salvataggio |
|---|---|---|
| `stayLimits` | `{ id, sectorId, startsOn, endsOn, minDays, maxDays }[]`, in ordine di `startsOn` | Gli stessi campi; `id` `null` per un limite nuovo |
| `legend` | `{ id, label, iconId, iconUrl }[]`, nell'ordine della legenda | `{ id, label, iconId }[]`; `id` `null` per un elemento nuovo |
| `onlineBookingBlock` | uno dei codici sotto | uno dei codici sotto |
| `version` | stringa opaca | nell'header `If-Match` |

```json
{
  "isEnabled": true,
  "isSuspendedByPlatform": false,
  "isMapPick": true,
  "isLocationRestricted": true,
  "allowedSpotIds": ["0199a1b2-3c4d-7e5f-8a6b-7c8d9e0f1a2b"],
  "isPeriodRestricted": true,
  "allowedPeriods": [{ "startsOn": "2027-05-15", "endsOn": "2027-09-20" }],
  "surchargeType": "disable",
  "surchargeValue": 0,
  "isBlocked": false,
  "onlineBookingBlock": "three_hours_before",
  "requiresManualConfirmation": false,
  "allowsSameDayBooking": true,
  "allowsHalfDay": true,
  "morningFrom": "08:00:00",
  "morningTo": "13:00:00",
  "afternoonFrom": "13:30:00",
  "afternoonTo": "19:00:00",
  "afternoonConversionTime": "12:30:00",
  "allowsSeasonal": false,
  "allowsSharing": false,
  "maxSpotsPerBooking": 4,
  "maxBookingsPerDay": null,
  "bookingHorizonDays": 180,
  "scope": "all",
  "paymentMethods": ["pay_now", "pay_at_property"],
  "usefulInfo": "<p>Il parcheggio è gratuito per i clienti.</p>",
  "goodToKnow": null,
  "stayLimits": [
    {
      "id": "0199a1b2-3c4d-7e5f-8a6b-7c8d9e0f1a2c",
      "sectorId": "0199a1b2-3c4d-7e5f-8a6b-7c8d9e0f1a2d",
      "startsOn": "2027-08-01",
      "endsOn": "2027-08-31",
      "minDays": 3,
      "maxDays": 14
    }
  ],
  "legend": [
    {
      "id": "0199a1b2-3c4d-7e5f-8a6b-7c8d9e0f1a2e",
      "label": "Ombrellone libero",
      "iconId": "0199a1b2-3c4d-7e5f-8a6b-7c8d9e0f1a2f",
      "iconUrl": "https://esempio.cloudfront.net/icons/ombrellone.svg"
    }
  ],
  "version": "7c1f03ab"
}
```

- Il corpo della `PUT` ha gli stessi campi, senza `isSuspendedByPlatform`, `version` e `iconUrl`; gli orari arrivano come
  `HH:mm`. La risposta è 200 con le opzioni salvate nella forma della lettura, come oggi.
- Il frontend manda **sempre tutti i campi e tutte le righe**, anche quello che il pannello non mostra (`allowedSpotIds`,
  `isBlocked`, i periodi e gli orari di un'opzione spenta): tornano come sono arrivati. `isBlocked` resta quello che è
  oggi (il blocco del pagamento online): non c'entra con `onlineBookingBlock`.
- **Permessi** come oggi: `catalog.read` per leggere, `catalog.settings` per salvare.
- **Una transazione sola** per opzioni, postazioni, periodi, limiti e legenda: se qualcosa non va non cambia niente. Oggi
  `SaveAsync` salva i periodi in due passi fuori da una transazione (`OnlineSettingsService.cs`, `ClearPeriods` e poi
  `AddPeriods`): se il secondo fallisce i periodi restano cancellati.

### Limiti di booking (`stayLimits`)

- La lista del corpo **sostituisce** quella salvata: un limite con `id` si aggiorna e tiene il suo `id`, uno senza `id` si
  crea, uno salvato che non c'è nel corpo si elimina (come fa oggi `DELETE /stay-limits/{id}`).
- Regole di oggi (`SaveStayLimitRequestValidator`): fila e date obbligatorie, `endsOn` non prima di `startsOn`, almeno uno
  fra minimo e massimo, da 1 a 366 giorni, il massimo non sotto il minimo.
- Un limite di una fila eliminata si legge con l'id di quella fila: oggi `ToDtoAsync` dà l'id vuoto
  (`00000000-0000-0000-0000-000000000000`). Salvarlo con quell'id risponde 404 `catalog.sector_not_found`.
- Il vincolo `ex_catalog_property_online_stay_limits_no_overlap` non è differibile: scambiare le date di due limiti della
  stessa fila nello stesso salvataggio deve riuscire (lo stato finale è valido), senza 500.

### Legenda (`legend`)

- La lista del corpo sostituisce quella salvata, nell'ordine ricevuto, come `PUT /legend` oggi (al massimo 30 elementi,
  `label` obbligatoria fino a 100 caratteri). `catalog.map_legend_items` ha bisogno di un id pubblico (`public_id`, uuid):
  un elemento mandato con il suo `id` lo tiene.
- **Un elemento rimandato con il suo `id` e lo stesso `iconId` letto tiene la sua icona**, anche se l'icona non è più attiva
  nel catalogo o non è nel catalogo (`iconId` `null` con `iconUrl` valorizzato, per esempio un'icona del vecchio
  gestionale). Oggi la legenda la perderebbe: `SaveLegendAsync` con `iconId` `null` toglie l'icona, e con l'id di un'icona
  spenta risponde 404 (`FindIconAsync` vuole `IsActive`, `ListIconsByAssetAsync` no).
- Un `iconId` nuovo deve essere un'icona attiva del catalogo: altrimenti 404 `map.icon_not_found`.

### Blocco delle prenotazioni online (`onlineBookingBlock`)

Le opzioni del pannello, con questi codici:

| Codice | Nel pannello |
|---|---|
| `disabled` | Disabilitato |
| `three_hours_before` | 3 ore prima |
| `five_hours_before` | 5 ore prima |
| `eight_hours_before` | 8 ore prima |
| `twelve_hours_before` | 12 ore prima |
| `twenty_four_hours_before` | 24 ore prima |
| `forty_eight_hours_before` | 48 ore prima |
| `seventy_two_hours_before` | 72 ore prima |

- Quando mancano meno ore di quelle scelte il cliente non può più prenotare online; dal gestionale non cambia niente. Vale
  per il preventivo online (`QuoteService.ValidateOnline`) e per la prenotazione online
  (`POST /api/booking/public/{slug}/reservations`).
- **Da quale momento si contano le ore** (le 00:00 del primo giorno, l'orario di apertura dello stabilimento...) e quale
  codice riceve il cliente lo decide lo sviluppatore del backend: l'agente glielo chiede prima di scrivere codice.
- Valore di partenza, anche per gli stabilimenti che hanno già le opzioni salvate: `disabled`. Colonna o enum li sceglie lo
  sviluppatore; i codici JSON sono quelli della tabella.

### Controllo di versione

Le stesse regole della sezione 1, con queste differenze:

- La versione copre tutto quello che la `PUT` scrive: opzioni, postazioni, periodi, limiti e legenda, anche quando li
  cambiano altre API (`/stay-limits`, `/legend`, o l'editor della mappa che elimina una postazione scelta). Non copre
  `isSuspendedByPlatform`.
- Anche qui vale per le opzioni mai salvate, per la riga eliminata (`property_online_booking_settings` è `ISoftDeletable` e
  il suo indice unico non ha il filtro) e per due salvataggi nello stesso momento.
- Con 409 il pannello si chiude e compare l'avviso; alla prossima apertura rilegge tutto.

### Errori della `PUT`

Nell'ordine in cui si controllano:

| Status | `code` | Quando |
|---|---|---|
| 400 | `validation.failed` | Uno o più campi non validi, in `errors[]`: quelli di oggi, più `onlineBookingBlock` (`validation.invalid`), `stayLimits[i].sectorId`, `.startsOn`, `.endsOn` (`validation.required`), `stayLimits[i].endsOn` (`validation.end_before_start`), `stayLimits[i].minDays` e `.maxDays` (`validation.invalid`, `validation.out_of_range`), `legend` (`validation.invalid`, più di 30), `legend[i].label` (`validation.required`, `validation.max_length`) |
| 403 / 404 | `catalog.property_required` / `catalog.property_not_found` | Token senza stabilimento / stabilimento che non esiste più |
| 409 | `resource.version_conflict` | `If-Match` assente, illeggibile o non più attuale |
| 404 | `catalog.sector_not_found` | Una fila dei limiti che non esiste (più) |
| 404 | `catalog.stay_limit_not_found` | Un `id` di limite che non è dello stabilimento |
| 404 | `map.icon_not_found` | Un'icona nuova della legenda che non è attiva nel catalogo |
| 422 | `map.spot_not_found` | Una postazione di `allowedSpotIds` che non c'è (più), con `args.ids` |
| 409 | `catalog.stay_limit_overlap` | Due limiti della stessa fila con giorni in comune |

Un `id` di elemento della legenda che non è dello stabilimento vale come un elemento nuovo. Il frontend mostra gli errori
dei campi sotto i campi, gli altri in fondo al pannello.

## 4. POST `/api/payment/connect-stripe`

Il gestore preme "Configura il tuo account Stripe" e va su Stripe Connect (OAuth, `scope=read_write`); Stripe lo rimanda al
gestionale con `code` e `state`. Il frontend controlla lo `state` (casuale, tenuto nella scheda del browser) e manda il
`code` a questa API.

- **Dove**: nel gruppo `/api/payment` di `Module.Payment/Endpoints/Endpoints.cs`.
- **Permesso**: `catalog.settings`.
- **Richiesta**: `{ "code": "ac_..." }`.
- **Cosa fa**: scambia il `code` con Stripe (token OAuth di Connect) con la chiave segreta della piattaforma, che sta solo
  nel backend, e salva l'id del conto in `payment.gateway_connected_accounts` per lo stabilimento del token. Se lo
  stabilimento ha già una riga, anche revocata, la aggiorna: `StripeAccountId`, `ConnectedAt`, `RevokedAt` `null` e lo stato
  dell'onboarding che dà Stripe.
- **Risposta**: 200 con il solo id del conto, come `get-stripe-information`.
- **Sicurezza**: `code`, token e chiavi non compaiono mai in risposte, log o registro; i token che Stripe restituisce non
  vanno al frontend. Se servono al backend li tiene come gli altri segreti.

| Status | `code` | Quando |
|---|---|---|
| 400 | `validation.failed` | `code` assente o vuoto (`validation.required` su `code`) |
| 403 | `payment.property_required` / `auth.forbidden` | Token senza stabilimento / utente senza il permesso |
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido |
| 422 | `payment.stripe_code_invalid` | Stripe rifiuta il `code` (scaduto, già usato, di un'altra piattaforma): niente cambia |
| 409 | `payment.stripe_account_taken` | Il conto è già collegato a un altro stabilimento (`uq_payment_gateway_connected_accounts_stripe_account`): niente cambia |
| 503 | `payment.stripe_unavailable` | Stripe non risponde: niente cambia |

La pagina di ritorno del frontend arriverà insieme a questa API, quando sarà registrato in Stripe l'indirizzo di ritorno.

## Verifica

API 1, con un token di un utente dello stabilimento che può leggere e salvare le opzioni:

1. `GET /api/catalog/online-settings/services` risponde con `version` (per esempio `v1`); la `PUT` con le opzioni lette,
   `surchargeValue` cambiato e `If-Match: "v1"`: 200 con le opzioni salvate e l'header `ETag` di una versione nuova,
   diversa da `v1`; una nuova `GET` risponde con `surchargeValue` cambiato e con quella versione.
2. La stessa `PUT` con `If-Match: "v1"` dopo la prima: 409 `resource.version_conflict` senza `errors[]`; le opzioni non
   cambiano.
3. `PUT` senza `If-Match`, con `If-Match: abc` e con `If-Match: "altro"`: 409 `resource.version_conflict`; le opzioni non
   cambiano.
4. Due `GET` senza salvataggi in mezzo: la stessa `version`. Un salvataggio delle opzioni di un altro stabilimento non
   cambia la `version` di questo.
5. Uno stabilimento senza opzioni salvate: la `GET` risponde con una `version`; la `PUT` con quella versione: 200 con
   `ETag`. Una seconda `PUT` con la stessa versione di prima: 409 `resource.version_conflict`.
6. Due `PUT` nello stesso momento con lo stesso `If-Match` e valori diversi, anche su uno stabilimento senza opzioni
   salvate: una 200, l'altra 409; nessun 500.
7. Uno stabilimento con la riga eliminata (`deleted_at`): la `GET` risponde con i valori di partenza e una `version`; la
   `PUT` con quella versione: 200.
8. `PUT` con `If-Match` di una versione vecchia e un corpo che la validazione rifiuta (`surchargeType` `percentage` e
   `surchargeValue` 101): 400 `validation.failed` con `validation.invalid` su `surchargeValue`; le opzioni e la versione
   non cambiano.
   `sectionTitle` di 2.000 caratteri: 200; di 2.001: 400 con `validation.max_length` e `args.max` 2000.

API 2:

9. Stabilimento con un conto collegato: 200 con la stringa `acct_…` e `Content-Type: application/json`.
10. Stabilimento senza riga, con la riga eliminata o con `revoked_at` valorizzato: 200 con `null`.
11. Il token di un altro stabilimento non vede mai il conto di questo. Utente senza `catalog.settings`: 403
    `auth.forbidden`. Token senza stabilimento: 403 `payment.property_required`.

API 3:

12. La `GET` risponde con opzioni, `stayLimits`, `legend`, `onlineBookingBlock` e `version` anche in uno stabilimento senza
    opzioni salvate (`onlineBookingBlock` `disabled`, liste vuote). Rimandando tutto con la `PUT` e la `version` letta: 200
    e niente cambia, nemmeno `allowedSpotIds`, `isBlocked` e gli `id` di limiti ed elementi della legenda.
13. `PUT` con un limite cambiato, uno nuovo e uno in meno: la lettura dopo ha il primo con lo stesso `id`, il secondo con un
    `id` nuovo e non ha più il terzo. Scambiare le date di due limiti della stessa fila: 200.
14. `PUT` con due limiti sovrapposti della stessa fila: 409 `catalog.stay_limit_overlap`; opzioni, periodi e legenda non
    cambiano.
15. Un elemento della legenda con un'icona spenta o fuori dal catalogo, rimandato com'è: 200 e la lettura dopo ha lo stesso
    `iconUrl`. Un `iconId` inesistente: 404 `map.icon_not_found` e niente cambia.
16. Le prove 2-8 della versione, sulle opzioni del pannello; in più: `POST /stay-limits` o `PUT /legend` cambiano la
    `version` della lettura.
17. Con `onlineBookingBlock` `three_hours_before` il cliente prenota online prima del termine e non dopo; dal gestionale si
    prenota sempre.

API 4:

18. Con un `code` valido (conto di prova di Stripe): 200 con `acct_…`, e `get-stripe-information` risponde con lo stesso id.
    Lo stesso `code` una seconda volta: 422 `payment.stripe_code_invalid`.
19. Il conto già collegato a un altro stabilimento: 409 `payment.stripe_account_taken`, nessuna riga cambia. Nei log non
    compaiono `code`, token o chiavi.
