# Tabella `services`

> Fonti: schema e dati reali (48 righe, `COMMENT='Bathouse Services Master'`), model
> [`back-pre/app/models/services.model.js`](../../back-pre/app/models/services.model.js), controller
> [`services.controller.js`](../../back-pre/app/controllers/services.controller.js) (letto per intero — un
> solo endpoint di sola lettura), controller collegato
> [`serviceFilters.controller.js`](../../back-pre/app/controllers/serviceFilters.controller.js) (funzione
> `getServiceFilter`, join diretto con `service_type_filters`), frontend
> `front-pre/src/components/bathhouse/ServicesList.js` (già letto per [`bathouses.md`](bathouses.md)),
> `front-customer-pre/src/components/consumer/propertyList/FilterList.js`.
>
> **Nota di contesto**: tabella **globale/master**, non ha `propertyId` — è il catalogo di riferimento di
> tutti i possibili servizi che uno stabilimento può offrire (es. "Wi-Fi", "Doccia", "Bar"). Come già
> documentato in [`bathouses.md`](bathouses.md), quando un gestore configura i servizi del proprio
> stabilimento, il frontend **copia integralmente** questa tabella dentro `bathouses.services` (stesso
> elenco di colonne, con `isChecked` sovrascritto in base alla selezione del gestore) — questa tabella non
> viene mai referenziata per id dal record dello stabilimento.

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Relazioni**: nessuna FK dichiarata a livello DB, ma referenziata applicativamente da
  `service_type_filters` tramite `services.serviceFilterType = service_type_filters.id` (join esplicito in
  `serviceFilters.controller.js:getServiceFilter`, vedi sotto) — l'unica relazione FK-like realmente
  utilizzata di questa tabella.
- **Nullabile**: No
- **Esempio di valori**: 1, 2, 3, ..

### serviceName / serviceItalian
- **Tipo**: `varchar`
- **Descrizione**: nome del servizio in inglese/italiano, mostrato nel form di selezione servizi del
  gestionale (`ServicesList.js`) e nel filtro di ricerca pubblico (`FilterList.js`).
- **Nullabile**: Sì
- **Esempio di valori**: `serviceName`: "Bar", "Surf"; `serviceItalian`: "Pacchetti all inclusive",
  "Escursioni"

### serviceIcon
- **Tipo**: `varchar`
- **Descrizione**: percorso/nome dell'icona associata al servizio.
- **Nullabile**: Sì
- **Esempio di valori**: "https://s3.eu-west-1.amazonaws.com/bathhouse-assets/services/bar.png"

### status
- **Tipo**: `varchar(10)`
- **Valori osservati**: sempre `"active"` (48/48 righe) — nessuna riga con altro valore nel campione,
  **confermato anche con query diretta sul DB dev**: tutte le 48 righe reali hanno `status = "active"`, senza
  eccezioni.
- **Descrizione**: non è stato individuato alcun endpoint che scriva un valore diverso da `"active"` (nessun
  create/update esposto, vedi criticità sotto) — coerente con l'assenza di variazione osservata. **Candidato
  a enumerazione**: il campo è testuale ma di fatto binario/mai variato (`"active"` su tutte le righe, mai
  osservato altro valore né alcun writer che lo cambi); trattandosi di un flag semplice senza necessità di
  traduzione o crescita futura del set di valori, un `ENUM('active','inactive')` a livello DB è sufficiente
  e preferibile a una tabella lookup dedicata.
- **Nullabile**: Sì
- **Esempio di valori**: "active"

### isChecked
- **Tipo**: `boolean`, default `false`
- **Valori osservati**: sempre `0` (48/48) nella tabella master, **confermato anche con query diretta sul DB
  dev** (48/48 righe reali con `isChecked = 0`) — coerente col fatto che questa è la tabella master/template
  e non viene mai selezionata "di per sé".
- **Descrizione**: campo di stato pensato per l'uso nella **copia per-stabilimento** (`bathouses.services`,
  vedi [`bathouses.md`](bathouses.md)), dove viene effettivamente sovrascritto in base alla selezione del
  gestore — nella tabella master resta sempre al valore di default, essendo il "template" da cui parte ogni
  nuova copia.
