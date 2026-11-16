# Stagioni e periodi

Rotta `/settings/seasons` · vista `views/settings/seasons/` (`Seasons`, `app-seasons`), rotte in `seasons.routes.ts`.

## Struttura

- Due riquadri:
  - sopra, le date di inizio e fine delle stagioni (`app-season-dates-panel`);
  - sotto, un riquadro con due tab come Informazioni stabilimento (`app-tab-nav`, una route figlia per tab,
    `seasons.routes.ts` e `seasons-tabs.ts`): Stagione Corrente (`/settings/seasons/1`) e Prossima Stagione
    (`/settings/seasons/2`).
- Le due tab sono la stessa pagina `views/settings/seasons/season-periods/` (`SeasonPeriods`), con `isNextSeason`
  nei `data` della route (false e true).
- La guardia delle modifiche non salvate sta sulla pagina (`canDeactivate` in `seasons.routes.ts`), non sulle tab:
  uscendo dalla pagina chiede, cambiando tab no, perché le date restano sullo schermo.
- I popup della pagina sono in `components/management/settings/seasons/`.

## Date delle stagioni (`app-season-dates-panel`)

- Titolo "Impostazione inizio e fine di Stagione" (`app-section-title` senza icona, testo 1rem).
- Righe Stagione corrente e Prossima stagione, ognuna un `app-form-row` bordato con "Dal" e "al" e due
  `app-filled-date-field` (icona `GeneralIcon_CalendarFilled.svg`); i nomi delle stagioni a 0.875rem.
- "Dal" e "al" con i loro campi stanno a fine riga, in una griglia, così restano incolonnati; "al" va sotto "Dal"
  quando la riga è stretta.
- Lettura: `GET /api/catalog/season` (`SeasonService.seasonDates`), stabilimento dal token. `SeasonDates` ha i
  nomi del backend `startsOn`, `endsOn`, `nextStartsOn`, `nextEndsOn` e date senza ora (`2026-05-01`), che
  `SeasonService` converte a mezzanotte UTC per la pagina.
  - 204 = stagione non impostata, quattro date vuote; la prossima stagione può essere null.
  - I confini della stagione sono quelli della tabella `catalog.pricing_seasons` del backend.
- Se `catalog/season` non manda un "Dal", al caricamento diventa l'1 gennaio dell'anno in corso (stagione corrente)
  o dell'anno dopo (prossima stagione), calcolato con `DateAdapter.today()`.
- Le date sono bloccate (grigie) finché non si preme "Modifica", sotto; allora si cambiano col calendario e il
  bottone diventa "Annulla" (rimette le date caricate, chiedendo prima se ci sono modifiche) e "Salva", come la
  tab Informazioni generali di Informazioni stabilimento.
- Il "Dal" della prossima stagione si sceglie solo dopo la fine della corrente, come chiede il backend: `minDate` è
  il giorno dopo `endsOn`; se la fine della corrente si sposta oltre, il "Dal" della prossima si svuota, come
  nelle coppie Dal / al.
- Tutte e quattro le date sono obbligatorie, anche quelle della prossima stagione: Salva spento finché ne manca una,
  errori sotto i campi.
- Salva: `PUT /api/catalog/season` (`SeasonService.saveSeasonDates`) con le quattro date con gli stessi nomi e senza
  ora, stabilimento dal token; risponde 200 con le date salvate, che il riquadro mostra e tiene come date caricate.

### Periodi fuori dalle nuove date

