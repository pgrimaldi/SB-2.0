# Logica di business — indice

> Cartella `analisi-pre/business-logic/`. Per ogni **tipo di utente** e per ogni **voce di menù / area
> funzionale** un documento descrive, a parole (nessun codice), la logica di business di ogni azione
> dell'utente: **calcoli** (operazioni su prezzi, date, quantità), **logiche** (condizioni, iterazioni,
> regole di ruolo) ed **eventi** (invio email/notifiche, chiamate ad API esterne, job schedulati,
> scritture su tabelle di log).
>
> Fonti incrociate per ogni scheda: `back-pre` (fonte primaria della logica), i quattro frontend
> (`front-pre` gestionale, `front-customer-pre` sito pubblico, `front-app-pre` app mobile,
> `front-modern-pre` libreria fiscale/cassa moderna), lo schema e i dati del DB, e le schede di dettaglio
> in [`analisi-pre/db/`](../db/). Le divergenze note fra i quattro frontend sono annotate dove rilevanti.
>
> **Documento correlato**: [`da-chiarire.md`](da-chiarire.md) raccoglie tutti i punti aperti emersi,
> raggruppati per tipologia. La cartella [`analisi-pre/azioni/`](../azioni/) descrive invece il
> **contratto client↔API** (input/output) delle stesse azioni.

## Struttura

```
business-logic/
├── _indice.md               (questo file)
├── da-chiarire.md           elementi da chiarire col committente, per tipologia
├── systemadmin/             portale di back-office della piattaforma (frontend: `front-admin/`)
├── stabilimento/            gestionale dello stabilimento (front-pre: sidebar + header)
└── cliente-finale/          sito/app di prenotazione pubblica (front-customer-pre, front-app-pre)
```

Ogni cartella tratta **una voce di menù (o area funzionale) per file**; tutte le funzionalità di una
voce stanno nello stesso file.

## systemadmin (portale piattaforma)

> **Aggiornamento 2026-09-09**: il pannello del `systemadmin` **è ora nel repository**, cartella
> [`front-admin/`](../../../../../../front-admin/) — SPA React 16 su tema Metronic 7.2.2 (progetto CRA `demo3`),
> ancora **work-in-progress** (base URL API e chiavi hardcoded, endpoint di "accesso diretto" verso
> `localhost`, molti moduli demo Metronic non rimossi). Le schede di quest'area erano state ricostruite
> **dal solo backend**; ora le azioni, gli endpoint invocati e le divergenze sono **verificabili anche
> sul client**. Le note "assenza di frontend" nelle singole schede sono aggiornate di conseguenza; il
> contratto client↔API è in [`analisi-pre/azioni/systemadmin/`](../azioni/systemadmin/). Le "aree
> funzionali" sostituiscono comunque le "voci di menù".
>
> **Decisione (committente)**: il **nuovo back-office verrà riscritto da zero**. `front-admin/` non è il
> target e non va evoluto: serve **solo come riferimento dei requisiti** insieme a queste schede. Tutto
> il debito legacy del pannello (chiavi/URL hardcoded, payload cifrati lato client, impersonation via
> `auth/send-token`, boilerplate Metronic) è da scartare.
>
> **Moduli reali di `front-admin/`** (gli altri sono demo Metronic non usati): `Auth` (login, password
> dimenticata/reset), `PropertyManagement` (code New/Approved/Rejected, dettaglio, approva/rifiuta,
> elimina, add/update Super Admin, "accesso diretto" al gestionale come titolare), `ConfigurationSettings`
> (catalogo icone mappa — scheda 02), `SubscriptionDetails` (lista iscritti newsletter `subscribes` +
> eliminazione + export Excel), `UserProfile`. Solo il ruolo `systemadmin` può autenticarsi (il login
> rifiuta ogni altro ruolo con "Unauthorized Access").

| # | Area | File |
|---|---|---|
| 01 | Gestione stabilimenti (onboarding, code, revisione, approvazione/rifiuto, modifica dati e commissione, eliminazione, licenza/abbonamento del titolare, prenotazione online, import CSV, job di disattivazione) | [systemadmin/01-gestione-stabilimenti.md](systemadmin/01-gestione-stabilimenti.md) |
| 02 | Catalogo icone mappa spiaggia (`configure_images`) | [systemadmin/02-catalogo-icone-mappa-spiaggia.md](systemadmin/02-catalogo-icone-mappa-spiaggia.md) |
| 03 | Dati di piattaforma e lookup (`licenses`, `country`, `services`, filtri, icone) | [systemadmin/03-dati-di-piattaforma-e-lookup.md](systemadmin/03-dati-di-piattaforma-e-lookup.md) |
| 04 | Iscritti newsletter (`subscribes`): elenco, eliminazione, export Excel — voce "Subscription Details" di `front-admin/` | logica CRUD minima, dettaglio in [`db/subscribes.md`](../db/subscribes.md) |

## stabilimento (gestionale)

