# Lingue e date/ore

> Documento di indirizzo su come trattare **localizzazione** (lingua dell'interfaccia vs lingua dei dati,
> traduzioni del catalogo, messaggi) e **istanti temporali** (UTC, fusi, scadenze) nel nuovo backend .NET 10.
> Complemento a [03-gestione-dati.md](03-gestione-dati.md) (modellazione dati — §3.3/§3.4 timestamp, §
> money), [pre/standard-risposta-api-dotnet.md](pre/standard-risposta-api-dotnet.md) (formato risposte, "il
> backend restituisce codici, il frontend traduce"), [pre/linee-guida-implementative-dotnet.md](pre/linee-guida-implementative-dotnet.md)
> §9 (i18n a livello di modulo) e [02-strategia-di-test.md](02-strategia-di-test.md) (determinismo su date).
>
> **Documento vivo**: da estendere quando emergono nuovi casi.

---

## Indice

1. [Guida rapida](#1-guida-rapida)
2. [PARTE 1 — Lingue](#parte-1--lingue)
3. [PARTE 2 — Date e ore](#parte-2--date-e-ore)
4. [PARTE 3 — Impatti sui test](#parte-3--impatti-sui-test)
5. [PARTE 4 — Riepilogo dei rischi](#parte-4--riepilogo-dei-rischi)

---

## 1. Guida rapida

### Due lingue diverse, da non confondere

| | Chi la gestisce | Come |
|---|---|---|
| **Lingua dell'interfaccia** (etichette, pulsanti, messaggi del frontend) | il frontend, col suo i18n | file di traduzione lato client |
| **Lingua dei dati** (nomi servizi, descrizioni catalogo, testi PDF/email) | il backend | header `Accept-Language` sulla richiesta + preferenza persistita a DB per i contenuti asincroni |

Sono indipendenti e possono divergere legittimamente: un operatore di stabilimento con interfaccia in
italiano che prepara una conferma di prenotazione per un cliente straniero tiene l'interfaccia in italiano ma
vuole il documento nella lingua del cliente. Legandole insieme, quel caso diventa impossibile.

Sul *dove* stanno i testi: quelli statici scritti dallo sviluppatore in **risorse di codice** per-modulo
(`.resx`), i contenuti di catalogo editabili dall'admin a **DB**; le descrizioni dei **codici d'errore** le
traduce il **frontend**, non il backend (§1.2, §1.5).

### Regola sulle date, in una riga

Nel database e nelle API si scrive **sempre e solo UTC**. La conversione al fuso locale è responsabilità del
client. Nessun tipo del backend, tranne `SystemDateTimeProvider`, può leggere l'orologio di sistema
(`DateTime.Now`/`UtcNow`, `DateTimeOffset.Now`): si usa `IDateTimeProvider` (già presente, vedi
[02-strategia-di-test.md §3](02-strategia-di-test.md) e il divieto via `BannedApiAnalyzers` in §15).

Fanno eccezione i **documenti resi dal backend stesso** (ricevuta Fiskaly, PDF, email): lì è il backend a
convertire l'istante verso l'ora civile italiana (`Europe/Rome`), ma la fonte dell'istante resta
`IDateTimeProvider` server-side, mai l'orologio del client o della cassa — vedi §2.7.

---

# PARTE 1 — LINGUE

## 1.1 Lo stato di partenza

Il legacy non ha un meccanismo di localizzazione: i testi mostrati all'utente sono in italiano, cablati nel
codice o nelle colonne di catalogo. Alcuni vocabolari hanno già un secondo valore lingua sporadico e
incoerente (es. `switchboard_wa_template.language` con `it` / `en_US`, che è però la lingua del template
Meta, non della UI). L'italiano è oggi la lingua di default e l'unica con contenuti reali.

**Obiettivo della riscrittura**: il *meccanismo* di localizzazione va predisposto ora, in modo che
aggiungere una lingua sia **inserire righe/risorse, non fare una migration**. Il *contenuto* delle
traduzioni è lavoro separato e successivo.

### Lingue gestite (decisione del 2026-10-08)

| Codice | Lingua | Note |
|---|---|---|
| `it` | italiano | **default** e fallback di ogni traduzione mancante |
| `en` | inglese | |
| `fr` | francese | |
| `es` | spagnolo | |
| `de` | tedesco | |
| `zh` | cinese | semplificato |
| `ar` | arabo | unica scritta da destra a sinistra: il frontend deve gestire il layout RTL |

L'elenco è nel codice in `Shared/Localization` (`AppLanguage`, `SupportedLanguages`): codici ISO 639-1, gli
stessi usati nelle API, nei file `.resx` (`Nome.fr.resx`, …) e come etichette degli enum Postgres. Dove una
colonna ha un insieme chiuso di lingue (`identity.ui_language`) l'aggiunta di una lingua richiede comunque
una migration (`ALTER TYPE … ADD VALUE`). Le tabelle di traduzione (`culture` libera) no. Stato al
2026-10-08:

- **Identity**: `identity.ui_language` con le sette lingue; export dei contatti tradotto in tutte (testi da
  far rivedere a un madrelingua); nomi dei piani di licenza seminati in tutte, tradotti (Base/Basic/Essentiel…; elenco in
  `analisi-post/docs/traduzioni-seed.xlsx`), poi gestiti dal systemadmin.
- **Notification**: le email transazionali sono ancora testi provvisori it/en; i modelli nelle sette lingue
  arrivano con il modulo ([85-promemoria.md](85-promemoria.md), PROMEMORIA-85-12).
- **Fiskaly**: `fiskaly.language_code` resta `it`/`en`, lingue del PDF/eReceipt accettate dal provider; da
  allargare solo se SIGN IT ne supporta altre (PROMEMORIA-85-12).

## 1.2 Da dove arriva la lingua

La lingua è una **preferenza volatile**, non un dato di identità. Non va messa nel token come autoritativa
(resterebbe lo snapshot al login, fino al refresh); al più un `locale` come default di comodo non
autoritativo.

| Caso | Fonte della lingua |
|---|---|
| Risposte API con errori | **non serve**: traduce il frontend dal `code` (vedi [standard-risposta-api-dotnet.md](pre/standard-risposta-api-dotnet.md)) |
| Contenuto di dominio in risposta sincrona (catalogo, descrizioni servizi) | header **`Accept-Language`** della richiesta, anche sugli endpoint pubblici/anonimi |
| Contenuti asincroni senza richiesta HTTP a monte (email di sistema, PDF di conferma/preventivo) | **preferenza persistita a DB**, del *cliente/prenotazione*, non dell'operatore che ha innescato l'azione |

Il codice lingua vive in campi distinti, per cose diverse:

| Campo (indicativo) | Significato |
|---|---|
| `user.preferred_language_code` | preferenza dell'operatore per l'interfaccia e per i contenuti che consulta |
| `booking.language_code` / `customer.language_code` | lingua del cliente finale — **fonte per email e PDF** legati a quella prenotazione |
| documento generato (es. `receipt`/`quote` PDF) | conserva la **propria** `language_code`: lo stesso documento può essere ristampato in più lingue, e sono record distinti |

La lingua del documento **non** è quella dell'operatore: un operatore italiano può emettere una conferma per
un cliente straniero. La fonte è la lingua del cliente/prenotazione, mai `user.preferred_language_code`.

## 1.3 Negoziazione di `Accept-Language`

Da configurare **una volta nell'host** (`RequestLocalizationOptions`), con comportamento sotto test:

- si rispettano i pesi `q=` (`de-DE,de;q=0.9,en;q=0.8` → tedesco);
- si confronta la **sola parte primaria** del tag: non si distingue `de-DE` da `de-AT`, `en-GB` da `en-US`;
- una lingua **non gestita non è un errore**: si serve il default (italiano) con **status 200**, come
  prevede HTTP;
- `q=0` è rispettato: significa "non voglio questa lingua" e la si scarta;
- header assente → default.

| `Accept-Language` | Risposta |
|---|---|
| `it` | italiano |
| `de` | tedesco, con fallback all'italiano dove la traduzione manca |
| `de-DE,de;q=0.9` | tedesco (si ignora la variante regionale) |
| `fr` (non gestita) | italiano, 200 |
| assente | italiano |

## 1.4 Cosa si traduce e cosa no

**Si traduce** (contenuto mostrato al cliente finale): nomi e descrizioni di servizi, tipologie, listini a
voce, categorie; testi del PDF (intestazioni, condizioni generali); messaggi di avvertimento della
validazione (vedi §1.8); nomi dei campioni/varianti visibili.

**Non si traduce mai**, e non è una dimenticanza:

| Cosa | Perché |
|---|---|
| I **codici** (`FISCAL_DEVICE`, valori enum come `pending`/`paid`, codici sconto) | sono identificatori: per la logica, mai per il testo a schermo. Li mappa il client |
| I **nomi propri** (stabilimenti, alcuni servizi) — da confermare col cliente quali | decisione commerciale: invariati in ogni lingua |
| `title`/`detail` di **Problem Details** | sono per sviluppatori e log; per messaggi localizzati il frontend usa `code` + `args` e lo status HTTP (vedi [standard-risposta-api-dotnet.md §1](pre/standard-risposta-api-dotnet.md)) |
| Codici ISO (valuta `EUR`, paese `IT`, fuso IANA) | insiemi chiusi "di libreria", non testo tradotto |

Gli endpoint **admin restituiscono sempre i valori grezzi** (lingua di default): l'amministratore modifica i
testi di partenza, non le traduzioni derivate quando è in gestione catalogo.

## 1.5 Dove vivono i testi: risorse di codice o DB

Il criterio non è il volume ma **chi scrive il testo e quando** (coerente con
[linee-guida-implementative-dotnet.md §9](pre/linee-guida-implementative-dotnet.md)). Si usano **entrambi**
i meccanismi, non solo il DB:

| Tipo di testo | Dove | Note |
|---|---|---|
| Statico, scritto dallo sviluppatore (etichette di sistema, testi fissi dei template, validazioni interne) | **`.resx` / `IStringLocalizer`**, per modulo (`Booking/Resources/`) | versionato col codice, in memoria, veloce |
| Editabile come **file** senza ricompilare l'assembly | **file JSON** di localizzazione | rideploy del solo file, nessun deploy di codice |
| Editabile **live dall'admin**, o **per-record / per-stabilimento** (nomi servizi, descrizioni catalogo, voci di listino) | **DB**, tabella di traduzione (sotto) | è il caso prevalente qui: catalogo visto dal cliente finale |

Il **meccanismo** (risoluzione cultura da `Accept-Language`, `RequestLocalizationOptions`, `IStringLocalizer`)
si configura **una volta nell'host**; le **risorse** stanno per-modulo, le poche comuni nel `SharedKernel`.

### Modello dati per i contenuti a DB

**Una tabella di traduzione per entità traducibile**, nello schema del modulo proprietario — **non** una
tabella unica per dominio, e **non** una tabella generica `(entity_type, entity_id, field, culture, value)`:

```
catalog.service_translation      ( service_id,      culture, name, description )   UNIQUE (service_id, culture)
catalog.service_type_translation ( service_type_id, culture, name )                UNIQUE (service_type_id, culture)
...
```

- **Perché per entità e non una unica per dominio**: la tabella generica avrebbe `entity_type`/`field` come
  stringhe libere non validate — un refuso (`"Services"` invece di `"Service"`, `"name"` minuscolo)
  produrrebbe una riga salvata regolarmente che nessuno rileggerà mai, senza alcun errore. La tabella per
  entità ha colonne tipizzate, FK reale, e compilatore/EF che verificano i nomi. Un modulo ha tipicamente
  **più** tabelle `*_translation`: è normale e voluto.

Regole di funzionamento:

1. **Il testo nella lingua di default sta nella colonna dell'entità** (`services.name` in italiano); la
   tabella di traduzione contiene **solo** le varianti nelle altre lingue. Ne discendono due proprietà:
   leggere in italiano non costa join, e una traduzione mancante ricade automaticamente sull'italiano invece
   di produrre un campo vuoto.
2. **Aggiungere una lingua = `INSERT` di righe** (la colonna `culture` esiste già). Nessun punto dello schema
   deve assumere un insieme chiuso di culture.
3. **Tradurre nella lingua di default va rifiutato (400)**: quel testo sta già nella colonna dell'entità;
   ammetterlo creerebbe due fonti destinate a divergere.
4. **Query "cosa manca da tradurre in lingua X"**: è ciò che rende gestibile il lavoro di traduzione e va
   progettata deliberatamente (non esce gratis con N tabelle tipizzate). Opzioni: una vista
   `translation_coverage` per modulo, oppure un helper admin che itera sui metadati EF confrontando le righe
   `*_translation` con le entità di base. Deve restituire entità, campo e **testo di partenza** in italiano.

Endpoint admin di gestione (per modulo):

| Endpoint | Cosa fa |
|---|---|
| `GET .../translations?entity=&culture=` | elenco |
| `GET .../translations/missing/{culture}` | cosa manca, con codice/id e testo di partenza |
| `PUT .../translations` | inserisce o aggiorna, e **invalida la cache** (§1.6) |
| `DELETE .../translations/{id}` | rimuove: il testo torna a mostrarsi nella lingua di default |

## 1.6 Cache e invalidazione

Il catalogo è **read-heavy** (letto a ogni caricamento del configuratore/ricerca, anche da visitatori
anonimi). Le traduzioni cambiano di rado e sono poche. Si tiene quindi una **cache in memoria/Redis** del
contenuto di dominio multilingua (coerente con [standard-risposta-api-dotnet.md §7](pre/standard-risposta-api-dotnet.md):
Redis serve per questo, **non** per i messaggi d'errore), granulare per **(tipo entità × cultura)**.

- Una JOIN per ogni campo traducibile in ogni proiezione, pagata a ogni richiesta, sarebbe il costo
  sbagliato: con la cache si paga una volta per invalidazione.
- La cache viene **invalidata subito** a ogni scrittura admin sulle traduzioni: senza, una correzione
  resterebbe invisibile fino alla scadenza del TTL. Da evitare l'errore già visto nel legacy di invalidare
  con una chiave costruita da un valore del body spesso assente (vedi [01-criticita §5](01-criticita-e-indicazioni-backend.md),
  cache pricelist invalidata con `req.body.propertyId`).

**Cosa NON sta in questa cache.** Solo il contenuto di dominio a DB. Ne restano fuori:

- le traduzioni dei **codici d'errore** (`code` → descrizione in lingua): vivono nei file i18n del
  **frontend**, che è chi traduce (§1.2, [standard-risposta-api-dotnet.md §1](pre/standard-risposta-api-dotnet.md));
  il backend non le conserva né le serve;
- le **risorse `.resx`**: sono già in memoria nell'assembly per natura — Redis non serve;
- le **stringhe dell'interfaccia**: sono del frontend.

Se una descrizione d'errore serve a un rendering fatto dal backend (email/PDF), viene dai `.resx` in
memoria, non da Redis.

## 1.7 Fallback per campo, non per record

Il fallback alla lingua di default è **per singolo campo**, non per record: in una lista richiesta in
tedesco si può ricevere una riga tradotta e la successiva in italiano. È voluto — meglio l'italiano di un
campo vuoto — ma **il DTO e il frontend non devono assumere che una lista sia omogenea per lingua**.

Regola di fallback: cultura richiesta → riga di traduzione → valore della colonna (italiano). **Mai una
stringa vuota.**

## 1.8 Avvisi e messaggi persistiti: si salva il codice, non il testo

Gli avvisi di validazione (es. regole su una prenotazione) che vanno mostrati e anche stampati sul PDF si
**congelano sul record come codice + parametri**, non come frase:

```
code             CANCELLATION_WINDOW_EXPIRED
parameters_json  { "noticeHours": 24, "bookingStart": "2026-08-01T08:00:00Z" }
```

Il testo si compone **al momento della resa** (schermo o PDF), nella lingua di quel contesto. Se si
salvasse la frase, ristampare in un'altra lingua richiederebbe di ricalcolare la validazione — e nel
frattempo i vincoli potrebbero essere cambiati, producendo un documento diverso.

- L'elenco dei codici è **chiuso** e vive in un catalogo condiviso backend↔frontend (stesso principio del
  `code` di Problem Details, [standard-risposta-api-dotnet.md §10 punto 8](pre/standard-risposta-api-dotnet.md)).
- N codici × M lingue stringhe da produrre, più i parametri per l'interpolazione.
- Il Domain resta **agnostico rispetto alla lingua** ([linee-guida §Domain](pre/linee-guida-implementative-dotnet.md)):
  solleva eccezioni/avvisi tipizzati con codice, la localizzazione avviene al bordo (Endpoints/PDF).

---

# PARTE 2 — DATE E ORE

## 2.1 La regola

Nel database e nelle API si scrive sempre e solo **UTC**. La conversione al fuso locale è responsabilità del
client.

## 2.2 Come è realizzata

Dettaglio già in [03-gestione-dati.md §3.3–3.4](03-gestione-dati.md); qui il riepilogo operativo:

| Livello | Comportamento |
|---|---|
| PostgreSQL | ogni colonna data è **`timestamptz`**, memorizzata in UTC. Mai `datetime`/`timestamp` senza time zone |
| EF Core | converter che forza `Kind = Utc` in scrittura e ri-etichetta `Utc` in lettura |
| API JSON | ISO 8601 con suffisso `Z`: `"2026-08-24T10:17:36.692Z"` |
| `created_at` / `updated_at` | valorizzati automaticamente in `SaveChanges` + default `now()` a livello DB, mai a mano |
| Date "solo giorno" (`booking_start_date`, `absence_date`) | tipo `date` quando l'ora non ha significato |

**Perché serve il converter.** Npgsql mappa `DateTime` su `timestamptz` e **rifiuta in scrittura** un valore
con `Kind` diverso da `Utc`. Senza converter basterebbe una data deserializzata da JSON, o una lettura da un
percorso legacy, per far fallire una `SaveChanges` in un punto raro, col punto di rottura lontano dalla
causa. Il legacy oggi fa il contrario — `moment.parseZone(...).format('YYYY-MM-DD HH:mm:ss')` salva stringhe
locali, con compensazioni `+1 giorno` sparse nel codice: è una sorgente di bug da chiudere per costruzione.

`timestamptz` memorizza sempre UTC e lo rende nel fuso della sessione: vedere `+02` in un client SQL non
significa che il dato sia salvato in ora locale.

## 2.3 `IDateTimeProvider`

- Nel backend si usa **`IDateTimeProvider`** (interfaccia già presente); l'unica implementazione che tocca
  l'orologio reale è `SystemDateTimeProvider`.
- `DateTime.Now` / `DateTime.UtcNow` / `DateTimeOffset.Now` sono **vietati** ovunque (regola architetturale
  in [02-strategia-di-test.md §7](02-strategia-di-test.md), enforcement via `BannedApiAnalyzers` §15).
- Motivo non solo stilistico: rende **collaudabili le scadenze** spostando l'orologio invece di aspettare
  (finestre di cancellazione, hold di prenotazione, validità token).

## 2.4 Le scadenze del dominio

Esempi di durate che il backend calcola a partire da un istante:

| Scadenza | Origine del valore |
|---|---|
| Finestra di cancellazione gratuita | `bathouses.cancellation` → campo strutturato `int? noticeHours` (`null` = non consentita), vedi [03-gestione-dati.md §Enumerazioni](03-gestione-dati.md) |
| Hold della risorsa in checkout | `resource_consumptions.held_until` (`timestamptz`), vedi [03-gestione-dati.md §hold](03-gestione-dati.md) |
| Access token / refresh token / OTP | costanti/configurazione del modulo Auth (vedi [01-criticita §7](01-criticita-e-indicazioni-backend.md)) |

**Punto da confermare col cliente.** Le scadenze commerciali (finestra di cancellazione, eventuali validità
di un preventivo/offerta) vanno intese **all'istante esatto** o **a fine giornata**? "Cancellabile fino a
24 ore prima" a partire da un check-in delle 08:00 significa "entro le 08:00 del giorno prima" (lettura
letterale) oppure "entro la fine di quel giorno"? E se fine giornata, in **quale fuso**? La differenza è di
poche ore proprio sul caso limite (una telefonata del cliente il giorno prima). La scelta va fissata **una
volta**, nel domain service che calcola la scadenza, non dedotta caso per caso.

## 2.5 Il fuso orario dell'utente

Non è memorizzato, ed è una scelta deliberata finché tutti gli attori operano nello stesso fuso (Italia). Se
un domani servisse (operatori/rivenditori in un altro fuso), il posto giusto è un campo
`user.preferred_time_zone` con identificatore IANA, **non** una conversione nel database: il DB resta in
UTC.

## 2.6 Formattazione: dove UTC diventa leggibile

| Contesto | Formato | Chi converte |
|---|---|---|
| API JSON | `2026-08-24T10:17:36Z` | il client |
| Interfaccia | fuso locale del browser | il client |
| Email di sistema | `dd/MM/yyyy HH:mm` in **ora civile italiana**, con etichetta di fuso esplicita ("ora italiana") | il backend |
| PDF (conferma, preventivo, ricevuta) | **solo la data**, `dd/MM/yyyy`, in ora civile italiana | il backend |

Un orario non va **mai** mostrato senza indicare il fuso: "cancellabile gratuitamente fino al 27/08 alle
09:15" è ambiguo, e le finestre di cancellazione sono proprio il caso in cui l'ambiguità costa. Poiché tutti
i destinatari operano in Italia (§2.5), nelle email si stampa l'ora civile italiana con un'etichetta
esplicita; se un domani ci fossero destinatari in altri fusi, l'alternativa è stampare UTC con la dicitura
"UTC". Sul PDF si stampa solo la data: un orario su un documento commerciale genererebbe solo domande. La
conversione di fuso prima di formattare conta **anche quando si stampa solo la data** — vedi §2.7.

## 2.7 Casi in cui è il backend a rendere l'istante (Fiskaly, scontrino RT, PDF, email)

"UTC nel DB/API, converte il client" copre le API e la UI. **Non** copre i casi in cui è il **backend** a
incorporare un istante dentro un documento o un payload verso l'esterno. Lì valgono due principi:

1. **La fonte dell'istante è sempre `IDateTimeProvider` lato server**, catturato quando l'evento accade
   (ricezione della richiesta di emissione, conferma del pagamento…). **Mai** l'orologio del client o della
   postazione cassa: un PC con l'ora sballata non deve poter datare un documento fiscale, e Fiskaly/AdE
   possono **rifiutare** una ricevuta con timestamp troppo distante dall'ora reale.
2. **Per i documenti fiscali e commerciali il fuso di riferimento è `Europe/Rome`**, non UTC: è l'ora civile
   che ha valore sul documento. Il backend converte UTC → `Europe/Rome` *al momento della resa*, con offset
   esplicito dove il formato lo prevede.

| Output | Che ora ci va | Fonte |
|---|---|---|
| **Ricevuta elettronica Fiskaly** (SIGN IT, modulo C4) | istante della transazione in ora civile italiana (`Europe/Rome`, ISO 8601 con offset — verificare il campo/formato attesi dall'API Fiskaly) | `IDateTimeProvider.UtcNow` alla creazione della ricevuta, convertito a `Europe/Rome` |
| **Scontrino su hardware RT** (Registratore Telematico — feature C4bis) | l'ora la timbra il **dispositivo RT**, che ha un orologio certificato: è quello legalmente autoritativo per quel documento | orologio del RT (via adapter lato client). Il backend **non** la impone |
| **PDF** (conferma, preventivo, ricevuta di cortesia) | **solo la data**, `dd/MM/yyyy`, in ora civile italiana; se un PDF mostra un orario (raro), stessa regola dell'email | `IDateTimeProvider.UtcNow` convertito a `Europe/Rome` *prima* di estrarre la data |
| **Email di sistema** | `dd/MM/yyyy HH:mm` in ora civile italiana con etichetta esplicita (§2.6) | `IDateTimeProvider.UtcNow` convertito a `Europe/Rome` |

**Trappola della mezzanotte.** Anche stampando *solo la data*, la conversione di fuso conta: un PDF generato
alle `23:30Z` del 24/08 è già il 25/08 in ora italiana d'estate. Estrarre la data dall'istante **UTC**
stamperebbe il giorno sbagliato. Regola: converti a `Europe/Rome`, *poi* prendi la parte data.

**Riconciliazione RT.** Per lo scontrino RT, la data che il backend registra a DB per quel documento e l'ora
timbrata dal dispositivo vanno confrontate: uno scarto oltre soglia è un **allarme operativo** (orologio del
RT da risincronizzare), non un dettaglio da ignorare.

**`Europe/Rome`, non offset fisso.** La conversione usa il fuso IANA `Europe/Rome` (`TimeZoneInfo`), che
gestisce il passaggio ora legale/solare; mai un `+01:00`/`+02:00` cablato.

## 2.8 Regole per il frontend

1. **Non troncare mai la `Z`.** `new Date("2026-08-24T10:17:36Z")` è corretto; senza `Z` la stringa viene
   interpretata come ora locale e sposta l'istante.
2. **Inviare sempre UTC** quando si mandano date al backend.
3. **Non calcolare le scadenze lato client.** I flag temporali (es. `cancellabileGratis`, `modificabile`,
   `pdfDisponibile`) arrivano già calcolati sul DTO: ricalcolarli lato client significa dipendere
   dall'orologio dell'utente. Coerente con il principio "autorità al backend" di
   [01-criticita §3.4](01-criticita-e-indicazioni-backend.md).
4. **Attenzione all'orologio del client sul JWT.** `ClockSkew` è azzerato lato server: un browser con l'ora
   indietro vedrà 401 anticipati, ai quali si reagisce con il refresh del token, non con un ritentativo
   immediato.

## 2.9 Numeri, valuta e misure

Gli importi arrivano come **numeri JSON**, senza simbolo e senza separatori di migliaia, sempre con 2 cifre
decimali (vedi [03-gestione-dati.md §money](03-gestione-dati.md)):

```jsonc
{ "total": 143.60, "vatRate": 22, "depositPct": 20 }
```

- La **formattazione per valuta/locale è responsabilità del client** (`Intl.NumberFormat`). Le percentuali
  sono numeri, non frazioni: `vatRate: 22` significa 22%.
- **Attenzione**: `it-IT` e `de-DE` condividono le stesse convenzioni (`5.392,40 €`), quindi provando solo
  in italiano/tedesco si può credere che la formattazione "funzioni da sola". Si scopre il contrario alla
  prima prova in inglese (`€5,392.40`). → serve un caso di test `en-GB`/`en-US` (§3).
- **Non fare aritmetica sugli importi lato client.** Il server calcola in `decimal` con arrotondamento
  half-away-from-zero; JavaScript userebbe i `double`: sommare le righe lato client può dare un totale
  diverso di qualche centesimo da quello stampato sul PDF/ricevuta. Il totale autoritativo lo calcola
  **sempre** il backend (`POST /bookings/quote`, vedi [03-gestione-dati.md §preventivo](03-gestione-dati.md));
  il frontend può fare una stima ottimistica per feedback immediato, ma va **riconciliata** con la risposta
  del backend prima di confermare/pagare.
- Le misure sono in **millimetri interi** (o nell'unità intera prevista dal campo): l'eventuale conversione
  a un'altra unità per la resa a schermo la fa il client.

---

# PARTE 3 — Impatti sui test

- **Determinismo su date** ([02-strategia-di-test.md §3, §7](02-strategia-di-test.md)): ogni logica su
  stagioni, listini, periodi di prenotazione, finestre di cancellazione e hold si collauda iniettando
  `IDateTimeProvider` e spostando l'orologio. Nessun `Thread.Sleep`, nessuna attesa reale.
- **Negoziazione `Accept-Language`**: test di integrazione sulla tabella di §1.3 (pesi `q=`, subtag
  primario, lingua non gestita → 200 in default, `q=0`).
- **Fallback per campo** (§1.7): un test che verifica una lista con righe parzialmente tradotte e si
  aspetta righe non omogenee, non un errore.
- **Invalidazione cache traduzioni** (§1.6): scrittura admin → lettura successiva riflette subito la
  modifica.
- **Formattazione importi**: caso `en-GB`/`en-US` oltre a `it`, per intercettare regressioni che `it`/`de`
  non mostrano (§2.9).
- **Conversione di fuso lato backend** (§2.7): un test che genera un documento con l'orologio finto a
  `23:30Z` e verifica che la data resa sia il giorno *dopo* (ora italiana d'estate), non il giorno UTC.
- **Somma degli importi**: il totale del backend è la fonte; un test verifica che la somma delle righe del
  breakdown torni al totale al centesimo (allocazione "largest remainder", [01-criticita §4](01-criticita-e-indicazioni-backend.md)).

---

# PARTE 4 — Riepilogo dei rischi

| Rischio | Stato / mitigazione |
|---|---|
| `DateTime.Now` che rompe `SaveChanges` sotto Npgsql | chiuso dal converter EF (`Kind = Utc`) |
| Date lette come `Unspecified` e confrontate male | chiuso: la lettura marca `Utc` |
| Stringhe di data locali con compensazioni `+1 giorno` (pattern legacy) | chiuso: `timestamptz` + UTC ovunque |
| `created_at` / `updated_at` dimenticati o duplicati (`createdAt` **e** `created_at`) | chiuso: automatici in `SaveChanges` + default DB, colonne uniche |
| Ambiguità di fuso in email/PDF | chiuso: ora sempre con etichetta di fuso; documenti resi dal backend in ora civile italiana (§2.6–2.7) |
| Documento fiscale/PDF datato con l'orologio del client (cassa/browser) | chiuso: istante da `IDateTimeProvider` server-side; l'hardware RT è l'eccezione, con orologio certificato proprio (§2.7) |
| PDF/ricevuta con data errata a cavallo della mezzanotte | chiuso: conversione a `Europe/Rome` *prima* di estrarre la data (§2.7) |
| Orologio del RT fuori sincrono | riconciliazione data DB ↔ ora RT; scarto oltre soglia = allarme operativo (§2.7) |
| Timestamp Fiskaly rifiutato per drift dall'ora reale | chiuso: istante server-side alla creazione, non client (§2.7) |
| Scadenze all'istante vs fine giornata | **da confermare col cliente** (§2.4), poi fissato nel domain service |
| Fuso orario per operatori fuori dall'Italia | non necessario oggi; se servirà, `user.preferred_time_zone` (§2.5) |
| Schema traduzioni "sfuso" con `entity_type`/`field` liberi | evitato: tabelle tipizzate per entità (§1.5) |
| Query "cosa manca in lingua X" non disponibile | da progettare esplicitamente (vista/helper admin, §1.5) |
| Traduzione mancante → campo vuoto in UI/PDF | chiuso: fallback per campo alla lingua di default, mai stringa vuota (§1.7) |
| Correzione di una traduzione invisibile per via della cache | chiuso: invalidazione immediata per (entità × cultura) (§1.6) |
| Frase di avviso congelata sul record e non ristampabile in altra lingua | chiuso: si salva `code` + `parameters_json`, testo composto alla resa (§1.8) |
| Descrizioni dei codici d'errore cercate nella cache/DB del backend | evitato: le traduzioni degli errori stanno nei file i18n del frontend; la cache backend contiene solo contenuto di dominio a DB (§1.6) |
| Testo statico del dev messo a DB (o contenuto per-record messo in `.resx`) | evitato: criterio "chi scrive e quando" — `.resx` per lo statico, DB per l'editabile/per-record (§1.5) |
| Formattazione importi "sembra funzionare" perché testata solo in `it`/`de` | mitigato: caso di test `en` obbligatorio (§2.9, §3) |
| Totali sommati lato client divergenti dal PDF | chiuso: totale autoritativo dal backend, stima client solo indicativa (§2.9) |
