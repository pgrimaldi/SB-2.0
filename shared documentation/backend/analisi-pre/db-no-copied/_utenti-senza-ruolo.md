# I 20 account non migrati: 17 senza ruolo, 3 senza email

> ✅ **Deciso il 2026-09-30 e applicato il 2026-10-01** (voce A6): gruppo A → ruolo `consumer`, gruppo B fuori, gruppo C recuperato dalla colonna `username` dove c'e' l'indirizzo (3 su 4). Restano esclusi **6** account: i 5 di prova e quello Apple senza email. Prodotto dai dati reali del dump legacy.
> Riferimento: [`_mancanti-da-sistemare.md`](_mancanti-da-sistemare.md) voce **A6.1**.

## Perche' sono rimasti fuori

Nel nuovo schema `identity.access_users` pretende **un ruolo** (`role_id` obbligatorio) e
**un'email** (e' l'identificativo di accesso). Nel legacy la tabella `users` non pretendeva
ne' l'uno ne' l'altra, e 20 righe ne sono senza. Si dividono in tre gruppi con risposte
diverse.

| | Righe | Cosa manca | Cosa propongo |
|---|---:|---|---|
| **A** | 11 | il ruolo; **nessuno stabilimento**, accesso con Google/Facebook/Apple | assegnare il ruolo `consumer` |
| **B** | 5 | il ruolo; hanno uno stabilimento, ma sono **account di prova** | lasciarli fuori |
| **C** | 4 | l'**email**; il ruolo `consumer` ce l'hanno, e hanno accessi recenti | serve una risposta |

---

## Gruppo A — 11 utenti dell'app che hanno perso il ruolo

Nessuno stabilimento, accesso con un'identita' social, email personali plausibili. Nel legacy
esiste un ruolo apposta per questi utenti, **`consumer`**, e ce l'hanno **6.004** account
identici a questi. Questi 11 no: verosimilmente la riga in `user_roles` non e' stata scritta
in un certo periodo di registrazione.

| id | Email | Nome | Accesso | Creato | Ultimo accesso | Esiste come cliente |
|---|---|---|---|---|---|---|
| 180 | `deshpandeshubham97@gmail.com` | SHUBHAM DESHPANDE | google | 2021-09-08 | 2022-06-14 | no |
| 188 | `smapsolace@gmail.com` | Shubham Solace | google | 2021-09-30 | 2021-10-25 | **si', 1 schede** |
| 191 | `adipolojiwar@gmail.com` | Aditya polojiwar | facebook | 2021-10-05 | 2021-10-13 | **si', 2 schede** |
| 197 | `adityapolojiwar7747@gmail.com` | Aditya Polojiwar | google | 2021-10-12 | 2021-10-12 | **si', 2 schede** |
| 441 | `giuseppebaldassarre90@gmail.com` | Giuseppe Baldassarre | google | 2022-07-31 | 2023-06-21 | **si', 6 schede** |
| 445 | `scopacasaannamaria@gmail.com` | Annamaria Scopacasa | google | 2022-08-01 | 2022-08-01 | no |
| 471 | `deadav3@gmail.com` | Davide De Angelis | google | 2022-08-06 | 2022-08-06 | **si', 1 schede** |
| 475 | `matteoemilialuna@alice.it` | Emilia Tragni | facebook | 2022-08-07 | mai | no |
| 478 | `aditypolojiwar.solace@gmail.com` | Aditya Polojiwar | google | 2022-08-09 | 2022-08-12 | no |
| 480 | `veronica.buia@gmail.com` | VERONICA BUIA | normal | 2022-08-11 | 2024-07-23 | **si', 7 schede** |
| 3787 | `nwwrg6b747@privaterelay.appleid.com` | — | apple | 2026-06-01 | mai | no |

**Perche' `consumer` non e' un fallback privilegiato.** La nota di migrazione vieta di inventare
un ruolo, e giustamente: attribuire `manager` o `admin` creerebbe un accesso che nessuno ha
concesso. Ma `consumer` e' il ruolo **meno privilegiato** dei tredici, e' quello che per
definizione ha chi si registra dall'app, e non da' alcun accesso al gestionale. Per un account
senza stabilimento, creato via Google o Apple, e' la sola lettura coerente: gli altri dodici
ruoli presuppongono uno stabilimento che questi non hanno.

Diversi di loro hanno **schede cliente** con la stessa email (ultima colonna), quindi sono
persone che hanno davvero usato il sistema, non registrazioni vuote.

