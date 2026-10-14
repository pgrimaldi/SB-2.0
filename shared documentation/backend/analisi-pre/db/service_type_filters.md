# Tabella `service_type_filters`

> Fonti: schema + **dati reali di produzione** (dump `sb_prod_dump_2026_09_08`, **6 righe**, query
> completa — contenuto identico: `Popular filters`, `Accessibility`, `Restuarant` (refuso), `Sports &
> Activities`, `Wellness & Personal Services`, `Beach Type`; tutte create il 2021-06-17, mai
> modificate). Model
> [`back-pre/app/models/serviceTypeFilter.model.js`](../../../../../../back-pre/app/models/serviceTypeFilter.model.js),
> controller [`serviceFilters.controller.js`](../../../../../../back-pre/app/controllers/serviceFilters.controller.js)
> (letto per intero, 80 righe), frontend
> `front-customer-pre/src/components/consumer/propertyList/FilterList.js` (pannello filtri ricerca
> pubblica) — lo stesso file/componente è presente **anche** in `front-pre` (`front-pre/src/data/actions/consumerAction.js`
> contiene la stessa azione `getServiceFilter`), ulteriore conferma della duplicazione di codice
> "consumer" fra i due frontend già segnalata in altre schede di questo dominio.
>
> **Nota di contesto**: tabella **globale/master**, solo 6 righe — le categorie in cui sono raggruppati i
> servizi di [`services.md`](services.md) nel pannello filtri della ricerca pubblica.

---

## Contenuto (query diretta, non un campione — tutte le 6 righe esistenti)

| id | serviceTypes |
|----|--------------|
| 1 | Popular filters |
| 2 | Accessibility |
| 3 | Restuarant *(sic — refuso presente nel dato reale, non di questa scheda)* |
| 4 | Sports & Activities |
| 5 | Wellness & Personal Services |
| 6 | Beach Type |

**Nota — verificato sul DB dev**: interrogando direttamente il DB dev (sola lettura), la tabella contiene in
realtà **7 righe**, non 6: è presente una riga aggiuntiva `id=7`, `serviceTypes="test filter"`, assente dal
campione locale a 6 righe citato sopra. La settima riga è coerente con l'osservazione, già presente più sotto,
che l'endpoint di creazione (`createServiceFilter`) non ha alcun consumer UI reale nei frontend analizzati —
è verosimilmente una riga di test inserita manualmente (via endpoint o direttamente a DB), non una categoria
di filtro realmente in uso.

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Relazioni**: referenziata applicativamente (nessuna FK dichiarata) da `services.serviceFilterType`
  (vedi [`services.md`](services.md)) e usata direttamente in un `INNER JOIN` SQL in
  `serviceFilters.controller.js:getServiceFilter` (riga 25:
  `INNER JOIN service_type_filters as filter ON services.serviceFilterType=filter.id`).
- **Nullabile**: No
- **Esempio di valori**: 1, 2, 3, ..

### serviceTypes
- **Tipo**: `varchar`
- **Descrizione**: nome della categoria di filtro, in **inglese soltanto** (nessuna colonna per la
  traduzione italiana, a differenza di quasi tutte le altre tabelle di catalogo di questo dominio) —
  confermato dal frontend, che nel pannello filtri pubblico confronta questo valore con stringhe inglesi
  hardcoded (`itm.serviceTypes === "Popular filters"`, ecc. in `FilterList.js`), mentre il **nome del
  singolo servizio** dentro ciascuna categoria viene tradotto separatamente tramite
  `services.serviceItalian`. Il refuso `"Restuarant"` (invece di "Restaurant") è quindi hardcoded sia nel
  dato DB sia, necessariamente, in ogni punto del frontend che lo confronta per uguaglianza esatta —
  correggerlo richiede di aggiornare entrambi contemporaneamente. **Candidato a enumerazione**: l'insieme dei
  valori è di fatto chiuso e stabile (6 categorie reali, mai cambiate nel tempo, più la riga di test) — dato
  che questa tabella **è già** una tabella lookup dedicata, la soluzione non è un ENUM testuale ma
  formalizzare come vera FK (oggi solo applicativa) il legame `services.serviceFilterType → 
  service_type_filters.id`, eliminando valori orfani come lo `0` già osservato, ed eventualmente aggiungere
  qui una colonna di traduzione italiana se il pannello filtri dovrà mai essere localizzato.
- **Nullabile**: Sì
- **Esempio di valori**: "Popular filters", "Accessibility", "Restuarant", "test filter"

### createdAt / updatedAt
- **Tipo**: `datetime`, default `Sequelize.NOW`, `allowNull: true` — standard.
- **Nullabile**: Sì
- **Esempio di valori**: "2023-11-02 10:15:00", "2023-11-02 10:15:00"

---

## Osservazioni verificate nel codice (criticità)

- **Matching per stringa esatta fra frontend e nome categoria in DB**: il pannello filtri pubblico
  (`FilterList.js`) individua le 6 sezioni confrontando `serviceTypes` con stringhe **hardcoded** identiche
  al valore in DB (incluso il refuso `"Restuarant"`) — rinominare una categoria in DB senza aggiornare il
  frontend farebbe sparire silenziosamente l'intera sezione dal pannello filtri, senza alcun errore
  visibile. Stesso pattern di accoppiamento fragile per stringa libera già segnalato più volte in questo
  dominio (`periods[].name`, `sectorRowsPkg`, voucher `selectedRow`).
- **`createServiceFilter` esposto ma senza consumer frontend individuato**: l'endpoint di creazione esiste
  ed è funzionante (`ServiceTypeFilters.create({ serviceTypes: req.body.servicenames })`), ma non è stato
  trovato alcun form di amministrazione che lo richiami nei quattro frontend — coerente con il fatto che le
  6 categorie sono fisse e mai cambiate; verosimilmente popolate una tantum via questo endpoint o
  direttamente a DB, non gestite correntemente da UI.
- **Nessuna FK reale verso `services.serviceFilterType`**: come già segnalato in
  [`services.md`](services.md#osservazioni-verificate-nel-codice-criticità), il collegamento è solo
  applicativo — confermato concretamente dal valore orfano `serviceFilterType = 0` osservato su una riga
  reale di `services`, che non corrisponde a nessun `id` di questa tabella.
- **`checkHourlyServiceAvailability`** (stessa `serviceFilters.controller.js`, righe 36-79) non ha alcuna
  relazione con questa tabella nonostante sia definita nello stesso file — opera su una tabella distinta
  (`hourly_service_booking_management`, non in perimetro in questa scheda) per calcolare la disponibilità
  residua di uno slot orario di un servizio a ore; segnalato solo per chiarire che la sua presenza in
  questo file non implica alcun legame con `service_type_filters`.
