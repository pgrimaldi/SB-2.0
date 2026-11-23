# Prenotazioni online - Frontend requests

Le modifiche che il backend deve fare perché la pagina **Prenotazioni online** del gestionale funzioni come la vuole
il frontend. Il documento è pensato per chi le implementa (anche un agente): ogni sezione dice cosa creare, con payload,
risposta, regole ed errori attesi.

La pagina è `/settings/online-booking-configuration`: il collegamento a Stripe, i comandi per le opzioni della
prenotazione online e il codice iframe. Il popup "Gestisci opzioni servizi" legge le opzioni della prenotazione online
dei servizi all'apertura e Salva le rimanda tutte; è l'unico punto della pagina che legge dati e poi li risalva.

## Riepilogo

| # | Metodo | Rotta | Cosa fa |
|---|---|---|---|
| 1 | GET + POST | `/api/booking/get-online-services-options` e `/api/booking/update-online-services-options` | Controllo di versione: `version` nella lettura, `If-Match` ed `ETag` nel salvataggio, 409 `resource.version_conflict` |

## Regole comuni

- **Le API della pagina**: rotte, corpi e risposte sono quelli del contratto del frontend (`docs/API_CONTRACTS.md` del
  frontend e `shared documentation/API_CONTRACTS.docx`, sezione `/booking`). Oggi il backend non ha queste rotte: ha
  `GET` e `PUT /api/catalog/online-settings/services` (`Endpoints/OnlineSettingsEndpoints.cs`,
  `OnlineSettingsService.GetServicesAsync` e `SaveServicesAsync`, dati in `ServiceBookingSettings`,
  `catalog.property_service_booking_settings`), con le stesse opzioni in un'altra forma (`ServiceOnlineSettingsDto`,
  `SaveServiceOnlineSettingsRequest`). Questo documento chiede solo il controllo di versione: vale per la coppia di API
  che servirà il popup. Se lo sviluppatore sceglie di servirlo con `GET` e `PUT /api/catalog/online-settings/services`,
  le regole sotto valgono per quelle due API; l'agente glielo chiede prima di scrivere codice.
- **Stabilimento**: lo prende dal token, come le altre API del gestionale: nessuna richiesta lo contiene.
- **JSON** in camelCase, con i nomi esatti di questo documento.
- **Errori** nel formato Problem Details del backend (`Boot/Web/ProblemDetailsWriter.cs`), con `code`, `traceId` ed
  `errors[]` quando ci sono.

## 1. Controllo di versione sui salvataggi

Oggi, se due utenti cambiano le opzioni dei servizi, vince chi salva per ultimo: il popup rimanda tutte le opzioni lette
all'apertura e cancella, senza che nessuno se ne accorga, quello che un altro utente ha salvato nel frattempo. Il
backend non ha un controllo di concorrenza sugli aggiornamenti: nessun token di concorrenza in
`ServiceBookingSettingsConfiguration`, nessun `If-Match` (`SaveServicesAsync` sovrascrive le opzioni). Il frontend
chiede lo schema di **concorrenza ottimistica** che il backend ha già progettato per le prenotazioni offline
(`shared documentation/backend/progettazione/19-frontend-offline.md`, sezioni 4.4 e 5.8), con un **codice di errore
unico per tutte le API**: `resource.version_conflict`.

### Cosa ci serve

- **Versione nella lettura**: `GET /api/booking/get-online-services-options` restituisce, insieme alle opzioni, il campo
  `version` (stringa opaca), allo stesso livello degli altri campi:

  ```json
  {
    "isServicesOnlineBookingEnabled": true,
    "supplementType": "Percentage",
    "supplementValue": 10,
    "isBookingConfirmationRequired": false,
    "isPayNowAccepted": true,
    "isPayAtPropertyAccepted": true,
    "isPayAtPropertyWithoutCardAccepted": false,
    "isDepositAccepted": false,
    "isPrepaidCardAccepted": true,
    "bookingPanelTitle": "<p>Prenota i tuoi servizi.</p>",
    "usefulInformation": "<p>Il <strong>parcheggio</strong> è gratuito per i clienti.</p>",
    "version": "9b27e4a0"
  }
  ```

  La versione cambia ogni volta che cambiano le opzioni che `update-online-services-options` scrive, anche quando le
  cambia un'altra API, e solo allora: due letture senza salvataggi in mezzo danno la stessa versione. Ogni stabilimento
  ha la sua: un salvataggio di un altro stabilimento non la cambia.
