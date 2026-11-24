# Sharing - Frontend requests

Le modifiche che il backend deve fare perché la pagina **Sharing** del gestionale funzioni come la vuole il frontend.
Il documento è pensato per chi le implementa (anche un agente): ogni sezione dice cosa creare, con payload, risposta,
regole ed errori attesi.

La pagina è `/settings/sharing`: due riquadri con le impostazioni dello Sharing dello stabilimento (rivendita dei giorni
e assegnazione dei crediti), sempre modificabili, e il bottone Salva, che rimanda tutte le impostazioni.

## Riepilogo

| # | Metodo | Rotta | Cosa fa |
|---|---|---|---|
| 1 | GET | `/api/booking/sharing/general-information` | Legge le impostazioni dello Sharing dello stabilimento |
| 2 | POST | `/api/booking/sharing/save-information` | Salva tutte le impostazioni |
| 3 | GET + POST | Le API 1 e 2 | Controllo di versione: `version` nella lettura, `If-Match` ed `ETag` nel salvataggio, 409 `resource.version_conflict` |

Sono tutte e due **nuove**: il modulo Booking oggi registra solo la persistenza (`Module.Booking/Module.cs`) e non ha endpoint. La radice è
`/api/booking`, come `/api/catalog` e `/api/identity` per i loro moduli.

## Regole comuni

- **Dati**: `booking.resale_settings` (`ResaleSettings`), una riga per stabilimento. Nel codice la tabella è ancora quella di partenza
  (`mode`, `both_options_disabled`, `credit_units_per_day`, `credit_calc`, `credit_amount`); la sua scheda,
  `shared documentation/backend/analisi-post/db/booking/booking_resale_settings.md`, ha già le colonne di questa pagina e dice di
  aggiornare entità, configurazione e migrazione con le prime API di Booking. Le due API usano **le colonne della scheda**: la modifica
  della tabella è la prima cosa da fare. Oggi nessun altro codice legge queste impostazioni.
- **Stabilimento dal token** (`ICurrentUser.PropertyId`, lo stesso uuid di `resale_settings.property_id`): nessuna richiesta lo contiene. Senza
  stabilimento nel token: 403 `catalog.property_required`, come le API di Catalog (il frontend ha già il testo). Se per Booking lo
  sviluppatore vuole un codice suo, l'agente lo scrive qui: il frontend deve aggiungerne il testo.
- **Permessi**: Booking non ne ha ancora (`Shared/Security/Permissions.cs`). Quali servono per leggere e per salvare lo decide lo
  sviluppatore del backend: l'agente glielo chiede prima di scrivere codice. Senza permesso: 403 `auth.forbidden`.
- **JSON** in camelCase, con i nomi esatti di questo documento.
- **Errori** nel formato Problem Details del backend (`Boot/Web/ProblemDetailsWriter.cs`), con `code`, `traceId` ed `errors[]` quando ci sono;
  gli errori di un campo con i nomi del corpo di questo documento. Senza token valido: 401 `auth.unauthenticated`.
