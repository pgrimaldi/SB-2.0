# Test e verifiche

Comandi di verifica, cosa si testa, test indipendenti, Prettier, server di sviluppo, backend locale e checklist prima
della consegna.

## Comandi di verifica

- `npm run verify` è la verifica unica: lint, controllo dei tipi dei test (`tsc -p tsconfig.spec.json --noEmit`, perché
  la build dei test non li controlla) e test unitari.
- Va lanciato in locale prima di consegnare una modifica. La pipeline (`buildspec.yml`, fase build) lo esegue prima
  della build: se fallisce non si pubblica nulla.
- Altri comandi standard: `npm run build:dev`, `npm run build:prod` quando pertinente, `npm run test:ci`,
  `npm run lint` (le regole del lint: vedi `architettura.md`).
- `npm audit` non è nella verifica: è un punto ancora da decidere.

## Cosa si testa

- Solo le chiamate alle API e i casi d'uso: flussi, errori, sicurezza, corse tra richieste.
- Un test non controlla mai la grafica: etichette, icone, classi CSS, posizioni, misure, testi mostrati, comportamenti
  propri di Material. La grafica si controlla a mano durante lo sviluppo e, una volta pulita, non si ritesta.
- Niente test ripetuti: un comportamento si prova in un solo test.
- I test di una pagina si scrivono quando la pagina è finita.

## Test indipendenti fra loro

- Il builder dei test di Angular esegue i file senza isolamento (`isolate: false`): i file nello stesso processo
  condividono `window` (anche `localStorage`, `sessionStorage` e `lang`/`dir` di `<html>`), timer e spie.
- Un test non assegna mai direttamente un oggetto globale (`window.x = …`, anche `document`, `globalThis`,
  `navigator`): usa `vi.stubGlobal`, `vi.spyOn` o `vi.useFakeTimers`. Il lint blocca l'assegnazione diretta nei file
  `.spec.ts`.
- `src/test-setup.ts` (in `angular.json`, `setupFiles`; escluso dalla build dell'app in `tsconfig.app.json`) dopo ogni
  test annulla stub, spie e timer finti, svuota `localStorage` e `sessionStorage` e rimette `lang`/`dir` di `<html>`.
- Motivo: un oggetto globale lasciato a metà fa fallire a caso, secondo l'ordine dei file, i test di altri file.

## Prettier

- Solo sui file modificati, con `--end-of-line auto`.
- Mai su tutta `src/app`.

## Server di sviluppo

- Il server su `localhost:4200` lo avvia e lo ferma l'utente: non si avvia, non si ferma, non si cita e non si danno
  link verso di esso. Come fare le verifiche nel browser: vedi `CLAUDE.md`.

## Mock e backend vero

- `npm start` (`ng serve`): frontend con i mock. È il modo normale di lavorare.
- `npm run start:backend`: frontend contro il backend vero, con la configurazione `local-backend` di `angular.json`:
  - build di sviluppo senza mock: `mock-interceptors.ts` è sostituito da `mock-interceptors.none.ts`;
  - proxy `proxy.local-backend.json`, che manda `/api` a `http://localhost:5197`, la porta fissa del profilo `http`
    del backend. Il proxy esiste solo in `ng serve`: la build di produzione non lo usa.
- In Visual Studio il backend va avviato con il profilo `http`, non con IIS Express, che rimanda a https e non legge i
  segreti locali.
- Le chiamate restano sulla stessa origine del frontend, così il cookie del rinnovo (Path `/api/identity`) funziona.
- Il backend deve accettare quell'origine: `http://localhost:4200` è in `Auth:AllowedOrigins` di
  `Boot/appsettings.Development.json` del backend; `http://localhost:4300` sta solo nei segreti locali di .NET
  (`dotnet user-secrets` sul progetto `Boot`, fuori dal repository).
- Per un test vicino al reale, nei segreti locali anche il cookie del rinnovo ha i valori di produzione
  (`Auth:RefreshCookie:Name` = `__Secure-sb_rt`, `Auth:RefreshCookie:Secure` = `true`): Chrome accetta i cookie
  `Secure` su `http://localhost`.
- Le pagine il cui backend non ha ancora le API rispondono con errori finché il backend non le aggiunge.

## Checklist prima della consegna

- La modifica risponde soltanto alla richiesta corrente; nessun elemento è stato anticipato.
- Asset e testi sono locali e traducibili.
- Non esistono import diretti di librerie UI fuori dai wrapper.
- Desktop e mobile sono stati controllati rispetto al riferimento, con un controllo visivo proporzionato al rischio.
- `npm run verify` e la build passano.
- `.gitignore` resta adeguato.
- Nessun secret, output generato o riferimento locale nella documentazione pubblica o di deploy.
- Se la pagina è pubblica, la review SEO (vedi `seo.md`) è stata fatta prima di dichiararla completata.
