# Sharing - Frontend requests

Le modifiche che il backend deve fare perché la pagina **Sharing** del gestionale funzioni come la vuole il frontend.
Il documento è pensato per chi le implementa (anche un agente): ogni sezione dice cosa creare, con payload, risposta,
regole ed errori attesi.

La pagina è `/settings/sharing`: due riquadri con le impostazioni dello Sharing dello stabilimento (rivendita dei giorni
e assegnazione dei crediti), sempre modificabili, e il bottone Salva, che rimanda tutte le impostazioni.

## Riepilogo

| # | Metodo | Rotta | Cosa fa |
|---|---|---|---|
| 1 | GET + POST | `/api/sharing/general-information` e `/api/sharing/save-information` | Controllo di versione: `version` nella lettura, `If-Match` ed `ETag` nel salvataggio, 409 `resource.version_conflict` |

## Regole comuni

- **Le API della pagina**: rotte, corpi e risposte sono quelli del contratto del frontend (`docs/API_CONTRACTS.md` del
  frontend e `shared documentation/API_CONTRACTS.docx`, sezione `/sharing`). Oggi il backend non le ha: i dati dello
  Sharing stanno in `ResaleSettings` (`booking.resale_settings`, modulo Booking), che non espone endpoint. Questo
  documento chiede solo quello che le due API devono avere in più per il controllo di versione.
- **Stabilimento**: lo prende dal token, come le altre API del gestionale: nessuna richiesta lo contiene.
- **JSON** in camelCase, con i nomi esatti di questo documento.
- **Errori** nel formato Problem Details del backend (`Boot/Web/ProblemDetailsWriter.cs`), con `code`, `traceId` ed
  `errors[]` quando ci sono.

## 1. Controllo di versione sui salvataggi

Senza un controllo, se due utenti cambiano le impostazioni dello Sharing vince chi salva per ultimo: Salva rimanda
tutte le impostazioni lette all'apertura della pagina e cancella, senza che nessuno se ne accorga, quello che un altro
utente ha salvato nel frattempo. Il backend non ha un controllo di concorrenza sugli aggiornamenti (nessun token di
concorrenza nelle configurazioni delle entità, nessun `If-Match`). Il frontend chiede lo schema di **concorrenza
ottimistica** che il backend ha già progettato per le prenotazioni offline
(`shared documentation/backend/progettazione/19-frontend-offline.md`, sezioni 4.4 e 5.8), con un **codice di errore
unico per tutte le API**: `resource.version_conflict`.

### Cosa ci serve

- **Versione nella lettura**: `GET /api/sharing/general-information` restituisce, insieme alle impostazioni, il campo
  `version` (stringa opaca), allo stesso livello degli altri campi:

  ```json
  {
    "isSharingEnabled": true,
    "isMinimumBookingDayEnabled": true,
    "minimumBookingDayQuantity": 3,
    "isOnlySeasonalEnabled": false,
    "isEnabledOnAllArticles": true,
    "isScheduledAbsenceSharing": true,
    "isManualCreditAssignmentEnabled": false,
    "resoldDayCreditQuantity": 1,
    "manualCreditValue": 2.5,
    "isAutomaticVoucherEnabled": true,
    "voucherCreditValue": 5.5,
    "isPrepaidCardTopUpEnabled": false,
    "prepaidCardCreditValue": 10,
    "version": "a41f09c2"
  }
  ```

  La versione cambia ogni volta che cambiano le impostazioni che `save-information` scrive, anche quando le cambia
  un'altra API, e solo allora: due letture senza salvataggi in mezzo danno la stessa versione. Ogni stabilimento ha la
  sua: un salvataggio di un altro stabilimento non la cambia.
- **`If-Match` nel salvataggio**: il frontend manda la versione letta nell'header `If-Match: "<version>"`; il corpo di
  `save-information` non cambia e non contiene la versione. Se non è più quella attuale, il backend non salva niente e
  risponde **409 `resource.version_conflict`**, senza `errors[]`. Senza `If-Match` risponde allo stesso modo: il
  frontend lo manda sempre.
- **`ETag` nella risposta**: il salvataggio riuscito risponde 204 senza corpo, come oggi nel contratto, con la nuova
  versione nell'header `ETag: "<version>"`. La pagina resta aperta e la usa per il salvataggio successivo.
- **Impostazioni mai salvate**: anche uno stabilimento che non ha ancora le impostazioni dello Sharing riceve dalla
  lettura una `version`, e il primo salvataggio con quella versione riesce. Se un altro utente salva per primo, chi
  salva dopo con la stessa versione riceve il 409.
- Il 409 `resource.version_conflict` si controlla dopo la validazione del corpo (400 `validation.failed`).
- **Come farlo lo sceglie lo sviluppatore del backend** (il token `xmin` di Npgsql come nel documento offline, un hash
  delle impostazioni, un contatore...): l'agente glielo chiede prima di scrivere codice. Il frontend tratta la versione
  come una stringa opaca e non la confronta mai.

### API coinvolte

| Lettura (restituisce `version`) | Salvataggio (manda `If-Match`, risponde `ETag`) | Dati coperti dalla versione |
|---|---|---|
| `GET /api/sharing/general-information` | `POST /api/sharing/save-information` | Tutte le impostazioni della pagina (tutti i campi del corpo di `save-information`) |

### Cosa fa il frontend

Con 409 `resource.version_conflict` la pagina mostra l'avviso "Nel frattempo un altro utente ha modificato questi
dati: li abbiamo ricaricati. Rifai le tue modifiche." e rilegge le impostazioni con la `GET`. Dopo ogni salvataggio
riuscito tiene la versione dell'`ETag`.

## Verifica

API 1, con un token di un utente dello stabilimento che può leggere e salvare le impostazioni:

1. `GET /api/sharing/general-information` risponde con `version` (per esempio `v1`);
   `POST /api/sharing/save-information` con le impostazioni lette, `isSharingEnabled` cambiato e `If-Match: "v1"`: 204
   senza corpo con l'header `ETag` di una versione nuova, diversa da `v1`; una nuova `GET` risponde con
   `isSharingEnabled` cambiato e con quella versione.
2. Lo stesso `POST` con `If-Match: "v1"` dopo il primo: 409 `resource.version_conflict` senza `errors[]`; le
   impostazioni non cambiano.
3. `POST` senza `If-Match`: 409 `resource.version_conflict`; le impostazioni non cambiano.
4. Due `GET` senza salvataggi in mezzo: la stessa `version`. Un salvataggio delle impostazioni di un altro stabilimento
   non cambia la `version` di questo.
5. Uno stabilimento senza impostazioni salvate: la `GET` risponde con una `version`; il `POST` con quella versione: 204
   con `ETag`. Un secondo `POST` con la stessa versione di prima: 409 `resource.version_conflict`.
6. `POST` con `If-Match` di una versione vecchia e un corpo che la validazione del backend rifiuta: 400
   `validation.failed` (la validazione viene prima della versione); le impostazioni e la versione non cambiano.
