# stabilimento — Sito vetrina Summer Booking Hub e generazione lead

> Area funzionale: sito **marketing** di Summer Booking Hub (rivolto ai gestori di stabilimenti), con
> le pagine **Vantaggi**, **Funzionalità**, **Booking**, **Prezzi**, la homepage "Beach Management
> System", le CTA di contatto ("Richiedi la demo gratuita", "Ti chiamiamo noi", "Assaggia il futuro"),
> il Magazine/F.A.Q. dedicati e il pulsante **"Accedi"** (login al gestionale).
> **Schermate**: board "Website pages HUB - ITA" (p. 42) e "- ENGL" (p. 43); homepage Hub (p. 44,
> header: Vantaggi · Funzionalità · Booking · Prezzi · lingua · Accedi); riquadro "Login and register
> your property" (pp. 42–43). Screenshot sito cliente as-is (p. 45) mostra anche
> "Registra la tua struttura | Sei un manager? | Registrati | Accedi".
>
> Il form "Registra la tua struttura" è in [`02-registrazione-struttura.md`](02-registrazione-struttura.md).
> Contenuti editoriali del Hub: stessa struttura di
> [`../cliente-finale/10-contenuti-editoriali-e-newsletter.md`](../cliente-finale/10-contenuti-editoriali-e-newsletter.md)
> con `site = "hub"`.

---

## 1. Apertura della homepage del sito Hub

**Descrizione**: il gestore potenziale apre la landing "Beach Management System" con blocchi "Tutto in
uno", "Massima agilità operativa", "Controllo mirato", "Sell booster", "Cambia passo" (+30%
prenotazioni, +30.000 prenotazioni online, +50% vendite servizi extra), testimonianze ("Dicono di
noi": Anna/Bagno Alba, Carla, Franco…) e rassegna stampa ("Parlano di noi": la Repubblica, Il Tirreno,
La Nazione).
**Schermata**: p. 44.
**Ruolo**: gestore (potenziale), non autenticato.

**Input → API** (`GET hub/landing/home`)
- `languageCode` (int) — IT/EN

**Output ← API**
- `hero` ({ title, subtitle, primaryCta: { label, url }, secondaryCta: { label, url } })
- `featureBlocks` (array) — `{ icon, title, body }`
- `stats` (array) — `{ value, label, note, linkLabel, linkUrl }`
- `testimonials` (array) — `{ author, role, quote }`
- `pressLogos` (array) — `{ name, logo, articleUrl }`

**Note**: i valori numerici possono essere contenuti statici da CMS.

---

## 2. Apertura di una pagina di prodotto (Vantaggi / Funzionalità / Booking / Prezzi)

**Descrizione**: dal menù del Hub il gestore apre una delle pagine descrittive; "Prezzi" mostra i
piani/licenze.
**Schermata**: p. 42–43 (colonne di pagine), `Funzionalità.pdf`, `Booking.pdf`, `Prezzi.png`.
**Ruolo**: gestore (potenziale).

**Input → API** (`GET hub/landing/{key}`)
- `key` (string) — `"vantaggi"|"funzionalita"|"booking"|"prezzi"`
- `languageCode` (int)

**Output ← API**
- `page` ({ key, title, sections: [ { type, heading, body, media, items[] } ] })
- solo per `prezzi`: `plans` (array) — `{ id, name, priceMonthly, priceYearly, currency, features[],
  highlighted (bool), ctaLabel, ctaUrl }` (correlati alle `licenses` — vedi
  [`db/licenses.md`](../../db/licenses.md))

---

## 3. Richiesta di demo gratuita

**Descrizione**: dai pulsanti "Richiedi la demo gratuita" / "Assaggia il futuro" il gestore invia una
richiesta di contatto commerciale.
**Schermata**: p. 44 (bottoni "Assaggia il futuro", "Richiedi la demo gratuita" ripetuti); footer Hub
"Richiedi la demo gratuita".
**Ruolo**: gestore (potenziale).

