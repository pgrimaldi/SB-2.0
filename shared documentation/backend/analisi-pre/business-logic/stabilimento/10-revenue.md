# stabilimento — Revenue (Revenue Management)

> **Voce di menù**: decima e ultima icona della sidebar (`_goToRevenue` → `components/revenue/Revenue.js`,
> rotta `/revenue`; etichetta "Revenue"). Calcola un **prezzo suggerito dinamico** per il pacchetto
> predefinito in base a meteo, stagione, weekend e disponibilità; permette di **applicarlo** al
> listino e di configurare il **banner sconto** sulla pagina pubblica.
>
> **Ruoli**: nascosta a `cashoperator`, `waiter`, `marketing`, `affiliate`, `lifeguard`,
> `advancedlifeguard`.
>
> **Metodo**: action file `revenueAction.js` + `revenue.controller.js`
> (`calculateSuggestedPrice`, `getSuggestedPriceForDefaultPackage`, `applySuggestedPrice`,
> `fetchNotificationDetails`, `saveNotificationDetails`) + `controllers/helpers/revenue.helper.js`
> (`RevenueService`) + `discounts.controller.js` (`associateRevenueDiscount`, `associateRevenueDiscount2`).
> Schede DB: [`revenue_notifications.md`](../../db/revenue_notifications.md),
> [`price_lists.md`](../../db/price_lists.md) (campo `suggestedPriceDetails`), [`bathouses.md`](../../db/bathouses.md)
> (`latitude`/`longitude`).

---

## 1. Calcolo del prezzo suggerito

**Azione** — `GET calculateSuggestedPrice` (`propertyId`, `startDate`, `endDate`): per ogni giorno
dell'intervallo, calcola un prezzo suggerito per il **pacchetto predefinito** (il `price_lists` con
`details.defaultPackage = 'true'`).

**Logiche e dati raccolti per ogni giorno** (una sola volta, poi riusati):

- **Pioggia** (`RevenueService.getRainByDate(lat, lon, data)`): **chiamata ad API esterna Open‑Meteo**
  (`api.open-meteo.com/v1/forecast` per date future, `archive-api.open-meteo.com/v1/archive` per date
  passate), usando `bathouses.latitude`/`longitude`; restituisce la quantità di pioggia.
- **Alta stagione** (`getSeason`): se la data cade in alta stagione.
- **Weekend imminente** (`isNextDayWeekend`): se il giorno successivo è sabato/domenica.
- **Disponibilità** (`calculateAvailability`): percentuale di postazioni libere per quella data.

**Calcolo del prezzo suggerito** (`RevenueService.getSuggestedPrice`), a partire dal prezzo base del
periodo (`weekend_price` se il giorno è sab/dom, altrimenti `daily_price`):

1. **Pioggia** (`rain > 0`): se **bassa** stagione → `× 0,70` (sconto 30%); se **alta** stagione →
   `× 0,80` (sconto 20%).
2. **Weekend imminente**: `× 1,25`. **Altrimenti**, se **alta stagione**: `× 1,20`.
3. **Disponibilità**: se libere > 30% → `× 0,70` (sconto 30%); se libere < 30% → `× 1,10`.

I moltiplicatori si **compongono** (es. pioggia in bassa stagione + tanta disponibilità →
`base × 0,70 × 0,70`). Il risultato (`suggestedPrice`, `discountPercentage`) è salvato/aggregato per
anno in `price_lists.suggestedPriceDetails`.