- Prima di salvare, "Salva" chiede `GET /api/seasons/period-list` delle due stagioni.
- Se un intervallo esce dalle nuove date della sua stagione non salva: rimette le date com'erano, torna a
  "Modifica" (come "Annulla") e mostra `app-alert-popup` ("Operazione non riuscita", testo
  `seasons.season_dates.periods_outside`: "Operazione annullata perché esistono periodi per questa stagione che non
  rientrano negli estremi selezionati...").

### Limiti dei periodi

- I periodi stanno sempre dentro la loro stagione. La pagina fornisce `SeasonDatesBehaviour`
  (`behaviours/settings/`), con le date caricate o appena salvate dal riquadro.
- Ogni tab ne prende primo e ultimo giorno della sua stagione e li passa come `minDate` / `maxDate` ai popup
  Imposta periodo (`app-period-calendar`) e Modifica Intervallo (`app-filled-date-field`): i giorni fuori stagione
  non si scelgono.
- Il backend rifiuta comunque con `validation.out_of_range`. Se la stagione non ha ancora le due date, nessun
  limite.

## Tab Stagione Corrente / Prossima Stagione (`SeasonPeriods`)

- Titolo "Periodi" (`app-section-title` con l'icona `GeneralIcon_CalendarFilled.svg`).
- Accanto al titolo (`appSectionTitleAside`) il bottone "Mostra tutti", che apre tutte le sottosezioni della tab e,
  quando sono tutte aperte, diventa "Nascondi tutti" e le chiude; a destra "Aggiungi periodo".
- Su un telefono (titolo sotto i 27rem) "Periodi" resta solo e i due bottoni scendono sotto e si dividono la riga,
  tutti e due nell'azzurro dei bottoni di aggiunta delle tabelle.
- Una sottosezione apribile (`app-expansion-panel`) per ogni periodo di
  `GET /api/seasons/period-list?idProperty=&isNextSeason=` (i periodi della stagione della tab).

### Righe degli intervalli

- Aperta, la sottosezione mostra una riga per intervallo:
  - a sinistra la casella di selezione (`app-selection-checkbox`);
  - Data inizio e Data fine (giorno, mese per esteso, anno, in UTC; i nomi delle colonne un po' più su delle righe);
  - a fine riga cestino e matita (`app-icon-button` senza cerchio, tooltip "Elimina" e "Modifica").
- Righe alternate bianche e nell'azzurro delle righe pari delle tabelle (`--color__layout__highlight`), ognuna con
  un bordo grigio sottile (`--color__layout__muted`) e 0.375rem fra l'una e l'altra.
- Sotto i 29rem la Data fine va sotto la Data inizio, in ogni lingua.

### Selezione e cancellazione degli intervalli

- La selezione vale per tutti i periodi della tab, è per `idInterval` e si azzera a ogni ricarica dei periodi e
  cambiando tab.
- Con almeno due intervalli selezionati, anche in periodi diversi, compaiono a destra di "Mostra tutti":
  - "Annulla selezione" (bianco con bordo e testo azzurri), che toglie tutte le selezioni;
  - il bottone rosso "Cancella selezionati" (`--color__layout__error`, come "Cancella dati" della pagina Database),
    nello stesso ordine della pagina Database.
  - Su telefono i bottoni vanno due per riga sotto "Periodi".
- Il cestino di una riga e "Cancella selezionati" aprono `app-delete-items-popup` come le tabelle ("Vuoi eliminare
  l'intervallo 01 maggio 2026 – 15 giugno 2026?" / "Vuoi eliminare i 2 intervalli selezionati?").
- Poi `POST /api/seasons/delete-intervals` (`DeleteIntervalsRequest`, `idInterval` delle righe di `period-list`),
  con lo spinner a pagina intera e ricarica dei periodi. Un periodo senza più intervalli resta.

### Azioni sul periodo

- A sinistra del nome di ogni periodo un cestino (`app-icon-button` nello slot `appExpansionPanelStart`, tooltip
  "Elimina"): apre `app-delete-items-popup` ("Vuoi eliminare il periodo …? Verranno eliminati anche tutti i suoi
  intervalli di date se presenti."), poi `POST /api/seasons/delete-period` (`DeletePeriodRequest`) con lo spinner a
  pagina intera e ricarica dei periodi della tab; errore in "Operazione non riuscita".
- Subito a destra del nome (`appExpansionPanelTitleAside`) una matita (tooltip "Modifica") e un "+"
  (`Seasons_AddInterval.svg`, il + sottile di Aggiungi nuova regola nell'azzurro di matita e cestino, tooltip
  "Aggiungi intervallo").

## Popup

- "Imposta periodo" (`app-season-period-add-popup`, da "Aggiungi periodo"): nome del periodo (obbligatorio) e
  `app-period-calendar` così com'è, solo a clic, con almeno un intervallo. Salva manda `SeasonPeriodAddRequest`,
  con `idProperty` e l'`isNextSeason` della tab, a `POST /api/seasons/add-period` e ricarica i periodi della tab.
- "Modifica Periodo" (`app-season-period-edit-popup`, dalla matita del periodo): il solo campo "Nome del periodo",
  obbligatorio e già riempito, Annulla e Salva; poi `POST /api/seasons/update-period`
  (`SeasonPeriodUpdateRequest` e `idProperty`).
- "Aggiungi intervallo" (`app-season-interval-add-popup`, dal "+"): copia di Modifica Intervallo con le date vuote e
  le stesse regole (giorni della stagione, Al da Dal in poi); poi `POST /api/seasons/add-interval` (le due date,
  `idPeriod` e `idProperty`).
- "Modifica Intervallo" (`app-season-interval-edit-popup`, dalla matita della riga): Data inizio e Data fine,
  `app-filled-date-field` obbligatori già riempiti con la riga, Annulla e Salva; poi
  `POST /api/seasons/update-interval` con la riga `SeasonPeriodIntervalItem` e `idProperty`; spinner, errori sotto
  i campi, avviso di modifiche non salvate.

## Intervalli senza sovrapposizioni

- Gli intervalli di una stagione non si sovrappongono mai, nemmeno di un giorno e anche tra periodi diversi.
- `SeasonPeriods.seasonIntervals` (tutti gli intervalli della tab) va ai popup:
  - Imposta periodo: `noOverlaps` sulla lista, anche tra i suoi intervalli;
  - Aggiungi intervallo e Modifica Intervallo: `noOverlap` sul form (la modifica esclude il proprio `idInterval`).
- Validatori in `components/shared/forms/validators.ts`. Il messaggio `invalidate.overlap` sta sopra i bottoni e
  Salva resta spento; il backend risponde `validation.invalid`.
- Di solito il messaggio non compare, perché i giorni presi sono grigi (`disabledPeriods`):
  - in Imposta periodo `app-period-calendar` riceve gli intervalli della stagione e quelli già scelti nel popup e,
    dopo il primo clic, mette in grigio anche i giorni dal prossimo intervallo preso in poi;
  - in Aggiungi intervallo e Modifica Intervallo `app-filled-date-field` riceve gli altri intervalli della stagione
    sia nel "Dal" sia nel "Al"; il "Al" ha come `maxDate` il giorno prima del prossimo intervallo preso dopo il
    "Dal" (`lastFreeDay`), o la fine della stagione.