---

## Gruppo B — 5 account di prova su stabilimenti

Questi hanno uno stabilimento, quindi *sembrano* personale. Da vicino non lo sono: **tutti e
cinque sono `inactive`**, due appartengono a stabilimenti **cancellati**, e tre hanno email
palesemente inventate, create lo stesso giorno dallo stesso account.

| id | Email | Nome | Stabilimento | Stato stab. | Creato da | Ultimo accesso |
|---|---|---|---|---|---|---|
| 100 | `soumyamishra.mishra88@gmail.com` | — | 1 `Goa Candolim Beach fast` | **cancellato** | `vijay@summer.com` | 2021-03-23 |
| 167 | `ssk35@yopmail.com` | — | 64 `Club Forest` | **cancellato** | `ssk75@yopmail.com` | mai |
| 433 | `gfdgfd@jkhkjh.it` | Salvatore Cunsolo | 96 `Fiera Rimini` | attivo | `fiera2021@summerbooking.it` | mai |
| 434 | `del_27/6/2022_gfdgfad@jkhkjh.it` | Salvatore Cunsolo | 96 `Fiera Rimini` | attivo | `fiera2021@summerbooking.it` | 2022-07-27 |
| 435 | `testarebag@gmail.eu` | Salvatore Cunsolo | 96 `Fiera Rimini` | attivo | `fiera2021@summerbooking.it` | 2022-07-27 |

Il dettaglio che chiude il discorso: `del_27/6/2022_gfdgfad@jkhkjh.it` porta il prefisso
`del_<data>_` con cui il vecchio gestionale marcava le email degli account cancellati. Le tre
righe su *Fiera Rimini* sono state create dallo stand di una fiera, e i nomi utente lo
confermano (`gfdgfd`, `testarebag`).

**Proposta: restano fuori.** Nessuno dei cinque ha mai avuto un ruolo, nessuno e' attivo, e un
account senza ruolo non poteva fare nulla nemmeno nel vecchio sistema: non si sta togliendo un
permesso a nessuno.

---

## Gruppo C — 4 account senza email

Il caso opposto, e il solo su cui serve davvero una risposta: qui manca l'**email**, non il
ruolo — **3 dei 4 hanno il ruolo `consumer`** (il quarto non ha nemmeno quello). Non sono
vecchi residui: **tutti hanno fatto accesso nel 2026**.

| id | Email | Nome | Ruolo | Accesso | Creato | Ultimo accesso |
|---|---|---|---|---|---|---|
| 1629 | **(vuota)** | Claudia | `consumer` | normal | 2025-06-02 | 2026-07-13 |
| 3788 | **(vuota)** | — | `—` | apple | 2026-06-01 | 2026-07-16 |
| 4488 | **(vuota)** | — | `consumer` | normal | 2026-06-18 | 2026-07-13 |
| 5285 | **(vuota)** | — | `consumer` | normal | 2026-07-08 | 2026-07-13 |

**Le domande, in ordine di importanza.**

1. Tre di questi hanno `loginMethod = normal`, cioe' accesso con credenziali — **ma con quale
   identificativo, se l'email e' vuota e il telefono pure?** Se esiste un modo di autenticarsi
   che non conosciamo, il nuovo schema (che usa l'email come chiave di accesso, con indice
   unico) non lo prevede: e' una cosa da sapere **prima** del cutover, perche' riguarda il
   disegno e non queste quattro righe.
2. L'utente **1629 si chiama "Claudia"** e ha fatto accesso il 2026-07-13: e' una persona reale?
   Se si', serve la sua email.
3. I due senza nome (4488, 5285) hanno fatto accesso **lo stesso giorno** di Claudia: sembra una
   sessione di prova. Da confermare.
4. Il quarto (**3788**) entra con **Apple** e non ha nemmeno il ruolo. L'accesso con Apple
   permette di nascondere la propria email, e infatti l'utente 3787 — stesso giorno, stesso
   metodo — ha un indirizzo `@privaterelay.appleid.com`. Qui l'email e' invece **vuota**: da
   capire se il vecchio sistema non la salvava in qualche caso, perche' se accade ancora si
   ripetera' a ogni nuova registrazione con Apple.

Senza email non possono essere migrati: nel nuovo schema l'email e' l'identificativo di accesso
con un indice unico, e inventarne una significherebbe creare un account con cui nessuno puo'
entrare. Con l'email rientrano alla prima rimigrazione, senza altro lavoro.
