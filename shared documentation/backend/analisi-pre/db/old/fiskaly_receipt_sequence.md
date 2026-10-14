# Tabella `fiskaly_receipt_sequence`

> Fonti: schema e dati reali (100 righe, `AUTO_INCREMENT=129`), model
> [`back-pre/app/models/fiskalyReceiptSequence.model.js`](../../back-pre/app/models/fiskalyReceiptSequence.model.js)
> (`{ freezeTableName: true, timestamps: false }` — nessuna colonna timestamp),
> uso in
> [`Service/fiskalyReceiptManagerService.js`](../../back-pre/app/Service/fiskalyReceiptManagerService.js):
> - `peekNextDocumentNumber(systemId, day)` — legge `last_number` (o `0`) e restituisce `+1`
>   **senza consumare** (usato allo step 4 di `createReceipt` per assegnare il numero progressivo
>   candidato del documento);
> - `commitDocumentNumber(systemId, day, number)` — `findOrCreate` con `defaults: { last_number: n }`
>   e, se la riga esiste con `last_number < n`, `update({ last_number: n })` (monotòno: non abbassa
>   mai il valore) — chiamato **solo dopo una firma riuscita**, così un tentativo fallito non "brucia"
>   un numero e i retry dello stesso giorno riusano lo stesso valore;
> - `getNextDocumentNumber(systemId)` — variante `findOrCreate` + `increment` atomico con gestione
>   `SequelizeUniqueConstraintError` (percorso alternativo/legacy: il flusso attuale usa peek+commit).
> Associazioni in [`models/index.js`](../../back-pre/app/models/index.js) (righe 361-365):
> `belongsTo(db.fiskalyCashRegister, { foreignKey: "system_id", targetKey: "system_id" })`.
> Nessun uso diretto dal frontend.
>
> **Nota di contesto** — **contatore progressivo giornaliero del numero documento commerciale per
> registratore di cassa**: **1 riga per (`system_id` × `date`)**, con vincolo `UNIQUE(system_id,
> date)`. `last_number` è l'ultimo numero fiscale emesso quel giorno su quella cassa; il numero va
> sul documento come [`fiskaly_receipt.document_number`](fiskaly_receipt.md) (che quindi **non** è
> univoco: riparte da 1 ogni giorno e per ogni cassa).
>
> **Dominio nella riscrittura**: **C4 — modulo `Fiskaly`** — tabella di sequenza/contatore;
> **senza** `deleted_at` ([`docs/03-gestione-dati.md` §3.5](../../docs/03-gestione-dati.md)).
> Concettualmente analoga a [`booking_logs`](booking_logs.md) (contatore) e a
> [`receipt_serial_numbers`](receipt_serial_numbers.md) (progressivo scontrino su stampante RT).

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Descrizione**: chiave tecnica; la chiave "vera" è la coppia (`system_id`, `date`).
- **Esempio di valori**: 1, 2, 3, ..

### system_id
- **Tipo**: `varchar(36)`, `NOT NULL` — UUID v7
- **Nullabile**: No
- **Relazioni**: FK reale `fiskaly_receipt_sequence_ibfk_1` →
  [`fiskaly_cash_register.system_id`](fiskaly_cash_register.md), `ON UPDATE CASCADE` (senza
  `ON DELETE`). Parte dell'indice `UNIQUE(system_id, date)`.
- **Descrizione**: registratore di cassa Fiskaly a cui la sequenza appartiene.
- **Esempio di valori**: `"019e8dc7-ae38-782a-9b97-5fb886342455"`,
  `"019eb70f-b0aa-7936-b1a4-3121fa22dd74"`, `"019e8e0e-a584-75d8-94ca-ed74795abc0e"`

### date
- **Tipo**: `date` (`DATEONLY`), `NOT NULL`
- **Nullabile**: No
- **Descrizione**: **giorno fiscale** a cui si riferisce il contatore. Calcolato come
  `signedMoment.format('YYYY-MM-DD')` nel fuso **Europe/Rome** (`peek`/`commit`), oppure
  `new Date().toISOString().slice(0,10)` cioè **UTC** in `getNextDocumentNumber` (incoerenza —
  vedi criticità). Parte dell'indice `UNIQUE(system_id, date)`.
- **Esempio di valori**: `2026-07-27`, `2026-07-26`, `2026-06-17`

### last_number
- **Tipo**: `int`, `NOT NULL`, `DEFAULT 0`
- **Nullabile**: No
- **Descrizione**: ultimo numero progressivo di documento **effettivamente emesso** quel giorno su
  quella cassa. Il prossimo documento userà `last_number + 1`. Aggiornato in modo **monotòno**
  (`commitDocumentNumber` non lo abbassa mai) e solo su firma riuscita.
- **Esempio di valori**: `0` (default), `1`, `5`, `9`, `11`, `52` (max osservato)

---

## Osservazioni verificate nel codice (criticità)

- **Incoerenza sul fuso della `date`**: `peekNextDocumentNumber`/`commitDocumentNumber` usano
  **Europe/Rome**, `getNextDocumentNumber` usa **UTC** (`toISOString().slice(0,10)`). Se entrambi i
  percorsi fossero attivi a cavallo di mezzanotte, potrebbero incrementare righe di date diverse per
  lo stesso giorno fiscale.
- **Due implementazioni** dello stesso contatore (`peek`+`commit` vs `getNextDocumentNumber`): nel
  target tenerne una sola, con incremento atomico lato DB (`INSERT … ON CONFLICT DO UPDATE SET
  last_number = GREATEST(last_number, EXCLUDED.last_number)` o `RETURNING`).
- **Nessun timestamp**: non si sa *quando* una sequenza è stata toccata l'ultima volta (utile per
  audit/riconciliazione). Aggiungere `updated_at` nel target.
- **`system_id` FK senza `ON DELETE`**: cancellare una cassa lascerebbe righe orfane
  (le casse comunque si soft-deletano, non si cancellano).
- **Contatore che non è enum**: `last_number` è un intero progressivo, non un vocabolario.
