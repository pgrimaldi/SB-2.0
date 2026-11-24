# Componenti e wrapper

Librerie UI, regole dei wrapper (famiglie, icone, testi, colori), popup, bottoni, titolo di pagina, la (i) informativa
e i wrapper delle librerie ammesse.

## Librerie UI

- Ordine di valutazione: prima Angular Material, poi un componente custom dentro un wrapper. Material è la scelta
  predefinita.
- La scelta si valuta, quando possibile, a livello di pagina, per la coerenza visiva.
- Prima di scrivere un controllo custom complesso si verifica se Material offre già una soluzione adeguata.
- Non si aggiungono librerie: quelle ammesse oltre ad Angular sono in `architettura.md`.

## Wrapper

- Ogni componente di Material o di un'altra libreria UI si usa attraverso un wrapper dell'app, anche se compare una
  sola volta. `views` e componenti di dominio non importano mai direttamente componenti di libreria.
- I wrapper stanno in `components/shared/ui/<famiglia>/` e sono pensati per una futura libreria esportabile:
  funzionano solo con quello che ricevono dai parametri, senza importare behaviours, services o entità dell'app.
- Solo `components/shared/ui/` andrà nella libreria; il resto di `components/shared/` è dell'app (vedi
  `architettura.md`).
- Il wrapper espone proprietà, eventi e tipi definiti da Summer Booking e tiene al suo interno il collegamento con la
  libreria sottostante: non espone tipi o configurazioni della libreria quando non serve.
- L'API pubblica del wrapper resta stabile, così la libreria sottostante si può sostituire senza modifiche diffuse.
- Il catalogo di tutti i wrapper, con i loro parametri e l'ordine delle icone, è `docs/WRAPPER_COMPONENTS.docx`: si
  aggiorna a ogni modifica di un wrapper (nuovo wrapper, parametro, evento, variante).

## Famiglie

- Una cartella per famiglia, con il nome al plurale: `buttons`, `cards`, `carousels`, `checkboxes`, `chips`,
  `datepickers`, `dialogs`, `expansion-panels`, `forms`, `inputs`, `maps`, `menus`, `radios`, `selects`, `spinners`,
  `tables`, `tabs`, `titles`, `toggles`, `uploads`.
- Esempi: `forms` ha `app-form-row` (righe "etichetta a sinistra, controlli a destra"), `maps` `app-location-map`,
  `tabs` `app-tab-nav` (tab che navigano fra rotte figlie), `uploads` `app-file-drop-zone`, `radios`
  `app-radio-group`.
- Cartelle di supporto della libreria: `data` (`DATA_RELOAD`: la libreria definisce il contratto, l'app lo riempie in
  `app.config.ts`), `icons`, `texts` e `theme`.
- Il codice comune a più wrapper della stessa famiglia sta una volta sola, a livello di famiglia (es.
  `inputs/_field.scss`, `dialogs/_popup.scss`, `cards/_card-surface.scss`).
- Una variante di un elemento con una struttura diversa è un nuovo wrapper fratello nella stessa famiglia, non una
  modifica di quello esistente (es. `app-warning-popup` accanto ad `app-confirm-popup`, `app-card-logo` accanto ad
  `app-card`); lo stile comune va nel mixin della famiglia.

## Icone dei wrapper

- I wrapper non hanno icone proprie: le ricevono da due parametri che sono elenchi, `matIcon` (nomi di icone
  Material, mostrate con `mat-icon`) oppure `pathIcon` (percorsi di immagini nostre in `assets/`).
- Anche una sola icona si passa come elenco. I due parametri non si mescolano: `matIcon` vince per intero, senza
  nessuno dei due non c'è icona.
- `matIcon` richiede il font Material Symbols, che l'app non carica.
- Ogni wrapper mette le icone al loro posto in base alla posizione, in un ordine che documenta (es. campo di ricerca
  `[lente, X]`); l'ordine va scritto anche in `docs/WRAPPER_COMPONENTS.docx`.
- La logica comune è `resolveIcons` in `components/shared/ui/icons/icons.ts`.

## Testi dei wrapper

- I wrapper non traducono (niente ngx-translate): ogni testo arriva già tradotto dalla pagina o dal componente che
  li usa (es. `[label]="'…' | translate"`).
- I testi fissi di un wrapper arrivano insieme nel parametro `texts`, un oggetto con i nomi dei campi (es.
  `TableTexts`); nel file di traduzione il gruppo ha gli stessi nomi: `[texts]="'table' | translate"`.
- I testi con numeri usano i segnaposto `{{nome}}` (`formatText` in `components/shared/ui/texts/`).
- Nessun testo è obbligatorio: se manca, il wrapper funziona lo stesso e il testo resta vuoto (è un errore da
  correggere, non un blocco).

## Colori

