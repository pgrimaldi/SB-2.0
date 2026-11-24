# Asset e icone

Regole per asset, font, immagini, bandiere e icone del gestionale con i loro nomi.

## Asset

- Gli asset dell'app stanno sotto `src/assets` e sono inclusi dalla build Angular.

## Font

- Il font del progetto è **Outfit**, in `src/assets/fonts/outfit/`: si usa il variable font locale, non una dipendenza
  remota.
- Outfit si dichiara globalmente con `@font-face` (`styles.scss`) e si usa anche nella configurazione tipografica di
  Angular Material.
- Font, pesi, dimensioni e colori dei testi restano il più vicini possibile al riferimento: si misurano sul riferimento
  `font-family`, `font-weight`, `font-size`, `line-height` e colore, e si confronta la larghezza dei testi.
- Anche lo spessore del tratto delle icone accanto ai testi incide sulla resa e va confrontato con il riferimento.

## Immagini

- Le immagini dell'hub stanno direttamente in `src/assets/images/hub/`, senza sottocartelle.
- Le immagini generali (es. le bandiere delle lingue `flag-<codice>.svg`) stanno direttamente in `src/assets/images/`.
- Le varianti desktop e mobile, quando fornite, restano file separati.
- Le immagini hanno `width` e `height` quando possibile; `alt` descrittivo per le immagini significative, `alt=""` per
  quelle solo decorative.
- La hero above-the-fold non è in lazy loading; le immagini successive lo usano quando serve.
- Niente immagini troppo pesanti e niente layout shift, senza sacrificare la qualità più del necessario.

## Bandiere dei paesi

- Le bandiere dei paesi (es. per il prefisso del telefono) sono `/assets/flags/<codice ISO in minuscolo>.svg`: la
  build le copia da `node_modules/flag-icons/flags/4x3` (libreria `flag-icons`), quindi non sono nel repository.

## Icone del gestionale

- Le icone con lo stesso disegno e lo stesso colore sono un solo file condiviso, `GeneralIcon_Nome.svg` (es.
  `GeneralIcon_Close.svg`, `GeneralIcon_Calendar.svg`, `GeneralIcon_EyeClose.svg`, `GeneralIcon_Bold.svg`), usato da
  tutte le pagine. Prima di aggiungere un'icona si controlla se esiste già una `GeneralIcon_` identica.
- Le icone usate da una sola pagina, e anche quelle con lo stesso disegno ma un colore diverso, sono
  `Pagina_Icona.svg`, con il nome della pagina nel codice (es. `Database_Restore.svg`, `Taxation_Printer.svg`,
  `Seasons_AddInterval.svg`). I popup e i form di una pagina usano le icone della pagina.
- Gli elementi condivisi aggiungono la parte di applicazione: `Manager_Header_Icona.svg`, `Manager_Menu_Icona.svg`,
  `Manager_Table_Icona.svg` (azioni di riga comuni di `standardRowActions`). Per l'hub: `Hub_Header_Icona.svg`.
- La home pubblica e le bandiere tengono i loro nomi.
- Come i wrapper ricevono le icone (`matIcon` / `pathIcon`): vedi `componenti-e-wrapper.md`.
