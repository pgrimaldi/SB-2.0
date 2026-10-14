# Settori citati dai listini e non piu' esistenti

> ## ⚠️ SUPERATO il 2026-09-30 — non c'e' piu' nulla da compilare
>
> Il committente ha stabilito che **il listino e' la lista dei settori**: un nome citato che
> nell'anagrafica non esiste va **creato**, non cercato. Le 263 voci si recuperano cosi',
> senza che nessuno debba ricostruire che fine ha fatto ogni settore.
>
> Questo documento resta come **fotografia del disallineamento** e per i dati che contiene
> (quali nomi, su quali stabilimenti, con quanto traffico). Vedi
> [`_mancanti-da-sistemare.md`](_mancanti-da-sistemare.md) voce **B11**.

> **Da compilare a mano**, una riga alla volta: per ogni nome che i listini citano, indicare
> a quale settore attuale corrisponde — oppure che non corrisponde a nessuno.
>
> Prodotto da `utility/01-migrazione-test/scripts_settori_da_verificare.py`.

## Perche' e' successo (spiegato dal committente, 2026-09-30)

> "La lista di settori e' quella che sta nel listino. Il problema si e' verificato perche'
> fino a pochi mesi fa **modificando il settore non modificava il listino**; poi il codice e'
> stato modificato e ora funziona, ma molti dati sono rimasti disallineati. Stessa cosa con
> stagioni e periodi."

Due conseguenze utili per chi compila:

1. **Il disallineamento non cresce piu'.** I listini toccati dopo la correzione sono
   allineati; questi sono quelli che nessuno ha piu' aperto da allora.
2. **Il nome citato non e' inventato**: era il nome vero di un settore, in un momento
   passato. La domanda non e' mai "cosa significa", ma **"che fine ha fatto"**.

## Il problema in due righe

Nel legacy un prezzo indica il proprio settore **per nome**, come stringa ripetuta dentro
`price_lists.mainPrices`; i settori veri stanno in `sectors_rows` e non c'e' alcuna chiave
fra le due. Rinominare un settore lascia i listini a citare il nome vecchio, e nulla lo
segnala. Nel nuovo schema `sector_id` e' una chiave esterna obbligatoria, quindi ogni nome
che non si risolve fa **perdere quella riga di prezzo**.

## Cosa e' gia' stato risolto, e cosa no

| | Voci | |
|---|---:|---|
| Differenze di **formato** | 40 | risolte in automatico (`FILA-2` = `FILA - 2`) |
| **Da verificare qui** | 263 | richiedono di sapere cosa e' successo a quel settore |
| di cui su stabilimenti **operativi** | 169 | approvati, non cancellati, con prenotazioni dal 2025-09 |

> **Perche' non le risolve un algoritmo.** Le coppie piu' somiglianti contengono sia refusi
> sia settori del tutto diversi, e dall'esterno non si distinguono: `Comitivo`/`Comitiva`
> (88%) sono lo stesso settore, `Caraibi3`/`Caraibi2` (88%) sono due settori diversi,
> `Dalla 2° Fila in poi`/`Dalla 3° Fila in poi` (95%) pure. Attribuire un prezzo al settore
> sbagliato e' peggio che lasciarlo mancante: un prezzo che manca si nota, uno sbagliato
> fattura male senza che nessuno se ne accorga.

## Come compilare

Ogni scheda qui sotto e' uno stabilimento. La riga sotto il titolo elenca **i settori che ha
davvero oggi**; la tabella elenca **i nomi che i suoi listini citano** e che fra quelli non
si trovano. Per ogni riga della tabella va detto a cosa corrisponde quel nome.

Nella colonna **Corrisponde a** si scrive una di tre cose:

| Cosa scrivere | Quando | Cosa succede |
|---|---|---|
| il **nome esatto** di un settore fra quelli elencati sopra la tabella | e' lo stesso settore, chiamato in un altro modo | il prezzo viene recuperato e collegato a quel settore |
| **`ELIMINATO`** | quel settore non esiste piu' | il prezzo resta fuori, ed e' corretto cosi' |
| **`NUOVO`** | il settore dovrebbe esistere ma manca dall'anagrafica | va creato prima di rimigrare |

La colonna **Note** e' libera: serve a chi rileggera' fra sei mesi per capire la scelta.

### Esempio svolto

Preso dal primo stabilimento di questo elenco. Settori che ha oggi: `Spiaggia`,
`Terrazze Belvedere`, `Terrazze Santa Teresa`.

