# Azioni utente (frontend) — indice

> Cartella `analisi-pre/azioni/`. Per ogni **ruolo** e per ogni **area funzionale** un documento elenca
> le **azioni che l'utente può compiere dal frontend**, ciascuna con: nome (frase breve), descrizione,
> **input inviati all'API** e **output ricevuto dall'API**. Lo scopo è supportare la **definizione delle
> firme delle API del frontend** (quali chiamate, con quali parametri in ingresso e in uscita).
>
> **Fonte**: mockup FIGMA in [`mockup/pdf/Summer Booking e Summer Booking Hub - mockups.pdf`](../../../../../../mockup/pdf/)
> (immagini equivalenti in [`mockup/immagini/`](../../../../../../mockup/immagini/)). Dove una schermata di dettaglio
> non è disegnata ma il flusso è chiaramente implicato dal mockup (es. checkout dopo "Prenota ora", form
> dietro "Accedi"/"Registrati", area personale), l'azione è comunque riportata e **marcata come
> «firma ipotizzata»**.
>
> **Fonte aggiuntiva — nuovo gestionale**: due documenti + board FIGMA descrivono il **nuovo design e
> le nuove funzionalità del gestionale**. Ogni azione cita il node FIGMA e, dove noto, l'endpoint
> attuale; molte firme sono **ipotizzate** perché i mockup non sono ancora legati a endpoint
> pubblicati.
> - `docs/pre/Pagine principali - azioni.docx` (board *Gestionale Summer Booking 1 e Sito Hub*):
>   header, Mappa e strumenti, «Dettagli e prenotazioni», Planner, Cassa, Contatti, Convenzioni,
>   sezione Prenotazioni con 9 sotto-pagine, Configurazione mappa, pannello Account →
>   [`stabilimento/04`](stabilimento/04-gestionale-header-navigazione-account.md)–[`12`](stabilimento/12-gestionale-configurazione-mappa.md).
> - `docs/pre/Pagine Impostazioni - azioni.docx` — **documento ancora in redazione (file da
>   completare)**: sezione **Impostazioni** (Informazioni stabilimento, Gestione utenti + log,
>   Stagioni e periodi, Settori e file, Configura mappa, Magazzino, Listino, Fiscalizzazione,
>   Database) → [`stabilimento/13`](stabilimento/13-gestionale-impostazioni.md). Da riverificare a
>   docx completo.
>
> **Riferimento agli endpoint esistenti**: quando noto, ogni azione riporta fra parentesi il nome
> dell'endpoint oggi usato dai frontend (`front-customer-pre`), es. `sb/propertyListOnHomePage`. È un
> aiuto per la mappatura, **non** un vincolo: la riscrittura può ridisegnare la firma. La logica di
> business di ciascuna azione (calcoli, regole, eventi) è descritta in
> [`analisi-pre/business-logic/`](../business-logic/); qui si descrive **solo** il contratto client↔API.

## Nota sulle frecce/annotazioni nei mockup

Alcune immagini contengono **frecce con brevi testi** (es. "H1: 48px", "Search bar button text: 18px",
"homepage hero image for mobile version", "Login and register your property"): sono **note di design per
chi guarda il mockup** e **non compaiono** nella schermata reale. Sono state ignorate nella stesura di
questo elenco, salvo dove chiariscono il comportamento di un elemento.

## Struttura

```
azioni/
├── _indice.md                         (questo file)
├── cliente-finale/                    sito/app di prenotazione pubblica (Summer Booking)
│   ├── 01-account-e-registrazione.md
│   ├── 02-ricerca-e-vetrina-stabilimenti.md
│   ├── 03-scheda-stabilimento.md
│   ├── 04-prenotazione-postazioni.md
│   ├── 05-prenotazione-servizi-e-listino.md
│   ├── 06-eventi.md
│   ├── 07-pagamento.md
│   ├── 08-area-personale.md
│   ├── 09-preferiti-e-condivisione.md
│   └── 10-contenuti-editoriali-e-newsletter.md
├── stabilimento/                      sito vetrina Hub + registrazione struttura + gestionale (nuovo design)
    ├── 01-sito-vetrina-hub-e-lead.md
    ├── 02-registrazione-struttura.md
    ├── 03-planner-strumenti-mappa.md          (mappa/planner dai mockup PDF)
    ├── 04-gestionale-header-navigazione-account.md
    ├── 05-gestionale-mappa.md
    ├── 06-gestionale-dettagli-e-prenotazioni.md
    ├── 07-gestionale-planner.md
    ├── 08-gestionale-cassa.md
    ├── 09-gestionale-contatti.md
    ├── 10-gestionale-convenzioni.md
    ├── 11-gestionale-prenotazioni.md          (9 sotto-pagine)
    ├── 12-gestionale-configurazione-mappa.md
    └── 13-gestionale-impostazioni.md          (sezione Impostazioni — docx in redazione)
└── systemadmin/                       portale di back-office (`front-admin/` — no FIGMA, contratto dal client)
    ├── 01-gestione-stabilimenti.md
    ├── 02-catalogo-icone-mappa-spiaggia.md
    └── 03-iscritti-newsletter.md
```

