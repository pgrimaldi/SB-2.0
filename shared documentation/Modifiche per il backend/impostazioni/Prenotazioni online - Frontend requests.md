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
- **Senza effetto oggi**: solo il supplemento arriva al preventivo; `isEnabled`, `requiresManualConfirmation` e
  `paymentMethods` dei servizi si salvano ma Booking usa quelli delle postazioni (`BookingCatalog.RulesAsync`). Nota, non
  richiesta.
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
- **Salvataggio che non cambia niente**: con la versione attuale risponde 200 e la versione resta la stessa.
- L'ordine di `paymentMethods` non conta. La `version` sta anche nel corpo della risposta della `PUT`, uguale all'`ETag`; si
  ricavano rileggendo i dati salvati, così sono quelle della prossima `GET`.
- `ServiceOnlineSettingsDto` finisce anche negli snapshot (`ConfigSnapshotBuilder`, `ServicesOnline` e `Settings`): lì
  `version` non ci va (per esempio con un DTO a parte per la lettura del gestionale).
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
  `payment.gateway_connected_accounts` (`ConnectedAccount`), al massimo una riga **non eliminata** per stabilimento
  (`ConnectedAccountConfiguration.cs:66-69`); oggi nessun codice la legge (solo entità e configurazione).
- **Richiesta**: nessun corpo e nessun parametro.
- **Risposta**: 200 con il solo id del conto, `StripeAccountId`, come stringa JSON (non un oggetto), per esempio
  `"acct_1QmDemo4Fz8Lw2Xc"`. `null` se lo stabilimento non ha un conto collegato: nessuna riga, riga eliminata
  (`deleted_at`) o collegamento revocato (`RevokedAt` valorizzato). Lo stato dell'onboarding non conta. Con `null` il
  frontend accetta sia il corpo `null` sia un corpo vuoto.
- **Non restituisce altro**: nessun dato del conto, nessuna chiave. Non scrive niente e non chiama Stripe.
- **Permesso**: `catalog.settings` (`Permissions.CatalogSettings`), come il salvataggio delle opzioni della pagina.
- **Nota**: oggi nessun codice valorizza `RevokedAt`. Finché non c'è il webhook di Stripe, l'id può essere di un conto che
  il gestore ha già scollegato da Stripe.

| Status | `code` | Quando |
|---|---|---|
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido |
| 403 | `auth.forbidden` | Utente senza il permesso |
| 403 | `payment.property_required` | Token senza stabilimento (`PropertyOf`, come le altre API di Payment) |

Se la chiamata fallisce la pagina resta com'è, senza messaggi.

## 3. GET + PUT `/api/catalog/online-settings`: tutte le opzioni del pannello

Il pannello "Opzioni prenotazioni online" legge tutto con **una `GET`** quando si apre e salva tutto con **una `PUT`**.
Oggi il backend ha tre gruppi di API (`Endpoints/OnlineSettingsEndpoints.cs:19-68`): le opzioni, i limiti uno per uno
(`/stay-limits`) e la legenda (`/legend`). Salvare con tante chiamate può fermarsi a metà. Le API `/stay-limits` e
`/legend` **restano come sono**: il frontend non le usa.

### Cosa si aggiunge alla lettura e al salvataggio

Tutti i campi di oggi (`OnlineSettingsDto`, `SaveOnlineSettingsRequest`) restano, con gli stessi nomi e le stesse regole. In
più:

| Campo | Lettura | Salvataggio |
|---|---|---|
| `stayLimits` | `{ id, sectorId, startsOn, endsOn, minDays, maxDays }[]`, in ordine di `startsOn` e poi di fila, come oggi `GET /stay-limits` | Gli stessi campi; `id` `null` per un limite nuovo |
| `legend` | `{ id, label, iconId, iconUrl }[]`, nell'ordine della legenda | Gli stessi campi; `id` `null` per un elemento nuovo |
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

