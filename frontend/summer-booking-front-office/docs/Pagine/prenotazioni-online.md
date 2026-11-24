# Prenotazioni online

Rotta `/settings/online-booking-configuration`, vista `views/settings/online-booking-configuration/`
(`OnlineBookingConfiguration`), testi `management.settings.online_booking_configuration.*`.

## Struttura

- Tre riquadri bianchi (mixin `settings-panel`, larghi come Sharing):
  - "Collega il tuo account Stripe": spiegazione, riquadro con "Configura il tuo account Stripe" e i due passi;
  - "Gestisci opzioni": i bottoni "Opzioni prenotazioni online" (pannello laterale) e "Opzioni prenotazioni online
    servizi" (popup);
  - "Codice iframe per il tuo sito web": il codice e "Copia Link".
- "Configura il tuo account Stripe" porta a Stripe Connect nella stessa scheda, con uno `state` casuale tenuto in
  `sessionStorage` (`sb.stripe-connect-state`); la pagina di ritorno non c'è ancora.
- Il conto Stripe collegato si legge all'apertura e resta in `stripeAccountId`, non ancora mostrato; se la lettura
  fallisce la pagina resta com'è.

## Popup "Opzioni prenotazioni online servizi"

`app-online-booking-services-options-popup`, un form su `OnlineBookingServicesOptionsUpdateRequest` (i campi del backend).

- A ogni apertura legge le opzioni: durante la lettura e il salvataggio i campi sono bloccati, e Salva resta spento
  finché la lettura non è riuscita. Se la lettura fallisce il popup si chiude e compare `app-read-error-popup`.
- "Tipo di supplemento" (`SupplementType`: `disable`, `percentage`, `fixed`); con un supplemento compare "Valore
  supplemento", obbligatorio: da 0 a 100 (`MAX_SURCHARGE_PERCENTAGE`) o da 0 a 10.000 € (`MAX_SURCHARGE_AMOUNT`), con al
  massimo 2 decimali. Scegliendo un supplemento il campo si svuota e il messaggio compare subito; togliendolo torna il
  valore letto senza supplemento, o 0.
- "Metodi di pagamento accettati": una casella per ogni codice di `OnlinePaymentMethod`, legata alla lista
  `paymentMethods`; un codice che il popup non conosce resta nella lista. L'ordine non conta come modifica.
- "Titolo del pannello prenotazione servizi" e "Informazioni utili" sono testi formattati (`app-rich-text-field`) con al
  massimo 2.000 e 10.000 caratteri, formattazione compresa (`maxHtmlLength`).
- Dopo la lettura i campi sono già segnati come toccati: un valore letto fuori dai limiti mostra subito il suo errore.
- Salva manda sempre tutte le opzioni con la versione letta. Se riesce il popup si chiude; un errore su un campo va sotto
  quel campo (con il supplemento spento, quello del suo valore va sopra i bottoni), ogni altro errore sopra i bottoni.
  Con 409 `resource.version_conflict` il popup si chiude e compare `app-version-conflict-popup`.
- Chiudere con modifiche non salvate chiede conferma.

## Pannello "Opzioni prenotazioni online"

`app-online-booking-options-panel`, un form su `OnlineBookingOptionsDraft`: i campi del backend
(`OnlineBookingOptionsUpdateRequest`) con in più l'interruttore della conversione pomeridiana
(`isAfternoonConversionEnabled`), acceso quando `afternoonConversionTime` non è `null`.

- A ogni apertura legge tutto con una sola `GET` e Salva rimanda tutto con una sola `PUT` (opzioni, limiti e legenda), con la
  versione letta. Durante le chiamate i campi sono bloccati; Salva è spento finché la lettura non è riuscita e finché il
  form non è valido. Se la lettura fallisce il pannello si chiude e compare `app-read-error-popup`.
- Quello che il pannello non mostra torna com'è arrivato: `allowedSpotIds` ("Vedi postazioni" non fa ancora nulla, vedi
  `docs/TODO.md`), `isBlocked`, i periodi e gli orari di un'opzione letta spenta. Spegnendo un'opzione letta accesa i suoi
  valori si svuotano; riportandola spenta com'era tornano quelli letti.
- Con `isSuspendedByPlatform` in cima compare l'avviso della vendita online sospesa.
- "Solo nei periodi selezionati": al massimo 50 periodi (`MAX_ONLINE_BOOKING_PERIOD_QUANTITY`) senza giorni in comune,
  l'errore sotto il calendario.
