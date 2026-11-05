# Utility a regime

Strumenti Windows per gestire il nuovo sistema SummerBooking su AWS. Si consegnano come eseguibili
(nessun Python sul PC di chi li usa); i sorgenti stanno qui accanto.

| Utility | A cosa serve | Pacchetto | Manuale |
|---|---|---|---|
| [14-gestione-utenti](14-gestione-utenti/README.md) | Utenti IAM dell'account AWS, ruoli, password, chiavi d'accesso | `dist/14-gestione-utenti.zip` | `manuali/Manuale-utility-14.docx` |
| [22-console-sistema](22-console-sistema/README.md) | Console del nuovo sistema: stato e orario, infrastruttura, deploy, backup e gestione del DB, file S3, archivi e retention, log, scansione, costi | `dist/22-console-sistema.zip` | `manuali/Manuale-utility-22.docx` |

Prima la 14 (crea gli utenti e i ruoli), poi la 22 (le funzioni si abilitano secondo quei ruoli).

## Struttura

```
Utility/
├── README.md               questo file
├── build-all.ps1           build degli eseguibili e creazione dei pacchetti in dist/
├── .gitattributes          dist/*.zip in Git LFS
├── dist/                   un pacchetto .zip per utility (Git LFS), pronto da consegnare
├── manuali/                manuali Word per il committente (inclusi anche nei pacchetti)
├── 14-gestione-utenti/     sorgenti (Python 3.13, Tkinter): README tecnico, setup, build
└── 22-console-sistema/     idem
```

Nel repository vanno solo sorgenti, configurazione di partenza (`config/settings.default.json`),
manuali e pacchetti. Restano fuori (`.gitignore` di ogni utility): `config/settings.local.json`,
`data/` (schede con le chiavi, backup del DB, file scaricati: **dati personali e segreti**), `logs/`,
`.venv/`, `build/`, `dist/` della singola utility.

Il repository usa **Git LFS** per i pacchetti: chi clona deve avere Git LFS installato (incluso in Git
per Windows) e averlo attivato una volta con `git lfs install`. Senza, al posto degli zip si trovano
piccoli file di testo («puntatori»).

## Usare una utility (committente)

1. Prendere `dist/<utility>.zip` ed estrarlo in una cartella dove si può scrivere (non in Programmi).
2. Seguire il manuale in `manuali/` (anche dentro lo zip): utente root, credenziali AWS, configurazione
   in `config\settings.local.json`, uso.

## Aggiornare una utility

### Sviluppatore: nuova versione

1. Modificare i sorgenti in `Utility/<utility>/` (avvio da sorgente: `setup.ps1` una volta, poi
   `.venv\Scripts\python.exe main.py`, oppure F5 da VS Code).
2. Se cambia il comportamento visibile, aggiornare il manuale in `manuali/` (Word) e il README della
   utility.
3. Da questa cartella:
   ```powershell
   powershell -ExecutionPolicy Bypass -File build-all.ps1        # tutte
   powershell -ExecutionPolicy Bypass -File build-all.ps1 22     # solo la 22
   ```
   Crea `.venv` se manca, fa la build con PyInstaller e riscrive `dist/<utility>.zip` (exe, `_internal`,
   configurazione di partenza, README, manuale). Lo script si ferma se nella build finisse una
   configurazione locale.
4. Provare il pacchetto: estrarlo in una cartella temporanea e avviare
   `<utility>-headless.exe --help`, poi la GUI.
5. Commit di sorgenti, manuale e zip insieme, così pacchetto e codice corrispondono.
6. Se la versione richiede passi in più a chi la usa (voci nuove in configurazione, «Prepara ruoli» nella
   14, «Installa riconciliatore» nella 22), scriverli nel messaggio di consegna.

### Committente: installare la nuova versione

1. Chiudere la utility.
2. Estrarre il nuovo zip in una cartella temporanea.
3. Nella cartella in uso sostituire `<utility>.exe`, `<utility>-headless.exe`, la cartella `_internal\`,
   `README.md` e il manuale con quelli nuovi.
4. **Non toccare** `config\settings.local.json`, `data\` e `logs\`.
5. Avviare e fare i passi indicati nella consegna (per la 14: **Verifica ruoli** e, se segnala
   differenze, **Prepara ruoli…**).

## Note

- I template dell'infrastruttura (`infra/dev/sb-dev.yaml`, `infra/summerbooking-aws.yaml`) non sono in
  questo repository: nella 22 il loro percorso si imposta in `ambienti.<ambiente>.infra.template` e serve
  solo alla linguetta Infrastruttura.
- Le utility usate solo per la migrazione dal sistema legacy restano fuori da questo repository.
