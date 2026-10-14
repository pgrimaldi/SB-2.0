# Tabella `chatbot_history`

> Fonti: **solo schema e dati reali** (26 righe, `created_at` 2026-02-12, tutte `property_id = 96`).
> **Nessun model** in [`back-pre/app/models/`](../../back-pre/app/models/) e **nessun riferimento** a
> `chatbot_history` / `chatbot` in `back-pre` (controller, service, route, cron), né nella solution
> [`backend/`](../../backend/). I componenti `ChatBot/` presenti in `front-pre` e `front-customer-pre`
> sono una **cosa diversa** — un widget assistente-prenotazione a regole con date picker
> (`ChatbotDate`), che non tocca questa tabella.
>
> **Nota di contesto** — tabella **non censita** prima di questa scheda. Fa parte di una **feature su un
> branch separato** (assistente/chatbot FAQ per l'app gestionale): il codice che la usa non è in
> `back-pre`. Le righe contengono coppie domanda/risposta in italiano sull'uso del gestionale
> (caricamento listino, calcolo prezzo, Revenue, settori/file), con URL di immagini FAQ incapsulati tra
> marcatori `[[[ ... ]]]` nel testo di risposta.
>
> **Dominio di destinazione proposto**: **C8 — Chatbot** (nuovo modulo dedicato, sviluppato nel branch a
> parte). Verso gli altri domini solo id copiati (`property_id`, `created_by`), mai FK cross-modulo.

---

## Campi

> Le descrizioni sotto sono **inferite da nome colonna + dati osservati**: non essendoci codice in
> `back-pre`, la semantica esatta va confermata sul branch che implementa la feature.

### id
- **Tipo**: `int` (presumibilmente `AUTO_INCREMENT`, PK)
- **Nullabile**: No
- **Esempio di valori**: 1, 2, 3, ..

### property_id
- **Tipo**: `int`
- **Nullabile**: No (nei dati sempre valorizzato)
- **Relazioni**: `properties.id` — stabilimento nel cui contesto è avvenuta la conversazione.
- **Esempio di valori**: `96`
- **Deprecato**: No

### input_text
- **Tipo**: `text`
- **Nullabile**: presunto Sì
- **Descrizione**: domanda posta dall'utente al chatbot.
- **Esempio di valori**: `"Come si carica il listino prezzi aggiornato dello stabilimento?"`,
  `"È possibile impostare prezzi differenziati per le diverse file e settori della spiaggia?"`
- **Deprecato**: No

### output_text
- **Tipo**: `text`
- **Nullabile**: presunto Sì
- **Descrizione**: risposta generata dal chatbot. Testo lungo, con passaggi numerati e riferimenti a
  immagini FAQ come URL S3 racchiusi in `[[[ ... ]]]` (es.
  `[[[https://summerbooking.s3.us-east-2.amazonaws.com/faq/images/<uuid>.jpg]]]`). Gli URL sono
  completi → nella riscrittura vanno scomposti/gestiti come da
  [`docs/03-gestione-dati.md` §1](../../docs/03-gestione-dati.md).
- **Esempio di valori**: risposta multi-paragrafo (vedi campione in
  [`_feature-tabelle.md`](_feature-tabelle.md) / query diretta)
- **Deprecato**: No

### app
- **Tipo**: `varchar(10)`
- **Nullabile**: presunto Sì
- **Descrizione**: applicazione da cui proviene la conversazione. Nei dati sempre `"manag"`
  (gestionale). Stesso ruolo del campo `app` di [`chatbot_manager_config`](chatbot_manager_config.md).
- **Esempio di valori**: `"manag"`
- **Candidato a enumerazione**: **sì** — `enum ChatbotApp { Manager, Customer, ... }` (da definire nel
  modulo C8; `"manag"` è un troncamento a 10 char da normalizzare).
- **Deprecato**: No

### created_by
- **Tipo**: `int`
- **Nullabile**: Sì
- **Relazioni**: `users.id` dell'operatore che ha usato il chatbot.
- **Esempio di valori**: `199`
- **Deprecato**: No

### created_at / updated_at
- **Tipo**: `datetime` (naming `snake_case`, diverso dal resto del legacy che usa `createdAt`)
- **Nullabile**: presunto No
- **Esempio di valori**: `2026-02-12 08:32:21`

---

## Osservazioni

- **Feature fuori da `back-pre`**: nessuna riga di codice nel backend legacy la scrive o la legge; da
  trattare esclusivamente nel contesto del branch/modulo **C8**.
- **Naming già `snake_case`** (`property_id`, `created_by`, `created_at`) — coerente con le convenzioni
  target di [`docs/03-gestione-dati.md` §3.1](../../docs/03-gestione-dati.md), a differenza del resto
  del DB legacy.
- **Dati concentrati**: 26 righe, un solo `property_id` (96), un solo `created_by` (199), un solo
  giorno → verosimilmente dati di test/demo della feature.
- **URL immagini FAQ completi nel testo**: prevedere in C8 un catalogo media (o almeno la scomposizione
  `origin` + `path`) invece di URL hardcoded nel corpo della risposta.
- **`app` troncato a `varchar(10)`**: `"manag"` per `"manager"` — normalizzare a un `enum`/valore
  canonico.