- I colori condivisi sono variabili CSS del tema di default (`components/shared/ui/theme/_theme.scss`, applicato in
  `styles.scss`): le quattro principali `--color__main__theme__one`, `--color__main__theme__two`,
  `--color__secondary__theme` e `--color__tertiary__theme`, gli slot di layout `--color__layout__*` e i colori di base
  `--color__white` / `--color__black`.
- Un nuovo colore condiviso si aggiunge al tema, mai scritto nei CSS dei componenti. Un colore quasi uguale a uno del
  tema (ogni canale RGB entro 8 punti) usa quello del tema.
- La regola vale in tutta l'app, non solo nei wrapper: il bianco è sempre `var(--color__white)`, il blu
  `var(--color__main__theme__one)` e così via.

## Popup

- Un nuovo popup estende `BasePopup` (`dialogs/base-popup/`), che gestisce apertura, chiusura immediata e
  distruzione, e usa il mixin `popup($block, $host)` di `dialogs/_popup.scss` per pannello, titolo e testo.
- Il popup aggiunge solo i suoi input e le opzioni del dialog (`dialogConfig()`).
- Ogni popup ha la X in alto a destra, disegnata da `BasePopup`: è la prima icona di `matIcon` / `pathIcon` e le icone
  del popup vengono dopo. Chi usa il popup passa sempre la X della sua pagina; il popup non disegna una X sua.
- La X fa come Esc (chiede prima, se il popup chiede prima di chiudere) e si spegne con `isCloseDisabled()`.
- Eccezione: il popup di login dell'hub (`components/hub/login/login-dialog`) non estende `BasePopup` e disegna la sua
  X.
- Ogni form in popup o pannello è un componente dedicato, che la pagina usa come `<app-… [(open)]="…" />`: vedi
  `architettura.md`. Chiusura con modifiche non salvate: vedi `form-e-validazioni.md`.

## Bottoni

- `app-button` ha solo quattro taglie (`ButtonSize`): `small` (1.75rem), `medium` (2rem), `large` (2.875rem, bottoni
  di invio dei form) ed `extralarge` (3.75rem).
- Non se ne aggiungono altre: se una grafica chiede un'altezza diversa si usa la taglia più vicina o si propone
  all'utente un modo diverso di gestire le dimensioni dei bottoni.
- Spinner sul bottone durante una chiamata (`[isLoading]`): vedi `form-e-validazioni.md`.

## Titolo di pagina

- Le pagine che lo prevedono mettono in cima, sopra tutto il resto, `app-page-title` (`titles/page-title/`, un `h1`)
  con il testo tradotto e l'icona della sezione, nel blu principale (es. Magazzino: `management.warehouse.title` e
  `GeneralIcon_Warehouse.svg`, icona in un campo della classe).
- Tutte le pagine con tabella usano `app-page-title`, con la stessa dimensione, e nessuna ha un `h1` suo. Un colore
  diverso del testo si dà con `--page-title-color` (es. il blu tenue del Cestino).
- Lo spazio sotto il titolo lo decide la pagina (nelle pagine a tabella il mixin `table-page`: vedi `tabelle.md`).
- Da 1920px il titolo è più grande (1.375rem, icona 1.625rem), in proporzione con la tabella più larga.

## La (i) informativa

- Ogni (i) accanto a un'etichetta è un `app-icon-button` con `GeneralIcon_Info.svg` e il mixin `info-button` di
  `styles/_info-button.scss` (`@include info.info-button;`: cerchio bianco con un'ombra leggera).
- La (i) apre `app-info-popup`, con titolo e testo come parametri (nel popup l'icona `GeneralIcon_InfoTitle.svg`).
- La (i) è sempre attiva, anche nei form in sola lettura.

## Wrapper delle librerie ammesse

- Testo formattato: solo con `app-rich-text-field` (`inputs/rich-text-field/`, su TipTap). L'editor ha
  `injectCSS: false`: gli stili che gli servono stanno nel wrapper e nessun `<style>` viene aggiunto alla pagina.
- Mappe: solo con `app-location-map` (`maps/location-map/`, su `@angular/google-maps`), che espone `MapPosition`
  (`latitude`, `longitude`) e non i tipi di Google.
- Lo script di Google Maps lo carica una volta sola `GoogleMapsBehaviour` (`behaviours/maps/`), nella lingua dell'app,
  con la chiave passata in build (vedi `sicurezza.md`) e `googleMapsMapId` dei file `environment`. Il caricamento
  iniziale dell'app non cambia.
- La pagina mostra la mappa solo a script caricato; se lo script non arriva o la chiave manca, mostra un messaggio al
  posto della mappa.
- Scelta dei file: `app-file-drop-zone` (`uploads/file-drop-zone/`) usa il campo del browser `<input type="file">`,
  senza librerie. Come si caricano i file: vedi `api-e-mock.md`.
- Perché queste librerie sono ammesse e con quali pacchetti: vedi `architettura.md`. Bandiere (`flag-icons`): vedi
  `asset-e-icone.md`.
