# Sconti e voucher - Frontend requests

Le modifiche che il backend deve fare perché la pagina **Sconti e voucher** del gestionale funzioni come la vuole il
frontend. Il documento è pensato per chi le implementa (anche un agente): ogni sezione dice cosa creare, con payload,
risposta, regole ed errori attesi.

La pagina è `/settings/vouchers`: una tabella con gli sconti e i voucher dello stabilimento (nome, validità, tipo e
valore dello sconto), con ordinamento e pagine, e i comandi per aggiungere, modificare, duplicare ed eliminare gli
sconti.

Le rotte di questo documento sono quelle che il frontend chiama oggi (`/api/vouchers/*`, a cui in locale rispondono i
mock). Il backend non le ha: per gli sconti ha `/api/catalog/discounts` (`Module.Catalog/Endpoints/DiscountEndpoints.cs`,
`Application/Services/DiscountsService.cs`), che il frontend non usa. Il confronto fra le API della pagina e quelle del
backend non è in questo documento: oggi il documento contiene solo il controllo di versione (sezione 1).

## Riepilogo

| # | Metodo | Rotta | Cosa fa |
|---|---|---|---|
| 1 | GET + POST | `/api/vouchers/get-selected-item` e `/api/vouchers/update-voucher-item` | Controllo di versione: `version` nella lettura, `If-Match` ed `ETag` nel salvataggio, 409 `resource.version_conflict` |

## 1. Controllo di versione sui salvataggi di modifica

Se due utenti modificano lo stesso sconto, vince chi salva per ultimo: il pannello di modifica (la matita di una riga)
rimanda tutto lo sconto letto all'apertura e cancella, senza che nessuno se ne accorga, quello che un altro utente ha
cambiato nel frattempo. Oggi il backend non ha un controllo di concorrenza sugli sconti: nessun token di concorrenza in
`DiscountConfiguration` (`catalog.pricing_discounts`) né nelle tabelle dei periodi e delle fasce orarie
(`catalog.pricing_discount_date_ranges`, `catalog.pricing_discount_time_ranges`), nessun `If-Match` in
`PUT /api/catalog/discounts/{id}`. Il frontend chiede lo schema di **concorrenza ottimistica** che il backend ha già
progettato per le prenotazioni offline (`shared documentation/backend/progettazione/19-frontend-offline.md`, sezioni
4.4 e 5.8), con un **codice di errore unico per tutte le API**: `resource.version_conflict`.

### Cosa ci serve

- **Versione nella lettura**: la lettura del pannello restituisce, insieme ai dati, il campo `version` (stringa
  opaca). Cambia ogni volta che cambiano i dati che il salvataggio scrive (tabella sotto), anche quando li cambia
  un'altra API, e solo allora: non cambia per i dati che il pannello non legge né salva (per esempio `used_count` e
  `updated_at` di `catalog.pricing_discounts`).
- I dati coperti stanno in più tabelle: la versione cambia anche quando un salvataggio cambia solo i periodi o le
  fasce orarie. Un token della sola riga di `catalog.pricing_discounts` (come `xmin`) basta solo se ogni salvataggio
  che cambia periodi o fasce aggiorna anche quella riga.
- **`If-Match` nel salvataggio**: il frontend manda la versione letta nell'header `If-Match: "<version>"`. Se non è più
  quella attuale, il backend non salva niente e risponde **409 `resource.version_conflict`**, senza `errors[]`. Senza
  `If-Match` risponde allo stesso modo: il frontend lo manda sempre.
- **`ETag` nella risposta**: il salvataggio riuscito risponde 204 senza corpo, con la nuova versione nell'header
  `ETag: "<version>"`.
- Il 409 `resource.version_conflict` si controlla dopo il 404 `resource.not_found` dello sconto e dopo gli errori di
  validazione del corpo (400 `validation.failed`), prima di salvare.
- **Come farlo lo sceglie lo sviluppatore del backend** (un hash dei dati coperti dalla versione, un contatore
  aggiornato da ogni API che li cambia...), nei limiti dei punti sopra: l'agente glielo chiede prima di scrivere
  codice. Il frontend tratta la versione come una stringa opaca e non la confronta mai.

