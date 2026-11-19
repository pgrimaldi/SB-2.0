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
- Lo stabilimento non viaggia mai: tutte le API della pagina lo prendono dal token.

## Date delle stagioni (`app-season-dates-panel`)

- Titolo "Impostazione inizio e fine di Stagione" (`app-section-title` senza icona, testo 1rem).
- Righe Stagione corrente e Prossima stagione, ognuna un `app-form-row` bordato con "Dal" e "al" e due
  `app-filled-date-field` (icona `GeneralIcon_CalendarFilled.svg`); i nomi delle stagioni a 0.875rem.
- "Dal" e "al" con i loro campi stanno a fine riga, in una griglia, così restano incolonnati; "al" va sotto "Dal"
  quando la riga è stretta.
- Lettura: `GET /api/catalog/season` (`SeasonService.seasonDates`), all'apertura e al cambio lingua. `SeasonDates` ha i
  nomi del backend `startsOn`, `endsOn`, `nextStartsOn`, `nextEndsOn` e date senza ora (`2026-05-01`), che
  `SeasonService` converte a mezzanotte UTC per la pagina.
  - 204 = stagione non impostata: nessuna data dal backend (i primi giorni li sceglie la pagina, vedi sotto); la
    prossima stagione può essere null.
  - I confini della stagione sono quelli della tabella `catalog.pricing_seasons` del backend.
  - Se la lettura fallisce, il messaggio del `code` compare sotto le due righe, sopra il bottone.
- Se `catalog/season` non manda un "Dal", al caricamento diventa l'1 gennaio dell'anno in corso (stagione corrente)
  o dell'anno dopo (prossima stagione), calcolato con `DateAdapter.today()`.
- Le date sono bloccate (grigie) finché non si preme "Modifica", sotto; allora si cambiano col calendario e il
  bottone diventa "Annulla" (rimette le date caricate, chiedendo prima se ci sono modifiche) e "Salva", come la
  tab Informazioni generali di Informazioni stabilimento.
- "Modifica" resta spento finché la lettura non è riuscita (`isLoaded`): se fallisce le date non si modificano e
  non si salvano.
