# Righe non migrate — `catalog.properties`

> Modulo **identity** · sorgente legacy **`users`** · **40 righe** non arrivate a destinazione.
>
> Dati grezzi completi, mai troncati: [`dati/catalog_properties.jsonl`](dati/catalog_properties.jsonl).

## Diagnosi

### Esclusa da una regola — 40 righe

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: identity.users#0 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna owner_user_public_id lasciata NULL
```
*(36 righe su 40)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: identity.users#79 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna owner_user_public_id lasciata NULL
```
*(1 righe su 40)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: identity.users#81 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna owner_user_public_id lasciata NULL
```
*(1 righe su 40)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: identity.users#82 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna owner_user_public_id lasciata NULL
```
*(1 righe su 40)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
riferimento non risolvibile: identity.users#145 non esiste nella tabella referenziata (chiave esterna gia' rotta nel legacy) - colonna owner_user_public_id lasciata NULL
```
*(1 righe su 40)*

> **Diagnosi non disponibile** per questa combinazione: e' comparsa dopo l'ultima revisione di `app/diagnosi_scarti.py`. Il messaggio qui sopra e' il dato grezzo; il verdetto va stabilito a mano.

## Righe

| id legacy | motivo | `target_row_id` | `ref_legacy_id` |
|---|---|---|---|
| `0` | Esclusa da una regola | 2 | 0 |
| `0` | Esclusa da una regola | 12 | 0 |
| `0` | Esclusa da una regola | 16 | 0 |
| `0` | Esclusa da una regola | 17 | 0 |
| `0` | Esclusa da una regola | 18 | 0 |
| `0` | Esclusa da una regola | 19 | 0 |
| `0` | Esclusa da una regola | 21 | 0 |
| `0` | Esclusa da una regola | 23 | 0 |
| `79` | Esclusa da una regola | 26 | 79 |
| `81` | Esclusa da una regola | 27 | 81 |
| `0` | Esclusa da una regola | 28 | 0 |
| `82` | Esclusa da una regola | 29 | 82 |
| `0` | Esclusa da una regola | 31 | 0 |
| `0` | Esclusa da una regola | 38 | 0 |
| `0` | Esclusa da una regola | 39 | 0 |
| `0` | Esclusa da una regola | 41 | 0 |
| `0` | Esclusa da una regola | 45 | 0 |
| `0` | Esclusa da una regola | 46 | 0 |
| `0` | Esclusa da una regola | 47 | 0 |
| `0` | Esclusa da una regola | 48 | 0 |
| `0` | Esclusa da una regola | 49 | 0 |
| `0` | Esclusa da una regola | 51 | 0 |
| `0` | Esclusa da una regola | 52 | 0 |
| `0` | Esclusa da una regola | 53 | 0 |
| `0` | Esclusa da una regola | 54 | 0 |
| `0` | Esclusa da una regola | 55 | 0 |
| `0` | Esclusa da una regola | 56 | 0 |
| `0` | Esclusa da una regola | 57 | 0 |
| `0` | Esclusa da una regola | 61 | 0 |
| `0` | Esclusa da una regola | 62 | 0 |
| `0` | Esclusa da una regola | 63 | 0 |
| `0` | Esclusa da una regola | 65 | 0 |
| `0` | Esclusa da una regola | 66 | 0 |
| `0` | Esclusa da una regola | 72 | 0 |
| `0` | Esclusa da una regola | 73 | 0 |
| `0` | Esclusa da una regola | 74 | 0 |
| `145` | Esclusa da una regola | 75 | 145 |
| `0` | Esclusa da una regola | 76 | 0 |
| `0` | Esclusa da una regola | 77 | 0 |
| `0` | Esclusa da una regola | 78 | 0 |
