# Tabella `servicetypes_icons`

> Fonti: schema + **dati reali di produzione** (dump `sb_prod_dump_2026_09_08`, **61 righe**, query
> completa; `type` `hourly` (25) / `normal` (25) / `bar` (11); tutte create nel 2021; `status` 1 su 60,
> 0 su 1; `category` `NULL` su tutte). Model
> [`back-pre/app/models/servicetypeicons.model.js`](../../../../../../back-pre/app/models/servicetypeicons.model.js)
> (`sequelize.define("servicetypes_icons", ...)`), controller
> [`servicetypeicons.controller.js`](../../../../../../back-pre/app/controllers/servicetypeicons.controller.js) (letto
> per intero — un solo endpoint di sola lettura), funzione di raggruppamento `groupArrayOfObjects` in
> `back-pre/app/global/globalConstants.js`, frontend `front-pre/src/components/servicetype/` (form di
> selezione icona per ciascun servizio bar/normale/a ore).
>
> **Nota di contesto**: tabella **globale/master** (nessun `propertyId`) — è il catalogo delle icone
> disponibili per i tre tipi di servizio gestiti in [`services_types.md`](services_types.md)
> (`barServices`/`normalServices`/`hourlyServices`), da cui il gestore sceglie quando crea un nuovo
> servizio (il campo `Servicemage`/icona dentro `services_types` viene popolato scegliendo fra queste
> righe).

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori**: 1, 2, 3, ..

### type
- **Tipo**: `varchar(100)`, `NOT NULL`
- **Valori osservati**: `hourly` (25/61), `normal` (25/61), `bar` (11/61)
- **Descrizione**: corrisponde esattamente alle tre liste di `services_types` — usato dal backend per
  raggruppare la risposta (vedi sotto). **Candidato a enumerazione**: solo 3 valori, identici e stabili nel
  tempo (le stesse tre liste di `services_types`, un dominio chiuso per definizione) — un `ENUM('normal',
  'hourly','bar')` a livello DB è appropriato, senza bisogno di una tabella lookup separata.
- **Nullabile**: No
- **Esempio di valori**: "normal", "hourly", "bar"

### name
- **Tipo**: `varchar`, `NOT NULL`
- **Descrizione**: nome descrittivo dell'icona (mostrato come etichetta/tooltip nel selettore icone).
- **Nullabile**: No
- **Esempio di valori**: "Accesible shower", "Secure Parking", "Wifi", "Berth"

### icon
- **Tipo**: `varchar(1000)`, `NOT NULL`
- **Descrizione**: percorso/nome file dell'icona.
- **Nullabile**: No
- **Esempio di valori**: "https://s3.eu-west-1.amazonaws.com/bathhouse-assets/icons/wifi.svg"

### status
- **Tipo**: `tinyint(1)`, `NOT NULL`, default `false`
- **Valori osservati**: `1` (60/61), `0` (1/61)
- **Descrizione**: filtro di attivazione — `servicetypeicons.controller.js` interroga esplicitamente
  `where: { status: true }`, quindi solo le icone attive sono restituite al frontend (a differenza di
  `services`, che non applica questo filtro — vedi [`services.md`](services.md#osservazioni-verificate-nel-codice-criticità)).
- **Nullabile**: No
- **Esempio di valori**: 1, 0

### category
- **Tipo**: `varchar(500)`
- **Valori osservati**: **sempre `NULL`** su tutte le 61 righe del campione, e non referenziato da nessun
  controller esaminato (`servicetypeicons.controller.js` non lo legge né lo filtra). **Confermato anche con
  query diretta sul DB dev**: `distinct(category) = 0` su tutte le 61 righe reali — colonna morta al 100%,
  mai valorizzata in nessun ambiente verificato.
- **Descrizione**: colonna presente nello schema e nel model ma **mai valorizzata né utilizzata** nei dati e
  nel codice osservati — probabile residuo di una funzionalità mai completata o dismessa. Da verificare con
  il team prodotto se debba essere portata nella riscrittura o rimossa.
- **Nullabile**: Sì
- **Esempio di valori**: nessuno — sempre `NULL` in tutti gli ambienti verificati
- **Deprecato**: Sì — colonna mai valorizzata (verificato sia sul campione locale sia sul DB dev, 61/61 righe
  `NULL`) e mai letta da alcun controller: residuo di una funzionalità mai completata o dismessa
- **Produzione**: **nessun valore in produzione** — tutti i record hanno `NULL` o stringa vuota `''`
  (colonna inclusa nell'elenco delle colonne vuote in produzione, verifica 2026-09-07).

### createdAt / updatedAt
- **Tipo**: `datetime`, `NOT NULL` — standard.
- **Nullabile**: No
- **Esempio di valori**: "2023-09-05 14:22:10", "2023-09-05 14:22:10"

---

## Osservazioni verificate nel codice (criticità)

- **Nessun endpoint di scrittura esposto**: come `services` e `configure_categories`, questa è una tabella
  di catalogo gestita solo tramite l'unico endpoint di lettura (`servicetypeicons`); nessun
  create/update/destroy trovato in nessun controller — gestione presumibilmente solo via intervento diretto
  sul DB.
- **Raggruppamento per `type` fatto interamente in memoria lato server**: `groupArrayOfObjects` (in
  `globalConstants.js`) raggruppa tutte le righe attive per `type` a ogni chiamata, senza cache — dato il
  volume ridotto (61 righe) l'impatto prestazionale è trascurabile, ma è un pattern da tenere presente se
  il catalogo dovesse crescere in futuro.
- **Colonna `category` di fatto morta**: come segnalato sopra, sempre `NULL` e mai letta — confermato anche
  con query diretta sul DB dev (61/61 righe `NULL`, non solo sul campione locale) — da trattare come
  vestigiale nella riscrittura salvo diversa indicazione del team prodotto.
