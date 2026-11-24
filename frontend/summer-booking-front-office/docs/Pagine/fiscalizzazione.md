# Fiscalizzazione

Rotta `/settings/taxation`, vista `views/settings/taxation/` (`Taxation`).

## Struttura

- Due riquadri apribili `app-expansion-panel`: "Stampante" (aperto) e "Scontrino elettronico" (chiuso).
- Nell'intestazione di "Scontrino elettronico", a destra prima della freccia, il bottone "Scarica la guida" (blu come
  "Aggiungi stampante", nello spazio `appExpansionPanelAction`), visibile a riquadro aperto e chiuso.
- All'apertura partono la lettura delle stampanti e quella dei termini dello scontrino elettronico.
- Le icone sono solo della pagina (`Taxation_*.svg`).

## Riquadro "Stampante"

- Le stampanti stanno sotto "Stampanti aggiunte" in un `app-table` con lo stile di default, senza ricerca, caselle e
  bottone crea. Righe con `idPrinter`, `printerName`, `printerIP`, `printerMode`, `isEnabled`.
- Colonne Nome, Indirizzo IP e Modalità (`management.settings.taxation.printer.table.*`; la modalità con i testi di
  `printer.form.modes`) e la colonna Azioni con l'interruttore
  `rowToggle`, che mostra `isEnabled`, e il cestino `GeneralIcon_Delete.svg` con il tooltip "Elimina" di
  `table.actions`.
- 5 stampanti alla volta, numero fisso senza selettore (`isPageSizeSelectorHidden`): il paginatore compare solo oltre 5
  stampanti. L'API restituisce la pagina chiesta, come le altre tabelle.
- Se la lettura fallisce compare `app-table-error-popup` "Errore" con Chiudi e Riprova.
- Sotto la tabella il bottone "Aggiungi stampante".

### Interruttore e cestino

- L'interruttore manda il nuovo stato della stampante. Accendendone una il backend spegne quella attiva con la stessa modalità;
  la tabella riletta le mostra tutte com'è il server.
- Il cestino chiede conferma (`app-confirm-popup` rossa, "Vuoi eliminare la stampante EPSON - 1?", testi
  `management.settings.taxation.printer.delete.*`) e poi elimina la stampante.
- Durante la chiamata la pagina è coperta da `app-page-spinner` e un'altra azione non parte.
- Alla fine, riuscita o no, la tabella rilegge le stampanti (`reload()`), così l'interruttore mostra sempre lo stato del
  server. Un errore compare nel popup d'errore (vedi `api-e-mock.md`, errori delle azioni).

### Popup "Aggiungi stampante"

- `app-add-printer-item-popup` (`components/management/settings/taxation/add-printer-item-popup/`, testi
  `management.settings.taxation.printer.form.*`).
- Campi: Nome, Modello (tendina con i 6 valori di `PrinterModel`), Indirizzo IP, Porta (numero), SSL; "Modalità" (scelta unica,
  `app-radio-group` sui valori di `PrinterMode`: Fiscale, Non fiscale, Comande, Testo); "Reparti"; Intestazione n°1,
  Intestazione n°2 e Piè di pagina; in basso "Salva".
- Obbligatori Nome (al massimo 120 caratteri), Modello, Indirizzo IP (255), Porta (1-65535) e Modalità; le tre righe di testo al
  massimo 160 caratteri. I limiti sono nelle costanti `MAX_PRINTER_*` e Salva resta spento finché il form non è valido. Le
  stampanti USB non si aggiungono.
- "Reparti" compare solo con Fiscale: cinque tendine Aliquota (0, 4, 5, 10 e 22%, `VAT_RATES`) che partono da "Nessuna"; un'aliquota
  già scelta non compare nelle altre. Cambiando modalità si svuotano.
- Il form è `AddPrinterItemRequest` (`entities/settings/taxation/`).
- Popup largo 32rem, alto al massimo il 90% dello schermo, con lo scorrimento dentro.
- Da 1280px in su: Nome e Modello sulla stessa riga (metà ciascuno), sotto Indirizzo IP, Porta e SSL (60%, 20% e 20%);
  le aliquote tre nella prima riga e due, centrate, nella seconda.