- **Nullabile**: Sì
- **Esempio di valori**: 0

### serviceFilterType
- **Tipo**: `int(11)`
- **Vincoli**: nessuna FK dichiarata a livello DB
- **Valori osservati** (query completa su tutte le 48 righe): `1`→15, `2`→2, `3`→4, `4`→15, `5`→5, `6`→6,
  `0`→1
- **Relazioni**: applicativa verso `service_type_filters.id` (vedi
  [`service_type_filters.md`](service_type_filters.md)) — un servizio appartiene a **una sola** categoria
  di filtro (es. "Popular filters", "Accessibility"), usata per raggruppare i servizi nel pannello filtri
  della ricerca pubblica. **Il valore `0` (1 riga) non corrisponde a nessun `id` esistente in
  `service_type_filters`** (che parte da `1`) — un caso concreto e verificato di riga orfana, che
  conferma il rischio descritto nelle criticità sotto: quel servizio è selezionabile nel form gestionale ma
  scompare silenziosamente dal pannello filtri pubblico. **Candidato a enumerazione**: l'insieme dei valori
  possibili è chiuso e coincide con gli `id` di `service_type_filters`, ma essendo un puntatore a una riga di
  un'altra tabella (con nomi mostrati all'utente e potenzialmente da tradurre/estendere) la soluzione
  corretta **non** è un ENUM ma vincolare finalmente questa colonna con una vera FK verso
  `service_type_filters.id` (ripulendo prima il valore orfano `0`).
- **Nullabile**: Sì
- **Esempio di valori**: 4, 1, 6, 5, 3, 2 (valore orfano osservato: 0)

### createdAt / updatedAt
- **Tipo**: `datetime` — standard.
- **Descrizione**: sui dati reali del DB dev, `createdAt` e `updatedAt` sono **identici su tutte le 48
  righe** (stesso timestamp) — conferma ulteriore che questa tabella è un seed statico caricato in blocco una
  sola volta, mai più modificato riga per riga.
- **Nullabile**: Sì
- **Esempio di valori**: "2023-06-14 08:00:00", "2023-06-14 08:00:00" (identico su tutte le righe)

---

## Osservazioni verificate nel codice (criticità)

- **Nessun endpoint di scrittura esposto**: `services.controller.js` contiene **solo** la funzione
  `services` (sola lettura, `Services.findAll()` senza filtri). Non è stato trovato alcun
  `Services.create`/`Services.update`/`Services.destroy` in nessun controller del backend — il catalogo
  master dei servizi non è gestibile dall'applicazione, ma solo tramite intervento diretto sul DB
  (coerente con `status` sempre `"active"` e `isChecked` sempre `0` osservati su tutte le righe).
  Comportamento in linea con `configure_categories`/`servicetypes_icons` (vedi
  [`configure_categories.md`](configure_categories.md)), tutte tabelle di catalogo "seed" senza pannello
  di amministrazione dedicato — un limite noto da confermare con il team prodotto prima della riscrittura
  (se debba restare seed statico o diventare gestibile da UI).
- **Relazione con `service_type_filters` non vincolata a livello DB**: `serviceFilterType` non ha una FK —
  un valore orfano (senza corrispondenza in `service_type_filters.id`) verrebbe semplicemente escluso
  dall'`INNER JOIN` di `getServiceFilter` senza errore, e il servizio scomparirebbe silenziosamente dal
  pannello filtri pur restando selezionabile nel form gestionale (`ServicesList.js`, che non fa questo
  join). Da vincolare con una vera FK nella riscrittura.
- **Endpoint `GET services` non filtra per `status`**: a differenza di `servicetypeicons`
  (`Servicetypeicons.findAll({ where: { status: true } })`, vedi
  [`servicetypes_icons.md`](servicetypes_icons.md)), `services.controller.js:services` restituisce
  **tutte** le righe indipendentemente da `status` — se in futuro venissero inserite righe con `status`
  diverso da `"active"` (es. per disattivare un servizio), continuerebbero comunque a essere proposte nel
  form di selezione, a differenza del comportamento (verosimilmente inteso) delle icone.