| Nome citato dai listini | Volte | Corrisponde a | Note |
|---|---:|---|---|
| `Terrazza Santa Teresa` | 16 | `Terrazze Santa Teresa` | singolare/plurale, stesso settore |
| `Piscina - Solarium Grecale - Terrazze Belvedere` | 16 | `Terrazze Belvedere` | nome lungo poi accorciato |
| `Fila 1-2` | 16 | `ELIMINATO` | vecchio assetto a file, sostituito dalle zone |
| `Dalla 3^ fila in poi` | 16 | `ELIMINATO` | idem |

Il ragionamento dietro ogni riga:

- **`Terrazza Santa Teresa`** differisce di una lettera dal settore esistente, singolare
  contro plurale: qualcuno ha corretto il nome nell'anagrafica e i listini sono rimasti
  indietro. Scrivendo il nome nuovo si recuperano 16 prezzi.
- **`Piscina - Solarium Grecale - Terrazze Belvedere`** contiene per intero il nome di un
  settore esistente, e la lettura piu' probabile e' un accorciamento. **Ma potrebbe anche
  essere che piscina e solarium fossero zone distinte, poi separate** — e allora la risposta
  e' un'altra. E' esattamente per righe come questa che serve una persona: dall'esterno si
  arriva a un'ipotesi, non alla certezza.
- **`Fila 1-2`** e **`Dalla 3^ fila in poi`**: nessuno dei settori attuali e' una fila. Lo
  stabilimento e' passato da un assetto per file a uno per zone, e quei prezzi si
  riferiscono a una configurazione che non esiste piu'. Recuperarli vorrebbe dire inventare
  un settore, quindi `ELIMINATO`.

Una raccomandazione: nel dubbio scrivere `ELIMINATO` e una nota, non un nome "che sembra
giusto". Un prezzo che manca si nota alla prima occasione e si sistema; un prezzo agganciato
al settore sbagliato fattura male senza che nessuno se ne accorga.

---

## `10622` — La Baia Blu

🟢 operativo · stato **approvato** · 164 prenotazioni totali, 31 dal 2025-09 · 16 listini coinvolti

Settori attualmente dichiarati: `Spiaggia`, `Terrazze Belvedere`, `Terrazze Santa Teresa`

| Nome citato dai listini | Volte | Corrisponde a | Note |
|---|---:|---|---|
| `Fila 1-2` | 16 | | |
| `Dalla 3^ fila in poi` | 16 | | |
| `Piscina - Solarium Grecale - Terrazze Belvedere` | 16 | | |
| `Terrazza Santa Teresa` | 16 | | |

## `1` — Goa Candolim Beach fast

⚪ cancellato · stato **in attesa** · 0 prenotazioni totali, 0 dal 2025-09 · 17 listini coinvolti

Settori attualmente dichiarati: `fascia arancione`, `fascia bianca`, `fascia gialla`

| Nome citato dai listini | Volte | Corrisponde a | Note |
|---|---:|---|---|
| `Row 2-5` | 17 | | |
| `Row 6-9` | 17 | | |
| `Row 1` | 17 | | |

## `115` — Lido Tuareg Beach

🟢 operativo · stato **approvato** · 2486 prenotazioni totali, 1088 dal 2025-09 · 22 listini coinvolti

Settori attualmente dichiarati: `Adriatico`, `Caraibi1`, `Caraibi2`, `Ionio`, `Maldive1`, `Maldive2`, `Maldive3`, `Tirreno`

| Nome citato dai listini | Volte | Corrisponde a | Note |
|---|---:|---|---|
| `Caraibi3` | 18 | | |
| `Maldive3 Plus` | 6 | | |
| `Canarie` | 1 | | |

## `133` — Lido San Felice

🟢 operativo · stato **approvato** · 1395 prenotazioni totali, 637 dal 2025-09 · 5 listini coinvolti

Settori attualmente dichiarati: `Fila 1-2-3`, `Fila 4-5-6`, `Fila 7-8-9`

| Nome citato dai listini | Volte | Corrisponde a | Note |
|---|---:|---|---|
| `Fila 1-2` | 5 | | |
| `Fila 3-4-5` | 5 | | |
| `Altre file` | 5 | | |

## `10556` — La Lampara - Passoscuro

🟢 operativo · stato **approvato** · 9856 prenotazioni totali, 4155 dal 2025-09 · 10 listini coinvolti

