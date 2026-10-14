# cliente-finale — Contenuti editoriali (Magazine, F.A.Q., pagine statiche) e Newsletter

> Area funzionale: blog/Magazine ("Il blog di Summer Booking"), pagina articolo, F.A.Q., pagine
> istituzionali del footer (Chi siamo, Contatti dell'azienda, Assistenza clienti, Note legali, Termini
> & Condizioni, Informativa privacy e cookie, "Gestisci le impostazioni sui cookie"), iscrizione alla
> newsletter.
> **Schermate**: Magazine lista pp. 11, 13, 21, 23, 26; Magazine articolo pp. 12, 14, 19–20; F.A.Q.
> pp. 27–28; box newsletter "Iscriviti per avere aggiornamenti" (home pp. 1–6, 11, 26). Le stesse
> pagine esistono per Summer Booking e per Summer Booking Hub, in IT e EN.
> Endpoint attuale per la newsletter: `sb/createSubscribe`.

---

## 1. Apertura della lista articoli del Magazine

**Descrizione**: da "Magazine" l'utente vede l'articolo in evidenza, la griglia di articoli con data e
tempo di lettura, la paginazione ("Precedente 1 2 3 … 67 68 Successiva") e la sezione "Articoli più
letti".
**Schermata**: pp. 11, 13, 21, 23.
**Ruolo**: ospite.

**Input → API** (`GET magazine/articles` — da definire; oggi contenuti probabilmente da CMS)
- `site` (string) — `"summerbooking"|"hub"`
- `languageCode` (int) — IT/EN
- `page` (int) / `pageSize` (int)
- `category` (string, opzionale) — filtro per categoria
- `tag` (string, opzionale)

**Output ← API**
- `featured` ({ id, slug, title, excerpt, coverImage, publishedAt, readingMinutes, category })
- `total` (int)
- `articles` (array) — `{ id, slug, title, excerpt, coverImage, publishedAt, readingMinutes,
  category, labels[] }`
- `mostRead` (array) — stessa forma, per "Articoli più letti"
- `categories` (array) — `{ key, label, count }`

---

## 2. Apertura di un articolo del Magazine

