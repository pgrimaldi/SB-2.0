# systemadmin — Dati di piattaforma e tabelle di lookup

> **Tipo di utente**: `systemadmin` (in pratica: dati gestiti a livello di piattaforma, spesso
> direttamente su DB perché privi di un vero pannello).
>
> **Ambito**: le tabelle **master/globali** senza `propertyId` che fanno da vocabolario condiviso a
> tutti gli stabilimenti: piani di abbonamento (`licenses`), paesi (`country`), catalogo servizi
> (`services`), categorie di filtro servizi (`service_type_filters`), icone dei tipi di servizio
> (`servicetypes_icons`), categorie icone mappa (`configure_categories`, già trattata nella scheda 02).
>
> **Fonti**: `back-pre` — `license.controller.js`, `country.controller.js`, `services.controller.js`,
> `serviceFilters.controller.js`, `servicetypeicons.controller.js`; rotte in `public.routes.js` e
> `user.routes.js`. Schede DB: [`licenses.md`](../../db/licenses.md), [`country.md`](../../db/country.md),
> [`services.md`](../../db/services.md), [`service_type_filters.md`](../../db/service_type_filters.md),
> [`servicetypes_icons.md`](../../db/servicetypes_icons.md), [`configure_categories.md`](../../db/configure_categories.md).
>
> **Sintesi**: quasi tutti questi dati sono **sola lettura** lato applicazione; esistono due soli
> endpoint di scrittura (`createCountry`, `createServiceFilter`) e **nessuno dei due ha un controllo di
> ruolo effettivo** (auth opzionale) né un consumer nei frontend. Nessuna di queste azioni comporta
> calcoli o eventi (email/notifiche/API esterne/job).

---

## 1. Piani di abbonamento (`licenses`)

- **Lettura** — `GET /api/public/licensetype` (pubblico, nessuna autenticazione): restituisce tutte le
  righe (oggi 2: `basic` = 3 mesi, `advanced` = 12 mesi). **Consumer**: il pannello systemadmin
  [`front-admin/`](../../../../../../../front-admin/) lo chiama (`propertyapicalls.js` → `_getLicenseType`) nei form
  "Add Super Admin" / "Update Super Admin" e nel dettaglio stabilimento per popolare la tendina "License
  Type"; nel body di `updateproperty`/`updatesuperadmin` invia poi `ownerlicensetype`/`license` +
  `licenseMonths` presi da quella riga — quindi `licenseMonths` continua ad arrivare **dal client** (vedi
  §"Logiche a valle"). Nessun consumer negli altri quattro frontend.
- **Scrittura**: nessun endpoint. La tabella si modifica solo via DB diretto.
- **Logiche a valle**:
  - Il **feature-gating del menù** del gestionale confronta `licenseType` per stringa
    (`licenseType !== "basic"` abilita Planner, Prenotazioni, Magazzino, Marketing — vedi
    [`analisi-pre/db/licenses.md`](../../db/licenses.md)).
  - La **durata** del piano (`months`) **non** viene usata dal backend per calcolare la scadenza
    abbonamento: in fase di creazione utente la scadenza è calcolata da `licenseMonths` **inviato dal
    client**, senza verificarlo contro `licenses.months`. Un client può quindi ottenere una scadenza
    incoerente col piano dichiarato. Da correggere calcolando la scadenza server-side dalla riga
    `licenses` corrispondente (vedi [`da-chiarire.md`](../da-chiarire.md) — "Licenze & abbonamenti").
- **Calcoli/eventi**: nessuno.

---

## 2. Paesi (`country`)

- **Lettura** — `POST /api/sb/country/countryList` (auth opzionale): restituisce l'elenco paesi con
  nome inglese e italiano. Usato dai frontend per le tendine "paese".
- **Scrittura** — `POST /api/sb/country/createCountry` (auth opzionale, **nessun ruolo richiesto**):
  - **Validazioni**: `country_english` e `country_italian` obbligatori non vuoti (400 se mancanti).
  - **Logica**: crea una riga con i due nomi. Nessun controllo di duplicati, nessuna normalizzazione,
    nessun codice ISO.
  - Nessun consumer nei frontend: endpoint di seed lasciato aperto.
- **Calcoli/eventi**: nessuno.
- **Debito**: endpoint di scrittura su tabella globale senza autorizzazione — da mettere sotto
  `systemadmin` o rimuovere; aggiungere codice ISO e unicità.

---

## 3. Catalogo servizi master (`services`)

- **Lettura** — `GET /api/public/services` (pubblico): elenco dei servizi selezionabili dagli
  stabilimenti (Wi‑Fi, bar, doccia, ecc.).
- **Scrittura**: nessun endpoint applicativo. Catalogo statico gestito via DB diretto.
- **Calcoli/eventi**: nessuno.
- **Uso a valle**: il gestore, in configurazione stabilimento, seleziona un sottoinsieme di questi
  servizi; la selezione viene **copiata** dentro `bathouses.services` (JSON) — copia per valore, non
  riferimento.

---

## 4. Categorie di filtro servizi (`service_type_filters`)

- **Lettura** — `POST /api/sb/serviceFilter` / `getServiceFilter` (auth opzionale): le categorie con
  cui si raggruppano i servizi nella ricerca pubblica (es. "Accessibilità").
- **Scrittura** — `createServiceFilter` (auth opzionale, **nessun ruolo**): richiede `servicenames`
  (obbligatorio); crea una riga con quel valore in `serviceTypes`. Nessun controllo di duplicati.
  Nessun consumer nei frontend.
- **Calcoli/eventi**: nessuno.
- **Debito**: come §2, endpoint di scrittura globale non protetto.

---

## 5. Icone dei tipi di servizio (`servicetypes_icons`)

- **Lettura** — `GET /api/public/servicetypeicons` (pubblico): catalogo master delle icone per i tre
  tipi di servizio venduti dagli stabilimenti (bar / normale / a ore).
- **Scrittura**: nessun endpoint applicativo.
- **Calcoli/eventi**: nessuno.

---

## 6. Categorie icone mappa (`configure_categories`)

- **Lettura** — `GET /api/public/conficats` (pubblico): 5 categorie fisse (`template`, `umbrella`,
  `tent`, `sunbed`, `cabin`). Dettaglio d'uso nella scheda [02](02-catalogo-icone-mappa-spiaggia.md).
- **Scrittura**: nessun endpoint. Tabella statica.

---

## 7. Riepilogo

| Tabella | Lettura | Scrittura applicativa | Protezione scrittura | Consumer frontend |
|---|---|---|---|---|
| `licenses` | pubblica | — | — | no (per il read pubblico) |
| `country` | auth opz. | `createCountry` | **nessuna** | no |
| `services` | pubblica | — | — | sì (read) |
| `service_type_filters` | auth opz. | `createServiceFilter` | **nessuna** | sì (read) |
| `servicetypes_icons` | pubblica | — | — | sì (read) |
| `configure_categories` | pubblica | — | — | sì (read) |

**Calcoli**: nessuno in tutta l'area. **Eventi**: nessuno (nessuna email/notifica/API/job).

**Punti da chiarire**: vedi [`../da-chiarire.md`](../da-chiarire.md) — "Autorizzazioni e ruoli"
(endpoint di seed non protetti), "Licenze & abbonamenti" (durata non verificata), "Integrità
referenziale & cataloghi" (copie per valore, assenza di FK/unicità).