- **Valori che non si leggono** (un testo al posto di un numero, un intero con i decimali o oltre i 32 bit): non arrivano ai validatori. 400
  `validation.failed` senza `errors[]`, come nelle altre pagine (`Impostazioni magazzino - Frontend requests.md`, "Valori che non si
  leggono"). Il frontend non li manda. Gli errori di campo di questo documento valgono per i valori che si leggono, e per quelli assenti o
  `null`.
- **Nessun valore del corpo dà un 500**, nemmeno per un vincolo del database o per due salvataggi nello stesso momento.

## Le impostazioni

Gli stessi campi nella lettura e nel salvataggio. Salva manda **sempre tutte** le impostazioni, anche quelle non cambiate.

| Campo | Colonna della scheda | Tipo | Regola |
|---|---|---|---|
| `isSharingEnabled` | `is_enabled` | boolean | "Abilita". Spento, le altre impostazioni si salvano lo stesso. |
| `isMinimumBookingDayEnabled` | `min_booking_days` non nullo | boolean | "Durata minima prenotazione per applicare lo Sharing". |
| `minimumBookingDayQuantity` | `min_booking_days` | number (intero) o `null` | "Giorni", da 1 a 2.147.483.647. Obbligatorio con `isMinimumBookingDayEnabled` vero; con falso si ignora e si salva nullo. |
| `isOnlySeasonalEnabled` | `seasonal_only` in `applies_to` | boolean | "Abilita solo sugli stagionali". |
| `isEnabledOnAllArticles` | `all_articles` in `applies_to` | boolean | "Abilita su tutti gli articoli". Può stare insieme al precedente. |
| `isScheduledAbsenceSharing` | `absences_as_sharing` | boolean | "Imposta assenze programmate come Sharing". |
| `isManualCreditAssignmentEnabled` | `manual_credit_enabled` | boolean | "Assegna crediti manualmente". |
| `resoldDayCreditQuantity` | `manual_credit_units_per_day` | number (intero) | "Numero crediti per giorno rivenduto", da 0 a 2.147.483.647; più di 0 con `isManualCreditAssignmentEnabled` vero. |
| `manualCreditValue` | `manual_credit_amount` | number (€) | "Valore credito" dei crediti manuali, da 0 a 100.000 con al massimo 2 decimali; più di 0 con `isManualCreditAssignmentEnabled` vero. |
| `isAutomaticVoucherEnabled` | `voucher_credit_enabled` | boolean | "Crea voucher automatico". |
| `voucherCreditValue` | `voucher_credit_amount` | number (€) | "Valore credito" del voucher, stessi limiti; più di 0 con `isAutomaticVoucherEnabled` vero. |
| `isPrepaidCardTopUpEnabled` | `card_top_up_enabled` | boolean | "Ricarica tessera prepagata". |
| `prepaidCardCreditValue` | `card_top_up_amount` | number (€) | "Valore credito" della ricarica, stessi limiti; più di 0 con `isPrepaidCardTopUpEnabled` vero. |

- **Le tre assegnazioni dei crediti sono indipendenti**, come nella scheda: si possono accendere insieme.
- **Un numero vuoto vale 0**: quando l'utente svuota un campo dei crediti il frontend manda 0, e la lettura dà 0 (le colonne non sono mai nulle).
- **Un'opzione spenta tiene il suo valore**: i crediti e gli importi si salvano anche con la casella spenta. Solo i giorni della durata minima si
  perdono, perché la colonna nulla vuol dire "nessun minimo".
- **Limiti nel database**: la scheda ha `min_booking_days > 0` e importi e crediti `>= 0`. Il "più di 0 con l'opzione accesa" e i 100.000 € li
  controllano le API.

## 1. GET /api/booking/sharing/general-information

La chiama la pagina all'apertura, al cambio lingua e dopo un conflitto di versione. Nessun parametro né corpo.

200 con tutti i campi di "Le impostazioni" e `version` (sezione 3):

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

- **Impostazioni mai salvate** (nessuna riga): 200 con i valori di partenza della scheda, cioè tutti i booleani falsi,
  `minimumBookingDayQuantity` `null`, crediti e importi 0, e una `version`. La lettura non crea la riga.
- **Dati già salvati fuori dalle regole**: si leggono come sono. Un'importazione può accendere i crediti manuali con un importo 0 (i vecchi
  valori in percentuale, dice la scheda): la pagina mostra subito l'errore sotto il campo e non salva finché l'utente non scrive il valore.

Errori, nell'ordine in cui si controllano: 401 `auth.unauthenticated`, 403 `auth.forbidden`, 403 `catalog.property_required`. Il frontend mostra
il testo del `code` sotto i riquadri; i campi restano bloccati e Salva spento finché una lettura non riesce.

## 2. POST /api/booking/sharing/save-information

Bottone Salva della pagina. Il corpo ha tutti i campi di "Le impostazioni", senza `version`, che va nell'header `If-Match` (sezione 3):

```json
{
  "isSharingEnabled": true,
  "isMinimumBookingDayEnabled": false,
  "minimumBookingDayQuantity": null,
  "isOnlySeasonalEnabled": false,
  "isEnabledOnAllArticles": true,
  "isScheduledAbsenceSharing": true,
  "isManualCreditAssignmentEnabled": true,
  "resoldDayCreditQuantity": 1,
  "manualCreditValue": 2.5,
  "isAutomaticVoucherEnabled": true,
  "voucherCreditValue": 5.5,
  "isPrepaidCardTopUpEnabled": false,
  "prepaidCardCreditValue": 0
}
```