> **`systemadmin` — fonte diversa dagli altri**: il portale `systemadmin` **non** compare in FIGMA né
> nei mockup PDF, ma da **2026-09-09** il suo frontend è nel repository, cartella
> [`front-admin/`](../../../../../../front-admin/) (React 16 / Metronic 7.2.2, WIP). Le azioni in
> `azioni/systemadmin/` sono quindi ricavate **dal codice del client** (moduli `PropertyManagement`,
> `ConfigurationSettings`, `SubscriptionDetails`, `Auth`), non da un mockup: input/output riflettono le
> chiamate reali `config.js`/`api/*Apicalls.js`. Allineate 1:1 con
> [`business-logic/systemadmin/`](../business-logic/systemadmin/).
>
> **Il nuovo back-office verrà riscritto da zero** (decisione committente): questi documenti valgono come
> **specifica dei requisiti** del pannello riscritto, non come fotografia di un target. `front-admin/` è
> solo la fonte da cui sono estratti; il suo debito legacy (chiavi/URL hardcoded, payload cifrati lato
> client, "accesso diretto" via `auth/send-token`) **non** va replicato.
>
> **In perimetro dal nuovo design (docx + board FIGMA gestionale)**: header e navigazione, pannello
> Account, Mappa e tutti i suoi strumenti, «Dettagli e prenotazioni», Planner, Cassa, Contatti,
> Convenzioni, sezione Prenotazioni (9 sotto-pagine), Configurazione mappa → `stabilimento/04`–`12`;
> sezione **Impostazioni** (Informazioni stabilimento, Gestione utenti + log, Stagioni e periodi,
> Settori e file, Configura mappa, Magazzino, Listino, Fiscalizzazione, Database) → `stabilimento/13`
> (da `docs/pre/Pagine Impostazioni - azioni.docx`, **ancora in redazione**).
> Restano non trattate le voci di sidebar non ridisegnate: Preventivi, Statistiche, Ricavi,
> Marketing; e la pagina Impostazioni «Sconti e voucher», citata ma «ancora da progettare in Figma».

## Mappa mockup → schermate

| Schermata | Pagine PDF | Immagini principali |
|---|---|---|
| Homepage sito cliente (varianti) | 1–6, 26 | `Homepage*.png`, `Homepage 1440px.png` |
| Homepage — screenshot sito live (as-is) | 45–46 | — |
| Risultati di ricerca + filtri | 8 | `Pagina di ricerca.png` |
| Scheda stabilimento / prenotazione da mappa | 7 | `Pag stabilimento - prenotazione da mappa.png` |
| Mappa spiaggia (postazioni) | 29, 35 | `immagine Mappa interattiva 1.png`, `Mappa postazioni libere.2*.svg` |
| Strumenti planner (Selezione multipla / Sposta / Crea gruppo / calendario) | 31–34 | — |
| Lista eventi (calendario + card) | 9 | `Pagina lista eventi.png` |
| Dettaglio evento | 10 | `Pagina dettaglio evento.png` |
| Magazine — lista articoli (SB / SB Hub, IT/EN) | 11, 13, 21, 23, 26 | `magazine sb*.png`, `magazine sbhub*.png` |
| Magazine — dettaglio articolo (SB / SB Hub, IT/EN) | 12, 14, 19–20 | — |
| F.A.Q. (SB / SB Hub, IT/EN) | 27–28 e board | `faq - sb *.png`, `faq - sbhub *.png` |
| Sito vetrina Hub — landing e pagine | 42 (ITA), 43 (ENG), 44 | `Funzionalità.pdf`, `Booking.pdf`, `Prezzi.png`, `Chi siamo*.png` |
| "Registra la tua struttura" + "Login gestore" | 42–43 (riquadro "Login and register your property") | `Group 1268.png` |

