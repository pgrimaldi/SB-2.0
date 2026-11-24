# Sharing - Frontend requests

Le modifiche che il backend deve fare perché la pagina **Sharing** del gestionale funzioni come la vuole il frontend.
Il documento è pensato per chi le implementa (anche un agente): ogni sezione dice cosa cambiare, con payload, risposta,
regole ed errori attesi.

La pagina è `/settings/sharing`: due riquadri con le impostazioni dello Sharing dello stabilimento (rivendita dei giorni
e assegnazione dei crediti), sempre modificabili, e il bottone Salva, che rimanda tutte le impostazioni.

## Riepilogo

| # | Metodo | Rotta | Cosa cambia |
|---|---|---|---|
| 1 | GET + PUT | `/api/booking/sharing/settings` | Controllo di versione: `version` nella lettura, `If-Match` ed `ETag` nel salvataggio, 409 `resource.version_conflict` |
| 2 | PUT | `/api/booking/sharing/settings` | Un'opzione accesa vuole i suoi valori maggiori di 0 |

Il frontend usa le due API che il backend ha già (`Module.Booking/Endpoints/Endpoints.cs:144-151`, `SharingService.GetSettingsAsync` e
`SaveSettingsAsync`), con i loro nomi: rotte, campi, permessi, limiti, codici e voce del registro **restano come sono**, a parte quello che
chiede questo documento.

## Come sono oggi

- **Dati**: `booking.resale_settings` (`ResaleSettings`), una riga per stabilimento; `SharingSettingsDto` nella lettura e nel salvataggio.
- **Stabilimento dal token** (`BookingScope.PropertyId`): senza, 403 `booking.property_required`. Nessuna richiesta lo contiene.
- **Permessi**: `bookings.read` per leggere, `catalog.settings` per salvare. Senza: 403 `auth.forbidden`. Senza token valido: 401
  `auth.unauthenticated`.