- Se lo stabilimento non ha ancora la riga, il salvataggio la crea.
- Risposta: 204 senza corpo, con la nuova versione nell'header `ETag`. La pagina resta aperta, tiene i valori mandati e mostra "Impostazioni
  salvate".
- **Registro**: se il salvataggio scrive una voce, il codice lo sceglie lo sviluppatore e lo scrive qui (il filtro del Log della pagina Gestione
  utenti ha l'elenco dei codici).

### Errori

Nell'ordine in cui si controllano:

| Status | `code` | Quando |
|---|---|---|
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido. |
| 403 | `auth.forbidden` | Utente senza il permesso di salvare. |
| 400 | `validation.failed` | Un campo non valido (tabella sotto), con `errors[]`, anche più di uno insieme. |
| 403 | `catalog.property_required` | Token senza stabilimento. |
| 409 | `resource.version_conflict` | Sezione 3. |

| Campo | `code` | Quando |
|---|---|---|
| Ogni boolean | `validation.invalid` | Mancante (assente non vale `false`). |
| `minimumBookingDayQuantity` | `validation.out_of_range` | Con `isMinimumBookingDayEnabled` vero: mancante o meno di 1. |
| `resoldDayCreditQuantity` | `validation.out_of_range` | Mancante, meno di 0, o 0 con `isManualCreditAssignmentEnabled` vero. |
| `manualCreditValue` | `validation.out_of_range` | Mancante, meno di 0, oltre 100.000, con più di 2 decimali, o 0 con `isManualCreditAssignmentEnabled` vero. |
| `voucherCreditValue` | `validation.out_of_range` | Come `manualCreditValue`, con `isAutomaticVoucherEnabled`. |
| `prepaidCardCreditValue` | `validation.out_of_range` | Come `manualCreditValue`, con `isPrepaidCardTopUpEnabled`. |

Il frontend controlla gli stessi limiti prima di salvare (errori sotto i campi, Salva spento) e mostra sotto i campi anche quelli del backend; ogni
altro errore sotto i riquadri.

```json
{
  "type": "https://errors.summerbooking/validation/failed",
  "title": "validation.failed",
  "status": 400,
  "code": "validation.failed",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
  "errors": [{ "field": "voucherCreditValue", "code": "validation.out_of_range" }]
}
```

## 3. Controllo di versione sui salvataggi

Senza un controllo, se due utenti cambiano le impostazioni dello Sharing vince chi salva per ultimo: Salva rimanda
tutte le impostazioni lette all'apertura della pagina e cancella, senza che nessuno se ne accorga, quello che un altro
utente ha salvato nel frattempo. Il backend non ha un controllo di concorrenza sugli aggiornamenti (nessun token di
concorrenza nelle configurazioni delle entità, nessun `If-Match`). Il frontend chiede lo schema di **concorrenza
ottimistica** che il backend ha già progettato per le prenotazioni offline
(`shared documentation/backend/progettazione/19-frontend-offline.md`, sezioni 4.4 e 5.8), con un **codice di errore
unico per tutte le API**: `resource.version_conflict`.

### Cosa ci serve

- **Versione nella lettura**: `GET /api/booking/sharing/general-information` restituisce, insieme alle impostazioni, il campo
  `version` (stringa opaca), allo stesso livello degli altri campi (esempio nella sezione 1). La versione cambia ogni volta che
  cambiano le impostazioni che `save-information` scrive, anche quando le cambia un'altra API, e solo allora: due letture senza
  salvataggi in mezzo danno la stessa versione. Ogni stabilimento ha la sua: un salvataggio di un altro stabilimento non la cambia.
- **`If-Match` nel salvataggio**: il frontend manda la versione letta nell'header `If-Match: "<version>"`; il corpo di
  `save-information` non contiene la versione. Se non è più quella attuale, il backend non salva niente e risponde **409
  `resource.version_conflict`**, senza `errors[]`. Senza `If-Match` risponde allo stesso modo: il frontend lo manda sempre.
- **`ETag` nella risposta**: il salvataggio riuscito risponde 204 senza corpo con la nuova versione nell'header
  `ETag: "<version>"`. La pagina resta aperta e la usa per il salvataggio successivo.
- **Impostazioni mai salvate**: anche uno stabilimento che non ha ancora le impostazioni dello Sharing riceve dalla
  lettura una `version`, e il primo salvataggio con quella versione riesce. Se un altro utente salva per primo, chi
  salva dopo con la stessa versione riceve il 409.
- **Controllo e salvataggio sono una cosa sola**: di due salvataggi nello stesso momento con lo stesso `If-Match`, anche il primo in assoluto,
  uno risponde 204 e l'altro 409 `resource.version_conflict`; mai i dati di tutti e due mescolati, mai un 500 (l'indice unico
  `uq_booking_resale_settings_property` fa fallire la seconda riga).
