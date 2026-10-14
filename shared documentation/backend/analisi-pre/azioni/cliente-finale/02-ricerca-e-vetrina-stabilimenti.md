# cliente-finale — Ricerca e vetrina stabilimenti

> Area funzionale: homepage vetrina, barra di ricerca (località + date), pagina risultati con filtri e
> ordinamento, scorciatoie "Cerca per regione" e "Offerte".
> **Schermate**: homepage pp. 1–6, 26 (+ screenshot as-is 45–46); pagina risultati p. 8 /
> `Pagina di ricerca.png`.
>
> Logica di business correlata: [`business-logic/systemadmin/01-gestione-stabilimenti.md`](../../business-logic/systemadmin/01-gestione-stabilimenti.md)
> §2–3 (stati `properties.status`, visibilità pubblica) e [`db/properties.md`](../../db/properties.md),
> [`db/bathouses.md`](../../db/bathouses.md).

---

## 1. Apertura della homepage del sito cliente

**Descrizione**: al caricamento della home vengono mostrati: barra di ricerca, banner "Offerte
dell'estate", carosello "In evidenza", "Cerca per regione", sezione editoriale "Lasciati ispirare",
"Stabilimenti balneari più frequentati", box newsletter.
**Schermata**: pp. 1–6, 26.
**Ruolo**: ospite.

**Input → API** (`sb/propertyListOnHomePage`)
- `languageCode` (int) — lingua corrente
- `sections` (string[], opzionale) — quali blocchi servire (`featured`, `mostBooked`, `regions`,
  `offers`, `inspiration`) se l'endpoint è unico; altrimenti chiamate separate

**Output ← API**
- `featured` (array) — stabilimenti "in evidenza": `{ id, slug, name, city, region, thumbnail,
  priceFrom, currency }`
- `mostBooked` (array) — "più frequentati", stessa forma
- `regions` (array) — `{ regionKey, label, image, propertyCount }` per "Cerca per regione"
- `offers` ({ title, subtitle, ctaUrl, validUntil }) — banner offerte
- `inspiration` (array) — anteprime articoli magazine: `{ id, slug, title, excerpt, image, category }`

**Note**: `priceFrom` = "A partire da 20,00 €" mostrato nelle card.

---

## 2. Autocompletamento della località / stabilimento

**Descrizione**: digitando in "Città, stabilimento" la UI propone comuni, regioni e stabilimenti che
combaciano.
**Schermata**: barra di ricerca pp. 1–8.
**Ruolo**: ospite.

**Input → API** (`sb/locationAutosuggestion`)
- `query` (string) — testo digitato (min. 2–3 caratteri)
- `limit` (int) — max risultati
- `languageCode` (int)

**Output ← API**
- `suggestions` (array) — voci eterogenee:
  - `{ type: "city", label, city, province, region, lat, lng }`
  - `{ type: "region", label, regionKey }`
  - `{ type: "property", label, propertyId, slug, city, region, thumbnail }`

---

## 3. Ricerca stabilimenti (località + intervallo date)

**Descrizione**: premendo "Cerca" nella barra, l'utente ottiene la pagina risultati per la
località/stabilimento e le date scelte.
**Schermata**: barra pp. 1–8; risultati p. 8 ("5 strutture trovate").
**Ruolo**: ospite.

**Input → API** (`sb/getBookingAreaList` + `sb/getBookingAreaListCount`)
- `location` (object) — dalla voce di autocomplete scelta: `{ type, city?, regionKey?, propertyId?,
  lat?, lng? }`; se testo libero: `text` (string)
- `checkIn` (date `YYYY-MM-DD`) — prima data (default: oggi, come nei mockup "21/06/2025")
- `checkOut` (date `YYYY-MM-DD`) — ultima data
- `seasonal` (bool) — ricerca stagionale anziché giornaliera
- `guests` (int, opzionale) — n. persone (non visibile nei mockup)
- `page` (int) / `pageSize` (int) — paginazione
- `sortBy` (string) — vedi azione 5
- `filters` (object) — vedi azione 4 (vuoto alla prima ricerca)
- `languageCode` (int)

**Output ← API**
- `total` (int) — "N strutture trovate"
- `results` (array) — card risultato:
  `{ propertyId, slug, name, address: { street, city, region }, thumbnail, images[],
     priceFrom, currency, hasOnlineReservation (bool), rating?, badges[] }`
- `mapBounds` ({ north, south, east, west }, opzionale) — per eventuale vista mappa
- `appliedFilters` (object) — echo dei filtri applicati
- `availableSortOptions` (string[])

**Note**: nei mockup l'ultima card ("Lido San Luca") ha solo **"Scopri di più"** (niente "Prenota
ora"): `hasOnlineReservation = false` → la UI nasconde la CTA di prenotazione.