Settori attualmente dichiarati: `RIVA 1 lettino`, `RIVA 1 lettino plus`, `Settore 1 lettino plus	`, `Settore 2 lettini`, `Settore 2 lettini plus`, `Settore 2 sdraio`, `Settore Ombrellone e lettino`, `Settore Ombrellone lettino e sdraio`

| Nome citato dai listini | Volte | Corrisponde a | Note |
|---|---:|---|---|
| `Settore ombrellone e lettino plus` | 5 | | |
| `RIVA 2 lettini` | 4 | | |
| `RIVA 2 lettini plus` | 4 | | |
| `Fila 1-2` | 1 | | |
| `Altre file` | 1 | | |

## `67` — Stabilimento Sicilia

⚪ cancellato · stato **approvato** · 0 prenotazioni totali, 0 dal 2025-09 · 4 listini coinvolti

Settori attualmente dichiarati: `Fila 1`

| Nome citato dai listini | Volte | Corrisponde a | Note |
|---|---:|---|---|
| `Fila 2` | 4 | | |
| `Fila 3` | 4 | | |
| `Altre file` | 4 | | |

## `109` — Goa Family beach

⚪ cancellato · stato **approvato** · 0 prenotazioni totali, 0 dal 2025-09 · 10 listini coinvolti

Settori attualmente dichiarati: `3`, `5`, `row1`, `row2-5`

| Nome citato dai listini | Volte | Corrisponde a | Note |
|---|---:|---|---|
| `row6-8` | 10 | | |

## `131` — Bagni Piccolo Jolly

🟢 operativo · stato **approvato** · 3792 prenotazioni totali, 1342 dal 2025-09 · 8 listini coinvolti

Settori attualmente dichiarati: `Settore Arancio`, `Settore Blu`, `Settore Giallo`, `Settore Verde`

| Nome citato dai listini | Volte | Corrisponde a | Note |
|---|---:|---|---|
| `Fila unica` | 8 | | |

## `64` — Club Forest

⚪ cancellato · stato **approvato** · 0 prenotazioni totali, 0 dal 2025-09 · 7 listini coinvolti

Settori attualmente dichiarati: `Row  2`, `Row 2-5`, `Row 6-9`, `dsd`

| Nome citato dai listini | Volte | Corrisponde a | Note |
|---|---:|---|---|
| `Row  1` | 7 | | |

## `110` — Bagno Argo

⚪ senza prenotazioni recenti · stato **approvato** · 0 prenotazioni totali, 0 dal 2025-09 · 7 listini coinvolti

Settori attualmente dichiarati: `Fila unica`

| Nome citato dai listini | Volte | Corrisponde a | Note |
|---|---:|---|---|
| `Cabina con Doccia` | 5 | | |
| `Fila 1` | 2 | | |

## `114` — Lido Stella Maris

🟢 operativo · stato **approvato** · 1455 prenotazioni totali, 621 dal 2025-09 · 6 listini coinvolti

Settori attualmente dichiarati: `1° FILA`, `2° & 3° FILA`, `4° FILA IN POI`

| Nome citato dai listini | Volte | Corrisponde a | Note |
|---|---:|---|---|
| `Fila unica` | 6 | | |

## `10559` — Lido Medusa

🟢 operativo · stato **approvato** · 781 prenotazioni totali, 332 dal 2025-09 · 6 listini coinvolti

Settori attualmente dichiarati: `Comitiva`, `Fila 1`, `Fila 2`, `Fila 3`, `Fila 4`, `Fila 5`, `Fila 6`

| Nome citato dai listini | Volte | Corrisponde a | Note |
|---|---:|---|---|
| `Comitivo` | 6 | | |

## `10564` — Barbatoja Beach Village

🟢 operativo · stato **approvato** · 690 prenotazioni totali, 290 dal 2025-09 · 2 listini coinvolti

Settori attualmente dichiarati: `Parcheggio generico`

| Nome citato dai listini | Volte | Corrisponde a | Note |
|---|---:|---|---|
| `Fila 1` | 2 | | |
| `Fila 2` | 2 | | |
| `Altre file` | 2 | | |

## `10588` — Marine Village

🟢 operativo · stato **approvato** · 7090 prenotazioni totali, 2981 dal 2025-09 · 6 listini coinvolti

Settori attualmente dichiarati: `1° e 2° fila con 2 lettini`, `1° e 2° fila con 3 lettini`, `Altre file con 2 lettini`, `Altre file/Prato con 3 lettini`, `Area Ombrelloni Spiaggia Natura Libera`, `Area lettini riva`, `Prato (2 extra)`, `Zona Pet`, `Zona comfort`, `Zona comfort 2`, `Zona comfort con 4 lettini`

