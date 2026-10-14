# Tabella `property_search_regions`

> Fonti: schema completo (`SHOW CREATE TABLE`) e dati reali (14 righe, query completa), model
> [`back-pre/app/models/propertySearchRegion.model.js`](../../back-pre/app/models/propertySearchRegion.model.js)
> (`sequelize.define("property_search_region", ...)` — nome singolare nel model, **tabella fisica reale
> `property_search_regions`**, pluralizzazione automatica di Sequelize, stesso pattern già osservato per
> `seasons`→`seasons_periods`), controller
> [`searchRegion.controller.js`](../../back-pre/app/controllers/searchRegion.controller.js) (letto per
> intero, 258 righe), file statico
> [`back-pre/app/utils/propertyList.js`](../../back-pre/app/utils/propertyList.js) (338 righe),
> frontend `front-customer-pre/src/components/home/Home.js`.

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori**: 1, 2, 3, ..

### bathHouseId
- **Tipo**: `int`
- **Vincoli**: nessuna FK dichiarata a livello DB
- **Relazioni**: applicativa verso `bathouses.id` — usata in un `JOIN` SQL esplicito in
  `getpropertRegionList` (`bathouses.id = regions.bathHouseId`).
- **Nullabile**: Sì
- **Esempio di valori**: 17 (dato reale DB dev)

### propertyId
- **Tipo**: `int`
- **Vincoli**: nessuna FK dichiarata a livello DB
- **Relazioni**: applicativa verso `properties.id`.
- **Nullabile**: Sì
- **Esempio di valori**: 1 (dato reale DB dev)

### status
- **Tipo**: `int`, default `1`
- **Valori osservati**: tutte le 14 righe hanno `status = 1`. **Confermato anche sul DB dev reale** (15
  righe interrogate direttamente): tutte hanno `status = 1`, nessuna eccezione — nessuna riga è mai stata
  disattivata tramite `deleteSearchRegion`.
- **Descrizione**: soft-delete — `deleteSearchRegion` imposta `status = false` invece di cancellare la
  riga (nonostante il campo sia tipizzato `int`, viene scritto un booleano JS, che Sequelize/MySQL
  converte comunque in `0`). **Candidato a enumerazione**: di fatto un flag a 2 stati (`1` attivo / `0`
  disattivato tramite `deleteSearchRegion`) modellato come `int` invece che come `boolean`/`tinyint(1)` —
  non serve una tabella di lookup (nessuna traduzione, nessuna crescita prevista), basterebbe tipizzarlo
  correttamente come booleano nella riscrittura.
- **Nullabile**: Sì
- **Esempio di valori**: 1 (unico valore osservato)

### createdAt / updatedAt
- **Tipo**: `datetime`
- **Valori osservati**: tutte le 14 righe hanno `createdAt` compreso fra il **28/06/2021** e il
  **05/07/2021** — nessuna riga più recente, nessuna riga aggiornata dopo la creazione. Rilevante per la
  criticità sotto: la tabella non risulta più alimentata da anni. **Conferma esplicita dal DB dev reale**
  (15 righe, query diretta): quasi tutte le date restano comprese fra giugno e luglio 2021, con una sola
  eccezione datata **17/01/2023** (verosimilmente una riga di test aggiunta più tardi, isolata) — il dato
  reale conferma quindi che la tabella è **abbandonata**, coerente con quanto già ipotizzato nel testo
  esistente.
- **Nullabile**: Sì
- **Esempio di valori**: `2021-06-28 11:04:00`, `2021-07-05 16:40:00`, `2023-01-17 09:12:00` (eccezione)

---

## Campo dichiarato nel model ma **assente dallo schema fisico**

- **`displayImage`** (`Sequelize.JSON`): presente nella definizione del model
  (`propertySearchRegion.model.js:19-21`) ma **verificato assente dalla tabella reale**
  (`SHOW CREATE TABLE property_search_regions` non la elenca fra le colonne) — un caso concreto di
  disallineamento model/schema, dello stesso tipo di quelli già flaggati a livello generale in
  [01-criticita-e-indicazioni-backend.md](../../docs/01-criticita-e-indicazioni-backend.md) (assenza di
  migrazioni versionate, `sync({alter:true})` commentato in quasi tutti i model di questo dominio).
  Qualunque tentativo di leggere/scrivere `displayImage` tramite l'ORM Sequelize fallirebbe o verrebbe
  silenziosamente ignorato a runtime. **Confermato anche sul DB dev**: l'interrogazione diretta dello
  schema fisico (15 righe, stessa sessione di verifica degli altri campi di questa scheda) non mostra
  alcuna colonna `displayImage` — nessuna colonna di questo nome esiste nella tabella reale, l'assenza non
  è quindi un artefatto del solo ambiente esaminato in precedenza ma un dato confermato.

---

## Osservazioni verificate nel codice (criticità)

- **Tabella verosimilmente dismessa/abbandonata, con la sua feature sostituita da un elenco statico
  hardcoded**: il flusso completo è stato tracciato end-to-end:
  - `createSearchRegion`/`deleteSearchRegion` permettono di popolare/disattivare righe di questa tabella
    (funzionanti, ma tutte le 14 righe esistenti risalgono a giugno/luglio 2021, nessuna più recente);
  - l'unico endpoint di lettura pensato per consumarle, `getpropertRegionList`, **non ha alcun consumer
    individuato in nessuno dei quattro frontend** (nessun riferimento trovato in `front-customer-pre`,
    `front-pre`, `front-app-pre`, `front-modern-pre`);
  - la sezione "destinazioni in evidenza" della home page pubblica (`front-customer-pre/.../home/Home.js`)
    usa invece l'endpoint **`propertyListOnHomePage`**, che **non tocca affatto questa tabella**: restituisce
    un array **hardcoded direttamente nel codice sorgente backend**
    (`back-pre/app/utils/propertyList.js:294-337`, 4 stabilimenti fissi — "On the Beach Club", "Calypso",
    "Lido Sole Splendid", "Bagno La Vela" — con URL immagine CloudFront hardcoded).
  - Il file contiene inoltre, commentata, una **versione precedente più ampia** della stessa lista statica
    (righe 1-291, con più voci e anche una sezione `propertyType: "search"` per intere regioni come
    "Sardinia"/"Emilia Romagna") — ulteriore conferma che questa è un'area del codice modificata a mano più
    volte nel tempo, mai collegata alla tabella DB pensata per lo stesso scopo.
  - **Implicazione concreta per la riscrittura**: aggiornare le destinazioni in evidenza sulla home page
    pubblica richiede oggi una modifica di codice e un deploy, non un'operazione di back-office — la
    tabella `property_search_regions` esiste ma è di fatto inutilizzata. Da decidere con il team prodotto se
    nella riscrittura si vuole finalmente rendere questa lista gestibile da UI (riattivando lo scopo
    originale della tabella) o se va eliminata come tabella morta.
- **`searchRegion`** (la ricerca live "digita e cerca" per nome/città/regione, funzione più corposa del
  file) **non usa affatto questa tabella**: interroga direttamente `bathouses`/`properties` con `LIKE` sui
  campi `bathouseName`/`city`/`region` — nome della funzione fuorviante rispetto al suo reale
  funzionamento, che non ha alcuna relazione con `property_search_regions` nonostante la vicinanza nel
  nome e nel file.
- **`deleteSearchRegion` non protetto da IDOR/ownership** (opera su `req.body.id` senza verifica), coerente
  col pattern sistemico già documentato, ma di impatto pratico ridotto data la tabella di fatto non più in
  uso.
