# Righe non migrate — `identity.access_users`

> Modulo **identity** · sorgente legacy **`users`** · **6 righe** non arrivate a destinazione.
>
> Dati grezzi completi, mai troncati: [`dati/identity_access_users.jsonl`](dati/identity_access_users.jsonl).

## Diagnosi

### Esclusa da una regola — 6 righe

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
nessun ruolo in user_roles e ha uno stabilimento: nessun fallback privilegiato per il personale
```
*(5 righe su 6)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
not email and not username
```
*(1 righe su 6)*

**Cosa significa** — Account applicativi **senza ruolo** (16) o **senza email** (4).

**Causa a monte** — Non sono 20 righe con lo stesso difetto. **16** hanno l'email e manca il **ruolo**; le altre **4** sono escluse prima, dalla regola `not email` della spec, e tre di loro hanno il ruolo `consumer` (l'email ce l'hanno nella colonna `username`, vedi `analisi-pre/db-no-copied/_utenti-senza-ruolo.md`). Nel dump gli utenti senza alcun ruolo sono 17: il diciassettesimo e' fra i 4, escluso per l'email prima di arrivare al controllo sul ruolo. Sul ruolo: nel legacy i ruoli stavano in `user_roles`, una relazione N:N mai usata come tale, e per questi utenti non contiene **nessuna** riga. Nel nuovo schema `role_id` e' obbligatorio, e la scheda di migrazione prescrive di non inventarne uno: attribuire d'ufficio il ruolo piu' basso creerebbe un accesso che nessuno ha mai concesso, attribuirne uno alto e' peggio. Senza ruolo la riga resta fuori.

**Verdetto: ❓ Da decidere** — il meccanismo e' corretto, ma serve una scelta di business sul dato

**Azione** — **Elenco completo con i dati per decidere in `analisi-pre/db-no-copied/_utenti-senza-ruolo.md`.** I 20 si dividono in tre gruppi: **11** senza stabilimento, con accesso Google/Facebook/Apple ed email personali (sei hanno anche una scheda cliente con la stessa email) - sono utenti dell'app che hanno perso la riga in `user_roles`, e il ruolo coerente e' `consumer`, il MENO privilegiato dei tredici, che ce l'hanno altri 6.004 account identici; **5** con stabilimento ma tutti `inactive`, due su stabilimenti cancellati e tre con email inventate create lo stesso giorno dallo stesso account - restano fuori; **4** senza email, di cui **tre hanno l'indirizzo nella colonna `username`** (il login legacy accettava email OR username) e vanno recuperati per decisione del committente del 2026-09-30, mentre il quarto entra con Apple e non ha nulla in nessuna delle due.

## Righe

| id legacy | motivo | `id` | `loginMethod` | `userStatus` | `createdAt` | `updatedAt` | `languageCode` |
|---|---|---|---|---|---|---|---|
| `100` | Esclusa da una regola | 100 | normal | inactive | 2021-03-04T12:39:52 | 2021-05-20T06:54:10 | 1 |
| `167` | Esclusa da una regola | 167 | normal | inactive | 2021-06-24T10:39:18 | 2021-06-24T11:07:04 | 2 |
| `433` | Esclusa da una regola | 433 | normal | inactive | 2022-07-27T15:12:52 | 2026-07-20T11:02:28 | 2 |
| `434` | Esclusa da una regola | 434 | normal | inactive | 2022-07-27T15:13:33 | 2026-07-20T11:02:28 | 2 |
| `435` | Esclusa da una regola | 435 | normal | inactive | 2022-07-27T15:15:17 | 2026-07-20T11:02:28 | 2 |
| `3788` | Esclusa da una regola | 3788 | apple | active | 2026-06-01T12:57:35 | 2026-07-16T09:20:53 | 2 |