| Nome citato dai listini | Volte | Corrisponde a | Note |
|---|---:|---|---|
| `Fila 1` | 3 | | |
| `Area lettini` | 2 | | |
| `Fila 2` | 1 | | |

## `108` — AccountProva93

⚪ cancellato · stato **approvato** · 0 prenotazioni totali, 0 dal 2025-09 · 5 listini coinvolti

Settori attualmente dichiarati: `Altre file`, `Fila 1`

| Nome citato dai listini | Volte | Corrisponde a | Note |
|---|---:|---|---|
| `Fila 2` | 5 | | |

## `10548` — Pink Village

🟢 operativo · stato **approvato** · 3480 prenotazioni totali, 1328 dal 2025-09 · 4 listini coinvolti

Settori attualmente dichiarati: `Altre file`, `Fila 1`, `Fila 2`, `Fila 3`

| Nome citato dai listini | Volte | Corrisponde a | Note |
|---|---:|---|---|
| `Lettino battigi` | 4 | | |

## `10766` — Il Fico a Mare

🟢 operativo · stato **approvato** · 267 prenotazioni totali, 267 dal 2025-09 · 4 listini coinvolti

Settori attualmente dichiarati: `1° Fila`, `2° Fila`, `Dalla 3° Fila in poi`

| Nome citato dai listini | Volte | Corrisponde a | Note |
|---|---:|---|---|
| `Dalla 2° Fila in poi` | 4 | | |

## `228` — Stabilimento balneare Demo

🟢 operativo · stato **approvato** · 1748 prenotazioni totali, 1302 dal 2025-09 · 1 listini coinvolti

Settori attualmente dichiarati: `Altre file`, `Cabine`, `Fila 1`, `Lettini`, `Tende`

| Nome citato dai listini | Volte | Corrisponde a | Note |
|---|---:|---|---|
| `Cabina standard ` | 1 | | |
| `Cabina con Doccia` | 1 | | |
| `Armadio` | 1 | | |

## `58` — On the beach Club

🟢 operativo · stato **approvato** · 1900 prenotazioni totali, 723 dal 2025-09 · 2 listini coinvolti

Settori attualmente dichiarati: `Fila 1`, `Fila 2-3`, `Fila 4-5`, `Fila 6-7-8`

| Nome citato dai listini | Volte | Corrisponde a | Note |
|---|---:|---|---|
| `Fila 6-7` | 2 | | |

## `60` — Stabilimento di prova

⚪ cancellato · stato **approvato** · 0 prenotazioni totali, 0 dal 2025-09 · 1 listini coinvolti

Settori attualmente dichiarati: `Settore bianco`

| Nome citato dai listini | Volte | Corrisponde a | Note |
|---|---:|---|---|
| `Settore giallo` | 1 | | |
| `Settore arancione` | 1 | | |

## `10720` — Il trentanove Beach

🟢 operativo · stato **approvato** · 113 prenotazioni totali, 113 dal 2025-09 · 1 listini coinvolti

Settori attualmente dichiarati: `Dalla 3° fila in poi`, `Fila 1`, `Fila 2`

| Nome citato dai listini | Volte | Corrisponde a | Note |
|---|---:|---|---|
| `A1 - 1 Fila` | 1 | | |
| `A2 - 1 Fila` | 1 | | |

## `176` — Lido Degli Scogli

🟢 operativo · stato **approvato** · 1318 prenotazioni totali, 13 dal 2025-09 · 1 listini coinvolti

Settori attualmente dichiarati: `Fila 1`, `Fila 2`, `Fila 3`

| Nome citato dai listini | Volte | Corrisponde a | Note |
|---|---:|---|---|
| `Fila 1-2` | 1 | | |

## `10604` — Rosapineta Camping Village

🟢 operativo · stato **approvato** · 1723 prenotazioni totali, 950 dal 2025-09 · 1 listini coinvolti

Settori attualmente dichiarati: `Fila 1`, `Fila 2`, `Fila 3 e 4`

| Nome citato dai listini | Volte | Corrisponde a | Note |
|---|---:|---|---|
| `Altre file` | 1 | | |

## `10793` — Eden Sport & Salute

🟢 operativo · stato **approvato** · 587 prenotazioni totali, 587 dal 2025-09 · 1 listini coinvolti

Settori attualmente dichiarati: `Altre file`, `Bordo piscina`

| Nome citato dai listini | Volte | Corrisponde a | Note |
|---|---:|---|---|
| `Fila unica` | 1 | | |