**Calcoli**: prodotto di moltiplicatori condizionali sul prezzo base giornaliero.
**Eventi**: **chiamata API esterna Open‑Meteo** (una per giorno dell'intervallo).

**`getSuggestedPriceForDefaultPackage`**: variante che restituisce il suggerito già calcolato/salvato
per il pacchetto predefinito (lettura di `suggestedPriceDetails`).

---

## 2. Applicare il prezzo suggerito al listino

**Azione** — `POST applySuggestedPrice` (`data` = array di oggetti `{ propertyId, id, date, name,
isSuggestedPriceApplied, isDiscountApplied, discountPercentage, isOriginalPriceApplied }`).

**Logiche**:

- Recupera `price_lists.suggestedPriceDetails` per il listino indicato (`id` + `propertyId`,
  `isDeleted = 0`).
- Per ogni voce del payload, per l'anno della `date`, aggiorna il flag scelto dall'operatore:
  - **applica prezzo suggerito** (`isSuggestedPriceApplied`), oppure
  - **applica sconto** (`isDiscountApplied` + `discountPercentage`), oppure
  - **ripristina prezzo originale** (`isOriginalPriceApplied`).
- Salva `suggestedPriceDetails` aggiornato sul listino.

**Effetto a valle**: il motore di prezzo lato client, in fase di prenotazione, se il suggerito è
attivo per quella data usa il prezzo suggerito al posto di quello di listino
(`getMatchedSuggestedPrice`, vedi [`_motore-di-prezzo-e-sconti.md`](_motore-di-prezzo-e-sconti.md) §2.8).

**Calcoli**: nessuno nuovo (usa i valori già calcolati). **Eventi**: nessuno.

---

## 3. Configurazione del banner sconto (pagina pubblica)

**Azioni** — `fetchNotificationDetails` (`GET`), `saveNotificationDetails` (`POST`), su
`revenue_notifications` (1 riga per `property_id`).

**Campi/logiche**:

- `isRevenuePricesEnabled` / `is_price_discount_enabled`: interruttori del prezzo suggerito e del
  banner sconto.
- `is_price_discount_enabled` = `1` → il **banner sconto** è visibile sulla pagina pubblica dello
  stabilimento.
- `dailyNotificationsCount`: quante volte al giorno mostrare il banner. **La logica di visibilità è
  oggi nel frontend**: divide la fascia oraria 10:00–20:00 in `dailyNotificationsCount` slot e mostra
  il banner se l'ora corrente cade in uno slot **e**
  `dailyNotificationsCount − dailyNotificationsDisplayedCount > 0`.
- `dailyNotificationsDisplayedCount`: contatore giornaliero, incrementato lato frontend a ogni
  visualizzazione; **azzerato** da un job (`cron.helper.updateNotificationCount`).
- `selectedLocations`: elenco postazioni a cui applicare lo sconto.
- `discounts` (JSON): **colonna morta** (mai letta/scritta significativamente).

**Nota**: `saveNotificationDetails` fa un `record.update(req.body)` sull'**intero body** (nessuna
whitelist di campi). Da irrigidire nella riscrittura, e da **portare lato server** la logica di
finestra oraria/contatore/decisione banner (oggi nel frontend).

**Calcoli**: suddivisione della fascia 10–20 in slot; confronto contatore. **Eventi**: job di reset
giornaliero del contatore.

---

## 4. Sconti associati al Revenue

**Azioni** — `associateRevenueDiscount` / `associateRevenueDiscount2` (`discounts.controller.js`):
elencano le prenotazioni del periodo con `booking_discount` valorizzato (query su `bookings` con
`status = 1`, `isDeleted = 0`, intervallo date) per misurare l'effetto degli sconti Revenue.

**Calcoli**: conteggio/somma degli sconti applicati nel periodo. **Eventi**: nessuno.

---

## 5. Riepilogo "calcoli / logiche / eventi" della voce Revenue

- **Calcoli**: prezzo suggerito = `prezzo_base_giorno ×` (moltiplicatori condizionali: pioggia
  0,70/0,80; weekend imminente 1,25; alta stagione 1,20; disponibilità 0,70/1,10); suddivisione fascia
  oraria 10–20 in slot; conteggi sconti applicati.
- **Logiche**: solo il pacchetto con `defaultPackage = 'true'`; iterazione giorno per giorno;
  scelta operatore fra "applica suggerito / applica sconto / ripristina originale" per data e anno;
  visibilità banner per slot orario + contatore residuo.
- **Eventi**: **chiamata API esterna Open‑Meteo** (meteo/pioggia, una per giorno dell'intervallo di
  calcolo); job di reset giornaliero del contatore visualizzazioni banner. Nessuna email/notifica.
