# Dizionario dei termini di dominio

> Glossario dei termini di business usati nell'applicativo, legacy e nuovo, divisi per dominio.
> Colonne: **Termine legacy** (nome/campo/tabella dell'app legacy Node/Sequelize, quando esiste),
> **Termine nuovo** (nome nel nuovo schema/backend .NET modulare), **Cos'è** (spiegazione in linguaggio
> di business, non tecnico).
>
> `— (nuovo)` in colonna "Termine legacy" = concetto che non esisteva prima o non aveva un nome/tabella
> dedicata nel legacy.
>
> Fonti principali: [`analisi-post/db/_feature-tabelle.md`](../analisi-post/db/_feature-tabelle.md) e le
> singole schede in [`analisi-post/db/`](../analisi-post/db/) (nuovo schema), le schede legacy in
> [`analisi-pre/db/`](../analisi-pre/db/), la business-logic in
> [`analisi-pre/business-logic/`](../analisi-pre/business-logic/) e
> [`analisi-pre/azioni/`](../analisi-pre/azioni/), i documenti di indirizzo in [`docs/`](.) (in
> particolare [`00-struttura.md`](00-struttura.md), [`03-gestione-dati.md`](03-gestione-dati.md),
> [`34-multitenant.md`](34-multitenant.md)).

---

## Indice