- Mezza giornata: la fine dopo l'inizio. Conversione pomeridiana: con l'interruttore acceso l'orario è obbligatorio.
- Supplemento come nel popup dei servizi (da 0 a 100% o a 10.000 €, 2 decimali).
- "Blocco prenotazioni online" (`OnlineBookingBlock`), chiesto al backend.
- Postazioni per prenotazione e prenotazioni al giorno da 1 a 32.767, giorni di anticipo da 0 a 730.
- Limiti di booking (`BookingLimitRule`): fila/settore, Dal e Al obbligatori, giorni minimi e massimi da 1 a 366, almeno uno
  dei due, il massimo non sotto il minimo, nessun giorno in comune con un'altra regola della stessa fila; l'errore della
  riga sta sotto la riga. Una fila salvata che non c'è più resta scelta come "Settore/fila non più disponibile".
- Metodi di pagamento come nel popup dei servizi.
- Legenda (`LegendItem`): al massimo 30 elementi (poi "Aggiungi elemento" si spegne), nome obbligatorio fino a 100
  caratteri. "Seleziona icona" apre `app-icon-picker-popup` con le icone del catalogo della mappa, senza gli sfondi, lette la
  prima volta che serve (spinner sul bottone; se la lettura fallisce il messaggio sta sotto la legenda e il bottone
  riprova). Un'icona si mostra solo se è https o del nostro dominio (`fileLinkOf`).
- "Da sapere" e "Informazioni utili": al massimo 10.000 caratteri, formattazione compresa.
- Se il salvataggio riesce il pannello si chiude. Un errore su un campo va sotto quel campo, gli altri in fondo, e il
  pannello scorre al primo (`showFirstError`). Con 409 `resource.version_conflict` si chiude e compare
  `app-version-conflict-popup`. Con 404 `catalog.sector_not_found` si rilegge la tendina, con 404 `map.icon_not_found` le
  icone si rileggono alla prossima scelta.
- Chiudere con modifiche non salvate chiede conferma; l'ordine dei periodi e dei metodi di pagamento non conta.

## Codice iframe

Il codice si compone con lo `slug` dello stabilimento (`PUBLIC_PROPERTY_URL`, indirizzo da confermare: vedi `docs/TODO.md`).
"Copia Link" lo copia negli appunti (`Clipboard` del CDK) e mostra `app-message-popup`, anche se la copia non riesce. Senza
slug il codice non c'è e il bottone è spento; se la lettura fallisce al posto del codice c'è il messaggio dell'errore.

## API

Lo stabilimento lo prende il backend dal token.

- `GET /api/catalog/online-settings` (`OnlineSettingsService.onlineBookingOptions`): tutte le opzioni del pannello con
  `version`. Il service converte i giorni (`AAAA-MM-GG`) nella mezzanotte UTC e gli orari in `HH:mm`.
- `PUT /api/catalog/online-settings` (`updateOnlineBookingOptions(options, version)`): tutte le opzioni e l'header `If-Match`
  con la versione letta; risposta 200 con le opzioni salvate e l'header `ETag` della nuova versione; 409
  `resource.version_conflict`.
- `GET /api/catalog/online-settings/services` (`onlineServicesOptions`): le opzioni dei servizi con `version`.
- `PUT /api/catalog/online-settings/services` (`updateOnlineServicesOptions(options, version)`): come sopra.
- `GET /api/catalog/map/icon-catalog` (`MapService.iconCatalog`): le icone della legenda.
- `GET /api/catalog/property-info` (`CatalogService.propertySlug`): solo lo `slug`.
- `GET /api/payment/get-stripe-information` (`PaymentService.stripeInformation`): l'id del conto Stripe collegato, la
  sola stringa, o `null`.
- `POST /api/catalog/sectors/sector-combobox` (`SectorService.sectorCombobox`): la tendina Fila/settore.

Le API delle opzioni, delle icone e della scheda sono quelle del backend, con i suoi nomi. Il controllo di versione, i
limiti e la legenda insieme alle opzioni, il blocco in ore, il titolo fino a 2.000 caratteri e le API di Stripe sono chiesti
in `shared documentation/Modifiche per il backend/impostazioni/Prenotazioni online - Frontend requests.md`; la tendina in
`Servizi - Frontend requests.md`.