**Input → API** (`POST hub/lead/demo-request`)
- `firstName` (string) / `lastName` (string)
- `businessName` (string) — nome dello stabilimento
- `email` (string)
- `phonePrefix` (string, default `+39`) / `phone` (string)
- `city` (string) / `region` (string, opzionale)
- `message` (string, opzionale)
- `languageCode` (int)
- `privacyAccepted` (bool)
- `source` (string) — pagina di provenienza (`"home"|"funzionalita"|"prezzi"|"footer"`)

**Output ← API**
- `status` / `message` — "richiesta inviata"
- `leadId` (int)
- `followUp` (string) — testo di conferma ("ti contatteremo entro X")

---

## 4. Richiesta di essere richiamati ("Ti chiamiamo noi")

**Descrizione**: il gestore lascia un numero e una fascia oraria per essere richiamato.
**Schermata**: p. 44 (bottone "Ti chiamiamo noi" nell'hero).
**Ruolo**: gestore (potenziale).

**Input → API** (`POST hub/lead/callback-request`)
- `firstName` (string) / `lastName` (string)
- `phonePrefix` (string) / `phone` (string)
- `preferredTime` (string) — `"morning"|"afternoon"|"evening"` o fascia oraria
- `businessName` (string, opzionale)
- `privacyAccepted` (bool)
- `languageCode` (int)

**Output ← API**
- `status` / `message`
- `leadId` (int)

---

## 5. Accesso al gestionale ("Accedi")

**Descrizione**: il gestore/staff entra nel gestionale dello stabilimento con le proprie credenziali.
**Schermata**: header Hub p. 44 ("Accedi"); header sito cliente ("Sei un gestore?" → "Accedi").
Form non disegnato → firma ipotizzata.
**Ruolo**: gestore.

**Input → API** (`auth/signin`)
- `email` (string) / `password` (string)
- `rememberMe` (bool)

**Output ← API**
- `status` / `message`
- `accessToken` (string) — JWT
- `refreshToken` (string) / `expiresIn` (int)
- `user` ({ id, firstName, lastName, email, roles[] }) — ruoli attesi: `superadmin` (titolare) o ruolo
  di staff (vedi [`db/roles.md`](../../db/roles.md))
- `properties` (array) — `{ propertyId, name, role }` stabilimenti a cui l'utente ha accesso
- `license` ({ type, startDate, endDate, status }) — stato abbonamento/licenza; se scaduto la UI
  mostra avviso (vedi business-logic §9)
- `subscriptionStatus` (string) — `"enable"|"disable"`

**Note**: la lista precisa delle azioni del gestionale (Mappa, Planner, Cassa, Prenotazioni,
Convenzioni, Statistiche, Magazzino, Marketing, Revenue, Configurazione, Impostazioni, Notifiche,
Account) **non è coperta dai mockup**; qui si documentano solo login e gli strumenti planner in
[`03-planner-strumenti-mappa.md`](03-planner-strumenti-mappa.md).

---

## 6. Cambio lingua del sito Hub

**Descrizione**: dal selettore lingua nell'header (IT/EN) il gestore cambia la lingua della vetrina.
**Schermata**: header p. 44; esistono board separate ITA (p. 42) ed ENGL (p. 43).
**Ruolo**: gestore (potenziale).

**Input → API**: nessuna (scelta locale) — le pagine successive sono richieste con `languageCode`
aggiornato.
**Output ← API**: n/a.

---

## 7. Iscrizione alla newsletter del Hub

**Descrizione**: box "Iscriviti per avere aggiornamenti" nelle pagine del Hub.
**Schermata**: footer/box Magazine Hub p. 42–43.
**Ruolo**: gestore (potenziale).

**Input → API** (`sb/createSubscribe`)
- `email` (string) / `languageCode` (int)
- `source` (string) — `"hub"`
- `audience` (string) — `"manager"` (per segmentare la lista contatti)

**Output ← API**
- `status` / `message` / `subscribed` (bool)