- [Termini trasversali e ruoli](#termini-trasversali-e-ruoli)
- [Dominio identity (C5) — autenticazione, account, clienti](#dominio-identity-c5--autenticazione-account-clienti)
- [Dominio privacy](#dominio-privacy)
- [Dominio catalog (C2) — catalogo & configurazione](#dominio-catalog-c2--catalogo--configurazione)
- [Dominio booking (C1) — prenotazioni](#dominio-booking-c1--prenotazioni)
- [Dominio payment (C3) — pagamenti & incassi](#dominio-payment-c3--pagamenti--incassi)
- [Dominio fiskaly (C4) — scontrino fiscale elettronico](#dominio-fiskaly-c4--scontrino-fiscale-elettronico)
- [Dominio fiscalprint (C4bis) — stampa fiscale hardware](#dominio-fiscalprint-c4bis--stampa-fiscale-hardware)
- [Dominio notification (C6) — notifiche & marketing](#dominio-notification-c6--notifiche--marketing)
- [Dominio switchboard (C7) — centralino telefonico & WhatsApp](#dominio-switchboard-c7--centralino-telefonico--whatsapp)
- [Dominio chatbot (C8) — assistente AI](#dominio-chatbot-c8--assistente-ai)
- [Dominio infra — client API & segreti](#dominio-infra--client-api--segreti)

---

## Termini trasversali e ruoli

| Termine legacy | Termine nuovo | Cos'è |
|---|---|---|
| "Sito Hub" (board FIGMA "Gestionale Summer Booking 1 e Sito Hub") | Summer Booking Hub | Sito vetrina rivolto ai gestori (landing/lead per la registrazione di una nuova struttura), distinto dal gestionale operativo |
| `properties` | `property` (modulo Catalog, C2) | Lo "stabilimento balneare": l'entità che nel nuovo sistema è anche il tenant |
| — (concetto architetturale) | Tenant | Ogni `property` è un tenant: i dati di uno stabilimento sono isolati da quelli degli altri stabilimenti sulla piattaforma |
| `properties.propertyOwner` | Titolare | L'utente proprietario/gestore principale dello stabilimento (ruolo legacy `superadmin`), assegnato una sola volta in fase di registrazione |
| ruolo `systemadmin` (`roles.id=1`) | Systemadmin / amministratore di piattaforma | Amministratore della piattaforma Summer Booking (non di un singolo stabilimento): approva/rifiuta stabilimenti, gestisce dati globali |
| ruolo `superadmin` (`roles.id=2`) | Titolare (owner dello stabilimento) | Proprietario/gestore principale di uno specifico stabilimento — da non confondere con `systemadmin`, che è di piattaforma |
| ruolo `admin` | Amministratore (staff) | Utente di stabilimento con permessi ampi di configurazione, creato da titolare o altro admin |
| ruolo `manager` | Gestore operativo | Operatore generico dello stabilimento (etichetta IT storica "Manager"); nel codice il termine "manager" è usato anche in senso generico per "chi gestisce lo stabilimento" |
| ruolo `lifeguard` / `advancedlifeguard` | Bagnino / Bagnino avanzato | Operatore con permessi ridotti (prezzi e pannelli avanzati nascosti solo lato client); la differenza fra le due varianti non è chiarita nel legacy |
| ruolo `cashoperator` | Operatore di cassa | Ruolo con accesso mirato alla Cassa e all'invio di comunicazioni ai clienti |
| ruolo `dailycashoperator` (nuovo) | Operatore di cassa giornaliero | Come `cashoperator`, ma non vede prenotazioni già finite né date passate; vede quelle iniziate nel passato e ancora in corso |
| ruolo `waiter` | Cameriere | Ruolo per il servizio bar/ristorante, oggi vincolato solo lato client (nessun controllo server-side) |
| ruolo `affiliate` | Affiliato | Utente a cui vengono assegnate solo alcune location/postazioni della mappa che può gestire |
| ruolo `consumer` | Cliente finale | Utente che prenota lato Summer Booking; ruolo di default per chi non ha ruoli operativi |
| ruolo `websiteadmin` / `user1` | `websiteadmin` / `user1` | Ruoli senza consumer riscontrato nel codice; **mantenuti** su conferma del committente (2026-10-07), con i permessi da definire nella matrice ruoli |
| — (nuovo) | Modulo / dominio (bounded context) | Unità di backend autonoma — un proprio schema DB, un `DbContext`, una history di migration — che possiede un'area di business (es. Catalog, Booking, Payment); 11 moduli mappati sui domini C1-C8/INFRA/Privacy, più **Audit** (dodicesimo, schema creato il 2026-10-07, ancora senza API) |
| — (nuovo) | Registro delle attività / audit trail | Registro trasversale di chi ha fatto cosa (azioni degli operatori, eventi di sicurezza, azioni del systemadmin), modulo `audit`, tabella `audit_trail_entries`; da non confondere con i log tecnici di dominio (es. `fiskaly_receipt_logs`) né con i log diagnostici su CloudWatch — [86-modulo-audit.md](86-modulo-audit.md) |
| `propertyId` (letto dal body richiesta) | Discriminatore tenant (`property_id`) | Colonna presente su ogni tabella multi-tenant che identifica a quale stabilimento appartiene la riga; nel nuovo sistema è sempre letta dal token di sessione, mai dalla richiesta del client |
| `id` (int autoincrementale) | Id interno (`id`) | Identificativo tecnico usato solo dentro il modulo che possiede il dato, mai esposto fuori dal proprio schema |
| — (nuovo) | Id pubblico (`public_id`) | L'identificativo usato nelle URL/API esterne e copiato dagli altri moduli quando devono riferirsi a quel dato; resta stabile anche se cambia la rappresentazione interna, è un UUID v7 |
| `isDeleted` / `status=0` / `IsDeleted` (nomi diversi per tabella) | Soft-delete (`deleted_at`) | Una riga "cancellata" non viene rimossa dal database ma marcata con la data di cancellazione; resta recuperabile per un periodo di audit/ripristino |
| — (nuovo) | Evento di dominio | Notifica interna che un modulo pubblica quando accade un fatto di business rilevante (es. "prenotazione confermata"); gli altri moduli reagiscono senza leggere direttamente il database altrui |
| — (nuovo) | Read-model / snapshot (`*_config_snapshots`) | Copia "pronta per la lettura" di una configurazione, calcolata a partire dai dati normalizzati, usata per rispondere velocemente al frontend senza ricostruire il dato ogni volta |
| URL completo salvato nei campi immagine/PDF | Media asset (`*_media_assets`) | Riferimento a un file scomposto in origine (dove si trova) + percorso, al posto dell'URL intero salvato a database |
| campi `varchar` liberi per stato/tipo/categoria | Enum di dominio / tabella di lookup | I due modi in cui un "vocabolario chiuso" viene modellato: enum quando aggiungere un valore richiede comunque nuovo codice, tabella quando un operatore deve poterlo estendere da pannello senza deploy |
| — (invariato) | Summer Booking | Nome del prodotto/piattaforma di prenotazione |
| `bathouses` | Stabilimento balneare | Il tipo di attività gestita sulla piattaforma (bagno/lido) |
| `hotels` (voce "Convenzioni") | Struttura partner / Convenzione | Hotel/B&B/camping collegato allo stabilimento che invia clienti, con listino e/o voucher dedicati |
| `hold_locations` | Hold (blocco temporaneo) | Blocco temporaneo di una postazione mentre un cliente completa la prenotazione, per evitare doppie prenotazioni |
| `sharing_details` | Sharing (condivisione postazione) | Meccanismo per cui più prenotazioni condividono la stessa postazione in giorni diversi |
| menu "Prenotazioni" (route interna `/credit`, componente `CreditParent`) | Prenotazioni / Ledger credito | Console di consultazione di tutte le prenotazioni dello stabilimento, comprensiva di una scheda "ledger/credito" per il credito cliente |
| `checkIn` / `checkOut` (bookings) | Check-in / Check-out | Stato di presenza fisica del cliente sulla postazione prenotata, usato sia in Prenotazioni (C1) sia nei report Convenzioni (`hotel_reports.searchType`) |

---

## Dominio identity (C5) — autenticazione, account, clienti

### Sottoarea access
| Termine legacy | Termine nuovo | Cos'è |
|---|---|---|
| `users` | account / utente (`identity_access_users`) | Anagrafica minimale di chi fa login: titolare, staff di stabilimento o cliente finale (consumer); un solo ruolo per account |
| `roles` | ruolo (`identity_access_roles`) | Catalogo dei 13 ruoli applicativi che determinano cosa un account può fare; nomi identici al legacy, nessuno dismesso |
| `systemadmin` | `systemadmin` | Amministratore della piattaforma (back-office multi-stabilimento), non scoped su una struttura |
| `superadmin` | `superadmin` (titolare) | Proprietario/gestore principale dello stabilimento, assegnato solo alla registrazione |
| `consumer` | `consumer` (cliente) | Ruolo di default del cliente finale che si registra sul sito pubblico |
| `admin` | `admin` (amministratore) | Staff con permessi ampi sullo stabilimento, assegnabile da pannello |
| `lifeguard` | `lifeguard` (bagnino) | Ruolo ristretto: accesso solo a Mappa/Magazzino, non modifica prenotazioni |
| `manager` | `manager` | Ruolo operativo di gestione dello stabilimento |
| `cashoperator` | `cashoperator` (operatore di cassa) | Ruolo per le operazioni di cassa |
| — (nuovo) | `dailycashoperator` (operatore di cassa giornaliero) | Come `cashoperator`, ma non vede le prenotazioni già finite e non può scegliere date passate nel calendario (decisione del 2026-10-07) |
| `websiteadmin` | `websiteadmin` | Ruolo storico senza utilizzo verificato nei frontend attuali, mantenuto su richiesta del committente |
| `user1` | `user1` | Ruolo dallo scopo non documentato, mantenuto su richiesta del committente |
| `waiter` | `waiter` (cameriere) | Ruolo enforced solo lato client nel legacy; da rendere effettivo anche lato server |
| `affiliate` | `affiliate` (affiliato) | Ruolo ristretto a un sottoinsieme di postazioni mappa assegnate |
| `advancedlifeguard` | `advancedlifeguard` (bagnino avanzato) | Come `lifeguard` più permesso di check-in/check-out prenotazione |
| `marketing` | `marketing` | Ruolo più recente (2026), abilitato alla visualizzazione contatti/invio comunicazioni |
| `users.affiliate_selected_locations` (JSON) | ambito risorse (`identity_access_user_resource_scopes`) | Elenco delle postazioni mappa su cui un account `affiliate` può operare, ora enforced lato server |
| `bathouses.isTwoFactorEnabled` | impostazioni sicurezza stabilimento (`identity_access_property_security_settings`) | Flag 2FA attivo per lo staff di uno stabilimento |
| — (nuovo, il legacy usava JWT senza scadenza reale/revoca) | refresh token (`identity_access_refresh_tokens`) | Token di sessione revocabile e ruotabile, sostituisce il JWT a 365 giorni del legacy |
| `two_factor_otp`/`loginOtp`/reset password via `Cryptr` | sfida di autenticazione (`identity_access_user_auth_challenges`) | Codice/token a tempo per OTP 2FA, verifica email, reset password o magic link, con scopi tipizzati (`purpose`) |

### Sottoarea account
| Termine legacy | Termine nuovo | Cos'è |
|---|---|---|
| `licenses` | piano di abbonamento (`identity_account_license_plans`) | Catalogo dei tier di abbonamento venduti allo stabilimento (`basic`, `advanced`) |
| — (nuovo) | traduzione piano (`identity_account_license_plan_translations`) | Etichetta e descrizione del piano di abbonamento per lingua |
| `users.license`/`startDate`/`endDate`/`subscriptionStatus` | abbonamento (`identity_account_subscriptions`) | Lo stato corrente dell'abbonamento di uno stabilimento alla piattaforma |
| stato abbonamento (`enable`/scaduto/sospeso) | stato abbonamento: `active`, `suspended`, `expired`, `cancelled` | Vocabolario dello stato dell'abbonamento: attivo, sospeso manualmente, scaduto per data, cessato |
| — (nuovo) | tipo di durata: `fixed_term` / `legacy_day_bank` | Distingue gli abbonamenti a scadenza contrattuale fissa da quelli storici "a banca giorni" che si consumano solo mentre attivi |
| — (nuovo, richiesta committente) | storico abbonamento (`identity_account_subscription_periods`) | Registro append-only di ogni periodo/rinnovo dell'abbonamento (piano, date, importo) |
| `contract_documents` (mai attivata, 0 righe) | documento contrattuale (`identity_account_contract_documents`) | Catalogo dei documenti contrattuali B2B (addendum, es. "Service Fee") che lo stabilimento deve accettare |
| `contract_acceptation` (mai attivata, 0 righe) | accettazione contrattuale (`identity_account_contract_acceptances`) | Registro append-only di chi ha accettato/revocato un documento contrattuale, quando, da quale IP |

### Sottoarea customer
| Termine legacy | Termine nuovo | Cos'è |
|---|---|---|
| `customers` | cliente/rubrica per-stabilimento (`identity_customer_customers`) | Anagrafica interna allo stabilimento dei nominativi che compaiono in prenotazioni/transazioni, spesso senza un vero account utente |
| `customers.referenceId` | `user_id` collegato | Collegamento opzionale tra la scheda cliente e un account utente reale (valorizzato solo su una minoranza di righe) |
| `property_like_logs` | "mi piace" struttura (`identity_customer_bathhouse_likes`) | Preferiti: un utente consumer segna uno stabilimento (`bathhouse`) come preferito |

---

## Dominio privacy

| Termine legacy | Termine nuovo | Cos'è |
|---|---|---|
| — (nuovo; il legacy cancellava con soft-delete anonimizzante silenzioso, senza traccia) | richiesta dell'interessato (`privacy_data_subject_requests`) | Testata di una richiesta GDPR di cancellazione (Art. 17, "diritto all'oblio") o di accesso/portabilità (Art. 15/20), self-service o inserita dallo staff |
| — (nuovo) | tipo richiesta (`request_kind`): `erasure` / `export` | Distingue una richiesta di cancellazione da una di esportazione dei dati personali |
| — (nuovo) | soggetto (`subject_kind`): `customer` / `user` | Chi è l'interessato della richiesta: un cliente finale in rubrica o un account con login |
| — (nuovo) | stato richiesta (`request_status`): `pending`, `in_progress`, `completed`, `partially_completed`, `rejected` | Avanzamento della richiesta: in coda, in lavorazione presso i moduli coinvolti, conclusa, conclusa parzialmente (dato trattenuto per obbligo legale/legal hold), oppure respinta |
| — (nuovo) | richiedente (`requester_kind`): `data_subject`, `staff`, `systemadmin` | Chi ha originato la richiesta: l'interessato stesso, lo staff dello stabilimento a suo nome, o il system admin |
| — (nuovo) | esito per modulo (`privacy_data_subject_request_module_outcomes`) | Traccia, per ciascun modulo applicativo coinvolto (booking, payment, identity, ecc.), se e come ha eseguito la richiesta (cancellato, anonimizzato, trattenuto, esportato) |
| — (nuovo; il legacy non aveva mai retention dichiarativa) | politica di retention (`privacy_retention_policies`) | Regola dichiarativa: per una data tabella/campo, dopo quanto tempo e con quale azione (cancellazione/anonimizzazione) un dato va trattato, con la relativa base giuridica |
| — (nuovo) | blocco legale / legal hold (`privacy_legal_holds`) | Flag impostato dall'ufficio legale che sospende una cancellazione automatica per un contenzioso in corso |
| — (nuovo) | registro di conformità (`privacy_compliance_audit_logs`) | Registro append-only di ogni cancellazione/anonimizzazione/export effettivamente eseguita dai moduli, come prova di conformità (Art. 5.2 GDPR) |

---

## Dominio catalog (C2) — catalogo & configurazione

### Radice (property, vetrina)
| Termine legacy | Termine nuovo | Cos'è |
|---|---|---|
| `properties` | `catalog_properties` / **property** (tenant root) | Anagrafica gestionale dello stabilimento: è la "radice" a cui si aggancia tutto il resto del sistema (mappa, listini, utenti, prenotazioni) — il tenant |
| `bathouses` | `catalog_bathhouses` / **bathhouse** (vetrina) | La pagina pubblica/vetrina dello stabilimento visibile ai clienti finali (foto, descrizione, dotazioni, prezzo minimo): concetto distinto dalla property — una property gestionale può non avere ancora una vetrina pubblicata |
| `properties.status` (0/1/2/4) | enum `property_status` | Stato di approvazione dello stabilimento: `pending` (in attesa), `approved`, `rejected`, `imported` (import massivo da CSV) |
| `bathouses.status` (vestigiale) | `published_at` (timestamp) | Se e quando la vetrina è stata pubblicata; sostituisce un flag di stato che nel legacy non veniva più usato realmente |
| `bathouses.photos` (JSON array) | `catalog_bathhouse_photos` | Le foto della galleria della vetrina, in ordine |
| `bathouses.services` (copia JSON) | `catalog_bathhouse_amenities` | Le dotazioni (Wi-Fi, bar, ecc.) che la vetrina dichiara di offrire |
| `online_reservations` + `properties.onlineReservation`/`onlineBookingOptions` | `catalog_property_online_booking_settings` | Configurazione del canale "prenotazione online" per le **postazioni** (chi può prenotare cosa, con quale anticipo, quali pagamenti) |
| `properties.serviceOnlineBooking`/`serviceBookingOptions` | `catalog_property_service_booking_settings` | Configurazione del canale online per i **servizi** (bar/dotazioni), separata da quella delle postazioni |
| — (nuovo) | `catalog_media_assets` | Riferimento a un file caricato (foto, icona…), scomposto in origine (dove sta) + percorso, invece di un URL completo salvato a mano |

### Sottoarea map (mappa/postazioni)
| Termine legacy | Termine nuovo | Cos'è |
|---|---|---|
| `sectors_rows` | `catalog_map_sectors` / **settore** | Un settore/fila della spiaggia: raggruppa più postazioni (es. "Fila A") |
| `configure_maps` | `catalog_map_layouts` / **layout mappa** | La griglia della mappa configurata dal gestore (dimensioni, colonne) su cui sono posizionate le postazioni |
| blob `configurations.beachMapData` | `catalog_map_spots` / **postazione (spot)** | La singola unità prenotabile (ombrellone, lettino, cabina…): è l'entità che il modulo prenotazioni (C1) referenzia come "risorsa" da prenotare |
| `items.name` (stringa libera) | enum `spot_type` | Tipo di postazione: `umbrella`, `sunbed`, `sunbed_xl`, `cabin`, `special_cabin`, ecc. — prima erano stringhe scritte a mano (con refusi) |
| `configure_categories` | `catalog_map_icon_categories` | Categoria delle icone usate in mappa |
| `configure_images` | `catalog_map_icons` | Icona master della mappa, gestita centralmente (SuperAdmin), non dal singolo stabilimento |
| `onlineBookingOptions.locationOptionObj.legendList` | `catalog_map_legend_items` | Voce di legenda mostrata nella mappa online ai clienti |
| — (concetto) | settore vs postazione | Il **settore** è un raggruppamento/fila; la **postazione** è la singola unità occupabile e prenotabile al suo interno — sono due livelli distinti della mappa |

### Sottoarea pricing (listini, sconti, revenue)
| Termine legacy | Termine nuovo | Cos'è |
|---|---|---|
| `seasons_periods` (scalari) | `catalog_pricing_seasons` / **stagione** | I confini della stagione corrente e di quella successiva di uno stabilimento (una per property) |
| `seasons_periods.periods` | `catalog_pricing_tariff_periods` / **periodo tariffario** | Un intervallo di date dentro la stagione con una propria fascia di prezzo (es. "bassa stagione", "agosto") |
| `price_lists` | `catalog_pricing_price_lists` / **listino** | Una voce vendibile: un **pacchetto** (es. "1 ombrellone + 2 lettini") o un **articolo aggiuntivo** (es. noleggio SUP), distinti dal campo `kind` |
| `price_lists.type` | enum `kind` | `package` (pacchetto composto) vs `additional` (articolo singolo aggiuntivo) |
| `enableOnlineReservation`/`enableOnlineOnly` | enum `sales_channel` | Dove è vendibile il listino: solo gestionale (`manager_only`), gestionale+online (`manager_and_online`), solo online (`online_only`) |
| `price_lists.mainPrices` | `catalog_pricing_price_list_sector_prices` | Prezzo base del listino per ciascun settore in cui è disponibile |
| `price_lists.detailPrices` | `catalog_pricing_price_list_period_prices` | Tariffe base del listino per periodo tariffario |
| `edit_price_data` (griglia 1-90 giorni) | `catalog_pricing_price_list_stay_prices` | Prezzo in base al **numero di giorni** di soggiorno: calcolato automaticamente, con possibilità di override manuale del gestore |
| `price_lists.holidayDates` | `catalog_pricing_price_list_date_prices` | Prezzo speciale per una data di calendario precisa o un festivo |
| `details.setReducedPrice` | `catalog_pricing_price_list_reduced_windows` | Intervallo di date con prezzo ridotto, applicato solo se la prenotazione combacia esattamente con quelle date |
| `price_lists.suggestedPriceDetails` | `catalog_pricing_suggested_prices` / **prezzo suggerito** | Output del **Revenue Management**: il prezzo che il sistema suggerisce (e opzionalmente applica) per una certa data, in base a domanda/occupazione |
| `discounts` | `catalog_pricing_discounts` / **voucher / sconto** | Un codice sconto (percentuale o importo fisso) spendibile dal cliente |
| `voucherType` | enum `scope` | A cosa si applica il voucher: `all` (tutto), `custom` (solo alcune categorie di servizio), `without_custom` |
| `checkManagementFlag`/`checkOnlineFlag` | enum `applicable_channel` | Su quale canale è utilizzabile il voucher: gestionale, online, o entrambi |
| `revenue_notifications` | `catalog_pricing_revenue_settings` | Configurazione del **Revenue Management** per lo stabilimento (non è notifiche C6, nonostante il nome legacy) |

### Sottoarea service (dotazioni/servizi)
| Termine legacy | Termine nuovo | Cos'è |
|---|---|---|
| `services` (master, 48 righe) | `catalog_service_amenities` / **dotazione (amenity)** | Una caratteristica dichiarabile dello stabilimento usata nella ricerca pubblica (Wi-Fi, bar, doccia…) — non è un servizio venduto, è un'etichetta della vetrina |
| `service_type_filters` | `catalog_service_amenity_categories` | Categoria che raggruppa le dotazioni nella ricerca pubblica |
| `services_types` (blob `barServices`/`normalServices`/`hourlyServices`) | `catalog_service_sale_items` / **servizio venduto** | Un servizio effettivamente acquistabile dal cliente presso lo stabilimento: bar, normale, oppure a ore (`kind`) — diverso dalla "dotazione" della vetrina |
| `hourlyServices[].timeslot_constants` | `catalog_service_sale_item_time_slots` | Fascia oraria interna a un servizio "a ore", con un proprio prezzo dedicato |
| `servicetypes_icons` | `catalog_service_type_icons` | Icona associata a uno dei 3 tipi di servizio venduto (bar/normale/orario) |

### Sottoarea inventory (magazzino)
| Termine legacy | Termine nuovo | Cos'è |
|---|---|---|
| `warehouses` | `catalog_inventory_items` / **articolo di magazzino** | Un bene fisico gestito a magazzino (es. teli, sedie) con quantità disponibile e soglia minima; il consumo giorno per giorno è tracciato nel modulo prenotazioni (C1), qui c'è solo l'anagrafica |

### Sottoarea agreement (convenzioni/hotel)
| Termine legacy | Termine nuovo | Cos'è |
|---|---|---|
| `hotels` | `catalog_agreement_partners` / **partner convenzionato** | Una struttura esterna (hotel, B&B, camping) che ha una "convenzione" con lo stabilimento e gli invia clienti; non è un modulo di prenotazione camere, ma un'anagrafica di partner commerciali |
| `hotel_price_lists` | `catalog_agreement_price_lists` / **listino convenzione** | Un listino o un voucher dedicato che il partner vende ai propri ospiti da spendere presso lo stabilimento (`list_type`: `regular` o `voucher`) |
| — (concetto) | "prenotazione hotel" | Non è una prenotazione di camere: è una normale prenotazione di postazioni/servizi marcata come proveniente da un partner convenzionato, con eventuale listino dedicato |

### Lookup e read-model
| Termine legacy | Termine nuovo | Cos'è |
|---|---|---|
| `country` | `catalog_countries` | Elenco dei paesi selezionabili (es. nell'anagrafica property), con traduzioni per lingua |
| — (nuovo) | `catalog_config_snapshots` / **snapshot di configurazione** | Una "fotografia" precalcolata (in JSON) di tutta la configurazione di uno stabilimento (mappa, listini, servizi, convenzioni…), pronta per essere inviata al client senza dover ricostruire tutto a ogni richiesta; si rigenera automaticamente quando qualcosa cambia |
| — (nuovo, sostituisce ricerca `serchProperty`) | `catalog_search_bathhouses` | Indice ottimizzato per la ricerca pubblica degli stabilimenti: una riga per ogni vetrina pubblicata, con prezzo minimo, dotazioni e posizione già pronti per filtrare/ordinare i risultati |

---

## Dominio booking (C1) — prenotazioni

### Nucleo (prenotazione, righe, consumi)
| Termine legacy | Termine nuovo | Cos'è |
|---|---|---|
| `bookings` (testata) + `service_reservation` (testata) | `booking_reservations` (Reservation) | La prenotazione: 1 riga per ogni prenotazione logica (un solo numero prenotazione), con postazioni e/o servizi |
| — (nuovo; era implicito nella scelta di tabella `bookings` vs `service_reservation`) | `reservation_kind` (`spot` / `service`) | Distingue se la prenotazione include postazioni (`spot`) o è di soli servizi senza postazione (`service`) |
| `bookings` (singola riga, 1 postazione × intervallo × fascia) | `booking_lines` (BookingLine) | Riga di dettaglio della prenotazione: una postazione per un intervallo di date contiguo e una fascia oraria |
| `status` (tinyint 0/1/2/3, sovraccarico) | `reservation_status` (`pending`/`confirmed`/`cancelled`/`no_show`) | Stato del ciclo di vita della prenotazione, separato dall'eliminazione e dal pagamento |
| `is_booking_confirmed` (semantica invertita, mai avanzava) | `online_confirmation_state` (`not_required`/`pending`/`confirmed`) | Stato della conferma esplicita che il cliente online deve dare (via codice) prima che la prenotazione sia valida |
| `bookingByAdmin` (`"0"`/`"1"` stringa) | `booking_channel` (`customer`/`manager`) | Chi ha creato la prenotazione: il cliente da sé (online) o un operatore da gestionale |
| `bookingType` (0/1/2) | `listing_type` (`standard`/`agreement_price_list`/`agreement_voucher`) | Tipo di listino applicato: listino normale dello stabilimento, listino di convenzione, oppure voucher di convenzione |
| `hotelId` | `agreement_partner_id` | Collega la prenotazione a una struttura partner convenzionata ("hotel"), voce anagrafica in C2 `catalog` |
| `isBookingDoneBySuggestedPrice` + `isRecalculate` + `isEditedFinalPrice` | `price_flags` (enum[]) | Tracciano come si è formato il prezzo finale: prezzo suggerito accettato, ricalcolato dopo modifiche, o forzato a mano — non esclusivi tra loro |
| `bookingNumber` (concatenazione fragile, non unico) | `booking_number` | Numero di prenotazione mostrato all'operatore, generato in modo affidabile e univoco lato server |
| `reservation_code` (lato client, non unico) | `reservation_code` | Codice usato nel link/QR pubblico con cui il cliente apre e conferma la propria prenotazione |
| `booking_agent` | `operator_user_id` | Operatore che ha creato la prenotazione da gestionale (`NULL` per le prenotazioni online) |
| `checkIn`/`checkOut` (su un'unica riga "rappresentativa") | `checked_in_at`/`checked_out_at` (per riga) | Check-in e check-out del cliente, tracciati per singola postazione anziché per l'intera prenotazione |
| `additionalList[]`/`certainAddItemData[]` | `booking_line_additional_items` | Articolo aggiuntivo (extra) venduto su una riga di postazione o sull'intera prenotazione |
| `booking_discount` / `additionalAutoVoucher[]` | `booking_discounts` | Sconto o voucher applicato alla prenotazione: fotografia (snapshot) dell'importo scontato al momento dell'uso |
| `guest_data[]` / `guest_ids[]` | `booking_guests` | Ospite che soggiorna nella prenotazione ma non ne è l'intestatario |
| `bookings_requested_details` | `booking_participants` | Account cliente che ha "agganciato" la prenotazione tramite il codice, per vederla nella propria area personale |
| `booking_logs` | `booking_number_sequences` | Contatore che genera il numero progressivo di prenotazione, uno per stabilimento |
| `booking_number_informations.quick_actions_data` | `booking_quick_action_dates` | Data segnata come "azione rapida" nel planner per quella prenotazione |
| `booking_items_counts_details` + `additional_items_counts_details` + `services_booking_counts` + `location_reservations` (conteggio) | `booking_resource_consumptions` | Occupazione di una risorsa (postazione, articolo, servizio) in un giorno specifico: è la base da cui si legge la disponibilità |
| — (nuovo; nel legacy calcolato a runtime con `GROUP BY`) | `booking_resource_availability_daily` | Disponibilità aggregata per risorsa e giorno (quanti posti/pezzi restano liberi), mantenuta pronta invece di ricalcolata ogni volta |
| `hotel_payments` (parte prenotazione) | `booking_agreement_lines` | Riga di listino o voucher di convenzione applicata alla prenotazione, con il prezzo congelato al momento dell'uso |

### Sottoarea service (servizi venduti)
| Termine legacy | Termine nuovo | Cos'è |
|---|---|---|
| `bookings.servicesData` / `service_reservation.servicesData` + `services_booking_counts` | `booking_service_lines` | Un servizio venduto (bar, normale, o a fascia oraria) collegato a una prenotazione |
| `service_type` (es. `"Bar Service"`, valori sporchi) | `kind` (`bar`/`normal`/`hourly`) | Tipo di servizio venduto |
| `hourly_service_booking_management` (parte "slot × giorno") | `booking_service_time_slots` | Fascia oraria (time slot) prenotata per un servizio a orario, es. un campo o un'attrezzatura alle 10:00 |
| `count` / `click` | `quantity` | Quantità del servizio venduto in quella riga |
| `summaryServicesData` (consumo pagato col borsellino) | `is_paid_with_wallet` | Indica che quel servizio è stato pagato col borsellino/credito cliente (il movimento vero e proprio vive in C3 `payment`) |

### Sottoarea hold (blocco/rivendita/sharing)
| Termine legacy | Termine nuovo | Cos'è |
|---|---|---|
| `location_reservations` + `hold_locations` | `booking_spot_holds` | "Hold": blocco morbido e temporaneo di una postazione senza una vera prenotazione dietro (es. mentre un operatore sta finalizzando la vendita, o durante un checkout) |
| calcolato lato applicazione, senza scadenza reale | `held_until` | Scadenza automatica del blocco, superata la quale la postazione torna libera da sola |
| `booking_absence_details` + parte "offerta" di `sharing_details` (+ JSON `bookings.booking_absence_dates`) | `booking_resale_offers` | Offerta di rivendita: un giorno/postazione che il titolare "restituisce" (per assenza pianificata o vera cessione) e che torna disponibile per un'altra prenotazione |
| `status` (2=disponibile) + `isBooked` | `offer_state` (`available`/`reassigned`/…) | Stato dell'offerta di rivendita: ancora disponibile, oppure già ripresa da un'altra prenotazione |
| — (distinzione introdotta ora tra due casi legacy sovrapposti) | `origin` (`absence`/`sharing`/`absence_as_sharing`) | Da cosa nasce l'offerta: un'assenza pianificata del cliente, oppure una cessione/condivisione vera e propria |
| `sharing_details.{number_of_credit, credit_value}` + cron `sharingCreditCron` (scriveva su `users`) | `booking_resale_credit_ledger` | Registro (append-only) dei crediti maturati dal titolare quando cede una giornata in rivendita, un movimento per volta invece di un saldo sovrascritto |
| campi "sharing" di `configurations` (`sharing_enable`, `credit_perday_resold`, `credit_value`, ecc.) | `booking_resale_settings` | Impostazioni per stabilimento della funzionalità di rivendita: se è attiva, come si calcola il credito, a chi si applica |
| "sharing" / "condivisione" (termine UI e nome tabelle legacy) | "rivendita" / `resale` (termine nuovo) | Stesso concetto di business, rinominato: nel legacy si chiamava "sharing" (condivisione tra clienti), nel nuovo modello è "resale" (rivendita di un giorno/postazione già pagato che torna sul mercato) |
| `booking_absences` (dismessa, nessun dato dopo il 2024) | — (non migrata) | Versione minimale e precedente del concetto di "assenza pianificata", sostituita da `booking_resale_offers` |

### Sottoarea group (raggruppamento)
| Termine legacy | Termine nuovo | Cos'è |
|---|---|---|
| `booking_groups` | `booking_groups` | Raggruppamento manuale di più prenotazioni distinte sotto un'unica etichetta (es. per gestirle e tariffarle insieme) |
| array `booking_groups.groupBookings[]` | `booking_group_members` | Appartenenza di una singola prenotazione a un gruppo (una prenotazione può stare in un solo gruppo) |
| — (logica applicativa, non una tabella) | prezzo cumulativo di gruppo | Quando più prenotazioni sono collegate in un gruppo (o multi-postazione), il prezzo non è la somma delle singole righe: il motore di prezzo lo ridistribuisce sul totale dei giorni-postazione |
| `parent_bookings` (dead code, 0 righe, mai realmente usato) | — (non migrata) | Tentativo incompleto di "pagamento cumulativo su più prenotazioni"; da riprogettare da zero se il committente lo richiede (competenza C3) |

---

## Dominio payment (C3) — pagamenti & incassi

| Termine legacy | Termine nuovo | Cos'è |
|---|---|---|
| `stripe_configrations` | Connected account (`payment_gateway_connected_accounts`) | Il conto Stripe collegato allo stabilimento (Stripe Connect), che riceve gli incassi online |
| `properties.commissionAmount` (JSON) / liste hardcoded | Commissioni gateway (`payment_gateway_commission_settings`) | Percentuale trattenuta sugli incassi online per stabilimento (tasso online, tasso iframe, eventuali esenzioni) |
| `user_stripe_card_details` / `users.stripeCustomerId` | Profilo cliente gateway (`payment_customer_gateway_profiles`) | Collegamento fra il cliente e il suo "Stripe Customer" (carta salvata), per stabilimento |
| `online_transaction_details` | Transazione online (`payment_transaction_charges`) | Un pagamento online con carta (PaymentIntent Stripe), sia "paga ora" che "paga in struttura" |
| `refund_transaction_details` | Rimborso (`payment_transaction_refunds`) | Un rimborso (anche parziale) di una transazione online, con motivo ed esito |
| fetta di `bookings_send_payment_summaries` | Link di pagamento (`payment_request_links`) | Il "riepilogo pagamento" Stripe inviato al cliente per pagare online (con scadenza e stato) |
| `transactions_payments` + JSON `bookings.booking_payments` / `service_reservation.payments` + fetta di `hotel_payments` | Movimento di incasso (`payment_ledger_entries`) | Una riga del libro incassi: acconto, pagamento, spesa, rimborso o rettifica su una prenotazione |
| campi per-articolo di `transactions_payments` | Riga movimento (`payment_ledger_entry_lines`) | Quota di un movimento di incasso attribuita a un singolo articolo/servizio, per l'export contabile |
| implicito in `card_transactions` + saldi su `users` | Conto borsellino (`payment_wallet_accounts`) | Il "conto tessera"/borsellino prepagato di un cliente presso uno stabilimento (o i suoi crediti da cessione posto) |
| `card_transactions` | Movimento borsellino (`payment_wallet_transactions`) | Una ricarica o un consumo del borsellino prepagato del cliente |
| JSON `card_transactions.debit_details` | Articolo movimento borsellino (`payment_wallet_transaction_items`) | Il singolo articolo/servizio consumato pagando col borsellino |
| — (nuovo, concetto implicito in `users`/`sharing_details`) | Crediti "sharing" (tipo `resale_credit` di `payment_wallet_accounts`) | Credito accumulato dal cliente cedendo il proprio posto ad un altro, spendibile come il borsellino |
| `teamsystem_expenses` + fetta di `booking_number_informations.ofsale_payments` | Addebito POS (`payment_pos_charges`) | Uno scontrino/addebito registrato da un POS esterno (OfSale, TeamSystem/Cassa in Cloud) e associato a una prenotazione |
| array `teamsystem_expenses.payload.data[]` / `saleItems[]` OfSale | Riga addebito POS (`payment_pos_charge_items`) | Il singolo prodotto/servizio dentro uno scontrino POS |
| `keysecret` (credenziali OfSale/TeamSystem/Transactions) | — (spostato in INFRA, `infra_integration_api_clients`) | Le credenziali API dei partner di pagamento, ora fuori dal dominio payment (secret manager) |

---

## Dominio fiskaly (C4) — scontrino fiscale elettronico

> Da non confondere con `fiscalprint` (C4bis): **fiskaly** è lo scontrino **elettronico** firmato via API
> Fiskaly SIGN IT; **fiscalprint** è la stampa su un registratore telematico **fisico**.

| Termine legacy | Termine nuovo | Cos'è |
|---|---|---|
| `fiskaly_account` + flag `properties.fiskaly*` | Account fiscale (`fiskaly_accounts`) | L'account dello stabilimento presso Fiskaly SIGN IT (l'intermediario che firma gli scontrini elettronici verso l'Agenzia delle Entrate), con lo stato di attivazione |
| `fiskaly_cash_register` | Cassa fiskaly / registratore virtuale (`fiskaly_cash_registers`) | Il "registratore di cassa" (dispositivo fiscale) associato all'account Fiskaly, che numera e firma gli scontrini |
| `fiskaly_receipt` + `fiskaly_non_fiscal_receipt` | Documento/scontrino elettronico (`fiskaly_receipts`) | Il documento commerciale emesso per una prenotazione: **scontrino fiscale elettronico** firmato da Fiskaly, oppure **pre-conto** non fiscale (stessa tabella, tipo diverso) |
| array `entries` dentro `fiskaly_receipt`/`fiskaly_non_fiscal_receipt` | Riga scontrino (`fiskaly_receipt_lines`) | Una voce del documento fiscale (servizio, articolo, sovrapprezzo...) con la sua aliquota IVA, congelata al momento dell'emissione |
| array `payments` dentro `fiskaly_receipt`/`fiskaly_non_fiscal_receipt` | Pagamento scontrino (`fiskaly_receipt_payments`) | La forma di pagamento (contanti, carta, bonifico, borsellino...) con cui è stato incassato l'importo del documento fiscale |
| `fiskaly_receipt_log` | Log fiskaly (`fiskaly_receipt_logs`) | Registro (append-only) di ogni tentativo/errore di comunicazione con l'API Fiskaly SIGN IT per uno scontrino |
| `fiskaly_receipt_sequence` | Sequenza numerazione (`fiskaly_receipt_sequences`) | Il contatore progressivo giornaliero del numero di scontrino, per ogni cassa fiskaly |
| costanti hardcoded `FiskalyVatRate`/`FiskalyVatExemption` | Codici IVA fiskaly (`fiskaly_vat_codes`) | Elenco delle aliquote/esenzioni IVA ammesse da Fiskaly SIGN IT (es. aliquota ordinaria, ridotta, natura N1-N6) |
| `fiskaly_receipt.e_receipt_path` | Allegato fiskaly (`fiskaly_media_assets`) | Il riferimento al PDF dello scontrino elettronico generato |
| `booking_number_informations.details_payments*` | (input delle) righe scontrino | I dati del pre-conto usati per calcolare le righe dello scontrino fiscale elettronico |

---

## Dominio fiscalprint (C4bis) — stampa fiscale hardware

> Meccanismo legacy alternativo allo scontrino elettronico Fiskaly (C4), usato da 3-4 stabilimenti.
> **Si mantiene come componente separato** — decisione di business del committente, indipendente
> dall'uso attuale: non è candidata a dismissione.

| Termine legacy | Termine nuovo | Cos'è |
|---|---|---|
| `printer_configs` | Configurazione stampante (`fiscalprint_printer_configs`) | La stampante fiscale/termica **fisica** (registratore telematico RT) configurata su uno stabilimento — meccanismo alternativo, più vecchio, allo scontrino elettronico Fiskaly |
| colonne `department_1/2/3` di `printer_configs` | Reparto stampante (`fiscalprint_printer_departments`) | Il "reparto" fisico del registratore di cassa RT collegato a un'aliquota IVA, usato per stampare correttamente lo scontrino |
| `booking_receipts` | Scontrino stampato (`fiscalprint_receipts`) | Il log di uno scontrino/ricevuta effettivamente stampato sul registratore RT fisico |
| `receipt_serial_numbers` | Numeratore RT (`fiscalprint_serial_numbers`) | Il contatore progressivo giornaliero dello scontrino stampato, per ogni stampante fiscale (matricola) e giorno |

---

## Dominio notification (C6) — notifiche & marketing

| Termine legacy | Termine nuovo | Cos'è |
|---|---|---|
| `email_logs` + flag sparsi (`*Sent` su prenotazioni, ricevute, wallet, report) | `notification_deliveries` (outbox) | Registro unico di ogni messaggio inviato a un destinatario su un canale (email/WhatsApp/push), con stato del tentativo di invio: in coda, in invio, inviato, consegnato, fallito, rimbalzato, saltato. Prima ogni funzionalità segnava "inviato/non inviato" a modo suo; ora è un unico registro con ritentativi automatici |
| `custom_templates` + `hotel_message_templates` + testo libero dentro automazioni/campagne | `notification_templates` | Modello di messaggio (oggetto + corpo con segnaposto) riutilizzabile per scopo, canale e lingua — es. "conferma prenotazione" in italiano via email. I segnaposto passano da 4 sintassi diverse a una sola (`{{ variabile }}`), con rendering lato server anziché nel browser |
| array URL negli allegati di template/campagne | `notification_template_attachments` / `notification_media_assets` | File allegato a un template (fattura, PDF, immagine) e relativo catalogo dei file caricati, con controllo di integrità (checksum) invece di un semplice URL in colonna |
| `bathhouse_email_configrations` | `notification_email_channel_settings` | Configurazione SMTP dello stabilimento per l'invio email in uscita (1 per stabilimento); la password non è più salvata in chiaro ma tramite riferimento al secret manager |
| `notification_details` | `notification_inapp_messages` | Feed delle notifiche "a campanella" nel gestionale: eventi operativi dello stabilimento (nuova prenotazione, pagamento, ecc.) mostrati come testo renderizzato da un template, non più testo già scritto e salvato in tabella |
| `notification_details.read` (per stabilimento) | `notification_inapp_message_reads` | Stato di lettura di una notifica, ora tracciato per singolo utente invece che genericamente per tutto lo stabilimento |
| `users.notifyEmail`/`notifyWhatsapp`/`disableNotification` | `notification_user_channel_preferences` | Preferenze personali di un utente dello staff su quali canali di notifica ricevere (email, WhatsApp, ecc.) e se essere silenziato |
| array `users.deviceTokens` | `notification_push_device_tokens` | Elenco dei dispositivi (cellulari/app) dello staff abilitati a ricevere notifiche push, uno per riga con stato attivo/scaduto |
| `contact_list` | `notification_marketing_contact_lists` | Lista/segmento di contatti marketing di uno stabilimento, usato per inviare campagne o attivare automazioni |
| `contact_list_member` | `notification_marketing_contact_list_members` | Appartenenza di un contatto a una lista marketing (relazione contatto ↔ lista) |
| — (nuovo, non esisteva una vera rubrica) | `notification_marketing_contacts` | Anagrafica dei contatti marketing di uno stabilimento (email/telefono con consenso), distinta dalla rubrica clienti: contiene solo chi è entrato in un pubblico marketing, con tracciamento esplicito del consenso privacy (chi, quando, come) |
| `subscribes` | `notification_marketing_suppressions` / `notification_marketing_newsletter_subscriptions` | La prima è la "lista nera" di chi ha revocato il consenso e non va più contattato su nessun canale (conservata a vita come prova); la seconda è l'iscrizione alla newsletter generale del sito (non di uno stabilimento), con doppia conferma via email |
| `marketing_email_campaign` | `notification_marketing_email_campaigns` + `notification_marketing_campaign_recipients` | Campagna email "una tantum" verso un segmento di contatti, con l'elenco dei destinatari effettivi e l'esito dell'invio per ciascuno (prima non tracciato) |
| `marketing_automation` | `notification_marketing_automations` + `notification_marketing_automation_triggers` | Automazione che invia email/push in automatico legata alle prenotazioni (es. messaggio di benvenuto o di arrivederci), con le regole di quando scattare (es. N giorni prima del check-in) |
| `marketing_event` + `marketing_event_notification` | `notification_marketing_events` + `notification_marketing_event_reminders` | Evento/iniziativa promozionale dello stabilimento (es. serata a tema) mostrabile in vetrina, con eventuale promemoria pianificato ai contatti interessati |
| `csv_manager_details` | `notification_establishment_claim_requests` | Richiesta ("lead") inviata dal sito pubblico da chi vuole rivendicare/richiedere la gestione della pagina del proprio stabilimento |

---

## Dominio switchboard (C7) — centralino telefonico & WhatsApp

| Termine legacy | Termine nuovo | Cos'è |
|---|---|---|
| `switchboard_config` | `switchboard_configs` | Configurazione del centralino virtuale (Telnyx) di uno stabilimento: numero di telefono, modalità di instradamento (menù vocale o inoltro diretto), credenziali SIP (ora come riferimento a secret manager) |
| fetta `reg_*` di `switchboard_config` | `switchboard_voice_number_registrations` | Fascicolo della pratica di attivazione regolamentare del numero telefonico italiano (dati KYC del richiedente: documento d'identità, dati anagrafici), separato per motivi di riservatezza dei dati sensibili |
| `switchboard_menu_item` | `switchboard_voice_menu_items` | Voce del menù vocale (IVR) del centralino: es. "premi 1 per la reception", organizzata ad albero |
| `switchboard_menu_action` | `switchboard_voice_menu_actions` | Azione eseguita da una voce di menù selezionata (agganciare, inoltrare la chiamata via SIP, inviare un messaggio WhatsApp) |
| colonne `device_fcm_token`/`device_voip_token` di `switchboard_config` | `switchboard_voice_softphone_devices` | Dispositivo softphone (telefono via app) dello staff che riceve l'avviso push prima dell'inoltro di una chiamata |
| `switchboard_call_register` | `switchboard_voice_call_records` | Registro delle chiamate gestite dal centralino: chi ha chiamato, quando, quale percorso nel menù ha seguito, come è finita (inoltrata, persa, ecc.) |
| `switchboard_call_log` | `switchboard_voice_call_diagnostics` | Log tecnico di dettaglio di ogni passo del flusso di una chiamata (per diagnosi problemi), con conservazione breve |
| `switchboard_audio_file` | `switchboard_media_assets` | File audio dei messaggi del centralino (es. messaggio di benvenuto, voce di un menù) |
| `switchboard_wa_template` | `switchboard_messaging_wa_templates` | Template di messaggio WhatsApp registrato e approvato su Meta per lo stabilimento (diverso dai template email/notifica di C6) |
| `switchboard_wa_msg_log` | `switchboard_messaging_wa_event_logs` | Registro degli eventi ricevuti dal webhook WhatsApp (conferme di consegna, messaggi in arrivo) sul numero del centralino, a scopo di verifica/forense |
| — (nuovo, concetto) | Centralino / IVR (routing_mode `ivr`/`direct_sip`) | Modalità con cui arriva una chiamata: passa da un menù vocale ad albero oppure viene inoltrata direttamente a un numero SIP |

---

## Dominio chatbot (C8) — assistente AI

| Termine legacy | Termine nuovo | Cos'è |
|---|---|---|
| `chatbot_history` | `chatbot_conversation_turns` | Storico delle conversazioni: ogni scambio domanda/risposta tra un utente dello staff (in futuro anche un cliente) e l'assistente virtuale del gestionale |
| `chatbot_manager_config` | `chatbot_manager_configs` | Limiti di utilizzo dell'assistente per uno stabilimento (quante domande al giorno, quanta cronologia recuperare) ed eventuale provider AI preferito |
| mappa cablata `LLM_IMPLEMENTATIONS` (`llmFactory.js`) | `chatbot_llm_providers` | Catalogo dei modelli/fornitori di intelligenza artificiale (es. GPT, Gemini) selezionabili dal modulo, gestibile senza dover modificare il codice, incluse le date di dismissione dei modelli superati |
| `historyId` (id sequenziale esposto) | `public_id` del turno di conversazione | Identificativo pubblico non indovinabile di uno scambio di conversazione, al posto dell'id progressivo del database |
| `CHATBOT_MANAGER_APIKEY`/`_SECRET` | Secret manager (INFRA) | Chiave d'accesso al fornitore AI, mai salvata a database: vive come segreto infrastrutturale |

---

## Dominio infra — client API & segreti

| Termine legacy | Termine nuovo | Cos'è |
|---|---|---|
| `keysecret` | `infra_integration_api_clients` | Anagrafica delle applicazioni partner esterne autorizzate a chiamare le API della piattaforma (es. gestionali collegati), con l'ambito di accesso (quale stabilimento, quale app); la chiave segreta non è più salvata in chiaro nella tabella ma tramite riferimento al secret manager |
| `apiKey` (colonna `keysecret`) | `client_id` | Identificativo del client API partner, ora univoco e indicizzato |
| `secret` in chiaro (colonna `keysecret`) | `client_secret_ref` | Riferimento al secret manager dove risiede davvero il segreto del client API, invece del valore in chiaro a database |
| password SMTP/SIP/API key in colonna (es. `smtpPassword`, `sip_password`, credenziali Fiskaly/Fisconline, API key LLM) | `*_secret_ref` + Secret Manager | Principio trasversale: ogni credenziale/segreto esce dal database applicativo e viene spostato in un gestore di segreti dedicato (AWS Secrets Manager / SSM Parameter Store); sulla tabella di dominio resta solo un riferimento, mai il valore vero |