### API coinvolte

| Lettura (restituisce `version`) | Salvataggio (manda `If-Match`, risponde `ETag`) | Dati coperti dalla versione |
|---|---|---|
| `GET /api/vouchers/get-selected-item?idVoucher=…` | `POST /api/vouchers/update-voucher-item` | Tutto lo sconto che il pannello legge e salva: nome, periodi di validità con giorni della settimana e fasce orarie, tipo, valore, ambito, numero massimo di utilizzi e stato (abilitato o no) |

Le altre API della pagina non ne hanno bisogno: la tabella legge soltanto, l'aggiunta crea uno sconto nuovo,
eliminazione e duplicazione non rimandano dati letti.

### Lettura

`GET /api/vouchers/get-selected-item?idVoucher=6d1e2f3a-4b5c-4d6e-8f7a-9b0c1d2e3f02`: lo sconto con tutti i campi del
pannello, più `version`.

```json
{
  "idVoucher": "6d1e2f3a-4b5c-4d6e-8f7a-9b0c1d2e3f02",
  "voucherName": "Ferragosto",
  "validityPeriods": [
    {
      "validFrom": "2026-08-12T00:00:00.000Z",
      "validTo": "2026-08-16T00:00:00.000Z",
      "daysOfWeek": [1, 2, 3, 4, 5],
      "timeSlots": [{ "from": "09:00", "to": "13:00" }]
    }
  ],
  "discountType": "Fixed",
  "discountValue": 5,
  "discountScope": "Services",
  "maxUseQuantity": 50,
  "isEnabled": false,
  "version": "5be2d0f7"
}
```

| Campo | Tipo | Note |
|---|---|---|
| `version` | string | La versione dello sconto, che il Salva rimanda in `If-Match`. Sempre presente. |

### Salvataggio

`POST /api/vouchers/update-voucher-item` con tutti i campi del pannello (`idVoucher` della lettura) e l'header
`If-Match: "5be2d0f7"`. Risposta 204 senza corpo con l'header `ETag: "<nuova versione>"`.

| Status | `code` | Quando |
|---|---|---|
| 409 | `resource.version_conflict` | La versione in `If-Match` non è più quella dello sconto, o manca `If-Match`. Niente cambia. |

Gli altri errori del salvataggio (404 `resource.not_found` per uno sconto che non c'è, 400 `validation.failed` con
`errors[]`) non fanno parte di questa sezione.

### Cosa fa il frontend

Con 409 `resource.version_conflict` il pannello di modifica si chiude, compare l'avviso "Nel frattempo un altro utente
ha modificato questi dati: li abbiamo ricaricati. Rifai le tue modifiche." e la tabella si ricarica; le modifiche scritte si
perdono. Riaprendo la matita il pannello rilegge lo sconto con `get-selected-item`. Il codice non compare nel messaggio
sopra i bottoni.

## Verifica

API 1, con un token con il permesso di modificare gli sconti (oggi `CatalogPricing`, quello di
`PUT /api/catalog/discounts/{id}`):

1. `get-selected-item` risponde con `version` (per esempio `v1`); `update-voucher-item` con `If-Match: "v1"`: 204 con
   l'header `ETag` di una versione nuova, diversa da `v1`; una nuova `get-selected-item` risponde con quella versione.
2. Lo stesso `update-voucher-item` con `If-Match: "v1"` dopo il primo: 409 `resource.version_conflict` senza
   `errors[]`; lo sconto non cambia.
3. `update-voucher-item` senza `If-Match`: 409 `resource.version_conflict`.
4. Due letture dello stesso sconto (versione `v1` per entrambe); la prima salva una fascia oraria in più: 204; la
   seconda salva con `If-Match: "v1"`: 409 `resource.version_conflict` e lo sconto resta quello del primo salvataggio.
5. Cambiare solo un dato che il pannello non legge (per esempio `used_count`) non cambia la versione: un
   `update-voucher-item` con la versione letta prima riesce.
6. `update-voucher-item` con un `idVoucher` di un altro stabilimento o eliminato: 404 `resource.not_found`, anche con
   un `If-Match` qualunque.
