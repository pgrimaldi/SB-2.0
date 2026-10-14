# Tabella `licenses`

> Fonti: schema e dati reali (2 righe, query completa), model
> [`back-pre/app/models/licenses.model.js`](../../back-pre/app/models/licenses.model.js), controller
> [`license.controller.js`](../../back-pre/app/controllers/license.controller.js) (letto per intero — un
> solo endpoint), routing in
> [`back-pre/app/routes/public.routes.js:20`](../../back-pre/app/routes/public.routes.js) (endpoint
> **pubblico**, nessuna autenticazione: `GET /api/public/licensetype`), riferimento incrociato con
> `users.license`/`users.subscriptionStatus` (già documentati in [`users.md`](users.md)), uso del valore
> `licenseType` per il feature-gating del menu in `front-pre/src/layout/sidebar/SideBar.js`.
>
> **Nota di contesto**: tabella **globale/master**, solo 2 righe — un catalogo statico dei piani di
> abbonamento disponibili, non collegato da alcuna FK né da alcun join applicativo alla tabella `users`.

---

## Contenuto (query diretta — entrambe le righe esistenti)

| type | months |
|------|--------|
| basic | 3 |
| advanced | 12 |

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria (non referenziata da nessun'altra
  tabella)
- **Nullabile**: No
- **Esempio di valori**: 1, 2

### type
- **Tipo**: `varchar`
- **Valori osservati**: `basic`, `advanced` — **stessi valori letterali** confrontati come stringa nel
  gating del menu gestionale (`SideBar.js:181,264,348,377`: `licenseType !== "basic"`) e coerenti con il
  campo `users.license` già documentato in [`users.md`](users.md). **Questa tabella è già essa stessa il
  lookup/enum** dei piani disponibili: non ha senso trasformare `type` in un `ENUM` a livello DB — il
  problema reale, come segnalato nelle criticità sotto, è che `users.license` non ha una vera FK verso
  `licenses.id`. La soluzione corretta è collegare `users` a `licenses` con una FK reale, non convertire
  `licenses.type` in `ENUM`: un `ENUM` sarebbe qui un passo indietro, perché aggiungere un nuovo piano
  richiederebbe un `ALTER TABLE` sull'`ENUM`, mentre con la tabella di lookup attuale basta un `INSERT`.
  **Candidato a enumerazione**: sì, ma è **già implementata correttamente** come tabella lookup — non va
  toccata la sua rappresentazione, va solo aggiunta la FK mancante da `users`.
- **Nullabile**: No
- **Esempio di valori**: "basic", "advanced"

### months
- **Tipo**: `int`
- **Descrizione**: durata in mesi del piano (3 per "basic", 12 per "advanced"). **Nessun controller
  esaminato calcola effettivamente `users.endDate` a partire da questo valore** — non è stato trovato
  alcun punto del backend che legga `licenses.months` per derivare una data di scadenza; la relazione fra
  durata dichiarata qui e le date reali di abbonamento salvate su ogni utente non è, allo stato attuale del
  codice, automatizzata (o quantomeno non nei file esaminati in questa scheda).
- **Nullabile**: No
- **Esempio di valori**: 3, 12

### createdAt / updatedAt
- **Tipo**: `datetime` — standard.
- **Nullabile**: No
- **Esempio di valori**: `2021-02-10 08:00:00`

---

## Osservazioni verificate nel codice (criticità)

- **Nessuna relazione reale con `users`**: non esiste alcuna FK, e non è stato trovato alcun
  `Licenses.findOne`/join che colleghi una riga di `users` a una riga di questa tabella — `users.license`
  è un campo stringa libero (documentato in [`users.md`](users.md)) che **per coincidenza** usa lo stesso
  vocabolario (`"basic"`/`"advanced"`) di `licenses.type`, ma senza alcun vincolo che li tenga sincronizzati.
  Se in futuro venisse aggiunto un terzo piano qui, andrebbe comunque aggiornato manualmente anche il
  confronto hardcoded in `SideBar.js` (e ovunque `users.license` sia confrontato per stringa) — nessuna
  fonte di verità unica.
- **Nessun endpoint di scrittura esposto**: `license.controller.js` contiene solo `licensetype` (sola
  lettura, `Licenses.findAll()` senza filtri) — coerente con solo 2 righe mai cambiate; gestione
  verosimilmente solo via DB diretto.
- **Import inutilizzato**: `auth.controller.js:13` importa `db.licenses` (`const Licenses = db.licenses`)
  ma non lo referenzia mai nel resto del file — codice morto, verosimilmente residuo di un collegamento
  `users`↔`licenses` pianificato ma mai implementato (coerente con l'assenza di relazione osservata sopra).
- **`users.endDate` calcolato a partire da un valore interamente fornito dal client, non da
  `licenses.months`**: in `auth.controller.js:usersignup` (righe 192-196), quando viene passato un
  `req.body.license`, la durata dell'abbonamento è presa da `req.body.licenseMonths` —
  `endDate = await dateWithMonthsDelay(licenseMonths)` — **senza alcuna verifica che il valore
  corrisponda a quanto dichiarato in `licenses.months` per quel `type`** (3 per "basic", 12 per
  "advanced"). Un client che invii `license: "basic"` insieme a un `licenseMonths` arbitrario (es. `999`)
  otterrebbe una data di scadenza abbonamento coerente con quel valore, non con la durata reale del piano
  "basic" — un problema di integrità di business logic concreto e verificato, da correggere nella
  riscrittura calcolando `endDate` server-side a partire dalla riga di `licenses` corrispondente al
  `type` scelto, non da un input diretto del client.
- **Endpoint pubblico senza autenticazione** (`GET /api/public/licensetype`): espone i piani disponibili
  senza richiedere login — impatto minimo trattandosi di dati puramente informativi/statici, ma nessun
  consumer è stato individuato in nessuno dei quattro frontend (`front-pre`, `front-customer-pre`,
  `front-app-pre`, `front-modern-pre`) — verosimilmente un endpoint predisposto per un flusso di
  registrazione/upgrade abbonamento non ancora implementato in UI, o dismesso.