- Il "Dal" della prossima stagione si sceglie solo dopo la fine della corrente, come chiede il backend: `minDate` è
  il giorno dopo `endsOn`; se la fine della corrente si sposta oltre, il "Dal" della prossima si svuota, come
  nelle coppie Dal / al. Se il backend lo rifiuta comunque (`validation.out_of_range` su `nextStartsOn`), sotto il
  campo compare `invalidate.seasons.next_season_too_early` ("La prossima stagione deve iniziare dopo la fine di quella
  corrente.").
- Tutte e quattro le date sono obbligatorie, anche quelle della prossima stagione: Salva spento finché ne manca una,
  errori sotto i campi.
- Salva: `PUT /api/catalog/season` (`SeasonService.saveSeasonDates`) con le quattro date con gli stessi nomi e senza
  ora; risponde 200 con le date salvate, che il riquadro mostra e tiene come date caricate.
- Errori del salvataggio: quelli del backend su una data (`errors[]`, es. `validation.end_before_start` su `endsOn`)
  sotto il suo campo, gli altri sotto le due righe, sopra i bottoni; le date restano modificabili.

### Periodi fuori dalle nuove date

- Prima di salvare, "Salva" chiede i periodi delle due stagioni (`GET /api/catalog/tariff-periods` e
  `GET /api/catalog/next-season/tariff-periods`): il backend non controlla che i periodi restino dentro le nuove date.
- Se un intervallo esce dalle nuove date della sua stagione non salva: rimette le date com'erano, torna a
  "Modifica" (come "Annulla") e mostra `app-alert-popup` ("Operazione non riuscita", testo
  `seasons.season_dates.periods_outside`: "Operazione annullata perché esistono periodi per questa stagione che non
  rientrano negli estremi selezionati...").
- Se una delle due letture fallisce non salva: il messaggio del `code` compare sopra i bottoni e le date restano
  modificabili.

### Limiti dei periodi

- I periodi stanno sempre dentro la loro stagione. La pagina fornisce `SeasonDatesBehaviour`
  (`behaviours/settings/`), con le date caricate o appena salvate dal riquadro.
- Ogni tab ne prende primo e ultimo giorno della sua stagione e li passa come `minDate` / `maxDate` ai popup
  Imposta periodo (`app-period-calendar`), Aggiungi intervallo e Modifica Intervallo (`app-filled-date-field`): i
  giorni fuori stagione non si scelgono.
- Il backend rifiuta comunque, con `pricing.period_outside_season` (creazione e `PUT` del periodo). Se la stagione
  non ha ancora le due date, nessun limite nei calendari, ma "Imposta periodo" non salva (vedi sotto).

## Tab Stagione Corrente / Prossima Stagione (`SeasonPeriods`)

- Titolo "Periodi" (`app-section-title` con l'icona `GeneralIcon_CalendarFilled.svg`).
- Accanto al titolo (`appSectionTitleAside`) il bottone "Mostra tutti", che apre tutte le sottosezioni della tab e,
  quando sono tutte aperte, diventa "Nascondi tutti" e le chiude; a destra "Aggiungi periodo".
- "Mostra tutti" e "Aggiungi periodo" sono nell'azzurro dei bottoni di aggiunta delle tabelle. Con il titolo più
  stretto di 43rem (es. su un telefono) "Periodi" resta solo e i bottoni scendono sotto, due per riga (sotto i 21rem
  uno per riga).
- Una sottosezione apribile (`app-expansion-panel`) per ogni periodo della stagione della tab, nell'ordine del
  backend (per primo giorno): `GET /api/catalog/tariff-periods` (Stagione Corrente) o
  `GET /api/catalog/next-season/tariff-periods` (Prossima Stagione), API del backend usate così come sono, all'apertura
  della tab, al cambio lingua e dopo ogni modifica dei periodi.
  - Senza periodi compare "Nessun periodo inserito" (`management.settings.seasons.periods.empty`).
  - Se la lettura fallisce, il messaggio del `code` compare sotto il titolo "Periodi".
- Ogni sottosezione si apre e si chiude per conto suo: se ne possono tenere aperte più insieme ("Mostra tutti" le
  apre tutte).
- `SeasonPeriodItem` ha i nomi del backend: `key` (numero, da 0, distinto per stagione), `name` e `ranges`
  (`SeasonPeriodInterval`: `startsOn`, `endsOn`). Il backend manda i giorni senza ora (`2026-05-01`), che
  `SeasonService` converte a mezzanotte UTC per la pagina e riconverte quando li manda. Gli intervalli non hanno un id.

### Righe degli intervalli

- Aperta, la sottosezione mostra una riga per intervallo:
  - a sinistra la casella di selezione (`app-selection-checkbox`);
  - Data inizio e Data fine (giorno, mese per esteso, anno, in UTC; i nomi delle colonne un po' più su delle righe);
  - a fine riga cestino e matita (`app-icon-button` senza cerchio, tooltip "Elimina" e "Modifica").
- Righe alternate bianche e nell'azzurro delle righe pari delle tabelle (`--color__layout__highlight`), ognuna con
  un bordo grigio sottile (`--color__layout__muted`) e 0.375rem fra l'una e l'altra.
- Sotto i 29rem la Data fine va sotto la Data inizio, in ogni lingua.

### Selezione e cancellazione degli intervalli

- La selezione vale per tutti i periodi della tab, tiene gli intervalli così come li ha dati la lista e si azzera a
  ogni ricarica riuscita dei periodi e cambiando tab.
- Finché la lista dei periodi non è stata letta (durante ogni ricarica, o se la ricarica fallisce) tutte le azioni sui
  periodi sono spente (`isLoaded`): Aggiungi periodo, cestini, matite, "+" ed "Elimina selezionati". Così un popup
  non parte mai da una lista vecchia e non rimanda intervalli già cambiati o eliminati.
- Con almeno due intervalli selezionati, anche in periodi diversi, compaiono a destra di "Mostra tutti":
  - "Annulla selezione" (bianco con bordo e testo azzurri), che toglie tutte le selezioni;
  - il bottone rosso "Cancella selezionati" (`--color__layout__error`, come "Cancella dati" della pagina Database),
    nello stesso ordine della pagina Database.
- Un periodo ha sempre almeno un intervallo: se il cestino di una riga o "Cancella selezionati" toglierebbe tutti
  gli intervalli di un periodo, non parte niente e compare `app-alert-popup` ("Operazione non riuscita", testo
  `seasons.delete_intervals.period_left_empty`: "Un periodo deve avere almeno un intervallo. Per eliminarlo del tutto
  usa il cestino accanto al suo nome.").
- Altrimenti aprono `app-delete-items-popup` come le tabelle ("Vuoi eliminare l'intervallo 01 maggio 2026 – 15
  giugno 2026?" / "Vuoi eliminare i 2 intervalli selezionati?").
- Poi una sola chiamata, anche per intervalli di periodi diversi: `POST /api/catalog/tariff-periods/bulk-delete-ranges`
  (o `/api/catalog/next-season/tariff-periods/bulk-delete-ranges`), corpo `{ ranges }` con un
  `SeasonPeriodIntervalItem` (`key` del periodo, `startsOn`, `endsOn` come giorni) per intervallo, tutti o nessuno;
  risponde 204 senza corpo. Spinner a pagina intera e ricarica dei periodi; un errore va in "Operazione non
  riuscita" e, se è un 404 (`pricing.period_not_found` o `pricing.period_range_not_found`: periodo o intervallo già
  eliminato da qualcun altro), la tab ricarica anche i periodi (`(failed)` del popup e `reloadIfGone`).
- È un'API chiesta al backend (sezioni 1 e 2 di
  `shared documentation/Modifiche per il backend/impostazioni/Stagioni e periodi - Frontend requests.md`): finché non
  c'è risponde il mock (`deleteSeasonRangesMock` in `services/mocks/seasons/seasons.mock.ts`).

### Azioni sul periodo

- A sinistra del nome di ogni periodo un cestino (`app-icon-button` nello slot `appExpansionPanelStart`, tooltip
  "Elimina"): apre `app-delete-items-popup` ("Vuoi eliminare il periodo …? Verranno eliminati anche tutti i suoi
  intervalli di date se presenti."), poi l'API del backend `DELETE /api/catalog/tariff-periods/{key}` (o
  `/api/catalog/next-season/tariff-periods/{key}`), senza corpo, 204 senza corpo, con lo spinner a pagina intera e
  ricarica dei periodi della tab; errore in "Operazione non riuscita". Su un 404 (`pricing.period_not_found`: periodo
  già eliminato da qualcun altro) la tab ricarica anche i periodi, con `(failed)` del popup e `reloadIfGone`, così
  chiuso l'avviso il periodo non c'è più.
- Un periodo della stagione corrente con prezzi in qualche listino non si elimina: il backend risponde
  `pricing.period_in_use` con i nomi dei listini e il popup dice "Il periodo ha dei prezzi nei listini …: togli prima
  i suoi prezzi da questi listini, poi eliminalo.".
- Il controllo sui servizi che usano il periodo è chiesto al backend (sezione 3 del documento di richieste della
  pagina): un periodo usato da servizi risponde `pricing.period_in_use_by_services` con i loro nomi (`args.services`)
  e il popup dice "Il periodo è usato dai servizi …: togli prima i suoi prezzi da questi servizi, poi eliminalo.".
  Bloccano solo i servizi con i prezzi per periodo attivi; i prezzi suggeriti non bloccano. I mock lo simulano con i
  servizi che hanno i prezzi per periodo attivi e prezzi o disponibilità sul periodo (`servicesUsingPeriod`).
- Subito a destra del nome (`appExpansionPanelTitleAside`) una matita (tooltip "Modifica") e un "+"
  (`Seasons_AddInterval.svg`, il + sottile di Aggiungi nuova regola nell'azzurro di matita e cestino, tooltip
  "Aggiungi intervallo").
- Un periodo ha al massimo 20 intervalli (`MAX_SEASON_PERIOD_RANGES`, come il backend): il "+" di un periodo che li
  ha già non apre il popup e mostra `app-alert-popup` ("Operazione non riuscita", testo
  `seasons.periods.period_full`: "Un periodo può avere al massimo 20 intervalli di date. Per aggiungerne un altro,
  eliminane prima uno.").

## Popup

- "Imposta periodo" (`app-season-period-add-popup`, da "Aggiungi periodo"): nome del periodo (obbligatorio, al
  massimo 80 caratteri come la colonna `name` del database: `MAX_SEASON_PERIOD_NAME_LENGTH` con `metadata(…,
  MAX_LENGTH, …)`, oltre non si scrive) e `app-period-calendar` così com'è, solo a clic, con almeno un intervallo e al
  massimo 20 (oltre, sotto il calendario: `invalidate.seasons.ranges.max`, "Un periodo può avere al massimo 20
  intervalli di date.").
  - Se la stagione della tab non ha le due date salvate (`season` vuoto), sopra i bottoni compare
    `invalidate.seasons.season_required` / `next_season_required` ("Imposta e salva prima le date di inizio e fine
    della stagione corrente." / "… della prossima stagione.") e Salva resta spento, come chiederebbe il backend.
  - Salva manda `SeasonPeriodAddRequest` (`name`, `ranges` come giorni) all'API del backend
    `POST /api/catalog/tariff-periods` (o `/api/catalog/next-season/tariff-periods`), usata così com'è: risponde 201
    con il periodo creato (con la sua `key`), che la tab non usa: ricarica i periodi.
  - Errori del backend: `name`, `pricing.period_name_taken` e `validation.max_length` sotto il nome; `ranges`,
    `ranges[i]` e `pricing.period_outside_season` sotto il calendario; gli altri (es. `pricing.season_required`,
    `catalog.property_required`) sopra i bottoni.
- Gli altri tre popup salvano tutti il periodo intero con l'API del backend
  `PUT /api/catalog/tariff-periods/{key}` (o `/api/catalog/next-season/tariff-periods/{key}`), corpo
  `SeasonPeriodUpdateRequest` (`name` e tutti i `ranges`, anche quelli che non cambiano, come giorni); risponde 200
  con il periodo salvato e la tab ricarica i periodi:
  - "Modifica Periodo" (`app-season-period-edit-popup`, dalla matita del periodo): il solo campo "Nome del periodo",
    obbligatorio (al massimo 80 caratteri, come in Imposta periodo) e già riempito, Annulla e Salva; manda il nuovo
    nome con gli intervalli che il periodo ha.
  - "Aggiungi intervallo" (`app-season-interval-add-popup`, dal "+"): copia di Modifica Intervallo con le date vuote e
    le stesse regole (giorni della stagione, Al da Dal in poi); manda gli intervalli del periodo più il nuovo.
  - "Modifica Intervallo" (`app-season-interval-edit-popup`, dalla matita della riga): Data inizio e Data fine,
    `app-filled-date-field` obbligatori già riempiti con la riga, Annulla e Salva; manda gli intervalli del periodo con
    quello cambiato.
- Spinner, avviso di modifiche non salvate; nei tre popup gli errori del backend sugli intervalli (`ranges`,
  `ranges[i]…`) vanno sopra i bottoni con il loro testo (`season-ranges-errors.ts`: `validation.invalid` su `ranges`
  dà `invalidate.seasons.ranges.invalid`, gli altri il testo del loro codice), anche quando arrivano insieme a un
  errore sul nome, che resta sotto il campo; gli altri codici (`pricing.*`) vanno sopra i bottoni.
- Modifica Periodo, Aggiungi intervallo e Modifica Intervallo passano ogni errore alla tab con `(failed)`: su un 404
  (`pricing.period_not_found`: periodo già eliminato da qualcun altro) il popup mostra l'errore come sempre e dietro
  la tab ricarica i periodi (`reloadIfGone`), come dopo le cancellazioni.
- Due periodi della stessa stagione non hanno lo stesso nome, senza distinguere maiuscole e minuscole e gli spazi ai
  lati, come nel backend. "Imposta periodo" e "Modifica Periodo" ricevono i periodi della tab (`[periods]`) e lo
  controllano subito (`isTextTaken` di `validators.ts`, la modifica esclude il proprio periodo): sotto il nome
  `invalidate.seasons.period_name.taken` ("Esiste già un periodo con questo nome in questa stagione.") e Salva spento.
  Se il backend risponde comunque `pricing.period_name_taken` (es. un nome cambiato intanto da un altro utente), il
  messaggio va sotto il nome, non sopra i bottoni.
- Se il backend rifiuta gli intervalli (`validation.invalid` su `ranges`: più di 20 o giorni in comune), "Imposta
  periodo" lo scrive sotto il calendario e gli altri tre popup sopra i bottoni, con
  `invalidate.seasons.ranges.invalid`: "Intervalli non validi: un periodo può averne al massimo 20 e non possono avere
  giorni in comune."

## Intervalli senza sovrapposizioni

- Gli intervalli di una stagione non si sovrappongono mai, nemmeno di un giorno e anche tra periodi diversi.
- `SeasonPeriods.seasonIntervals` (tutti gli intervalli della tab) va ai popup:
  - Imposta periodo: `noOverlaps` sulla lista, anche tra i suoi intervalli;
  - Aggiungi intervallo e Modifica Intervallo: `noOverlap` sul form (la modifica esclude il proprio intervallo).
- Validatori in `components/shared/forms/validators.ts`. Il messaggio `invalidate.overlap` sta sotto il calendario in
  Imposta periodo e sopra i bottoni negli altri due popup; Salva resta spento.
- Il backend controlla le sovrapposizioni solo dentro lo stesso periodo (`SaveTariffPeriodRequestValidator`, per le
  due stagioni; nel database il vincolo `ex_catalog_pricing_tariff_periods_no_overlap`, solo per la stagione
  corrente): quelle tra periodi diversi le impedisce solo il frontend. Il controllo è chiesto al backend (sezioni 4 e 5
  del documento di richieste della pagina), con il codice `pricing.period_overlap` (`args.periodName`), che il popup
  mostra sopra i bottoni ("Le date si sovrappongono al periodo …"); i mock lo simulano.
- Di solito il messaggio non compare, perché i giorni presi sono grigi (`disabledPeriods`):
  - in Imposta periodo `app-period-calendar` riceve gli intervalli della stagione e quelli già scelti nel popup e,
    dopo il primo clic, mette in grigio anche i giorni dal prossimo intervallo preso in poi;
  - in Aggiungi intervallo e Modifica Intervallo `app-filled-date-field` riceve gli altri intervalli della stagione
    sia nel "Dal" sia nel "Al"; il "Al" ha come `maxDate` il giorno prima del prossimo intervallo preso dopo il
    "Dal" (`lastFreeDay`), o la fine della stagione.
