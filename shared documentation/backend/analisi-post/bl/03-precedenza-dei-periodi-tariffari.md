# Precedenza dei periodi tariffari: vince il periodo più breve

> **Stato**: regola **confermata dal committente** il 2026-09-30. Non era documentata da nessuna
> parte, pur essendo già il comportamento atteso dagli stabilimenti: è emersa verificando le
> sovrapposizioni di date nei dati reali
> ([`analisi-pre/db-no-copied/_mancanti-da-sistemare.md`](../../analisi-pre/db-no-copied/_mancanti-da-sistemare.md),
> voci A3 e B10).

## 1. La regola

Quando un giorno è coperto da **più di un periodo tariffario** della stessa stagione, vale il prezzo
del **periodo più breve** fra quelli che lo coprono.

```sql
-- risoluzione del periodo per una data, dentro una stagione
SELECT id, name, period_group_key
  FROM catalog.pricing_tariff_periods
 WHERE deleted_at IS NULL
   AND season_id = :stagione
   AND :giorno BETWEEN starts_on AND ends_on
 ORDER BY (ends_on - starts_on) ASC, id ASC
 LIMIT 1;
```

Il secondo criterio di ordinamento (`id`) non è decorativo: a parità di durata due periodi
darebbero un risultato non deterministico, e un prezzo che cambia fra due chiamate identiche è il
tipo di difetto che nessuno riesce a riprodurre.

## 2. Perché esiste, con un esempio reale

Non è un caso limite: è il modo in cui gli stabilimenti fanno pagare di più un **giorno di festa o
un ponte** dentro un periodo ordinario.

```
'Bassa stagione'  (tier low)   11 giugno → 29 giugno
'Alta stagione'   (tier high)  12 giugno → 12 giugno      ← un giorno solo, dentro
```

Per il 12 giugno esistono due periodi validi. Senza la regola, il prezzo dipenderebbe dall'ordine in
cui il motore li incontra — e sarebbe **il giorno che frutta di più** a essere prezzato male.

Nei dati migrati questa forma compare **22 volte** su 41 sovrapposizioni totali, su stabilimenti
diversi: *Bagno La Vela*, *Lido San Felice*, *Club House Management*, *Goa Family Beach*.

## 3. Perché va scritta adesso e non quando si farà il motore

Un motore che scegliesse «il primo trovato» o «l'id più basso» **non sbaglierebbe in modo
visibile**: restituirebbe un prezzo plausibile, semplicemente non quello giusto, e solo su alcuni
giorni dell'anno. Non c'è un errore da leggere in un log, non c'è una riga che manca: c'è un importo
diverso da quello che il committente ha impostato.

E vale doppio per il **motore di prezzo offline**
([`docs/19-frontend-offline.md`](../../progettazione/19-frontend-offline.md) §): deve dare **lo stesso**
risultato del server, perché il server rivalida il preventivo calcolato dal client. Due
implementazioni della stessa regola non documentata divergono con certezza, e la divergenza si
manifesta come un preventivo che cambia importo al momento della conferma — davanti al cliente.

## 4. Rapporto col vincolo di database

Il vincolo `ex_catalog_pricing_tariff_periods_no_overlap` (migration
`20260930123407_AddSectorNameUniqueAndPeriodExcludes`) vieta le sovrapposizioni **dentro lo stesso
gruppo** di periodi, non fra gruppi diversi. È deliberato e le due cose si tengono:

- **fra gruppi diversi** la sovrapposizione è la funzione descritta qui, e il database la permette;
  è il motore a risolverla con la regola del §1;
- **dentro lo stesso gruppo** la sovrapposizione è redondanza, perché il prezzo è agganciato al
  gruppo (`pricing_price_list_period_prices.tariff_period_group`) e non alla singola riga: due
  periodi dello stesso gruppo hanno per costruzione lo stesso prezzo, quindi la sovrapposizione non
  aggiunge informazione e il database la rifiuta.

Detto al contrario: il database impedisce l'ambiguità che non significa niente, e lascia passare
quella che significa «questo giorno costa diversamente». La regola del §1 serve per la seconda.

## 5. Cosa resta da fare

- [ ] Implementare l'ordinamento del §1 nella risoluzione del periodo, lato server.
- [ ] Applicare la **stessa** regola nel motore di prezzo offline, e coprirla con un test che
      confronti i due risultati sullo stesso insieme di periodi sovrapposti — è l'unico modo di
      accorgersi di una divergenza prima che la veda un cliente.