- **`If-Match` nel salvataggio**: il frontend manda la versione letta nell'header `If-Match: "<version>"`; il corpo di
  `update-online-services-options` non cambia e non contiene la versione. Se non è più quella attuale, il backend non
  salva niente e risponde **409 `resource.version_conflict`**, senza `errors[]`. Senza `If-Match` risponde allo stesso
  modo: il frontend lo manda sempre.
- **`ETag` nella risposta**: il salvataggio riuscito risponde 204 senza corpo, come oggi nel contratto, con la nuova
  versione nell'header `ETag: "<version>"`.
- **Opzioni mai salvate**: anche uno stabilimento senza opzioni dei servizi salvate (nessuna riga in
  `property_service_booking_settings`: oggi `SaveServicesAsync` la crea al primo salvataggio) riceve dalla lettura una
  `version`, e il primo salvataggio con quella versione riesce. Se un altro utente salva per primo, chi salva dopo con
  la stessa versione riceve il 409.
- Il 409 `resource.version_conflict` si controlla dopo la validazione del corpo (400 `validation.failed`).
- **Come farlo lo sceglie lo sviluppatore del backend** (il token `xmin` di Npgsql come nel documento offline, un hash
  delle opzioni, un contatore...): l'agente glielo chiede prima di scrivere codice. Il frontend tratta la versione come
  una stringa opaca e non la confronta mai.

### API coinvolte

| Lettura (restituisce `version`) | Salvataggio (manda `If-Match`, risponde `ETag`) | Dati coperti dalla versione |
|---|---|---|
| `GET /api/booking/get-online-services-options` | `POST /api/booking/update-online-services-options` | Tutte le opzioni della prenotazione online dei servizi (tutti i campi del corpo di `update-online-services-options`) |

Le altre API della pagina non ne hanno bisogno: `GET /api/system/get-stripe-information` e
`POST /api/catalog/sectors/sector-combobox` (chiesta in `Servizi - Frontend requests.md`, sezione 25) leggono soltanto.

### Cosa fa il frontend

Con 409 `resource.version_conflict` il popup "Gestisci opzioni servizi" si chiude e compare l'avviso "Nel frattempo
un altro utente ha modificato questi dati: li abbiamo ricaricati. Rifai le tue modifiche.". Come dopo un salvataggio
riuscito, alla prossima apertura il popup rilegge opzioni e versione con la `GET`.

## Verifica

API 1, con un token di un utente dello stabilimento che può leggere e salvare le opzioni:

1. `GET /api/booking/get-online-services-options` risponde con `version` (per esempio `v1`);
   `POST /api/booking/update-online-services-options` con le opzioni lette, `supplementValue` cambiato e
   `If-Match: "v1"`: 204 senza corpo con l'header `ETag` di una versione nuova, diversa da `v1`; una nuova `GET`
   risponde con `supplementValue` cambiato e con quella versione.
2. Lo stesso `POST` con `If-Match: "v1"` dopo il primo: 409 `resource.version_conflict` senza `errors[]`; le opzioni
   non cambiano.
3. `POST` senza `If-Match`: 409 `resource.version_conflict`; le opzioni non cambiano.
4. Due `GET` senza salvataggi in mezzo: la stessa `version`. Un salvataggio delle opzioni di un altro stabilimento non
   cambia la `version` di questo.
5. Uno stabilimento senza opzioni salvate: la `GET` risponde con una `version`; il `POST` con quella versione: 204 con
   `ETag`. Un secondo `POST` con la stessa versione di prima: 409 `resource.version_conflict`.
6. `POST` con `If-Match` di una versione vecchia e un corpo che la validazione del backend rifiuta: 400
   `validation.failed` (la validazione viene prima della versione); le opzioni e la versione non cambiano.