## Gestionale (nuovo design) — sezione docx → documento azioni

Sorgente: `docs/pre/Pagine principali - azioni.docx` (board FIGMA *Gestionale Summer Booking 1 e Sito Hub*).

| Sezione docx | Documento azioni |
|---|---|
| Header e menu Summer Booking Hub · Pannello Account | [stabilimento/04-gestionale-header-navigazione-account.md](stabilimento/04-gestionale-header-navigazione-account.md) |
| Mappa: modalità Vedi info/icone, zoom, menu azioni rapide, strumenti, Filtri/Settori/Arrivi e partenze, Ricerca smart | [stabilimento/05-gestionale-mappa.md](stabilimento/05-gestionale-mappa.md) |
| Pagina «Dettagli e prenotazioni» · Pannello informazioni rapide · Sezione convenzioni in prenotazione | [stabilimento/06-gestionale-dettagli-e-prenotazioni.md](stabilimento/06-gestionale-dettagli-e-prenotazioni.md) |
| Planner (righe/settori, ricerca combinata, ricerca guidata, servizi orari) | [stabilimento/07-gestionale-planner.md](stabilimento/07-gestionale-planner.md) |
| Cassa (pagina Ordini, Movimenti, scontrini emessi, scontrino elettronico) | [stabilimento/08-gestionale-cassa.md](stabilimento/08-gestionale-cassa.md) |
| Contatti | [stabilimento/09-gestionale-contatti.md](stabilimento/09-gestionale-contatti.md) |
| Convenzioni (strutture, prenotazioni, listino, voucher, report) | [stabilimento/10-gestionale-convenzioni.md](stabilimento/10-gestionale-convenzioni.md) |
| Prenotazioni — 9 sotto-pagine (Prenotazioni, Transazioni, Richieste di pagamento, Prenotazioni online, Prenotazioni online servizi, Sharing creati, Ingressi, Scadenziario, Gruppi) | [stabilimento/11-gestionale-prenotazioni.md](stabilimento/11-gestionale-prenotazioni.md) |
| Configurazione mappa | [stabilimento/12-gestionale-configurazione-mappa.md](stabilimento/12-gestionale-configurazione-mappa.md) |

Sorgente: `docs/pre/Pagine Impostazioni - azioni.docx` (**documento in redazione — «file da completare»**; board FIGMA *Summer Booking e Summer Booking Hub — mockups* e, per Fiscalizzazione, *Gestionale Summer Booking 1 e Sito Hub*).

| Sezione docx | Documento azioni |
|---|---|
| Impostazioni: Informazioni stabilimento · Gestione utenti (+ Tutti i log, ruolo «Operatore di cassa giornaliero») · Stagioni e periodi · Settori e file · Configura mappa · Magazzino · Listino · Fiscalizzazione · Database (+ Cestino) | [stabilimento/13-gestionale-impostazioni.md](stabilimento/13-gestionale-impostazioni.md) |

## Legenda ruoli

- **ospite** — visitatore non autenticato del sito cliente.
- **cliente autenticato** — utente finale loggato (ha un account creato via `auth/usersignup` / social).
- **gestore** — titolare/staff di uno stabilimento (accede al gestionale e/o al sito vetrina Hub).
- **systemadmin** — amministratore della piattaforma (ruolo `roles.id = 1`); unico ruolo ammesso al
  login di `front-admin/`. Usato solo dalle azioni in `azioni/systemadmin/`.

## Template di ogni azione

```
### N. <Nome azione — frase breve>

**Descrizione**: cosa fa l'utente e da quale schermata.
**Schermata**: <pagina/e mockup>
**Ruolo**: ospite | cliente autenticato | gestore | systemadmin

**Input → API** (`endpoint logico`; endpoint attuale: `sb/...`)
- `campo` (tipo) — significato / origine nel form

**Output ← API**
- `campo` (tipo) — significato / uso in UI

**Note**: eventuali precisazioni; «firma ipotizzata» se la schermata non è nei mockup.
```