- Il 409 `resource.version_conflict` si controlla dopo la validazione del corpo (400 `validation.failed`).
- **Come farlo lo sceglie lo sviluppatore del backend** (il token `xmin` di Npgsql come nel documento offline, un hash
  delle impostazioni, un contatore...): l'agente glielo chiede prima di scrivere codice. Il frontend tratta la versione
  come una stringa opaca e non la confronta mai.

### Cosa fa il frontend

Con 409 `resource.version_conflict` la pagina mostra l'avviso "Nel frattempo un altro utente ha modificato questi
dati: li abbiamo ricaricati. Rifai le tue modifiche." e rilegge le impostazioni con la `GET`. Dopo ogni salvataggio
riuscito tiene la versione dell'`ETag`.

## Verifica

Con un token di un utente dello stabilimento che può leggere e salvare le impostazioni:

1. `GET /api/booking/sharing/general-information` risponde con `version` (per esempio `v1`);
   `POST /api/booking/sharing/save-information` con le impostazioni lette, `isSharingEnabled` cambiato e `If-Match: "v1"`: 204
   senza corpo con l'header `ETag` di una versione nuova, diversa da `v1`; una nuova `GET` risponde con
   `isSharingEnabled` cambiato e con quella versione.
2. Lo stesso `POST` con `If-Match: "v1"` dopo il primo: 409 `resource.version_conflict` senza `errors[]`; le
   impostazioni non cambiano.
3. `POST` senza `If-Match`: 409 `resource.version_conflict`; le impostazioni non cambiano.
4. Due `GET` senza salvataggi in mezzo: la stessa `version`. Un salvataggio delle impostazioni di un altro stabilimento
   non cambia la `version` di questo.
5. Uno stabilimento senza impostazioni salvate: la `GET` risponde con i valori di partenza e una `version`, e non crea la riga; il `POST` con
   quella versione: 204 con `ETag`. Un secondo `POST` con la stessa versione di prima: 409 `resource.version_conflict`. Due primi `POST` nello
   stesso momento: uno 204 e uno 409, mai un 500.
6. `POST` con `If-Match` di una versione vecchia e un corpo che la validazione rifiuta: 400 `validation.failed` (la
   validazione viene prima della versione); le impostazioni e la versione non cambiano.
7. `POST` con ogni campo diverso dal valore di partenza: la `GET` li dà uguali, uno per uno. `isOnlySeasonalEnabled` e
   `isEnabledOnAllArticles` accesi insieme, e le tre assegnazioni dei crediti accese insieme: 204.
8. `POST` con `isMinimumBookingDayEnabled` falso e `minimumBookingDayQuantity` 3: 204; la `GET` dà `isMinimumBookingDayEnabled` falso e
   `minimumBookingDayQuantity` `null`. Con vero e `null` o 0: 400 `validation.out_of_range` su `minimumBookingDayQuantity`.
9. Ogni assegnazione accesa con il suo valore a 0 (per i crediti manuali anche il numero): 400 `validation.out_of_range` su quel campo. Spenta con
   0: 204. Spenta con un valore: 204 e la `GET` lo dà.
10. Un importo 100000.01, -1 o 2.555: 400 `validation.out_of_range`. Senza un boolean: 400 `validation.invalid` su quel campo. Giorni 3000000000:
    400 `validation.failed`. Mai un 500.
11. Le due API senza token: 401; con un token senza il permesso: 403 `auth.forbidden`; con un token senza stabilimento: 403
    `catalog.property_required`.