- Sotto i 1280px i campi stanno uno sotto l'altro, ma Porta e SSL condividono una riga, che occupano tutta: SSL prende
  solo il suo spazio e Porta il resto.
- "Salva" chiama l'aggiunta con lo spinner sul bottone e i campi bloccati. Se riesce il popup si chiude ed emette
  `added`, con cui la pagina ricarica la tabella.
- Se fallisce resta aperto: l'errore di un campo va sotto quel campo (anche sotto SSL, la modalità e le aliquote), gli altri
  sopra "Salva" (`submit()` + `apiFieldErrorsOrGeneral`); gli errori rimasti si tolgono a ogni apertura.
- Chiede prima di perdere i dati scritti (vedi `form-e-validazioni.md`).

## Riquadro "Scontrino elettronico"

### Termini e condizioni

- Se lo scontrino elettronico non è abilitato per lo stabilimento (403 `fiskaly.not_enabled` dalla lettura dei termini o da "Invia")
  al posto dei termini c'è il messaggio, senza caselle né "Invia"; "Scarica la guida" resta.
- I termini sono HTML nella lingua di `Accept-Language`, ricaricati al cambio lingua, mostrati con `[innerHTML]` (Angular
  toglie script, attributi di evento e link `javascript:`) sotto "Termini e condizioni".
- Sotto, le caselle "Accetto" e "Non accetto" (etichetta prima della casella) e, sulla stessa riga in fondo a destra, il
  bottone "Invia" (`management.settings.taxation.receipt.send`).
- Le due caselle sono una scelta unica: all'apertura è spuntato "Non accetto" (valore `false`), così il cliente deve
  scegliere "Accetto" di sua volontà. La casella spuntata non si toglie: si cambia cliccando l'altra.
- Con "Non accetto" "Invia" è spento e a destra della casella compare, in rosso, "Per utilizzare lo scontrino elettronico
  devi accettare i termini di utilizzo" (`management.settings.taxation.receipt.refused_warning`).
- "Invia" chiama l'attivazione con lo spinner sul bottone e le caselle bloccate; un errore va nel popup d'errore. Solo
  con una risposta 2xx il riquadro mostra il form "Configurazione account".
- La pagina ricorda l'accettazione solo per questa visita (`isReceiptActivated`): la risposta del login non dice se lo
  scontrino è già stato accettato, quindi riaprendo la pagina si riparte dai termini.
- Da telefono (sotto 48rem) le due caselle stanno su una riga, l'avviso rosso e "Invia" ciascuno su una riga sua, "Invia"
  a destra.
- Sotto 30.5rem, dove "Scontrino elettronico" va su due righe, la sua intestazione è alta 3.75rem invece di 2.5rem,
  chiusa e aperta (classe `taxation__receipt`, solo questa pagina).

### Form "Configurazione account"

- `app-electronic-receipt-account-form` (`components/management/settings/taxation/electronic-receipt-account-form/`,
  testi `management.settings.taxation.receipt.account.*`), modello `ElectronicReceiptAccount`.
- Campi: Nome azienda, Indirizzo azienda, Numero civico, Codice postale, Città, Nazione (testo), Codice fiscale, Codice
  Agenzia Entrate (testo), Partita IVA, PIN e Password. PIN e Password sono nascosti con l'occhietto
  (`GeneralIcon_EyeClose.svg`, `GeneralIcon_EyeStart.svg`, `autocomplete="new-password"`).
- Tutti obbligatori (`requiredText`, asterisco, "Campo obbligatorio" sotto il campo, Salva spento finché ne manca uno);
  PIN e Password solo finché il server non ne ha uno salvato.
- Non c'è ancora una lettura dell'account: il form parte vuoto e Salva scrive senza controllo di versione. Lettura e
  versione sono chieste al backend (`Fiscalizzazione - Frontend requests.md`, sezione 7); il punto è in `docs/TODO.md`.