**Descrizione**: da "Leggi l'articolo" l'utente apre la pagina dell'articolo: titolo, label, autore
("Di Redazione Summer Booking"), data, tempo di lettura, indice interno ("In questo articolo:
Titolo 1/2/3"), corpo con immagini e citazioni, box "Prenota il tuo ombrellone" e blocco "Domande
frequenti".
**Schermata**: pp. 12, 14, 19–20.
**Ruolo**: ospite.

**Input → API** (`GET magazine/articles/{slug}`)
- `slug` (string) **o** `id` (int)
- `site` (string) / `languageCode` (int)

**Output ← API**
- `article` ({ id, slug, title, labels[], author: { name, avatar }, publishedAt, updatedAt,
  readingMinutes, coverImage, body (HTML con blocchi immagine/quote/sezione), toc: [ { anchor,
  label } ] })
- `relatedFaq` (array) — `{ question, answer }` per il blocco "Domande frequenti su Summer Booking"
- `cta` ({ title, text, buttonLabel, url }) — box "Prenota il tuo ombrellone"
- `relatedArticles` (array) — forma card come azione 1

---

## 3. Apertura della pagina F.A.Q.

**Descrizione**: da "F.A.Q." l'utente vede l'elenco di domande a fisarmonica ("Come si inserisce una
prenotazione sulla mappa?" …) con testo espandibile; sottotitolo diverso per SB ("Hai dubbi? Siamo al
tuo fianco per guidarti nelle prenotazioni online.") e per SB Hub ("Hai domande? Ti supportiamo in ogni
aspetto del gestionale.").
**Schermata**: pp. 27–28.
**Ruolo**: ospite.

**Input → API** (`GET faq`)
- `site` (string) — `"summerbooking"|"hub"`
- `languageCode` (int)
- `category` (string, opzionale)

**Output ← API**
- `intro` ({ title, subtitle })
- `faqCategories` (array, opzionale) — `{ key, label }`
- `faqItems` (array) — `{ id, categoryKey, question, answer (HTML), sortOrder }`

---

## 4. Espansione di una domanda F.A.Q.

**Descrizione**: cliccando una domanda si apre/chiude la risposta.
**Schermata**: pp. 27–28 (prima voce espansa).
**Ruolo**: ospite.

**Input → API**: nessuna — la risposta è già in `faqItems[].answer` (azione 3). Solo se il testo è
caricato on-demand: `GET faq/{id}` → `{ id, question, answer }`.

**Output ← API**: n/a.

---

## 5. Apertura di una pagina istituzionale del footer

**Descrizione**: dai link del footer (Chi siamo, Contatti/Contatti e sedi, Assistenza clienti, Note
legali, Termini & Condizioni, Informativa su privacy e cookie) l'utente apre la pagina di contenuto
statico corrispondente.
**Schermata**: footer di tutte le pagine; "Chi siamo" pp. `Chi siamo-*.png`.
**Ruolo**: ospite.

**Input → API** (`GET pages/{key}`)
- `key` (string) — `"about"|"contacts"|"customer-support"|"legal-notes"|"terms"|"privacy-cookie"`
- `site` (string) / `languageCode` (int)

**Output ← API**
- `page` ({ key, title, body (HTML), updatedAt, seo: { title, description } })
- per "Contatti e sedi": `offices` (array) — `{ label, address, city, zip, phone, email, vat,
  mapUrl }` (nel footer: "Sede legale Via Conte Fazio 56122 Pisa", "Sede operativa Via Giuntini 63
  56023 Navacchio (PI)")

---

## 6. Iscrizione alla newsletter

**Descrizione**: dal box "Iscriviti per avere aggiornamenti" l'utente inserisce la propria email e si
iscrive per ricevere offerte delle strutture preferite.
**Schermata**: home pp. 1–6, 11, 26 (campo "La tua e-mail" + pulsante "Iscriviti").
**Ruolo**: ospite e cliente autenticato.

**Input → API** (`sb/createSubscribe`)
- `email` (string)
- `languageCode` (int)
- `source` (string) — `"homepage"|"magazine"|"footer"`
- `consent` (bool) — spunta/accettazione informativa (implicita nel box)
- `userId` (int, opzionale) — se loggato

**Output ← API**
- `status` / `message` — "iscrizione registrata" / "email già iscritta" / "email non valida"
- `subscribed` (bool)
- `doubleOptInSent` (bool) — se è previsto un link di conferma via email

---

## 7. Gestione del consenso ai cookie

**Descrizione**: dal banner cookie e dal link "Gestisci le impostazioni sui cookie" l'utente accetta,
rifiuta o personalizza le categorie di cookie.
**Schermata**: link nel footer "Gestisci le impostazioni sui cookie".
**Ruolo**: ospite.

**Input → API**: tipicamente **nessuna** (preferenze salvate in cookie/localStorage lato client). Se
loggata la scelta a server (`POST consent`): `{ userId?, categories: { necessary: true, analytics,
marketing, preferences }, policyVersion }` → `{ status, savedAt }`.

**Output ← API**: n/a / `{ status }`.

---

## 8. Anteprima editoriale in homepage ("Lasciati ispirare")

**Descrizione**: la sezione "Lasciati ispirare — Idee, luoghi e storie per vivere l'estate italiana"
mostra 5 anteprime di articoli con titolo e sommario.
**Schermata**: home pp. 1–6, 26.
**Ruolo**: ospite.

**Input → API**: coperta da `sb/propertyListOnHomePage` (campo `inspiration`, vedi
[`02-ricerca-e-vetrina-stabilimenti.md`](02-ricerca-e-vetrina-stabilimenti.md) azione 1) **oppure**
`GET magazine/articles?featuredHome=true&limit=5`.

**Output ← API**
- `items` (array) — `{ id, slug, title, excerpt, image, category }`
