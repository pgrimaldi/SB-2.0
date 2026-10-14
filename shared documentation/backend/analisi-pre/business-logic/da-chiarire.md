# Business logic — elementi da chiarire col committente

> Raccolta di **tutti** i punti aperti emersi nella stesura della logica di business
> (`analisi-pre/business-logic/`), raggruppati per tipologia. Ogni voce rimanda alla scheda in cui è
> descritta nel dettaglio. Sono decisioni di prodotto/business, non semplici bug: vanno sciolte prima
> di consolidare i requisiti della riscrittura .NET.

---

## A. Prezzi, importi e commissioni

1. **Motore di prezzo interamente lato client, nessun ricalcolo server** — prezzo pacchetto,
   scaglioni, weekend/festivi, cumulativo multi‑postazione, sconti, commissione: tutto calcolato nel
   browser e salvato as‑is dal backend. Confermare che la riscrittura deve avere un `IPricingEngine`
   server‑side come unica fonte di verità.
   *(→ [stabilimento/_motore-di-prezzo-e-sconti.md](stabilimento/_motore-di-prezzo-e-sconti.md),
   [cliente-finale/04](cliente-finale/04-prenotazione-postazioni.md),
   [cliente-finale/07](cliente-finale/07-pagamento.md))*
2. **`priceCalculation` "sporco"** — valori `proportional` / `daily` / `Daily price with weekend` /
   `logarithmic` / `proportional price` / `""`: quali sono metodi realmente distinti e quali refusi da
   consolidare? `logarithmic` è ancora raggiungibile da UI?
   *(→ [price_lists.md](../db/price_lists.md), _motore-di-prezzo-e-sconti.md §1)*
3. **`weekendSetting` / `monthlySetting`** — più varianti testuali dello stesso concetto: mappa
   canonica da definire.