- **Riga mancante**: la lettura dà i valori di partenza (`ResaleSettings.Default`) senza creare la riga; il salvataggio la crea.
- **Registro**: una voce `booking.sharing_settings_updated` per salvataggio.
- **Chi legge le impostazioni**: la cessione dei giorni (`POST /reservations/{id}/released-days` e quella dell'area cliente) e la maturazione dei
  crediti (`SharingLedger`): ogni salvataggio ha effetto subito.

```json
{
  "isEnabled": true,
  "minBookingDays": 3,
  "seasonalOnly": false,
  "allArticles": true,
  "absencesAsSharing": true,
  "manualCreditEnabled": false,
  "manualCreditUnitsPerDay": 1,
  "manualCreditAmount": 2.5,
  "voucherCreditEnabled": true,
  "voucherCreditAmount": 5.5,
  "cardTopUpEnabled": false,
  "cardTopUpAmount": 10
}
```

| Campo | Nella pagina | Regola (`SharingSettingsValidator`) |
|---|---|---|
| `isEnabled` | "Abilita" | |
| `minBookingDays` | "Durata minima prenotazione per applicare lo Sharing": l'interruttore è acceso se non è `null` | `null` o da 1 a 366 |
| `seasonalOnly` | "Abilita solo sugli stagionali" | |
| `allArticles` | "Abilita su tutti gli articoli" | |
| `absencesAsSharing` | "Imposta assenze programmate come Sharing" | |
| `manualCreditEnabled` | "Assegna crediti manualmente" | |
| `manualCreditUnitsPerDay` | "Numero crediti per giorno rivenduto" | Da 0 a 1000 |
| `manualCreditAmount` | "Valore credito" dei crediti manuali (€) | Da 0 a 100.000, al massimo 2 decimali |
| `voucherCreditEnabled` | "Crea voucher automatico" | |
| `voucherCreditAmount` | "Valore credito" del voucher (€) | Da 0 a 100.000, al massimo 2 decimali |
| `cardTopUpEnabled` | "Ricarica tessera prepagata" | |
| `cardTopUpAmount` | "Valore credito" della ricarica (€) | Da 0 a 100.000, al massimo 2 decimali |

- Il frontend manda **sempre tutti i campi**, anche quelli non cambiati; un numero lasciato vuoto vale 0, `minBookingDays` con l'interruttore spento è
  `null`.
- Il frontend controlla gli stessi limiti prima di salvare e mostra sotto i campi gli errori del backend (400 `validation.failed` con `errors[]`:
  `validation.out_of_range` fuori dai limiti, `validation.invalid` oltre i 2 decimali); ogni altro errore sotto i riquadri. Un utente senza
  stabilimento vede la pagina vuota e bloccata: il frontend non chiama l'API.

## 1. Controllo di versione

Oggi, se due utenti cambiano le impostazioni dello Sharing, vince chi salva per ultimo: Salva rimanda tutte le impostazioni lette all'apertura
della pagina e cancella, senza che nessuno se ne accorga, quello che un altro utente ha salvato nel frattempo. `ResaleSettings` non ha un token di
concorrenza e `PUT /sharing/settings` non guarda `If-Match`. Il frontend chiede lo stesso schema di **concorrenza ottimistica** degli altri
documenti, con un **codice di errore unico per tutte le API**: `resource.version_conflict`.

**Non è quello che Booking ha già per le prenotazioni** (`Endpoints.cs:253-255`, `ReservationService.cs:114-115`): lì l'`If-Match` è un numero, se
manca o non si legge non si controlla niente, e il conflitto è 412 o 409 `booking.version_conflict`. Qui valgono le regole sotto: con un altro
stato o un altro codice la pagina non rileggerebbe i dati.

### Cosa ci serve

- **Versione nella lettura**: `GET /api/booking/sharing/settings` restituisce, insieme alle impostazioni, il campo `version` (stringa opaca), allo
  stesso livello degli altri campi, anche senza riga salvata. Cambia ogni volta che un salvataggio cambia le impostazioni, e solo allora: due
  letture senza modifiche in mezzo danno la stessa versione. Ogni stabilimento ha la sua.
- **`If-Match` nel salvataggio**: il frontend manda la versione letta nell'header `If-Match: "<version>"`; il corpo della `PUT` non la contiene. Se
  non è più quella attuale, il backend non salva niente e risponde **409 `resource.version_conflict`**, senza `errors[]`. Con un `If-Match` assente
  o illeggibile risponde allo stesso modo: il frontend lo manda sempre.
- **`ETag` nella risposta**: il salvataggio riuscito risponde 200 con le impostazioni salvate, come oggi, e con la nuova versione nell'header
  `ETag: "<version>"`. La pagina resta aperta e la usa per il salvataggio successivo. Frontend e API stanno sulla stessa origine (`/api`): se non
  fosse così, il CORS dovrebbe esporre l'header `ETag`.
- **Impostazioni mai salvate**: il primo salvataggio con la versione letta riesce. Se un altro utente salva per primo, chi salva dopo con la stessa
  versione riceve il 409.
- **Riga eliminata** (`deleted_at` valorizzato, per esempio da un'importazione: nessun codice la elimina oggi): vale come impostazioni mai salvate. La
  lettura dà i valori di partenza e una `version`, e il salvataggio riesce. Oggi darebbe un 500 a ogni salvataggio: il filtro globale la nasconde
  (`ResaleSettings` è `ISoftDeletable`) ma l'indice unico `uq_booking_resale_settings_property` non ha il filtro.
- **Controllo e salvataggio sono una cosa sola**: di due salvataggi nello stesso momento con lo stesso `If-Match`, che cambiano le impostazioni in
  modo diverso, uno risponde 200 e l'altro 409 `resource.version_conflict`; mai i dati di tutti e due mescolati, mai un 500. Vale anche per il primo in
  assoluto: oggi la seconda riga viola `uq_booking_resale_settings_property` e `ResaleRepository.SaveChangesAsync` non traduce l'errore.
- **Salvataggio che non cambia niente**: con la versione attuale risponde 200; la versione può restare la stessa.
- Il 409 si controlla dopo la validazione del corpo (400) e dopo il 403 dello stabilimento.
- **Come farlo lo sceglie lo sviluppatore del backend** (il token `xmin` di Npgsql, un hash delle impostazioni, un contatore...): l'agente glielo chiede
  prima di scrivere codice. Il frontend tratta la versione come una stringa opaca e non la confronta mai.

### Cosa fa il frontend

Con 409 `resource.version_conflict` la pagina mostra l'avviso "Nel frattempo un altro utente ha modificato questi dati: li abbiamo ricaricati. Rifai
le tue modifiche." e rilegge le impostazioni con la `GET`. Dopo ogni salvataggio riuscito tiene la versione dell'`ETag`.

## 2. Un'opzione accesa vuole i suoi valori

Oggi si può salvare un'opzione accesa con il valore a 0 (per esempio un voucher automatico da 0 €). Il frontend non lo permette; il backend deve
controllarlo allo stesso modo:

| Campo | `code` | Quando |
|---|---|---|
| `manualCreditUnitsPerDay`, `manualCreditAmount` | `validation.out_of_range` | 0 con `manualCreditEnabled` vero |
| `voucherCreditAmount` | `validation.out_of_range` | 0 con `voucherCreditEnabled` vero |
| `cardTopUpAmount` | `validation.out_of_range` | 0 con `cardTopUpEnabled` vero |

- Con l'opzione spenta lo 0 va bene, e il valore si salva lo stesso. Le tre assegnazioni restano indipendenti.
- **Nota**: voucher automatico e ricarica della tessera oggi si salvano ma non fanno niente quando un giorno viene rivenduto, perché gli eventi
  `ResaleCreditEarned` e `ResaleCreditReversed` arrivano a gestori vuoti (`90-api-booking.md`, "Non ancora disponibile"). Non è una richiesta di questa
  pagina.
- **Dati già salvati con 0**: si leggono come sono; la pagina mostra subito l'errore sotto il campo e non salva finché l'utente non scrive il valore.

```json
{
  "type": "https://errors.summerbooking/validation/failed",
  "title": "validation.failed",
  "status": 400,
  "code": "validation.failed",
  "args": {},
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
  "errors": [{ "field": "voucherCreditAmount", "code": "validation.out_of_range" }]
}
```

## Verifica

Con un token che ha `bookings.read` e `catalog.settings`:

1. `GET /api/booking/sharing/settings` risponde con `version` (per esempio `v1`); `PUT` con le impostazioni lette, `isEnabled` cambiato e
   `If-Match: "v1"`: 200 con le impostazioni salvate e l'header `ETag` di una versione nuova; una nuova `GET` risponde con `isEnabled` cambiato e con
   quella versione.
2. La stessa `PUT` con `If-Match: "v1"` dopo la prima, senza `If-Match`, o con un `If-Match` qualunque: 409 `resource.version_conflict` senza
   `errors[]`; le impostazioni non cambiano.
3. Due `GET` senza salvataggi in mezzo: la stessa `version`. Un salvataggio di un altro stabilimento non la cambia.
4. Uno stabilimento senza riga: la `GET` risponde con i valori di partenza e una `version`, e non crea la riga; la `PUT` con quella versione e un campo
   cambiato: 200 con `ETag`. Una seconda `PUT` con la stessa versione di prima: 409. Due prime `PUT` diverse nello stesso momento: una 200 e una 409,
   mai un 500. Uno stabilimento con la riga eliminata (`deleted_at` valorizzato nel database di prova): come senza riga, la `PUT` dà 200.
5. `PUT` con una versione vecchia e un corpo non valido: 400 `validation.failed`; niente cambia.
6. Ogni assegnazione accesa con il suo valore a 0 (per i crediti manuali anche il numero): 400 `validation.failed` con
   `validation.out_of_range` su quel campo. Spenta con 0: 200. Le tre accese insieme con i loro valori: 200.
7. I limiti di oggi non cambiano: `minBookingDays` 0 o 367, `manualCreditUnitsPerDay` 1001, un importo 100000.01: `validation.out_of_range`; un importo
   2.555: `validation.invalid`.
8. Con un token senza `bookings.read` la `GET` dà 403 `auth.forbidden`; con uno che legge ma non ha `catalog.settings` la `PUT` dà 403
   `auth.forbidden`; con uno senza stabilimento 403 `booking.property_required`.
9. Dopo un salvataggio riuscito nel registro c'è una voce `booking.sharing_settings_updated`, come oggi; dopo un 409 no.
