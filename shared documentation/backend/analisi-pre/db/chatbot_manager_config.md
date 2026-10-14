# Tabella `chatbot_manager_config`

> Fonti: **solo schema e dati reali** (dump di produzione `sb_prod_dump_2026_09_08`, **1 riga**:
> `property_id = 96`, `max_call_per_day = 100`, `max_returned_history = 10`, `created_by = NULL`,
> `created_at`/`updated_at` `2026-02-12 09:32:07` — coerente con la sessione demo di
> [`chatbot_history`](chatbot_history.md)). **Nessun model** in
> [`back-pre/app/models/`](../../../../../../back-pre/app/models/) e **nessun riferimento** a `chatbot_manager_config`
> / `chatbot` in `back-pre` né nella solution [`backend/`](../../../../backend/).
>
> **Nota di contesto** — tabella **non censita** prima di questa scheda. Fa parte della stessa **feature
> su branch separato** di [`chatbot_history`](chatbot_history.md): è la **configurazione per
> stabilimento** del chatbot dell'app gestionale (limiti di utilizzo). Il codice che la usa non è in
> `back-pre`.
>
> **Dominio di destinazione proposto**: **C8 — Chatbot** (nuovo modulo dedicato, branch a parte).

---

## Campi

> Descrizioni **inferite da nome colonna + dati**: nessun codice in `back-pre` da tracciare; confermare
> sul branch della feature.

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — PK (confermato dallo schema del dump di produzione)
- **Nullabile**: No
- **Esempio di valori** (prod): `1` (unica riga).

### property_id
- **Tipo**: `int`
- **Nullabile**: No
- **Relazioni**: `properties.id` — stabilimento a cui si applica la configurazione. Verosimilmente
  **1 riga per property** (da confermare con un `UNIQUE` nella riscrittura).
- **Esempio di valori**: `96`
- **Deprecato**: No

### max_call_per_day
- **Tipo**: `int`
- **Nullabile**: presunto Sì
- **Descrizione**: numero massimo di richieste al chatbot consentite per giorno (per stabilimento).
  Limite anti-abuso / controllo costi API.
- **Esempio di valori**: `100`
- **Deprecato**: No

### max_returned_history
- **Tipo**: `int`
- **Nullabile**: presunto Sì
- **Descrizione**: quante voci di [`chatbot_history`](chatbot_history.md) restituire/riutilizzare come
  contesto della conversazione (finestra di storico).
- **Esempio di valori**: `10`
- **Deprecato**: No

### created_by
- **Tipo**: `int`
- **Nullabile**: Sì (nei dati `NULL`)
- **Relazioni**: `users.id` di chi ha creato la configurazione.
- **Esempio di valori**: `NULL`
- **Deprecato**: No

### created_at / updated_at
- **Tipo**: `datetime` (naming `snake_case`)
- **Nullabile**: presunto No
- **Esempio di valori**: `2026-02-12 08:32:07`

---

## Osservazioni

- **Feature fuori da `back-pre`**: da trattare solo nel modulo **C8**; nessun uso nel backend legacy.
- **1 sola riga** (property 96) → dati di test/demo; aggiungere `UNIQUE(property_id)` nella riscrittura
  se la semantica "1 config per stabilimento" è confermata.
- **`created_by` non valorizzato**: se la configurazione è creata da un job/seed, prevedere comunque
  l'attore per audit.
- **Naming già `snake_case`**, coerente con le convenzioni target.
- Manca un campo `app` (presente invece in `chatbot_history`): la config sembra implicitamente solo per
  l'app gestionale ("manager") — verificare se serve una config anche per l'app cliente.