- Il corpo della `PUT` ha gli stessi campi, senza `isSuspendedByPlatform` e `version`; gli orari arrivano come `HH:mm`. La
  risposta è 200 con le opzioni salvate nella forma della lettura, come oggi, con la `version` nuova.
- Il frontend manda **sempre tutti i campi e tutte le righe**, anche quello che il pannello non mostra (`allowedSpotIds`,
  `isBlocked`, i periodi e gli orari di un'opzione spenta): tornano come sono arrivati.
- **Liste assenti o `null`**: `stayLimits` e `legend` assenti o `null` danno 400 `validation.required` su quel campo; solo
  `[]` svuota. Oggi una lista `null` vale vuota (`OnlineSettingsService.cs:59` e `:76`): con i limiti e la legenda un corpo
  incompleto cancellerebbe tutto.
- **Id ripetuti**: due righe con lo stesso `id` in `stayLimits` o in `legend` danno 400 `validation.invalid` su
  `stayLimits[i].id` o `legend[i].id` (la seconda).
- **Permessi** come oggi: `catalog.read` per leggere, `catalog.settings` per salvare.
- **Una transazione sola** per opzioni, postazioni, periodi, limiti e legenda: se qualcosa non va non cambia niente. Oggi
  `SaveAsync` salva i periodi in due passi fuori da una transazione (`ClearPeriods` e poi `AddPeriods`): se il secondo
  fallisce i periodi restano cancellati.
- **Snapshot e ricerca pubblica si aggiornano una volta sola, dopo il commit**, e un loro errore non annulla il salvataggio.
  Oggi `UnitOfWork.SaveChangesAsync` li rifà dopo ogni `SaveChanges` (`CatalogRepositories.cs:34-66`): dentro una
  transazione aperta lo snapshot, che ha un'altra connessione, leggerebbe i dati vecchi, e una query della ricerca fallita
  annullerebbe la transazione. Una transazione aperta senza passare da `UnitOfWork` (come
  `SeasonRepository.ReplacePeriodAsync`) non li aggiornerebbe affatto.
- **Cosa non finisce negli snapshot**: `OnlineSettingsDto` è anche negli snapshot (`ConfigSnapshotBuilder`, `Online`). Lì
  `version`, `stayLimits` (che c'è già come `StayLimits`) e `legend` non ci vanno: un cambio della legenda o di una
  postazione rigenera solo `beach_map` (`SnapshotTriggers`), quindi resterebbero vecchi. Per esempio con un DTO a parte per
  il gestionale. `onlineBookingBlock` invece ci va; dopo la migration gli snapshot vanno rigenerati (all'avvio si costruiscono solo
  quelli mancanti): lo sviluppatore decide anche `CurrentSchema`.
- **Altre risposte invariate**: `LegendItemDto` esce anche dal sito pubblico, dallo snapshot `beach_map` e da `/legend`, e
  `StayLimitDto` da `/stay-limits`: quelle risposte non cambiano (per l'`id` della legenda serve un DTO a parte).
- **Registro**: una voce `catalog.online_settings_updated` per salvataggio, come oggi; in più `catalog.stay_limits_updated` e
  `catalog.online_legend_updated` quando cambiano i limiti o la legenda, una ciascuna. Nessuna voce dopo un errore.

### Limiti di booking (`stayLimits`)

- La lista del corpo **sostituisce** quella salvata: un limite con `id` si **aggiorna** e tiene il suo `id`, uno senza `id` si
  crea, uno salvato che non c'è nel corpo si elimina (come fa oggi `DELETE /stay-limits/{id}`). Le righe tenute non si
  eliminano e ricreano: `uq_catalog_property_online_stay_limits_public_id` non ha il filtro sulle eliminate.
- Regole di oggi (`SaveStayLimitRequestValidator`): `endsOn` non prima di `startsOn`, almeno uno fra minimo e massimo, da 1 a
  366 giorni, il massimo non sotto il minimo.
- **Fila e date obbligatorie**: per la fila è la regola di oggi (`NotEmpty`), per le date è nuova. Oggi `StartsOn` e `EndsOn`
  sono `DateOnly` (`SaveStayLimitRequest`): assenti valgono 0001-01-01 e `null` dà un 400 senza `errors[]`. Nel DTO della
  `PUT` i tre campi sono nullable, così danno `validation.required`; `endsOn` si confronta con `startsOn` solo se ci sono
  tutti e due.
- **Fila eliminata**: non capita dall'editor (una fila con limiti non si elimina, `SectorService`), solo con importazioni o
  salvataggi insieme. Il limite si legge con l'id di quella fila: oggi `ToDtoAsync` dà l'id vuoto
  (`00000000-0000-0000-0000-000000000000`). Salvarlo con quell'id risponde 404 `catalog.sector_not_found`: l'utente sceglie
  un'altra fila o toglie la regola.
- Il vincolo `ex_catalog_property_online_stay_limits_no_overlap` non è differibile: devono riuscire, senza 500, uno scambio
  di date fra due limiti della stessa fila, una catena di tre, un limite eliminato e uno nuovo negli stessi giorni, un
  cambio di fila. Conta lo stato finale.
- **Quantità**: oggi nessun tetto (i periodi ne hanno 50, le postazioni 5.000, la legenda 30). Se serve un tetto l'agente lo
  chiede e lo scrive qui, con il suo errore.

### Legenda (`legend`)

- La lista del corpo sostituisce quella salvata, nell'ordine ricevuto, come `PUT /legend` oggi: al massimo 30 elementi,
  `label` obbligatoria fino a 100 caratteri. Un'etichetta più lunga già salvata (la colonna è `varchar(120)`) si legge
  intera; il 400 al salvataggio resta e il frontend lo mostra sotto quell'elemento.
- **La legenda è dello stabilimento, non delle opzioni** (`MapLegendItem.PropertyId`): si legge anche quando le opzioni non
  sono mai state salvate o la loro riga è eliminata.
- `catalog.map_legend_items` ha bisogno di un id pubblico: `public_id uuid NOT NULL` con indice unico, valorizzato per le righe
  esistenti, anche eliminate (come `20261025193541_OnlineBookingOptions` per i limiti). `PUT /legend`, che resta, deve
  valorizzarlo.
- Un elemento mandato con il suo `id` lo tiene: la riga si **aggiorna**, non si elimina e ricrea. Un `id` che non è dello
  stabilimento vale come un elemento nuovo, con un `id` nuovo (mai quello mandato).
- **Icona**:
  - `iconId` con valore: l'icona del catalogo. Se è lo stesso letto, l'elemento tiene la sua icona anche se non è più attiva;
    se è diverso deve essere un'icona attiva, altrimenti 404 `map.icon_not_found`;
  - `iconId` `null` con `iconUrl` valorizzato: l'elemento tiene l'icona che ha (per esempio un'icona del vecchio gestionale,
    che non è nel catalogo). Il valore di `iconUrl` non si usa: conta solo che non sia `null`;
  - `iconId` `null` con `iconUrl` `null`: nessuna icona.

  Oggi `SaveLegendAsync` toglie sempre l'icona con `iconId` `null`, e con l'id di un'icona spenta risponde 404
  (`FindIconAsync` vuole `IsActive`, `ListIconsByAssetAsync` no).
- Se più icone hanno lo stesso file, `iconId` è sempre lo stesso fra due letture (oggi `ListIconsByAssetAsync` non ha un
  ordine), preferendo quella attiva; "la stessa icona" si confronta sul file salvato.
- **Catalogo delle icone**: la legenda sceglie fra quelle di `GET /api/catalog/map/icon-catalog`, senza gli sfondi
  (`template`). Il catalogo deve quindi essere popolato, anche se per Configura mappa si sceglie la strada senza catalogo
  (`Configura mappa - Frontend requests.md`, scelta 1): le icone della legenda possono essere diverse da quelle della mappa.

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

- Quando mancano **meno** ore di quelle scelte il cliente non può più prenotare online (con esattamente 3 ore può ancora); dal
  gestionale non cambia niente, e nemmeno la conferma del gestore di una richiesta già arrivata.
- **Solo con almeno una postazione**: un ordine online di soli servizi non si blocca (altrimenti chi è già in spiaggia non
  potrebbe ordinare dal bar). Oggi anche quelle prenotazioni passano da `ValidateOnline`
  (`ReservationValidators.cs:308-310`, `ReservationWriter.QuoteAsync`): il controllo guarda se c'è una postazione.
- Vale per il preventivo online (`QuoteService.ValidateOnline`, `POST /api/catalog/public/bathhouses/{slug}/quote`) e per la
  prenotazione online (`POST /api/booking/public/{slug}/reservations`).
- **Da quale momento si contano le ore** lo decide lo sviluppatore del backend: l'agente glielo chiede prima di scrivere
  codice, con questi fatti:
  - Booking conta già le "ore prima" di un annullamento dalle 00:00 italiane del primo giorno
    (`CustomerCancellation.Deadline`, `CustomerArea.cs:100-106`);
  - contando dalle 00:00, con qualunque valore diverso da `disabled` non si prenota più per il giorno stesso
    (`allowsSameDayBooking`) e la conversione pomeridiana non serve più: sono opzioni dello stesso pannello;
  - l'orario di apertura esiste (`Bathhouse.OpenHoursFrom`) ma può mancare;
  - una mezza giornata pomeridiana comincia dopo (`afternoonFrom`, che può mancare);
  - l'ora è quella italiana (`ItalianTime`), anche con l'ora legale.
- Lo sviluppatore decide anche il codice che riceve il cliente (con le ore fra gli `args`) e se esporre il valore nelle regole
  pubbliche (`PublicOnlineRulesDto`): oggi il sito conosce il giorno stesso e l'orizzonte, e la disponibilità
  (`GET /api/booking/public/{slug}/availability`) non applica regole; senza, il cliente scopre il blocco solo alla fine.
- **Non è `isBlocked`**: `isBlocked` resta quello che è oggi nel codice, il blocco del pagamento online
  (`IsOnlinePaymentBlocked`, usato da `OnlineBookingService` per i metodi con carta), anche se 89-api-catalog.md e
  `OnlineSettingsDtos.cs` lo chiamano «Blocca prenotazioni online». Non va riusato né rinominato.
- **Valore di partenza**, anche per gli stabilimenti che hanno già le opzioni salvate: `disabled`. Il vecchio
  `blockReservation` ha tre descrizioni diverse nei documenti: l'agente chiede allo sviluppatore se conteneva ore da portare.
  Colonna o enum li sceglie lo sviluppatore; i codici JSON sono quelli della tabella. `onlineBookingBlock` assente, `null` o
  sconosciuto: `validation.invalid`.

### Controllo di versione

Le stesse regole della sezione 1, con queste differenze:

- La versione copre tutto quello che la `PUT` scrive: opzioni, postazioni, periodi, limiti e legenda. Cambia anche quando li
  cambiano altre API: `/stay-limits`, `/legend`, l'editor della mappa che elimina una postazione scelta (anche con
  l'eliminazione di una riga o colonna e con `POST /map/reset`), il systemadmin che elimina un'icona usata dalla legenda
  (`PlatformMapCatalogService.DeleteIconAsync` passa la legenda a un'altra icona).
- **Non la cambiano**: spostare una postazione (l'ordine di `allowedSpotIds` e di `paymentMethods` non conta), sostituire
  l'immagine di un'icona (`iconUrl` non conta) e `isSuspendedByPlatform`.
- **Opzioni mai salvate o riga eliminata** (`property_online_booking_settings` è `ISoftDeletable` e il suo indice unico non ha
  il filtro): `stayLimits` è vuoto, `legend` è quella salvata e la versione la copre. Salvando, periodi, postazioni e limiti
  della riga eliminata non ricompaiono e non contano più (oggi `CountUsagesAsync` conterebbe i suoi limiti ancora vivi, e la
  fila non si eliminerebbe per regole che nessuno vede).
- **Mai un 500** nemmeno quando la `PUT` arriva insieme a `/stay-limits`, `/legend` o all'editor della mappa: uno dei due
  risponde con un 409 (oggi la violazione di `ex_catalog_property_online_stay_limits_no_overlap` non è tradotta).
- Con 409 il pannello si chiude e compare l'avviso; alla prossima apertura rilegge tutto.

### Errori della `PUT`

Nell'ordine in cui si controllano:

| Status | `code` | Quando |
|---|---|---|
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido |
| 403 | `auth.forbidden` | Utente senza il permesso |
| 400 | `validation.failed` | Uno o più campi non validi, in `errors[]` (vedi sotto) |
| 403 / 404 | `catalog.property_required` / `catalog.property_not_found` | Token senza stabilimento / stabilimento che non esiste più |
| 409 | `resource.version_conflict` | `If-Match` assente, illeggibile o non più attuale |
| 404 | `catalog.sector_not_found` | Una fila dei limiti che non esiste (più) |
| 404 | `catalog.stay_limit_not_found` | Un `id` di limite che non è dello stabilimento |
| 404 | `map.icon_not_found` | Un'icona nuova della legenda che non è attiva nel catalogo |
| 422 | `map.spot_not_found` | Una postazione di `allowedSpotIds` che non c'è (più), con `args.ids` |
| 409 | `catalog.stay_limit_overlap` | Due limiti della stessa fila con giorni in comune |

In `errors[]`: quelli di oggi (`SaveOnlineSettingsRequestValidator`) e

| `field` | `code` | Quando |
|---|---|---|
| `onlineBookingBlock` | `validation.invalid` | Assente, `null` o sconosciuto |
| `stayLimits`, `legend` | `validation.required` | Assenti o `null` |
| `stayLimits[i].id`, `legend[i].id` | `validation.invalid` | `id` ripetuto |
| `stayLimits[i].sectorId`, `.startsOn`, `.endsOn` | `validation.required` | Assenti o `null` |
| `stayLimits[i].endsOn` | `validation.end_before_start` | Prima di `startsOn` |
| `stayLimits[i].minDays` | `validation.invalid` | Né minimo né massimo |
| `stayLimits[i].minDays`, `.maxDays` | `validation.out_of_range` (`args.from` 1, `args.to` 366) | Fuori dai limiti |
| `stayLimits[i].maxDays` | `validation.out_of_range` (`args.value`) | Sotto il minimo |
| `legend` | `validation.invalid` | Più di 30 elementi |
| `legend[i].label` | `validation.required`, `validation.max_length` (`args.max` 100) | Vuota / troppo lunga |

Il frontend controlla prima le stesse regole. Gli errori dei campi del primo livello stanno sotto i campi; quelli delle righe
(`stayLimits[i]`, `legend[i]`, `allowedPeriods[i]`) e gli altri in fondo al pannello, con il testo del primo `code`.

**Note** (non richieste): `maxSpotsPerBooking`, `afternoonConversionTime` e `isMapPick` oggi si salvano ma nessun controllo li
usa. Con `isLocationRestricted` e nessuna postazione, o `isPeriodRestricted` e nessun periodo, il preventivo online rifiuta
tutto: il frontend non lo permette (vuole almeno un periodo, e non accende l'interruttore senza postazioni); al backend non è
chiesta la stessa regola.

## 4. POST `/api/payment/connect-stripe`

Il gestore preme "Configura il tuo account Stripe" e va su Stripe Connect (OAuth, `scope=read_write`); Stripe lo rimanda al
gestionale con `code` e `state`. Il frontend controlla lo `state` e manda il `code` a questa API.

- **Dove**: nel gruppo `/api/payment` di `Module.Payment/Endpoints/Endpoints.cs`.
- **Prerequisiti**: oggi nel codice non c'è nessun pacchetto né configurazione Stripe (`PendingIntegrations.cs`); i disegni sono
  in 17-secrets.md (`Stripe__SecretKey`) e 35-integrazioni-esterne.md (`IPaymentGateway`). La chiave deve essere della stessa
  piattaforma e della stessa modalità (prova o produzione) del `client_id` del frontend. Pacchetto e configurazione li
  decide lo sviluppatore.
- **Permesso**: `catalog.settings`. Una sessione impersonata dal systemadmin riceve 403 `auth.forbidden`: non collega il conto
  che riceve i soldi.
- **Richiesta**: `{ "code": "ac_..." }`, da 1 a 500 caratteri.
- **`state`**: oggi lo genera il frontend e lo tiene nella scheda del browser; la pagina di ritorno controllerà anche che
  stabilimento e utente siano quelli di partenza. L'agente chiede allo sviluppatore se lo `state` deve emetterlo e verificarlo
  il backend, legato a utente e stabilimento (nel vecchio sistema portava lo stabilimento): in quel caso servono un'API che lo
  emette e il campo `state` in questa.
- **Cosa fa**: controlla che lo stabilimento esista (come farlo da Payment lo decide lo sviluppatore), scambia il `code` con Stripe (token OAuth di Connect) con la chiave segreta
  della piattaforma, che sta solo nel backend, e salva il conto in `payment.gateway_connected_accounts` per lo stabilimento del
  token: `StripeAccountId`, `ConnectedAt`, `RevokedAt` `null`, `ChargesEnabled`, `PayoutsEnabled` e `OnboardingState`, che si
  ricava dallo stato del conto letto da Stripe (la regola, e cosa fare se quella lettura fallisce, le decide lo sviluppatore).
  - Nessuna riga, o riga eliminata: si crea.
  - Lo stesso conto di prima, anche revocato: si aggiorna la riga.
  - Un **conto diverso**: i profili dei clienti (`payment.customer_gateway_profiles`) sono legati alla riga e i loro `cus_…`
    vivono sul conto vecchio. Non devono restare validi per il conto nuovo: come trattarli, se tenere la riga vecchia come
    revocata e quali eventi pubblicare lo decide lo sviluppatore.
- **I token di Stripe non si salvano** (come dice `analisi-post/db/payment/payment_gateway_connected_accounts.md`): si lavora con
  la chiave della piattaforma e l'id del conto.
- **Nessun nuovo tentativo automatico** dello scambio: il `code` vale una volta sola. Il gestore standard della resilienza
  (35-integrazioni-esterne.md, `AddStandardResilienceHandler`) qui va senza ritentativi. Mai `deauthorize` dopo un 409:
  staccherebbe il conto dell'altro stabilimento.
- **"Niente cambia" vale per il database**: se l'errore arriva dopo lo scambio, su Stripe il collegamento c'è già. Dopo una
  risposta persa il frontend rilegge `get-stripe-information` prima di mostrare l'errore.
- **Risposta**: 200 con il solo id del conto, come `get-stripe-information`.
- **Registro**: una voce `payment.stripe_account_connected` (categoria `Operator`), con l'id del conto precedente e di quello nuovo
  fra i metadati. Il frontend la aggiungerà alle azioni del Log.
- **Sicurezza**: `code`, token e chiavi non compaiono mai in risposte, log o registro.
- **Chiamate insieme**: due chiamate per lo stesso stabilimento, o due stabilimenti con lo stesso conto, non danno mai un 500
  (`uq_payment_gateway_connected_accounts_property` e `..._stripe_account`): con due conti diversi vince uno solo.
- **Conto revocato altrove**: `uq_payment_gateway_connected_accounts_stripe_account` guarda solo `deleted_at`, quindi una riga
  revocata di un altro stabilimento occupa ancora il conto. Non deve bloccare il collegamento: come liberarlo (indice o
  eliminazione logica) lo sceglie lo sviluppatore.

| Status | `code` | Quando |
|---|---|---|
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido |
| 403 | `auth.forbidden` | Utente senza il permesso, o sessione impersonata |
| 400 | `validation.failed` | `code` assente, vuoto (`validation.required`) o oltre 500 caratteri (`validation.max_length`) |
| 403 / 404 | `payment.property_required` / `catalog.property_not_found` | Token senza stabilimento / stabilimento che non esiste più |
| 422 | `payment.stripe_code_invalid` | Stripe rifiuta il `code` (`invalid_grant`: scaduto, già usato, di un'altra piattaforma): niente cambia |
| 409 | `payment.stripe_account_taken` | Il conto è già collegato, e non revocato, a un altro stabilimento: niente cambia |
| 503 | `payment.stripe_unavailable` | Stripe non risponde, risponde 429 o 5xx, o scade il tempo: niente cambia |

Un altro errore di Stripe (per esempio la chiave della piattaforma sbagliata) è un 500 come gli altri errori non gestiti, senza
dettagli nella risposta.

La pagina di ritorno del frontend arriverà insieme a questa API, quando sarà registrato in Stripe l'indirizzo di ritorno.

## Verifica

Le prove con `deleted_at`, `revoked_at`, icone fuori catalogo o etichette lunghe si preparano nel database di prova; quelle sulle
icone spente o eliminate vogliono un token del systemadmin.

API 1, con un token di un utente dello stabilimento che può leggere e salvare le opzioni:

1. `GET /api/catalog/online-settings/services` risponde con `version` (per esempio `v1`); la `PUT` con le opzioni lette,
   `surchargeValue` cambiato e `If-Match: "v1"`: 200 con le opzioni salvate, la `version` nuova e lo stesso valore nell'header
   `ETag`, diverso da `v1`; una nuova `GET` risponde con `surchargeValue` cambiato e con quella versione.
2. La stessa `PUT` con `If-Match: "v1"` dopo la prima: 409 `resource.version_conflict` senza `errors[]`; le opzioni non
   cambiano.
3. `PUT` senza `If-Match`, con `If-Match: abc` e con `If-Match: "altro"`: 409 `resource.version_conflict`; le opzioni non
   cambiano.
4. Due `GET` senza salvataggi in mezzo, e una `PUT` che non cambia niente: la stessa `version`. Un salvataggio delle opzioni di
   un altro stabilimento non cambia la `version` di questo.
5. Uno stabilimento senza opzioni salvate: la `GET` risponde con una `version`; la `PUT` con quella versione: 200 con
   `ETag`. Una seconda `PUT` con la stessa versione di prima: 409 `resource.version_conflict`.
6. Due `PUT` nello stesso momento con lo stesso `If-Match` e valori diversi, anche su uno stabilimento senza opzioni
   salvate: una 200, l'altra 409; nessun 500.
7. Uno stabilimento con la riga eliminata (`deleted_at`): la `GET` risponde con i valori di partenza e una `version`; la
   `PUT` con quella versione: 200.
8. `PUT` con `If-Match` di una versione vecchia e un corpo che la validazione rifiuta (`surchargeType` `percentage` e
   `surchargeValue` 101): 400 `validation.failed` con `validation.invalid` su `surchargeValue`; le opzioni e la versione
   non cambiano. `sectionTitle` di 2.000 caratteri: 200; di 2.001: 400 con `validation.max_length` e `args.max` 2000.

API 2:

9. Stabilimento con un conto collegato: 200 con la stringa `acct_…` e `Content-Type: application/json`.
10. Stabilimento senza riga, con la riga eliminata o con `revoked_at` valorizzato: 200 con `null` o corpo vuoto.
11. Il token di un altro stabilimento non vede mai il conto di questo. Utente senza `catalog.settings`: 403
    `auth.forbidden`. Token senza stabilimento: 403 `payment.property_required`.

API 3:

12. La `GET` risponde con opzioni, `stayLimits`, `legend`, `onlineBookingBlock` e `version` anche in uno stabilimento senza
    opzioni salvate (`onlineBookingBlock` `disabled`, `stayLimits` vuoto, `legend` quella salvata con `PUT /legend`). Rimandando
    tutto con la `PUT` e la `version` letta: 200 e niente cambia, nemmeno `allowedSpotIds`, `isBlocked` e gli `id` di limiti ed
    elementi della legenda.
13. `PUT` con un limite cambiato, uno nuovo e uno in meno: la lettura dopo ha il primo con lo stesso `id`, il secondo con un
    `id` nuovo e non ha più il terzo. Scambiare le date di due limiti della stessa fila, e togliere un limite aggiungendone uno
    negli stessi giorni: 200.
14. `PUT` con due limiti sovrapposti della stessa fila: 409 `catalog.stay_limit_overlap`. Con una fila inesistente: 404
    `catalog.sector_not_found`. Con un `id` di limite di un altro stabilimento: 404 `catalog.stay_limit_not_found`. Con una
    postazione inesistente: 422 `map.spot_not_found`. In tutti e quattro i casi opzioni, periodi, limiti e legenda non cambiano.
15. Un elemento della legenda con un'icona spenta, e uno con un'icona fuori dal catalogo, rimandati come sono: 200 e la lettura
    dopo ha lo stesso `iconUrl`. Con `iconId` e `iconUrl` `null`: senza icona. Un `iconId` inesistente: 404
    `map.icon_not_found` e niente cambia, nemmeno i periodi. Un `id` sconosciuto: l'elemento nasce con un `id` nuovo.
16. 400 con il suo `errors[]`: `stayLimits` o `legend` assenti o `null`; `id` ripetuti; un limite senza fila, senza date, senza
    minimo e massimo, con il massimo sotto il minimo o con 367 giorni; 31 elementi; `label` vuota e di 101 caratteri;
    `onlineBookingBlock` assente e sconosciuto. In ognuno niente cambia.
17. Versione: le prove 1-7 con `bookingHorizonDays` al posto di `surchargeValue`. In più `POST /stay-limits`, `PUT /legend`,
    l'eliminazione di una postazione scelta e quella di un'icona usata dalla legenda cambiano la `version`; spostare una
    postazione scelta no. Una `PUT` insieme a `POST /stay-limits` negli stessi giorni: una risponde 409, nessun 500.
18. Dopo una `PUT` le risposte di `GET /stay-limits`, `GET /legend` e della mappa pubblica hanno la stessa forma di prima; lo
    snapshot `property_management_config` ha i valori nuovi e non contiene `version` né `legend`; la ricerca pubblica è
    aggiornata. Il registro ha una voce per quello che è cambiato e nessuna dopo un errore.
19. Con `onlineBookingBlock` `three_hours_before` e l'orologio del backend spostato, `POST /api/catalog/public/bathhouses/{slug}/quote`
    e `POST /api/booking/public/{slug}/reservations` con una postazione riescono prima del termine e non dopo; con soli servizi
    riescono sempre; dal gestionale si prenota sempre e si conferma una richiesta già arrivata.

API 4, con un conto di prova di Stripe:

20. Con un `code` valido: 200 con `acct_…`, e `get-stripe-information` risponde con lo stesso id; nel registro c'è una voce.
    Senza `code`: 400. Utente senza permesso e sessione impersonata: 403. Con Stripe irraggiungibile: 503 e niente cambia.
21. Un `code` già usato: 422 `payment.stripe_code_invalid` (prima va controllato su Stripe se riusarlo revoca il collegamento:
    in quel caso la prova si fa con un `code` scaduto).
22. Il conto già collegato a un altro stabilimento: 409 `payment.stripe_account_taken`, nessuna riga cambia e l'altro
    stabilimento resta collegato. Lo stesso conto su una riga revocata di un altro stabilimento: 200.
23. Riga revocata dello stesso stabilimento: si riattiva. Riga eliminata: ne nasce una nuova. Un conto diverso: i profili dei
    clienti del vecchio conto non si usano più.
24. Due chiamate insieme, per lo stesso stabilimento e per due: nessun 500 e una sola riga per stabilimento. Nei log non
    compaiono `code`, token o chiavi.