> Voci della sidebar di `front-pre` dall'alto in basso, poi le azioni dell'header a destra.
> Documento trasversale: [`stabilimento/_motore-di-prezzo-e-sconti.md`](stabilimento/_motore-di-prezzo-e-sconti.md)
> (come sono calcolati prezzi e sconti — richiamato dalle voci Mappa, Planner, Cassa, Prenotazioni).

| # | Voce di menù | File |
|---|---|---|
| — | Motore di prezzo e sconti (trasversale) | [stabilimento/_motore-di-prezzo-e-sconti.md](stabilimento/_motore-di-prezzo-e-sconti.md) |
| 01 | Mappa (spiaggia) | [stabilimento/01-mappa.md](stabilimento/01-mappa.md) |
| 02 | Planner | [stabilimento/02-planner.md](stabilimento/02-planner.md) |
| 03 | Cassa | [stabilimento/03-cassa.md](stabilimento/03-cassa.md) |
| 04 | Contatti | [stabilimento/04-contatti.md](stabilimento/04-contatti.md) |
| 05 | Prenotazioni (Credit) | [stabilimento/05-prenotazioni.md](stabilimento/05-prenotazioni.md) |
| 06 | Convenzioni (strutture partner / hotel) | [stabilimento/06-convenzioni.md](stabilimento/06-convenzioni.md) |
| 07 | Statistiche | [stabilimento/07-statistiche.md](stabilimento/07-statistiche.md) |
| 08 | Magazzino | [stabilimento/08-magazzino.md](stabilimento/08-magazzino.md) |
| 09 | Marketing (Newsletter / Campagne / Automazioni / Eventi) | [stabilimento/09-marketing.md](stabilimento/09-marketing.md) |
| 10 | Revenue (Revenue Management) | [stabilimento/10-revenue.md](stabilimento/10-revenue.md) |
| 11 | Configurazione (header) | [stabilimento/11-configurazione.md](stabilimento/11-configurazione.md) |
| 12 | Impostazioni (header) | [stabilimento/12-impostazioni.md](stabilimento/12-impostazioni.md) |
| 13 | Assistente virtuale, Notifiche, Account (header) | [stabilimento/13-assistente-notifiche-account.md](stabilimento/13-assistente-notifiche-account.md) |

## cliente-finale (sito/app pubblica)

> Nessun menù a voci: aree funzionali (allineate a [`analisi-pre/azioni/cliente-finale/`](../azioni/cliente-finale/)).

| # | Area | File |
|---|---|---|
| 01 | Account e registrazione | [cliente-finale/01-account-e-registrazione.md](cliente-finale/01-account-e-registrazione.md) |
| 02 | Ricerca e vetrina stabilimenti | [cliente-finale/02-ricerca-e-vetrina-stabilimenti.md](cliente-finale/02-ricerca-e-vetrina-stabilimenti.md) |
| 03 | Scheda stabilimento | [cliente-finale/03-scheda-stabilimento.md](cliente-finale/03-scheda-stabilimento.md) |
| 04 | Prenotazione postazioni | [cliente-finale/04-prenotazione-postazioni.md](cliente-finale/04-prenotazione-postazioni.md) |
| 05 | Prenotazione servizi e listino | [cliente-finale/05-prenotazione-servizi-e-listino.md](cliente-finale/05-prenotazione-servizi-e-listino.md) |
| 06 | Eventi | [cliente-finale/06-eventi.md](cliente-finale/06-eventi.md) |
| 07 | Pagamento | [cliente-finale/07-pagamento.md](cliente-finale/07-pagamento.md) |
| 08 | Area personale | [cliente-finale/08-area-personale.md](cliente-finale/08-area-personale.md) |
| 09 | Preferiti e condivisione | [cliente-finale/09-preferiti-e-condivisione.md](cliente-finale/09-preferiti-e-condivisione.md) |
| 10 | Contenuti editoriali e newsletter | [cliente-finale/10-contenuti-editoriali-e-newsletter.md](cliente-finale/10-contenuti-editoriali-e-newsletter.md) |

## Note trasversali

- **Il prezzo/importo autorevole è calcolato lato client** e salvato dal backend senza ricalcolo
  (`booking_payments`, `booking_discount`, importi Stripe). Nella riscrittura va portato server‑side.
- **Eventi ricorrenti**: notifiche in‑app (`notification_details`) create dai flussi prenotazione/
  pagamento/scontrino e consegnate via socket.io; email accodate in `email_logs` e inviate dal cron
  `news-letter.js` (1 min) o direttamente; cron marketing (`campaignEmail` 1 min, `automationEmail`
  5 min); cron `cron-disableproperty` (00:00) per gli abbonamenti scaduti; API esterne: **Stripe**
  (pagamenti), **Fiskaly** (scontrino elettronico), **Open‑Meteo** (Revenue), **WhatsApp** (conferma
  servizio, report convenzioni via deep link).
