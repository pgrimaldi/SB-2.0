# Sharing

Rotta `/settings/sharing`, vista `views/settings/sharing/` (`Sharing`).

## Struttura

- Due riquadri bianchi (mixin `settings-panel`, larghi come la Fiscalizzazione) che mostrano la risposta
  `SharingGeneralInformation` (`entities/settings/sharing/`):
  - "Sharing" (titolo con linea sotto): interruttori Abilita, Durata minima con il campo dei giorni e "Giorni", Abilita
    solo sugli stagionali, Abilita su tutti gli articoli, Imposta assenze programmate come Sharing;
  - "Assegnazione crediti": tre caselle con la spiegazione sotto; a destra "etichetta, campo, €" per numero crediti per
    giorno rivenduto e valore credito; voucher automatico e tessera prepagata con il loro valore.
- Usa `app-toggle`, `app-checkbox` e `app-filled-number-field` (importi con `isCurrency` senza icona e "€" dopo il
  campo) collegati a un Signal Form.
- Il bottone "Salva" sta a destra sotto i riquadri.
- Il mockup mostra le frecce su/giù nel campo dei crediti: `app-filled-number-field` non le ha.

## Comportamenti

- Tutto è sempre modificabile (nessun bottone Modifica). I campi sono bloccati (`disabled`, perché interruttore e casella
  non hanno la sola lettura) solo durante ogni lettura (anche al cambio lingua e dopo un conflitto di versione), finché
  non è arrivata, e durante il salvataggio.
- Il campo dei giorni segue l'interruttore "Durata minima": spento con l'interruttore spento (regola `disabled` sul
  campo), acceso con l'interruttore acceso.
- Acceso, il valore dei giorni deve essere maggiore di 0 (`greaterThan`, messaggio "Il valore dei giorni deve essere
  superiore a 0" sotto il campo, mostrato ogni volta che l'utente accende l'interruttore, anche se era già acceso
  all'apertura); finché non lo è Salva resta spento. Un effetto segna il campo come toccato quando l'interruttore è
  toccato e acceso: Signal Forms ignora `markAsTouched()` su un campo spento.
- Salva manda sempre tutti i valori della pagina (anche quelli non cambiati), con lo spinner sul bottone.
- Se il salvataggio riesce, i valori restano, il form non ha più modifiche e compare `app-message-popup` "Impostazioni
  salvate" / "Salvataggio avvenuto correttamente." con Chiudi (testi `management.settings.sharing.saved`).
- Un errore su un campo va sotto il campo (`submit()` + `apiFieldErrorsOrGeneral`); ogni altro errore, come quello della
  lettura, va in rosso sotto i riquadri.
- Controllo di versione: la lettura porta `version`, che Salva rimanda nell'header `If-Match`; dopo un salvataggio
  riuscito la pagina tiene la versione nuova dell'`ETag` per il salvataggio successivo. Con 409
  `resource.version_conflict` (un altro utente ha cambiato le impostazioni dopo la lettura) compare
  `app-version-conflict-popup`, nessun errore sotto i riquadri, e la pagina rilegge le impostazioni: l'utente rifà le
  modifiche.
- Uscendo con modifiche non salvate la pagina chiede conferma (`UnsavedChangesBehaviour`).

## API

In `SharingService` (lo stabilimento lo prende il backend dal token):

- `GET /api/sharing/general-information` (`generalInformation`), senza parametri: all'apertura, al cambio lingua e
  dopo un conflitto di versione; risposta `SharingGeneralInformation`, cioè le impostazioni e la loro `version`.
- `POST /api/sharing/save-information` (`saveInformation(information, version)`): corpo con tutti i valori della
  pagina (`SharingSaveInformationRequest`, senza la versione) e header `If-Match` con la versione letta; risposta 204
  con l'header `ETag` della nuova versione; 409 `resource.version_conflict`.