---

## 4. Applicazione dei filtri ai risultati

**Descrizione**: nella colonna "Filtra per" l'utente seleziona una o più caratteristiche; i risultati
si aggiornano.
**Schermata**: p. 8 / `Pagina di ricerca.png`.
**Ruolo**: ospite.

**Input → API**: come azione 3 con `filters` valorizzato. Gruppi e voci presenti nel mockup:
- `popular` (string[]) — Parcheggio bici, Cabine, Area giochi, Infermeria, Personale multilingua,
  Parcheggio incluso, Parcheggio, Animali ammessi, Piscina, Cassette di sicurezza, Parcheggio custodito,
  Tabacchi/edicola, Wifi, Pacchetti all inclusive, Servizio spiaggia
- `accessibility` (string[]) — Doccia per disabili, Accesso disabili
- `restaurant` (string[]) — Bar, Happy hour, Pizzeria, Ristorante
- `sports` (string[]) — Animazione, Beach soccer, Beach volley, Posto barca, Canoa, Escursioni,
  Palestra, Beach tennis, Calcio balilla, Kitesurf, Motoscafo, Pedalò, Sub/Snorkeling, Surf, Moto d'acqua
- `wellness` (string[]) — Idromassaggio, Doccia, Toilette, Doccia calda, Ventilatore sotto l'ombrellone
- `beachType` (string[]) — Spiaggia per single, del silenzio, per anziani, per famiglie, lgbt, naturista

**Output ← API**: come azione 3 (lista + `total` aggiornati).

**Note**: serve un endpoint di supporto per **popolare dinamicamente** l'elenco filtri con i relativi
conteggi.

---

## 5. Elenco filtri disponibili (con conteggi)

**Descrizione**: la colonna "Filtra per" è costruita da un elenco di caratteristiche/servizi, idealmente
con il numero di strutture per ciascuna nel set di risultati corrente.
**Schermata**: p. 8.
**Ruolo**: ospite.

**Input → API** (`sb/getServiceFilter` + `public/servicetypeicons`)
- `location` / `checkIn` / `checkOut` (come azione 3) — per conteggi contestuali
- `languageCode` (int)

**Output ← API**
- `filterGroups` (array) — `{ groupKey, groupLabel, items: [ { key, label, icon, count } ] }`

---

## 6. Ordinamento dei risultati

**Descrizione**: dal menù "Ordina per" l'utente cambia il criterio di ordinamento della lista.
**Schermata**: p. 8 (in alto a destra, "Ordina per").
**Ruolo**: ospite.

**Input → API**: come azione 3 con `sortBy` fra: `relevance`, `priceAsc`, `priceDesc`, `distance`,
`ratingDesc`, `nameAsc` (set esatto da confermare).

**Output ← API**: lista riordinata (come azione 3).

---

## 7. Ricerca rapida per regione

**Descrizione**: dalla sezione "Cerca per regione" della home l'utente clicca una regione (Liguria,
Toscana, Emilia Romagna, …) e arriva ai risultati filtrati per quella regione.
**Schermata**: home pp. 1–6, 26.
**Ruolo**: ospite.

**Input → API**: come azione 3 con `location = { type: "region", regionKey }` e date di default.

**Output ← API**: lista risultati della regione (come azione 3).

---

## 8. Apertura del banner "Offerte dell'estate"

**Descrizione**: da "Vedi tutte le offerte" l'utente apre l'elenco delle strutture che applicano uno
sconto.
**Schermata**: home pp. 1–6, 26; screenshot as-is p. 45 ("Trova le strutture che offrono uno sconto").
**Ruolo**: ospite.

**Input → API** (`sb/getpropertyDiscount` / lista dedicata)
- `languageCode` (int)
- `page` / `pageSize` (int)

**Output ← API**
- `offers` (array) — `{ propertyId, slug, name, city, region, thumbnail, discountLabel,
  discountPercent, priceFrom, priceFromDiscounted, validFrom, validUntil }`
- `total` (int)

---

## 9. Apertura di una card risultato / "in evidenza"

**Descrizione**: cliccando su una card (nome, immagine o "Prenota ora"/"Scopri di più") si apre la
scheda dello stabilimento.
**Schermata**: home e p. 8.
**Ruolo**: ospite.

**Input → API**: nessuna dedicata qui; la navigazione porta a
[`03-scheda-stabilimento.md`](03-scheda-stabilimento.md) azione 1, passando `slug` (o `propertyId`) e le
date correnti come query string.

**Output ← API**: n/a (vedi scheda stabilimento).

**Note**: dal `slug` la scheda risale a `propertyId` + `bathHouseId` con
`sb/getPropertyIdAndBathhouseFromSlug`.