- "Salva" in basso a destra, con lo spinner sul bottone e i campi bloccati. Se riesce:
  - sulla riga del titolo, al centro del riquadro, compare l'etichetta "Account configurato con successo" con la spunta
    (`management.settings.taxation.receipt.account.saved`, icona `Taxation_AccountSaved.svg`; sotto i 1280px va sotto il
    titolo);
  - tutti i campi passano in sola lettura (regola `readonly` dei Signal Forms) e "Salva" diventa "Modifica".
- PIN e Password, una volta salvati, lasciano la pagina come la password SMTP: i campi tornano vuoti e mostrano "PIN
  impostato: scrivi per cambiarlo" e "Password impostata" (`pin_saved`, `password_saved`). Un campo lasciato vuoto
  viaggia come `null`, che per il backend mantiene il valore salvato.
- "Modifica" rende modificabili tutti i campi tranne la Password e mostra "Annulla" a sinistra di "Salva". "Annulla"
  rimette i valori dell'ultimo salvataggio e torna alla sola lettura; "Salva", se riesce, torna alla sola lettura con
  "Modifica".
- L'errore di un campo va sotto quel campo, gli altri sopra "Salva".
- Da 1280px i campi stanno su quattro righe (in sessantesimi, con `columns` di ogni campo): Nome azienda 30%, Indirizzo
  azienda 55%, Numero civico 15%; Codice postale 20%, Città 40%, Nazione 40%; Codice fiscale, Codice Agenzia Entrate e
  Partita IVA un terzo ciascuno; PIN e Password metà ciascuno. Sotto i 1280px un campo per riga.

### Passaggio fra termini e form

- La freccia "‹" del form riporta ai termini in sola lettura ("Accetto" spuntato, caselle disabilitate con `[disabled]`
  di `app-checkbox`, "Invia" spento); la freccia "›" accanto a "Termini e condizioni" riporta al form.
- Il form resta montato (nascosto con `hidden`): passando dai termini non si perde quello che si è scritto.
- Uscendo dalla pagina chiede prima di perdere i dati (vedi `form-e-validazioni.md`).

## Scarica la guida

- Chiama il download della guida con lo spinner sul bottone e salva il file ricevuto, di un tipo non ancora deciso (nome
  di riserva `electronic-receipt-guide`; vedi `api-e-mock.md`, API che restituiscono un file).
- Un errore compare nel popup d'errore.

## API

In `TaxationService` (lo stabilimento lo prende il backend dal token):

- `POST /api/fiscalprint/printers/printer-list` (`printerList`): corpo di tutte le tabelle, risposta `Page<PrinterItemListResponse>`.
- `GET /api/fiskaly/electronic-receipt` (`electronicReceipt`), senza parametri: i termini e condizioni come HTML,
  risposta `string | null` (il corpo è il testo da solo).
- `POST /api/fiscalprint/printers/set-is-active` (`setPrinterIsActive`): corpo `{ idPrinter, isEnabled }` con il nuovo stato.
- `POST /api/fiscalprint/printers/delete-printer` (`deletePrinter`): corpo `{ idPrinter }`.
- `POST /api/fiscalprint/printers/add-printer` (`addPrinter`): corpo `AddPrinterItemRequest`.
- `POST /api/fiskaly/electronic-receipt-activation` (`activateElectronicReceipt`): corpo
  `{ isElectronicReceiptAccepted }`.
- `POST /api/fiskaly/save-electronic-receipt-account` (`saveElectronicReceiptAccount`): corpo
  `ElectronicReceiptAccount`.
- `POST /api/fiskaly/download-guide-electronic-receipt` (`downloadGuideElectronicReceipt`): POST senza corpo, risposta
  un file (nome in `Content-Disposition`).

Sono tutte API nuove, chieste in `shared documentation/Modifiche per il backend/impostazioni/Fiscalizzazione - Frontend
requests.md` sotto le radici dei due moduli del backend: `/api/fiscalprint` (stampanti) e `/api/fiskaly` (scontrino
elettronico). Errori propri: `fiscalprint.printer_not_found`, `fiskaly.not_enabled`,
`fiskaly.terms_not_accepted`, `fiskaly.unavailable`.