4. **Commissione online** — `commissionAmount` quasi sempre `NULL` a DB, con valori "sporchi" (`"2"`,
   `"4"`) in alcune righe; **5 `propertyId` esenti** via lista hardcoded nel bundle del sito cliente +
   1 "connected & platform account". Portare a dato (piano commerciale sull'entità property). Definire
   default e range; regola `+ 0,50 €` fissa da confermare.
   *(→ [properties.md](../db/properties.md#commissionamount),
   [systemadmin/01](systemadmin/01-gestione-stabilimenti.md) §4.1)*
4b. **Semantica `superAdminOnlineFlag` / `isAdminDisableOnLinePayment`** — il valore `1` significa
   "online abilitato" (produttore) o "pagamento online disabilitato" (nome del campo restituito)?
   Possibile inversione fra `updatesuperadmin` e `getOnlineReservationStatusByAdmin`.
   *(→ [systemadmin/01](systemadmin/01-gestione-stabilimenti.md) §6–§7,
   [cliente-finale/03](cliente-finale/03-scheda-stabilimento.md) §2)*
5. **Prezzo suggerito Revenue** — moltiplicatori (pioggia 0,70/0,80; weekend imminente 1,25; alta
   stagione 1,20; disponibilità 0,70/1,10) e loro composizione: sono i valori voluti? Vanno resi
   configurabili per stabilimento?
   *(→ [stabilimento/10-revenue.md](stabilimento/10-revenue.md) §1)*
6. **Sconto/voucher** — importo calcolato lato client; backend valida solo esistenza/validità/utilizzi.
   Confermare il calcolo server‑side dal solo codice. `TransactionEntryForPayAtProperty` è l'unico
   punto che ricalcola lo sconto: estenderlo a tutti i percorsi.
   *(→ [cliente-finale/07](cliente-finale/07-pagamento.md) §5)*
7. **Tetto del pagamento parziale / link di pagamento** — oggi non verificato che `importo ≤ residuo`
   (vincolo solo in `sessionStorage`). Definire la regola server‑side.
8. **Media `totalAmount ÷ totalBookings`** nelle statistiche per periodo — divisione per zero se il
   periodo non ha prenotazioni: comportamento atteso?
   *(→ [stabilimento/07-statistiche.md](stabilimento/07-statistiche.md) §1)*

---

## B. Autorizzazioni, ruoli e scoping per stabilimento

9. **Endpoint di scrittura senza autorizzazione reale** — `POST /api/sb/booking`,
   `update/booking`, `deleteBooking`, `service/reservation`, `importcsvdata`, `createCountry`,
   `createServiceFilter`, `propertydetails`, `getPropertyWithBathHouseDetails`,
   `getOnlineReservationStatusByAdmin` sono montati con `optionalAuth` o con il ruolo commentato.
   Elenco definitivo di cosa va sotto quale ruolo/permesso.
   *(→ [systemadmin/01](systemadmin/01-gestione-stabilimenti.md) §0.1,
   [systemadmin/03](systemadmin/03-dati-di-piattaforma-e-lookup.md),
   [stabilimento/01-mappa.md](stabilimento/01-mappa.md))*
10. **Limitazioni `lifeguard` / `waiter` / `affiliate` / `advancedlifeguard`** — oggi solo lato UI
    (prezzi nascosti, postazioni oscurate, redirect). Vanno rese autorizzazioni server‑side reali,
    oppure modellate esplicitamente come "preferenze di vista" non di sicurezza?
    *(→ [roles.md](../db/roles.md), stabilimento/01, /02, /03)*
11. **`superadmin` non scoped per stabilimento** — il controllo verifica solo il nome del ruolo: un
    superadmin di uno stabilimento può operare sui dati di un altro (catalogo, listini, sconti,
    settori, mappa). Introdurre scoping property nel token.
    *(→ [stabilimento/11-configurazione.md](stabilimento/11-configurazione.md))*
12. **`activeconfimages` protetto da `isSuperAdmin`** ma chiamato dal pannello mappa del **gestore**:
    permesso di rotta sbagliato o componente esposto a un ruolo che non dovrebbe raggiungerlo?
    *(→ [systemadmin/02](systemadmin/02-catalogo-icone-mappa-spiaggia.md) §0.1)*
13. **Fallback ruolo `systemadmin`** — se un endpoint di registrazione utente non riceve `roles`,
    assegna `[1]` = `systemadmin` (ruolo di livello più alto). Da eliminare.
    *(→ [stabilimento/12-impostazioni.md](stabilimento/12-impostazioni.md) §1,
    [roles.md](../db/roles.md))*
14. **`systemadmin`** — ✅ **risolto**: il pannello è stato reperito ([`front-admin/`](../../../../../../front-admin/),
    React/Metronic, WIP) e **il nuovo back‑office verrà riscritto da zero**. `front-admin/` **non** è il
    target: vale solo come **riferimento dei requisiti** insieme a queste schede; tutto ciò che contiene
    di legacy (chiavi/URL hardcoded, payload cifrati lato client, "accesso diretto" via `auth/send-token`,
    moduli demo Metronic) è **da scartare**, non da portare. **`websiteadmin`**: ✅ **mantenuto** su
    conferma del committente (2026-10-07), anche se oggi nessun frontend lo usa.
    *(→ [systemadmin/_indice](systemadmin/), [roles.md](../db/roles.md))*
15. **`user1`** — nome e scopo non ricostruibili dal codice. ✅ **Mantenuto** con lo stesso nome su
    conferma del committente (2026-10-07).
16. **Gruppi di prenotazione** — unicità nome e "una prenotazione in un solo gruppo" sono **globali**,
    non per stabilimento. Confermare scoping per `property_id`.
    *(→ [stabilimento/02-planner.md](stabilimento/02-planner.md) §4)*

---

## C. Licenze e abbonamenti

17. **Durata licenza non verificata** — la scadenza abbonamento è calcolata da `licenseMonths`
    **inviato dal client**, senza confronto con `licenses.months` (3 = basic, 12 = advanced). Calcolare
    server‑side dalla riga `licenses`.
    *(→ [systemadmin/03](systemadmin/03-dati-di-piattaforma-e-lookup.md) §1,
    [licenses.md](../db/licenses.md))*
18. **Logica di sospensione abbonamento divergente** — `updateproperty` (systemadmin) "congela"
    correttamente i giorni residui; `updateuser` / `updatesuperadmin` (impostazioni) usano
    un'aritmetica che di fatto **raddoppia** la distanza residua. Quale comportamento è corretto?
    *(→ [systemadmin/01](systemadmin/01-gestione-stabilimenti.md) §4.2,
    [stabilimento/12-impostazioni.md](stabilimento/12-impostazioni.md) §1,
    [users.md](../db/users.md#subscriptionstatus))*
19. **Confronto stato abbonamento su campo inesistente** (`ownersubscription` in `updateproperty`):
    confermare che i rami "sospendi/riattiva" devono scattare solo su reale cambio di stato.
20. **Propagazione licenza/scadenza** — `updateproperty` la applica a **tutto lo staff** della
    property; `updatesuperadmin` solo al singolo utente. Modellare la licenza come entità dello
    stabilimento (una volta sola).
21. **`updateuser` imposta `subscriptionEnableDate` ad ogni salvataggio non‑disable** (non solo alla
    riattivazione): comportamento voluto?

---

## D. Ciclo di vita dello stabilimento

22. **`status = 4` (import CSV) e visibilità pubblica** — la maggioranza degli stabilimenti a DB ha
    `status = 4` e **non** compare nella ricerca pubblica (filtro `[1, 3]`). È staging voluto o bug
    storico (doveva essere `3`, o il filtro doveva includere `4`)? Serve una coda "importati"?
    *(→ [systemadmin/01](systemadmin/01-gestione-stabilimenti.md) §0.3, §2, §8,
    [properties.md](../db/properties.md#status))*
23. **Notifica di nuova registrazione all'amministratore** — oggi disattivata (codice commentato).
    Ripristinare come email / digest / notifica interna?
24. **Foto obbligatorie in registrazione stabilimento** — il form le tratta come opzionali ma il
    backend va in errore senza foto. Obbligatorie sì/no?
25. **Eliminazione stabilimento** — serve reversibilità esplicita? Come gestire prenotazioni future e
    pagamenti pendenti al momento dell'eliminazione? Nessuna cascata oggi.
26. **Anomalia lingua email** in `updateproperty` / `deleteproperty` — sempre italiano per un bug
    (array vs record). Confermare correzione: lingua dal record del titolare.

---

## E. Eventi, notifiche e comunicazioni

27. **Comunicazioni scadenza/disattivazione abbonamento** — assenti. Introdurre avviso preventivo
    ("scade fra X giorni") + avviso di avvenuta disattivazione (il cron notturno spegne lo stabilimento
    senza avvisare)?
    *(→ [systemadmin/01](systemadmin/01-gestione-stabilimenti.md) §9)*
28. **Prenotazione servizio online (consumer)** — **non** invia email/notifica/WhatsApp alla
    creazione, a differenza del percorso operatore che invia tutto. Divergenza voluta?
    *(→ [cliente-finale/05](cliente-finale/05-prenotazione-servizi-e-listino.md) §2)*
29. **Notifiche pianificate degli eventi marketing** (`marketing_event_notification`) — le righe
    vengono create ma **nessun mittente** le invia. Completare o rimuovere la feature?
    *(→ [stabilimento/09-marketing.md](stabilimento/09-marketing.md) §5,
    [cliente-finale/06](cliente-finale/06-eventi.md))*
30. **Automazioni "welcome" / "goodbye"** — create ma **saltate** dal cron. Vanno implementate?
    *(→ [stabilimento/09-marketing.md](stabilimento/09-marketing.md) §4)*
31. **Mittente campagne email** — `noreply@summerbooking.it` (piattaforma), **non** l'SMTP della
    property (diverso da newsletter e report). È voluto?
32. **Invio report convenzioni via WhatsApp** — non invia nulla: costruisce solo un deep link `wa.me`
    che l'operatore apre manualmente. Va reso un invio automatico (API WhatsApp)?
    *(→ [stabilimento/06-convenzioni.md](stabilimento/06-convenzioni.md) §4)*
33. **Iscrizione a `subscribes` senza consenso esplicito** — l'email entra in lista marketing ad ogni
    registrazione/creazione utente, senza double opt‑in. Rivedere per conformità (GDPR).
    *(→ [cliente-finale/10](cliente-finale/10-contenuti-editoriali-e-newsletter.md) §2)*
34. **2FA / OTP** — OTP a 4 cifre con `Math.random()`, nessun rate‑limit, **non invalidato dopo
    l'uso**. Ridisegnare.
    *(→ [stabilimento/12-impostazioni.md](stabilimento/12-impostazioni.md) §7,
    [cliente-finale/01](cliente-finale/01-account-e-registrazione.md) §5)*
35. **Chatbot / assistente virtuale** — oggi FAQ statica a pattern‑matching (il ramo "OpenAI" non
    chiama nulla). Deve diventare un vero assistente LLM o resta guida statica?
    *(→ [stabilimento/13-assistente-notifiche-account.md](stabilimento/13-assistente-notifiche-account.md) §1)*
36. **Modulo "Centralino/Switchboard"** (~159 rotte, tabelle `switchboard_*` + `chatbot_*`) — nessun
    frontend nel repo. Reperire l'interfaccia o definirne i requisiti da zero; branch `CX` a parte.

---

## F. Disponibilità, concorrenza e integrità

37. **Double booking** — `checkAvailabilityRow` / `checkLocationIsAvailable` non sono richiamati
    internamente da `booking` prima dell'INSERT: nessuna garanzia atomica contro prenotazioni
    concorrenti sulla stessa postazione/data.
    *(→ [cliente-finale/04](cliente-finale/04-prenotazione-postazioni.md) §5)*
38. **Controllo magazzino** — solo sui nuovi inserimenti, non atomico, accoppiamento per **nome
    stringa** con alias hardcoded (`"sedia regista"` → `"Directors Chair"`). Nessun evento di
    sotto‑scorta al superamento della soglia.
    *(→ [stabilimento/08-magazzino.md](stabilimento/08-magazzino.md) §5)*
39. **Capienza/sovrapposizione slot orari servizi** — verificata solo lato client.
    *(→ [stabilimento/03-cassa.md](stabilimento/03-cassa.md) §1,
    [cliente-finale/05](cliente-finale/05-prenotazione-servizi-e-listino.md) §1)*
40. **Limiti di soggiorno per settore** — enforced solo lato client; l'endpoint server
    `check-bookinglimit` esiste ma non è mai chiamato.
    *(→ [stabilimento/12-impostazioni.md](stabilimento/12-impostazioni.md) §5)*
41. **Accoppiamento per nome stringa** diffuso — settori, periodi tariffari, icone, servizi, categorie
    icone sono collegati per **nome**, non per id/FK. Introdurre FK reali.
    *(→ price_lists.md, seasons_periods.md, sectors_rows.md, configure_images.md)*
42. **Catalogo icone mappa** — eliminazione senza controllo d'uso (mappe che la referenziano restano
    con URL rotto); copia per **valore** degli URL in `configure_maps`; vocabolari (`category`, `type`)
    come testo libero non normalizzato.
    *(→ [systemadmin/02](systemadmin/02-catalogo-icone-mappa-spiaggia.md))*
43. **`SQL injection`** in `getOnlineReservationStatusByAdmin` (e altre query con interpolazione di
    stringa): parametrizzare tutte.

---

## G. Contenuti e dati mancanti

44. **Magazine / blog / F.A.Q.** — nessun backend nel repo: contenuti statici o CMS esterno.
    Prevedere un CMS nel nuovo sistema o mantenere statici?
    *(→ [cliente-finale/10](cliente-finale/10-contenuti-editoriali-e-newsletter.md) §1)*
45. **`country`** senza codice ISO né unicità; **`services`/`service_type_filters`** senza pannello di
    gestione (solo DB diretto). Definire chi li gestisce nel nuovo sistema.
46. **Deduplica contatti** — nessun controllo su email/telefono duplicati in `customers` per la stessa
    property.
    *(→ [stabilimento/04-contatti.md](stabilimento/04-contatti.md) §2)*
47. **Cancellazione account cliente** — soft delete anonimizzante, nessuna email di conferma, dati
    collegati non trattati. Confermare il comportamento (e la conformità GDPR sul "diritto all'oblio").
    *(→ [cliente-finale/01](cliente-finale/01-account-e-registrazione.md) §7)*
48. **Sincronizzazione fra i 4 frontend** — divergenze verificate (ruolo `marketing` assente in
    `front-customer-pre`, upload foto, ecc.). Stabilire quale versione è "di produzione" per ciascuna
    piattaforma.
