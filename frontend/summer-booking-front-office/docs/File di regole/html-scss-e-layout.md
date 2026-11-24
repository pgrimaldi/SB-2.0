# HTML, SCSS e layout

Regole per HTML e SCSS: nomi delle classi, dimensione dei file, stili condivisi, mockup, misure, risoluzioni e focus.

## SCSS

- I nuovi componenti usano file SCSS, non CSS: Angular è configurato con `inlineStyleLanguage: scss`.
- Nei nomi delle classi le parole si separano con il doppio underscore `__` (es. `home__information__content`,
  `home__intermezzo__panel`). Il numero di parole dipende dal caso: nessun limite né schema fisso.
- Ogni classe inizia con il nome del componente o della vista, derivato dal nome del file: `header.ts` → `header`,
  `header__logo`; `login-dialog.ts` → `login__dialog__panel`; `carousel.ts` → `carousel__slide`; `home.ts` →
  `home__hero`, `home__booster__text`. Nessun prefisso estraneo (`hub__`, `app__`).
- Un elemento dell'HTML ha una classe solo se lo SCSS del componente ha una regola che la usa (o se un test la usa per
  trovare l'elemento). Un elemento senza stile proprio resta senza classe (es. un `<p>` di testo normale).
- Si scrivono solo gli stili necessari. Il CSS custom serve soprattutto per layout, responsive, branding e requisiti
  specifici; non ricrea componenti già disponibili nelle librerie UI. Componenti e token di Angular Material vengono
  prima del CSS custom.
- Gli SCSS restano contenuti: prima di aggiungere una sezione si riusano le famiglie di classi comuni della pagina e si
  scrivono solo le differenze (in home: `home__information` per le sezioni di card, `home__intermezzo` per i riquadri
  sfumati, `home__action__desktop` / `home__action__mobile` per i pulsanti responsive, `home__information__carousel`
  per i caroselli).
- Un solo blocco `@media` per breakpoint.
- Posizionamento, spaziatura e breakpoint con SCSS moderno, mai con attributi HTML obsoleti come `align`, `left` o
  `right`.
- Proprietà logiche invece di sinistra/destra, per l'arabo: vedi `traduzioni-e-lingue.md`.

## Limite di 150 righe

- Un file `.scss` non supera le 150 righe di codice, contate senza righe vuote e commenti.
- Se le supera, va aggiunto a `docs/POSSIBLE_FUTURE_REFACTORING.md` (file locale, non versionato) con le possibili
  ottimizzazioni, dando la precedenza ai componenti e ai token di Angular Material rispetto al CSS custom.

## Stili condivisi dell'app (`styles/`)

- `src/app/styles/` contiene gli stili SCSS dell'app condivisi da più pagine, come mixin. Esempi:
  - `_table-page.scss`: layout delle pagine con titolo e tabella; `@include page.table-page(warehouse);` genera
    `.warehouse` e `.warehouse__title`;
  - `_info-button.scss`: la (i) accanto a un'etichetta che apre `app-info-popup`; `@include info.info-button;`
    sull'`app-icon-button`, sempre attiva, anche nei form in sola lettura;
  - `_price-grid.scss`: le griglie dei prezzi del Listino (`scroll`, `table`, `price-table`, `price-field`).
- Sono dell'app, non della libreria: le misure seguono il nostro header, il nostro menu e gli schermi che dobbiamo far
  stare. Se i file diventano molti si raggruppano in sottocartelle.
- Gli stili dei wrapper restano nella loro famiglia (`components/shared/ui/<famiglia>/_*.scss`).

## HTML

- HTML semantico: `header`, `nav`, `main`, `section`, `article`, `footer`, heading corretti, `button` per le azioni e
  `a` / `routerLink` per la navigazione.

## Mockup e misure

- Da un mockup si riprendono lo stile e le posizioni (un bottone a destra sta a destra, l'ordine e i raggruppamenti
  degli elementi), non le misure al pixel.
- Bottoni, titoli, etichette e campi usano le misure standard dei wrapper anche quando il mockup ne disegna di diverse
  (nel gestionale: bottoni da 1.75rem con testo da 0.75rem, una delle 4 misure di `app-button`; X e cestini con
  `app-icon-button`).
- Non si transige invece, in tutte le lingue e a tutte le larghezze: le parole non vanno a capo (messaggi di errore
  compresi), i bottoni non sbordano, i testi dei campi non vengono tagliati. Se una frase non ci sta si accorcia la
  traduzione; se un gruppo non ci sta, va intero alla riga sotto.
- Spaziature uniformi: sezioni dello stesso tipo (es. i set di card `home__information`, gli intermezzi
  `home__intermezzo` come il booster e `home__growth`) condividono le stesse regole di margine e padding, anche quando
  il riferimento è incoerente.
- Gli elementi dello stesso tipo hanno le stesse dimensioni in tutta la pagina (es. tutte le `app-card`). Quando
  dipendono dal contenuto non si fissa un valore: la direttiva `appEqualHeight`
  (`components/shared/layout/equal-height/`) misura il più alto e lo applica a tutti.
- Desktop e mobile restano coerenti con il riferimento.

## Risoluzioni e unità

- **Requisito obbligatorio**: resa ottimale a 720p, 1080p, 1440p e 2160p e sui principali dispositivi, in portrait e
  in landscape.
- Ogni blocco si verifica almeno a 1280×720, 1920×1080, 2560×1440, 3840×2160, telefono portrait/landscape e tablet
  portrait/landscape.
- Niente pixel fissi negli stili: `rem`, percentuali, unità viewport (`svh`, `vw`), `clamp()` / `min()`. Breakpoint
  in `rem`.
- La root `font-size` in `styles.scss` è fluida (`clamp(100%, 0.625vw, 150%)`): resta al valore dell'utente fino a
  2560px di larghezza (come il riferimento, che tra 1920 e 2560px non cambia) e cresce fino a 1,5x a 3840px, così
  tutto ciò che è in `rem` scala sugli schermi 4K.
- Il riferimento si misura sempre anche a 1920px: alcune sue pagine cambiano misure da 1920px in su (es. tabelle con
  testo 18px e contenitore 1522px). Quei valori si replicano con `@media (width >= 120rem)`.
- Oltre i 2560px lavora la scala fluida: il riferimento sopra i 2560px torna alle misure piccole e non si replica.

## Movimento e focus

- Le animazioni non essenziali rispettano `prefers-reduced-motion`.
- Il contorno di focus da tastiera è uno solo per tutta l'app: mixin `focus-ring` del tema
  (`components/shared/ui/theme/_theme.scss`, incluso in `styles.scss`), 0.125rem pieno nel colore del testo, staccato
  di 0.125rem.
- Un componente non riscrive il contorno: imposta solo ciò che cambia con `--focus-ring-color` e `--focus-ring-offset`
  sull'elemento (es. il carosello usa l'azzurro secondario, il menu del gestionale il contorno interno con
  `-0.125rem`).
- La regola ha specificità zero: gli indicatori di focus di Material e le regole dei componenti hanno sempre la
  precedenza. Fa eccezione `app-icon-button`, che è un bottone Material e tiene il suo `:focus-visible`.
