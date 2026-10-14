# Tabella `configure_categories`

> Fonti: schema e dati reali (5 righe, query completa), model
> [`back-pre/app/models/conficats.model.js`](../../back-pre/app/models/conficats.model.js)
> (`sequelize.define("configure_categories", ...)`, nome modello JS `Conficats`), controller
> [`confimages.controller.js`](../../back-pre/app/controllers/confimages.controller.js) (funzione
> `conficats`, righe 43-64 — condivide il file con il controller di `configure_images`, non ha un
> controller dedicato), frontend `front-pre/src/components/configuremap/EditTemplate.js`.
>
> **Nota di contesto**: tabella **globale/master**, solo 5 righe — l'elenco delle categorie in cui sono
> raggruppate le icone di [`configure_images.md`](configure_images.md).

---

## Contenuto (query diretta — tutte le 5 righe esistenti)

| id | category | status |
|----|----------|--------|
| 1 | template | 1 |
| 2 | umbrella | 1 |
| 3 | tent | 1 |
| 4 | sunbed | 1 |
| 5 | cabin | 1 |

*(tutte le 5 righe hanno `status = 1`; i valori di `category` coincidono esattamente con l'elenco
riportato nel commento SQL della colonna `configure_images.category`: `'template, umbrella, tent, sunbed,
cabin'` — coerenza incrociata verificata fra le due tabelle)*

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori**: 1, 2, 3, ..

### category
- **Tipo**: `varchar`
- **Relazioni**: applicativa verso `configure_images.category` — **confrontata per stringa**, non per id
  (nessuna FK dichiarata, nessun controller la usa per join, solo per popolare l'elenco delle categorie
  selezionabili nel form di caricamento icone). **Candidato a enumerazione**: solo 5 valori distinti,
  stabili (coincidono esattamente con le 5 righe della tabella stessa) — trattandosi già di una tabella di
  lookup di 5 righe, il miglioramento reale non è un ENUM su questo campo, ma sostituire il collegamento
  per-stringa da `configure_images.category` con una vera FK verso `configure_categories.id`.
- **Nullabile**: Sì
- **Esempio di valori**: "template", "umbrella", "tent", "sunbed", "cabin"

### status
- **Tipo**: `boolean`, default `true`. **Candidato a enumerazione**: sui dati reali il campo ha un solo
  valore osservato (`1`) su tutte le 5 righe — di fatto già un booleano; il codice (`conficats`,
  `Conficats.findAll()` senza `where`) non lo usa mai come filtro, quindi il campo risulta **inerte**
  (presente nello schema ma senza alcun effetto sul comportamento applicativo osservato) più che realmente
  deprecato.
- **Nullabile**: Sì
- **Esempio di valori**: 1 (unico valore osservato su tutte le 5 righe)

### createdAt / updatedAt
- **Tipo**: `datetime` — standard.
- **Nullabile**: Sì per entrambi
- **Esempio di valori**: "2023-05-12 10:15:00", "2024-11-03 09:42:17"

---

## Osservazioni verificate nel codice (criticità)

- **Nessun endpoint di scrittura esposto**: l'unica funzione che tocca questa tabella è `conficats`
  (sola lettura, `Conficats.findAll()` senza filtri, **non filtra nemmeno per `status`** — a differenza di
  `configure_images`/`servicetypes_icons`, restituisce anche le righe eventualmente disattivate). Nessun
  `create`/`update`/`destroy` trovato in nessun controller — le 5 categorie sono verosimilmente fisse e
  gestite solo a livello DB, coerente con [`services.md`](services.md) e
  [`servicetypes_icons.md`](servicetypes_icons.md) per lo stesso motivo.
- **Nessuna FK reale verso `configure_images.category`**: il collegamento è puramente per uguaglianza di
  stringa — rinominare una categoria qui senza aggiornare contestualmente tutte le righe di
  `configure_images` interromperebbe silenziosamente l'associazione (le icone con la vecchia stringa non
  comparirebbero più sotto la categoria rinominata). Stesso pattern di accoppiamento debole già segnalato
  ripetutamente in questo dominio.
- **Funzione condivisa nel file del controller di `configure_images`**: `conficats` non ha un controller
  dedicato nonostante sia logicamente una tabella distinta — da valutare se nella riscrittura separare le
  due risorse anche a livello di modulo/servizio.
