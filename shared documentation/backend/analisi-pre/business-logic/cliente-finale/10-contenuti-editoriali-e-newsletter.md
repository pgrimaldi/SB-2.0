# cliente finale — Contenuti editoriali e newsletter

> **Ambito**: le pagine di contenuto del sito pubblico (magazine/blog, F.A.Q., pagine legali,
> "chi siamo") e l'iscrizione alla newsletter dal sito.
>
> **Metodo**: `front-customer-pre/src/components/newsLetter/*`, `homeDetails/*`, `supportguides/*`,
> `assets/staticPages/*.html` + `subscribe.controller.js` (`createSubscribe`, `getSubscribe`,
> `deleteSubscribe`). Schede DB: [`subscribes.md`](../../db/subscribes.md).

---

## 1. Magazine / articoli e F.A.Q.

**Azione**: il visitatore apre la lista articoli del magazine (varianti SB / SB Hub, IT/EN), un
articolo, o la pagina F.A.Q.

**Logiche**:

- Nel materiale disponibile **non esiste un backend per gli articoli** del magazine (nessun controller
  `article`/`blog`/`magazine`/`post` in `back-pre`): i contenuti sono **statici** (pagine/HTML nel
  bundle o CMS esterno non incluso nel repository). Anche le pagine legali / "chi siamo" / customer
  service sono file HTML statici (`assets/staticPages/`).
- La navigazione è puramente client‑side; nessuna personalizzazione per utente.

**Calcoli**: nessuno. **Eventi**: nessuno.

> Da chiarire col committente se il magazine/blog dovrà avere un CMS nel nuovo sistema o resterà
> contenuto statico (vedi [`da-chiarire.md`](../da-chiarire.md)).

---

## 2. Iscrizione alla newsletter dal sito

**Azione** — `POST /api/sb/createSubscribe` (`subscribe.controller.js:createSubscribe`): il visitatore
inserisce la propria email nel form newsletter.

**Logiche**:

- Aggiunge l'indirizzo alla tabella `subscribes`. Nessuna verifica double opt‑in, nessuna email di
  conferma.
- La stessa tabella viene popolata **automaticamente** anche da: registrazione cliente
  ([01-account-e-registrazione](01-account-e-registrazione.md)), registrazione stabilimento, creazione
  utente (helper `createSubscribes`). L'iscrizione esplicita dal form è quindi solo uno dei canali.
- `getSubscribe` / `deleteSubscribe`: consultazione ed eventuale disiscrizione — dal 2026-09-09
  consumati dalla voce "Subscription Details" del pannello systemadmin
  [`front-admin/`](../../../../../../../front-admin/) (elenco + eliminazione + export Excel; vedi
  [`db/subscribes.md`](../../db/subscribes.md)).

**Calcoli**: nessuno. **Eventi**: **nessuno** all'iscrizione (nessuna email di benvenuto/conferma).
Gli invii veri (newsletter, campagne) partono dal lato stabilimento — vedi
[../stabilimento/09-marketing](../stabilimento/09-marketing.md).

---

## 3. Ricezione delle comunicazioni

Il cliente iscritto può ricevere: la **newsletter estemporanea** dello stabilimento (accodata in
`email_logs` e inviata dal cron `news-letter.js` ogni minuto), le **campagne email** programmate
(cron `campaignEmail()` dal mittente `noreply@summerbooking.it`), le **automazioni** (welcome/goodbye/
custom, cron `automationEmail()` ogni 5 minuti). Dettagli e formule di programmazione:
[../stabilimento/09-marketing](../stabilimento/09-marketing.md).

**Calcoli**: (lato mittente) conversione data programmazione in UTC; data invio = evento ±
`scheduleValue` giorni. **Eventi** (lato mittente): email via SMTP; `email_logs`.

---

## 4. Riepilogo "calcoli / logiche / eventi"

- **Calcoli**: nessuno lato cliente.
- **Logiche**: contenuti editoriali statici (nessun backend nel repo); `subscribes` popolata da più
  canali, senza double opt‑in.
- **Eventi**: **nessuno** originato dal cliente in quest'area. Le email che il cliente riceve sono
  generate dai flussi marketing dello stabilimento (cron dedicati).
