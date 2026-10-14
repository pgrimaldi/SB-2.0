# Righe non migrate — `notification.marketing_newsletter_subscriptions`

> Modulo **notification** · sorgente legacy **`subscribes`** · **1010 righe** non arrivate a destinazione.
>
> Dati grezzi completi, mai troncati: [`dati/notification_marketing_newsletter_subscriptions.jsonl`](dati/notification_marketing_newsletter_subscriptions.jsonl).

## Diagnosi

### Esclusa da una regola — 1010 righe

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
email duplicata: vince l'iscrizione con id piu' alto
```
*(1003 righe su 1010)*

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
not email
```
*(7 righe su 1010)*

**Cosa significa** — Iscrizioni alla newsletter con **email duplicata**: si tiene la piu' recente.

**Causa a monte** — Le righe hanno l'email, ed e' proprio quello il punto: nel legacy `subscribes` non aveva alcun vincolo di unicita' sull'email, e nel dump reale la stessa email compare fino a **15 volte**. Nel nuovo schema l'email e' unica, perche' e' la chiave della lista di soppressione: due righe per la stessa persona vorrebbero dire che disiscriversi da una non la ferma dall'altra. Il transform raggruppa per email e tiene l'`id` piu' alto, cioe' l'iscrizione (e lo stato iscritto/disiscritto) piu' recente; le precedenti vengono scartate.

**Verdetto: ✅ Voluto** — nessuna correzione: il comportamento e' quello previsto dal disegno

**Azione** — Nessuna: e' la deduplicazione che serve. **1.010 righe su 6.548** non sono 1.010 persone perse — sono 1.010 doppioni collassati, e nessun indirizzo esce dalla lista. Vale comunque la pena chiedersi come si siano creati, perche' se il form che li produce e' ancora senza vincolo continuera' a produrne.

## Righe

| id legacy | motivo | `id` | `email` | `createdAt` | `updatedAt` | `status` |
|---|---|---|---|---|---|---|
| `8` | Esclusa da una regola | 8 | danieleduranti@ymail.com | 2021-06-01T09:36:11 | 2021-06-01T09:36:11 | 1 |
| `10` | Esclusa da una regola | 10 | salvoc-@hotmail.it | 2021-06-12T16:46:38 | 2021-06-12T16:46:38 | 1 |
| `45` | Esclusa da una regola | 45 | pbkushare@yopmail.com | 2021-06-17T12:29:22 | 2021-10-04T06:13:57 | 0 |
| `46` | Esclusa da una regola | 46 | apedemak0@gmail.com | 2021-06-17T12:29:22 | 2021-09-24T12:29:22 | 1 |
| `47` | Esclusa da una regola | 47 | bagnomilano.fiumetto@gmail.com | 2021-06-17T12:29:22 | 2021-09-24T12:29:22 | 1 |
| `48` | Esclusa da una regola | 48 | amministrazionelidosandomenico@gmail.com | 2021-06-17T12:29:22 | 2021-09-24T12:29:22 | 1 |
| `49` | Esclusa da una regola | 49 | info@lidoulisse.it | 2021-06-17T12:29:22 | 2021-09-24T12:29:22 | 1 |
| `50` | Esclusa da una regola | 50 | vijay@summer.com | 2021-06-17T12:29:22 | 2021-09-29T09:54:37 | 0 |
| `54` | Esclusa da una regola | 54 | info@lido-onthebeach.it | 2021-06-17T12:29:22 | 2021-09-24T12:29:22 | 1 |
| `57` | Esclusa da una regola | 57 | kusharepradip@yopmail.com | 2021-06-17T12:29:22 | 2021-09-29T09:54:48 | 0 |
| `59` | Esclusa da una regola | 59 | salvatore.lapuma@gmail.com | 2021-06-17T12:29:22 | 2021-09-24T12:29:22 | 1 |
| `60` | Esclusa da una regola | 60 | l.piseddu@studenti.unipi.it | 2021-06-17T12:29:22 | 2021-09-24T12:29:22 | 1 |
| `62` | Esclusa da una regola | 62 | francescolagana63@libero.it | 2021-06-17T12:29:22 | 2021-09-24T12:29:22 | 1 |
| `64` | Esclusa da una regola | 64 | sachinsolace@yopmail.com | 2021-06-17T12:29:22 | 2021-09-29T09:55:01 | 0 |
| `65` | Esclusa da una regola | 65 | paralia.2021@libero.it | 2021-06-17T12:29:22 | 2021-09-24T12:29:22 | 1 |
| `66` | Esclusa da una regola | 66 | magisrl@sistemic.it | 2021-06-17T12:29:22 | 2021-09-24T12:29:22 | 1 |
| `68` | Esclusa da una regola | 68 | pradipkushare@gmail.com | 2021-06-17T12:29:22 | 2021-09-29T09:55:06 | 0 |
| `70` | Esclusa da una regola | 70 | info@bagnoskiuma.com | 2021-06-17T12:29:22 | 2021-09-24T12:29:22 | 1 |
| `72` | Esclusa da una regola | 72 | matteo.bagnoskiuma@outlook.it | 2021-06-17T12:29:22 | 2021-09-24T12:29:22 | 1 |
| `90` | Esclusa da una regola | 90 | pradipkushare@gmail.com | 2021-06-17T12:29:22 | 2021-09-29T09:53:27 | 0 |
| `99` | Esclusa da una regola | 99 | roshani.mahale@solacetechnologies.co.in | 2021-06-17T12:29:22 | 2021-09-29T09:50:38 | 0 |
| `122` | Esclusa da una regola | 122 | salvoc-@hotmail.it | 2021-09-28T08:29:22 | 2021-09-29T10:00:39 | 0 |
| `124` | Esclusa da una regola | 124 | p.grimaldi@qbitsoft.it | 2021-09-28T08:29:22 | 2021-09-28T08:29:22 | 1 |
| `169` | Esclusa da una regola | 169 | matteo.bagnoskiuma@outlook.it | 2021-09-28T08:29:22 | 2021-09-29T09:58:13 | 0 |
| `171` | Esclusa da una regola | 171 | ssk10@yopmail.com | 2021-09-28T08:29:22 | 2021-09-29T09:43:12 | 0 |
| `178` | Esclusa da una regola | 178 | shiva.pulikanti@solacetechnologies.co.in | 2021-09-28T08:29:22 | 2021-09-29T09:44:14 | 0 |
| `205` | Esclusa da una regola | 205 | fierarimini@summerbooking.it | 2021-10-12T21:21:34 | 2021-10-12T21:29:43 | 0 |
| `209` | Esclusa da una regola | 209 | promozioniqbitsoft@gmail.com | 2021-10-25T21:55:46 | 2022-09-26T10:03:54 | 0 |
| `216` | Esclusa da una regola | 216 | conf@conf.it | 2021-11-23T08:52:25 | 2021-11-23T08:55:35 | 0 |
| `227` | Esclusa da una regola | 227 | baubeach.legorette@gmail.com | 2022-02-26T10:59:53 | 2022-02-26T10:59:53 | 1 |
| `233` | Esclusa da una regola | 233 | mexicool541@gmail.com | 2022-03-10T08:04:35 | 2022-03-10T08:04:35 | 1 |
| `235` | Esclusa da una regola | 235 | jdndskjaldsk@gm.com | 2022-03-10T08:09:03 | 2022-03-23T23:25:06 | 0 |
| `236` | Esclusa da una regola | 236 | jdndskjaldsk@gm.com | 2022-03-10T08:09:32 | 2022-03-23T23:25:11 | 0 |
| `239` | Esclusa da una regola | 239 | mexicool541@gmail.com | 2022-03-10T08:19:12 | 2022-03-10T08:19:12 | 1 |
| `242` | Esclusa da una regola | 242 | vishalsolace@yopmail.com | 2022-03-21T04:24:13 | 2022-03-23T23:24:46 | 0 |
| `243` | Esclusa da una regola | 243 | vishalsolace@yopmail.com | 2022-03-22T10:09:11 | 2022-03-23T23:24:27 | 0 |
| `246` | Esclusa da una regola | 246 | pradipsolace@yopmail.com | 2022-03-22T13:28:38 | 2022-09-26T10:03:09 | 0 |
| `247` | Esclusa da una regola | 247 | duepuntozero@yahoo.com | 2022-03-22T15:07:30 | 2022-03-27T11:51:18 | 0 |
| `248` | Esclusa da una regola | 248 | duepuntozero@yahoo.com | 2022-03-22T15:10:45 | 2022-03-22T15:10:45 | 1 |
| `250` | Esclusa da una regola | 250 | account93@prova.it | 2022-03-23T23:21:48 | 2022-03-23T23:24:05 | 0 |
| `255` | Esclusa da una regola | 255 | bagnoargo@iol.it | 2022-03-26T15:14:48 | 2022-03-27T11:50:39 | 0 |
| `258` | Esclusa da una regola | 258 | Morejunes@gmail.com | 2022-03-26T17:34:05 | 2022-03-27T11:50:17 | 0 |
| `259` | Esclusa da una regola | 259 | Morejunes@gmail.com | 2022-03-26T17:34:12 | 2022-03-27T11:50:24 | 0 |
| `260` | Esclusa da una regola | 260 | Morejunes@gmail.com | 2022-03-26T17:34:13 | 2022-03-26T17:34:13 | 1 |
| `261` | Esclusa da una regola | 261 | Morejunes@gmail.com | 2022-03-26T17:34:19 | 2022-03-27T11:50:29 | 0 |
| `262` | Esclusa da una regola | 262 | Morejunes@gmail.com | 2022-03-26T17:34:23 | 2022-03-27T11:50:52 | 0 |
| `263` | Esclusa da una regola | 263 | Morejunes@gmail.com | 2022-03-26T17:34:25 | 2022-03-27T11:50:45 | 0 |
| `264` | Esclusa da una regola | 264 | Morejunes@gmail.com | 2022-03-26T17:34:34 | 2022-03-27T11:50:57 | 0 |
| `267` | Esclusa da una regola | 267 | ssk89yopmail.com | 2022-04-08T06:56:42 | 2022-05-12T23:53:37 | 0 |
| `270` | Esclusa da una regola | 270 | pradipsolace1@yopmail.com | 2022-04-15T09:34:19 | 2022-05-12T23:53:24 | 0 |
| `271` | Esclusa da una regola | 271 | rmirabelli@yahoo.it | 2022-04-16T08:05:56 | 2022-04-28T21:32:52 | 0 |
| `273` | Esclusa da una regola | 273 | carla6709@gmail.com | 2022-04-20T20:09:35 | 2022-04-20T20:09:35 | 1 |
| `274` | Esclusa da una regola | 274 | bagnobigini@gmail.com | 2022-04-21T15:01:18 | 2022-04-21T15:01:18 | 1 |
| `276` | Esclusa da una regola | 276 | test@yopmail.com | 2022-04-22T04:54:29 | 2022-04-28T21:32:25 | 0 |
| `277` | Esclusa da una regola | 277 | fmultiservizi@gmail.com | 2022-04-27T16:49:30 | 2022-04-27T16:49:30 | 1 |
| `279` | Esclusa da una regola | 279 | info@tuaregbeach.it | 2022-04-28T17:46:39 | 2022-04-28T17:46:39 | 1 |
| `281` | Esclusa da una regola | 281 | antonelloguaragna@yahoo.it | 2022-04-29T09:20:18 | 2022-05-06T16:56:43 | 0 |
| `283` | Esclusa da una regola | 283 | ssk10@yopmail.com | 2022-05-03T05:07:02 | 2022-05-12T23:53:02 | 0 |
| `285` | Esclusa da una regola | 285 | pradipsolace1@yopmail.com | 2022-05-03T06:12:21 | 2022-05-12T23:53:12 | 0 |
| `286` | Esclusa da una regola | 286 | ssk10@yopmail.com | 2022-05-03T06:25:19 | 2022-05-06T16:56:22 | 0 |
| `287` | Esclusa da una regola | 287 | ssk10@yopmail.com | 2022-05-03T07:06:21 | 2022-05-06T16:56:34 | 0 |
| `288` | Esclusa da una regola | 288 | carla6709@gmail.com | 2022-05-03T09:37:32 | 2022-05-06T16:56:28 | 0 |
| `291` | Esclusa da una regola | 291 | eraclesport2018@gmail.com | 2022-05-03T15:44:19 | 2022-05-06T16:57:21 | 0 |
| `293` | Esclusa da una regola | 293 | ssk10@yopmail.com | 2022-05-05T06:38:58 | 2022-05-06T16:55:59 | 0 |
| `294` | Esclusa da una regola | 294 | ssk10@yopmail.com | 2022-05-05T09:50:36 | 2022-05-06T16:55:49 | 0 |
| `295` | Esclusa da una regola | 295 | ssk10@yopmail.com | 2022-05-05T10:54:16 | 2022-05-06T16:55:54 | 0 |
| `296` | Esclusa da una regola | 296 | ssk10@yopmail.com | 2022-05-05T10:58:02 | 2022-05-06T16:56:03 | 0 |
| `297` | Esclusa da una regola | 297 | ssk10@yopmail.com | 2022-05-05T12:18:18 | 2022-05-06T16:56:08 | 0 |
| `298` | Esclusa da una regola | 298 | gardenlidokr@gmail.com | 2022-05-05T18:02:16 | 2022-05-06T16:56:13 | 0 |
| `300` | Esclusa da una regola | 300 | vincenzo.meleca1@gmail.com | 2022-05-07T15:17:45 | 2022-05-12T23:50:48 | 0 |
| `325` | Esclusa da una regola | 325 | pjbar2021@gmail.com | 2022-05-09T08:36:33 | 2022-05-12T23:50:20 | 0 |
| `330` | Esclusa da una regola | 330 | ssk10@yopmail.com | 2022-05-10T08:31:49 | 2022-05-12T23:50:09 | 0 |
| `331` | Esclusa da una regola | 331 | lidosanfelice1977@gmail.com | 2022-05-12T13:05:03 | 2022-05-12T13:05:03 | 1 |
| `342` | Esclusa da una regola | 342 | shivatest@yopmail.com | 2022-05-17T08:19:34 | 2022-05-18T20:25:05 | 0 |
| `364` | Esclusa da una regola | 364 | annacersosimo@libero.it | 2022-05-21T14:26:26 | 2022-09-26T10:02:47 | 0 |
| `370` | Esclusa da una regola | 370 | adityasolace16@yopmail.com | 2022-05-23T13:20:24 | 2022-06-11T16:49:48 | 0 |
| `374` | Esclusa da una regola | 374 | sabinaaltamura54@gmail.com | 2022-05-23T17:08:57 | 2022-05-31T12:58:42 | 0 |
| `377` | Esclusa da una regola | 377 | monicamanera@hotmail.it | 2022-05-23T21:32:13 | 2022-06-11T16:49:18 | 0 |
| `379` | Esclusa da una regola | 379 | campeggio.internaz@libero.it | 2022-05-24T17:44:27 | 2022-05-24T17:44:27 | 1 |
| `381` | Esclusa da una regola | 381 | ashishs@solaceinfotech.co.in | 2022-05-26T05:16:56 | 2022-09-26T10:01:54 | 0 |
| `383` | Esclusa da una regola | 383 | shiva.pulikanti@solacetechnologies.co.in | 2022-05-26T05:20:44 | 2022-09-26T10:02:11 | 0 |
| `390` | Esclusa da una regola | 390 | martischim@libero.it | 2022-05-27T17:39:25 | 2022-06-11T16:48:58 | 0 |
| `396` | Esclusa da una regola | 396 | shivatest@yopmail.com | 2022-05-30T04:25:53 | 2022-05-31T12:57:33 | 0 |
| `402` | Esclusa da una regola | 402 | lucaoppido31@gmail.com | 2022-06-04T08:08:46 | 2022-06-04T08:08:46 | 1 |
| `403` | Esclusa da una regola | 403 | lucaoppido31@gmail.com | 2022-06-04T08:08:57 | 2022-06-04T14:04:26 | 0 |
| `404` | Esclusa da una regola | 404 | lucaoppido31@gmail.com | 2022-06-04T08:09:03 | 2022-06-04T14:04:32 | 0 |
| `405` | Esclusa da una regola | 405 | lucaoppido31@gmail.com | 2022-06-04T08:10:19 | 2022-06-04T14:04:37 | 0 |
| `409` | Esclusa da una regola | 409 | carbo.ste64@gmail.com | 2022-06-05T16:52:04 | 2022-06-11T16:48:41 | 0 |
| `410` | Esclusa da una regola | 410 | carbo.ste64@gmail.com | 2022-06-05T16:52:12 | 2022-06-11T16:48:31 | 0 |
| `412` | Esclusa da una regola | 412 | lidoarcomagno@libero.it | 2022-06-06T15:07:43 | 2022-06-11T16:48:37 | 0 |
| `416` | Esclusa da una regola | 416 | grazianocrispino@libero.it | 2022-06-08T17:14:03 | 2022-06-08T17:14:03 | 1 |
| `429` | Esclusa da una regola | 429 | valentinadiamante@hotmail.it | 2022-06-13T09:00:58 | 2022-06-13T09:00:58 | 1 |
| `432` | Esclusa da una regola | 432 | pradipsolace1@yopmail.com | 2022-06-14T11:26:05 | 2022-09-26T10:00:21 | 0 |
| `433` | Esclusa da una regola | 433 | pradipsolace1@yopmail.com | 2022-06-14T11:26:19 | 2022-09-26T10:00:26 | 0 |
| `434` | Esclusa da una regola | 434 | ssk10@yopmail.com | 2022-06-14T11:46:10 | 2022-09-26T10:00:31 | 0 |
| `435` | Esclusa da una regola | 435 | pradipsolace1@yopmail.com | 2022-06-14T11:52:58 | 2022-09-26T10:00:36 | 0 |
| `436` | Esclusa da una regola | 436 | ssk10@yopmail.com | 2022-06-14T11:58:43 | 2022-09-26T09:59:10 | 0 |
| `437` | Esclusa da una regola | 437 | duepuntozero@yahoo.com | 2022-06-14T12:16:56 | 2022-06-14T12:16:56 | 1 |
| `438` | Esclusa da una regola | 438 | duepuntozero@yahoo.com | 2022-06-14T12:23:01 | 2022-09-26T09:59:56 | 0 |
| `439` | Esclusa da una regola | 439 | ssk10@yopmail.com | 2022-06-14T12:38:14 | 2022-09-26T09:59:14 | 0 |
| `440` | Esclusa da una regola | 440 | ssk10@yopmail.com | 2022-06-14T12:38:57 | 2022-09-26T09:59:19 | 0 |
| `441` | Esclusa da una regola | 441 | pradipsolace1@yopmail.com | 2022-06-14T12:42:20 | 2022-09-26T09:59:24 | 0 |
| `442` | Esclusa da una regola | 442 | pradipsolace1@yopmail.com | 2022-06-14T12:44:15 | 2022-09-26T09:59:29 | 0 |
| `443` | Esclusa da una regola | 443 | pradipsolace1@yopmail.com | 2022-06-14T12:52:17 | 2022-09-26T09:59:41 | 0 |
| `458` | Esclusa da una regola | 458 | lidosolesi@gmail.com | 2022-06-15T17:05:07 | 2022-06-15T17:05:07 | 1 |
| `463` | Esclusa da una regola | 463 | pradipsolace1@yopmail.com | 2022-06-16T14:24:33 | 2022-09-26T09:56:55 | 0 |
| `466` | Esclusa da una regola | 466 | pradipsolace1@yopmail.com | 2022-06-17T03:05:29 | 2022-09-26T09:57:14 | 0 |
| `468` | Esclusa da una regola | 468 | aditya@yopmail.com | 2022-06-17T03:23:37 | 2022-09-26T09:56:44 | 0 |
| `470` | Esclusa da una regola | 470 | enrico.sposato@icloud.com | 2022-06-17T14:07:28 | 2022-06-17T14:07:28 | 1 |
| `477` | Esclusa da una regola | 477 | duepuntozero@yahoo.com | 2022-06-20T02:57:10 | 2022-06-20T02:57:10 | 1 |
| `481` | Esclusa da una regola | 481 | harishmogal@yopmail.com | 2022-06-21T06:15:29 | 2022-06-21T21:36:09 | 0 |
| `488` | Esclusa da una regola | 488 | salvoc----@hotmail.it | 2022-06-21T21:27:28 | 2022-06-21T21:35:43 | 0 |
| `494` | Esclusa da una regola | 494 | salvoc------@hotmail.it | 2022-06-22T22:36:33 | 2022-06-23T15:39:11 | 0 |
| `498` | Esclusa da una regola | 498 | baywatchclub@libero.it | 2022-06-24T09:14:15 | 2022-06-30T19:25:38 | 0 |
| `508` | Esclusa da una regola | 508 | renatomaiolo@tiscali.it | 2022-06-29T08:45:28 | 2022-06-30T19:25:26 | 0 |
| `509` | Esclusa da una regola | 509 | renatomaiolo@tiscali.it | 2022-06-29T08:50:48 | 2022-06-29T08:50:48 | 1 |
| `532` | Esclusa da una regola | 532 | abbondantemariano@gmail.com | 2022-07-12T08:59:43 | 2022-07-12T08:59:43 | 1 |
| `534` | Esclusa da una regola | 534 | abbondantemariano@gmail.com | 2022-07-12T09:06:42 | 2022-07-25T21:26:32 | 0 |
| `535` | Esclusa da una regola | 535 | abbondantemariano@gmail.com | 2022-07-12T12:54:41 | 2022-07-25T21:26:23 | 0 |
| `554` | Esclusa da una regola | 554 | Francy_a96@libero.it | 2022-07-25T13:51:28 | 2022-07-25T21:33:56 | 0 |
| `558` | Esclusa da una regola | 558 | dsds@sdsd.it | 2022-07-25T21:28:16 | 2022-07-25T21:33:29 | 0 |
| `559` | Esclusa da una regola | 559 | dfddf@fdfdf.it | 2022-07-25T21:28:38 | 2022-07-25T21:33:34 | 0 |
| `567` | Esclusa da una regola | 567 | vestrter@tem.eu | 2022-07-27T15:55:36 | 2022-07-27T15:57:02 | 0 |
| `568` | Esclusa da una regola | 568 | vestrter@tem.eu | 2022-07-27T15:57:59 | 2022-07-29T10:42:17 | 0 |
| `576` | Esclusa da una regola | 576 | annamaria.scopacasa@alice.it | 2022-08-01T14:03:18 | 2022-08-01T14:03:18 | 1 |
| `577` | Esclusa da una regola | 577 | annamaria.scopacasa@alice.it | 2022-08-01T14:18:13 | 2022-08-02T22:57:13 | 0 |
| `582` | Esclusa da una regola | 582 | serena.ieraci18@gmail.com | 2022-08-01T15:20:40 | 2022-08-01T15:20:40 | 1 |
| `586` | Esclusa da una regola | 586 | agaveombrelloni@libero.it | 2022-08-01T15:57:03 | 2022-08-02T22:56:27 | 0 |
| `587` | Esclusa da una regola | 587 | agaveombrelloni@libero.it | 2022-08-01T17:04:04 | 2022-08-01T17:04:04 | 1 |
| `590` | Esclusa da una regola | 590 | serena.ieraci18@gmail.com | 2022-08-02T07:11:21 | 2022-08-02T22:56:43 | 0 |
| `596` | Esclusa da una regola | 596 | serena.ieraci18@gmail.com | 2022-08-02T17:01:58 | 2022-09-26T09:54:48 | 0 |
| `598` | Esclusa da una regola | 598 | serena.ieraci18@gmail.com | 2022-08-02T18:41:00 | 2022-08-02T22:56:12 | 0 |
| `605` | Esclusa da una regola | 605 | gianfranconirello24@gmail.com | 2022-08-03T15:30:06 | 2022-08-03T15:30:06 | 1 |
| `609` | Esclusa da una regola | 609 | romanoantonio598@gmail.com | 2022-08-06T09:40:58 | 2022-08-06T09:40:58 | 1 |
| `617` | Esclusa da una regola | 617 | kristianilbrutto@gmail.com | 2022-08-12T10:41:10 | 2022-08-12T10:41:10 | 1 |
| `628` | Esclusa da una regola | 628 | hussain.shaikh@solace.infotech.com | 2022-08-17T11:01:08 | 2022-09-26T09:54:00 | 0 |
| `631` | Esclusa da una regola | 631 | hussain.shaikh@solace.infotech.com | 2022-08-18T09:28:43 | 2022-08-18T09:28:43 | 1 |
| `634` | Esclusa da una regola | 634 | miriam.scrivo@yahoo.it | 2022-08-19T10:28:31 | 2022-08-19T10:28:31 | 1 |
| `644` | Esclusa da una regola | 644 | fildat@gmail.com | 2022-09-01T13:48:11 | 2022-09-01T14:01:19 | 0 |
| `646` | Esclusa da una regola | 646 | aditya123@yopmail.com | 2022-09-01T14:22:25 | 2022-09-26T09:51:42 | 0 |
| `649` | Esclusa da una regola | 649 | aditya123@yopmail.com | 2022-09-01T14:28:24 | 2022-09-26T09:51:55 | 0 |
| `650` | Esclusa da una regola | 650 | aditya123@yopmail.com | 2022-09-01T14:29:24 | 2022-09-26T09:52:19 | 0 |
| `651` | Esclusa da una regola | 651 | aditya123@yopmail.com | 2022-09-01T14:37:07 | 2022-09-26T09:52:07 | 0 |
| `653` | Esclusa da una regola | 653 | nashikgodavari@yopmail.com | 2022-09-01T15:03:10 | 2022-09-26T09:52:34 | 0 |
| `654` | Esclusa da una regola | 654 | nashikgodavari@yopmail.com | 2022-09-01T15:19:55 | 2022-09-26T09:52:48 | 0 |
| `655` | Esclusa da una regola | 655 | nashikgodavari@yopmail.com | 2022-09-01T15:26:46 | 2022-09-26T09:50:36 | 0 |
| `656` | Esclusa da una regola | 656 | nashikgodavari@yopmail.com | 2022-09-01T15:29:46 | 2022-09-26T09:50:41 | 0 |
| `657` | Esclusa da una regola | 657 | nashikgodavari@yopmail.com | 2022-09-01T15:33:14 | 2022-09-26T09:50:46 | 0 |
| `658` | Esclusa da una regola | 658 | nashikgodavari@yopmail.com | 2022-09-01T15:36:36 | 2022-09-26T09:50:51 | 0 |
| `659` | Esclusa da una regola | 659 | nashikgodavari@yopmail.com | 2022-09-02T06:24:52 | 2022-09-26T09:51:02 | 0 |
| `661` | Esclusa da una regola | 661 | nashikgodavari@yopmail.com | 2022-09-02T06:37:59 | 2022-09-26T09:51:13 | 0 |
| `662` | Esclusa da una regola | 662 | nashikgodavari@yopmail.com | 2022-09-02T07:03:45 | 2022-09-26T09:51:19 | 0 |
| `676` | Esclusa da una regola | 676 | demo@summerbooking.it | 2022-10-11T06:35:41 | 2022-10-20T09:04:35 | 0 |
| `678` | Esclusa da una regola | 678 | horecanetitalia@libero.it | 2022-10-19T09:29:49 | 2022-10-20T09:04:31 | 0 |
| `684` | Esclusa da una regola | 684 | pasqualepalumbo1975@gmail.com | 2022-11-18T09:37:27 | 2023-01-02T12:36:20 | 0 |
| `686` | Esclusa da una regola | 686 | mstirparo@alice.it | 2022-11-25T12:18:51 | 2022-11-25T12:18:51 | 1 |
| `689` | Esclusa da una regola | 689 | info@lidosanbasilio.it | 2023-01-24T10:47:18 | 2023-02-02T15:36:04 | 0 |
| `692` | Esclusa da una regola | 692 | francescapiscionieri@live.it | 2023-02-09T09:22:00 | 2023-02-09T09:22:00 | 1 |
| `699` | Esclusa da una regola | 699 | pietrebianchevip@gmail.com | 2023-02-20T22:09:21 | 2023-02-20T22:09:21 | 1 |
| `716` | Esclusa da una regola | 716 | yixata5230@luxeic.com | 2023-03-07T21:43:30 | 2023-03-07T21:43:30 | 1 |
| `717` | Esclusa da una regola | 717 | mattiacerasoli@gmail.com | 2023-03-08T17:04:37 | 2023-03-08T17:04:37 | 1 |
| `724` | Esclusa da una regola | 724 | Tonirotundo6@gmail.com | 2023-03-14T10:12:23 | 2023-03-14T10:12:23 | 1 |
| `751` | Esclusa da una regola | 751 | ashish.shinde27@gmail.com | 2023-03-28T09:13:51 | 2023-03-28T09:13:51 | 1 |
| `770` | Esclusa da una regola | 770 | francesco.concolino@libero.it | 2023-04-06T06:08:00 | 2023-04-06T06:08:00 | 1 |
| `771` | Esclusa da una regola | 771 | francesco.concolino@libero.it | 2023-04-06T06:11:40 | 2023-04-06T06:11:40 | 1 |
| `772` | Esclusa da una regola | 772 | francesco.concolino@libero.it | 2023-04-06T06:17:11 | 2023-04-06T06:17:11 | 1 |
| `773` | Esclusa da una regola | 773 | francesco.concolino@libero.it | 2023-04-06T18:36:53 | 2023-04-06T18:36:53 | 1 |
| `774` | Esclusa da una regola | 774 | francesco.concolino@libero.it | 2023-04-06T18:39:15 | 2023-04-06T18:39:15 | 1 |
| `775` | Esclusa da una regola | 775 | francesco.concolino@libero.it | 2023-04-07T12:18:48 | 2023-04-07T12:18:48 | 1 |
| `777` | Esclusa da una regola | 777 | Mariaclaudia.mosca@gmail.com | 2023-04-10T14:14:59 | 2023-04-10T14:14:59 | 1 |
| `778` | Esclusa da una regola | 778 | mariaclaudia.mosca@gmail.com | 2023-04-10T17:20:46 | 2023-04-10T17:20:46 | 1 |
| `782` | Esclusa da una regola | 782 | natipittella@hotmail.com | 2023-04-12T21:52:32 | 2023-04-12T21:52:32 | 1 |
| `792` | Esclusa da una regola | 792 | info@circoloarca.org | 2023-04-20T08:40:15 | 2023-04-20T08:40:15 | 1 |
| `802` | Esclusa da una regola | 802 | ashish.shinde27@gmail.com | 2023-04-21T14:12:37 | 2023-04-21T14:12:37 | 1 |
| `805` | Esclusa da una regola | 805 | beach@yopmail.com | 2023-04-21T14:24:28 | 2023-05-11T21:22:49 | 0 |
| `807` | Esclusa da una regola | 807 | beach@yopmail.com | 2023-04-21T14:31:31 | 2023-05-11T21:23:04 | 0 |
| `809` | Esclusa da una regola | 809 | summerbeachview@yopmail.com | 2023-04-21T15:02:53 | 2023-05-11T21:22:35 | 0 |
| `810` | Esclusa da una regola | 810 | summerbeachview@yopmail.com | 2023-04-21T15:07:21 | 2023-05-11T21:22:55 | 0 |
| `811` | Esclusa da una regola | 811 | summerbeachview@yopmail.com | 2023-04-21T15:08:06 | 2023-05-11T21:22:59 | 0 |
| `812` | Esclusa da una regola | 812 | summerbeachview@yopmail.com | 2023-04-21T15:12:09 | 2023-05-11T21:22:14 | 0 |
| `813` | Esclusa da una regola | 813 | summerbeachview@yopmail.com | 2023-04-21T15:22:56 | 2023-05-11T21:22:19 | 0 |
| `817` | Esclusa da una regola | 817 | testingaccount@yopmail.com | 2023-04-24T08:44:23 | 2023-05-11T21:22:03 | 0 |
| `819` | Esclusa da una regola | 819 | zona27.bagniole@gmail.com | 2023-04-26T08:35:52 | 2023-05-11T21:21:58 | 0 |
| `823` | Esclusa da una regola | 823 | c.loiarro@tiscali.it | 2023-04-28T15:41:24 | 2023-04-28T15:41:24 | 1 |
| `824` | Esclusa da una regola | 824 | franci_cirillo@yahoo.it | 2023-04-30T10:26:47 | 2023-05-11T21:21:44 | 0 |
| `828` | Esclusa da una regola | 828 | marinabluclub@gmail.com | 2023-05-03T13:00:21 | 2023-05-11T21:21:37 | 0 |
| `829` | Esclusa da una regola | 829 | marinabluclub@gmail.com | 2023-05-03T13:01:44 | 2026-03-18T15:10:12 | 0 |
| `831` | Esclusa da una regola | 831 | manuelacalio@libero.it | 2023-05-05T10:05:43 | 2023-05-05T10:05:43 | 1 |
| `835` | Esclusa da una regola | 835 | sognodimezzestate@gmail.com | 2023-05-08T08:37:26 | 2023-05-08T08:37:26 | 1 |
| `843` | Esclusa da una regola | 843 | amorusob@gmail.com | 2023-05-25T16:23:42 | 2023-05-25T16:23:42 | 1 |
| `844` | Esclusa da una regola | 844 | amorusob@gmail.com | 2023-05-25T16:35:16 | 2023-05-25T16:35:16 | 1 |
| `850` | Esclusa da una regola | 850 | sportingclubcb@yahoo.it | 2023-05-29T14:42:50 | 2023-05-29T14:42:50 | 1 |
| `861` | Esclusa da una regola | 861 | raffaele.vitiello9988@gmail.com | 2023-06-02T12:56:49 | 2023-06-02T12:56:49 | 1 |
| `872` | Esclusa da una regola | 872 | lidolascogliera@alice.it | 2023-06-09T08:33:57 | 2023-06-09T08:33:57 | 1 |
| `875` | Esclusa da una regola | 875 | info@sicilyboatrental.com | 2023-06-12T10:34:03 | 2023-06-12T10:34:03 | 1 |
| `881` | Esclusa da una regola | 881 | info@sacoloniabeach.it | 2023-06-15T16:49:44 | 2023-06-15T16:49:44 | 1 |
| `895` | Esclusa da una regola | 895 | ktmsxf410@gmail.com | 2023-06-19T07:52:25 | 2023-06-19T07:52:25 | 1 |
| `900` | Esclusa da una regola | 900 | info@lidotirreno.it | 2023-06-21T10:26:55 | 2023-06-21T10:26:55 | 1 |
| `902` | Esclusa da una regola | 902 | asgrigio@gmail.com | 2023-06-21T14:52:24 | 2023-06-21T14:52:24 | 1 |
| `908` | Esclusa da una regola | 908 | ritagirone.rg@libero.it | 2023-06-24T10:38:13 | 2023-06-24T10:38:13 | 1 |
| `909` | Esclusa da una regola | 909 | virgypini05@gmail.com | 2023-06-24T10:58:51 | 2023-06-24T10:58:51 | 1 |
| `911` | Esclusa da una regola | 911 | ritagirone.rg@libero.it | 2023-06-24T11:32:52 | 2023-06-24T11:32:52 | 1 |
| `912` | Esclusa da una regola | 912 | ritagirone.rg@libero.it | 2023-06-24T11:32:59 | 2023-06-24T11:32:59 | 1 |
| `913` | Esclusa da una regola | 913 | ritagirone.rg@libero.it | 2023-06-24T11:33:06 | 2023-06-24T11:33:06 | 1 |
| `929` | Esclusa da una regola | 929 | holidaybeach@libero.it | 2023-06-30T09:10:27 | 2023-06-30T09:10:27 | 1 |
| `955` | Esclusa da una regola | 955 | angela-pizzuti@hotmail.it | 2023-07-08T12:49:28 | 2023-07-08T12:49:28 | 1 |
| `956` | Esclusa da una regola | 956 | angela-pizzuti@hotmail.it | 2023-07-08T12:49:30 | 2023-07-08T12:49:30 | 1 |
| `961` | Esclusa da una regola | 961 | mafricca@libero.it | 2023-07-08T15:25:06 | 2023-07-08T15:25:06 | 1 |
| `981` | Esclusa da una regola | 981 | gemiliano21@tiscali.it | 2023-07-12T10:20:24 | 2023-07-12T10:20:24 | 1 |
| `1015` | Esclusa da una regola | 1015 | kristianberaldo@gmail.com | 2023-07-16T14:43:31 | 2023-07-16T14:43:31 | 1 |
| `1036` | Esclusa da una regola | 1036 | Kettytuscano05@gmail.com | 2023-07-22T18:33:16 | 2023-07-22T18:33:16 | 1 |
| `1041` | Esclusa da una regola | 1041 | arcangeloguzzo@studiocompagno.it | 2023-07-24T08:30:49 | 2023-07-24T08:30:49 | 1 |
| `1042` | Esclusa da una regola | 1042 | studiolegalegrillone@tiscali.it | 2023-07-24T14:11:25 | 2023-07-24T14:11:25 | 1 |
| `1052` | Esclusa da una regola | 1052 | domenico.nicotera@libero.it | 2023-07-27T15:41:42 | 2023-07-27T15:41:42 | 1 |
| `1069` | Esclusa da una regola | 1069 | lee.82@live.it | 2023-07-30T17:57:15 | 2023-07-30T17:57:15 | 1 |
| `1122` | Esclusa da una regola | 1122 | Marco.orru@inwind.it | 2023-08-18T14:50:38 | 2023-08-18T14:50:38 | 1 |
| `1124` | Esclusa da una regola | 1124 | Marco.orru@inwind.it | 2023-08-19T02:02:44 | 2023-08-19T02:02:44 | 1 |
| `1152` | Esclusa da una regola | 1152 | valentino.rocco2004@gmail.com | 2023-10-02T17:46:40 | 2023-10-02T17:46:40 | 1 |
| `1153` | Esclusa da una regola | 1153 | valentino.rocco2004@gmail.com | 2023-10-02T17:54:00 | 2023-10-02T17:54:00 | 1 |
| `1156` | Esclusa da una regola | 1156 | nomemil880@glalen.com | 2023-11-07T17:58:29 | 2023-11-07T17:58:29 | 1 |
| `1168` | Esclusa da una regola | 1168 | lidoigabbiani@libero.it | 2024-01-23T14:50:09 | 2024-01-23T14:50:09 | 1 |
| `1172` | Esclusa da una regola | 1172 | hotelalbs@gmail.com | 2024-01-30T10:40:51 | 2024-01-30T10:40:51 | 1 |
| `1175` | Esclusa da una regola | 1175 | bagnolavela@gmail.com | 2024-02-14T10:03:59 | 2024-02-14T10:03:59 | 1 |
| `1177` | Esclusa da una regola | 1177 | info@lalamparabeach.it | 2024-02-20T16:35:10 | 2024-02-20T16:35:10 | 1 |
| `1178` | Esclusa da una regola | 1178 | info@lalamparabeach.it | 2024-02-20T16:36:03 | 2024-02-20T16:36:03 | 1 |
| `1182` | Esclusa da una regola | 1182 | info@bagnocris.it | 2024-02-29T10:35:18 | 2024-02-29T10:35:18 | 1 |
| `1186` | Esclusa da una regola | 1186 | amministrazione@barbatoja.it | 2024-03-06T16:17:35 | 2024-03-06T16:17:35 | 1 |
| `1189` | Esclusa da una regola | 1189 | infinitisrls@gmail.com | 2024-03-13T16:47:00 | 2024-03-13T16:47:00 | 1 |
| `1191` | Esclusa da una regola | 1191 | Ferrotiziano-@libero.it | 2024-03-16T22:31:24 | 2024-03-16T22:31:24 | 1 |
| `1193` | Esclusa da una regola | 1193 | Ferrotiziano-@libero.it | 2024-03-22T19:29:01 | 2024-03-22T19:29:01 | 1 |
| `1195` | Esclusa da una regola | 1195 | titomax55@hotmail.com | 2024-04-04T07:07:54 | 2024-04-04T07:07:54 | 1 |
| `1201` | Esclusa da una regola | 1201 | residencehotelsrl@gmail.com | 2024-04-10T14:08:24 | 2024-04-10T14:08:24 | 1 |
| `1203` | Esclusa da una regola | 1203 | Colacinomariarosa@libero.it | 2024-04-10T15:23:01 | 2024-04-10T15:23:01 | 1 |
| `1204` | Esclusa da una regola | 1204 | colacinomariarosa@libero.it | 2024-04-10T15:26:47 | 2024-04-10T15:26:47 | 1 |
| `1206` | Esclusa da una regola | 1206 | amministrazione@barbatoja.it | 2024-04-12T08:39:36 | 2024-04-12T08:39:36 | 1 |
| `1209` | Esclusa da una regola | 1209 | rita.ranieri@uniecampus.it | 2024-04-18T09:20:33 | 2024-04-18T09:20:33 | 1 |
| `1212` | Esclusa da una regola | 1212 | info@lidosolesplendid.it | 2024-05-02T08:04:55 | 2024-05-02T08:04:55 | 1 |
| `1215` | Esclusa da una regola | 1215 | andreacurto02@gmail.com | 2024-05-07T15:08:15 | 2024-05-07T15:08:15 | 1 |
| `1222` | Esclusa da una regola | 1222 | molinaro.alessia99@gmail.com | 2024-05-19T16:13:25 | 2024-05-19T16:13:25 | 1 |
| `1225` | Esclusa da una regola | 1225 | lidoholidaybeach@gmail.com | 2024-05-21T09:33:48 | 2024-05-21T09:33:48 | 1 |
| `1234` | Esclusa da una regola | 1234 | adele.mercurio@tiscali.it | 2024-05-29T06:27:38 | 2024-05-29T06:27:38 | 1 |
| `1239` | Esclusa da una regola | 1239 | jessicares@libero.it | 2024-05-30T13:00:54 | 2024-05-30T13:00:54 | 1 |
| `1242` | Esclusa da una regola | 1242 | cieroriccardo@gmail.com | 2024-05-30T13:57:06 | 2024-05-30T13:57:06 | 1 |
| `1243` | Esclusa da una regola | 1243 | agaveombrelloni@libero.it | 2024-05-31T11:48:16 | 2024-05-31T11:48:16 | 1 |
| `1244` | Esclusa da una regola | 1244 | flav.savi@gmail.com | 2024-05-31T17:35:06 | 2024-05-31T17:35:06 | 1 |
| `1245` | Esclusa da una regola | 1245 | cieroriccardo@gmail.com | 2024-06-01T07:38:00 | 2024-06-01T07:38:00 | 1 |
| `1248` | Esclusa da una regola | 1248 | antoniolabbate430@gmail.com | 2024-06-01T13:31:08 | 2024-06-01T13:31:08 | 1 |
| `1254` | Esclusa da una regola | 1254 | ercole.vagnozzi@gmil.com | 2024-06-03T12:17:17 | 2024-06-03T12:17:17 | 1 |
| `1256` | Esclusa da una regola | 1256 | ercole.vagnozzi@gmail.com | 2024-06-03T12:43:30 | 2024-06-03T12:43:30 | 1 |
| `1263` | Esclusa da una regola | 1263 | flav.savi@gmail.com | 2024-06-04T11:27:50 | 2024-06-04T11:27:50 | 1 |
| `1269` | Esclusa da una regola | 1269 | sergidomenico19@gmail.com | 2024-06-05T07:34:29 | 2024-06-05T07:34:29 | 1 |
| `1293` | Esclusa da una regola | 1293 | vertigo00d@gmail.com | 2024-06-07T13:40:18 | 2024-06-07T13:40:18 | 1 |
| `1300` | Esclusa da una regola | 1300 | giuseppediserafino@gmail.com | 2024-06-08T09:39:27 | 2024-06-08T09:39:27 | 1 |
| `1304` | Esclusa da una regola | 1304 | francescomandolfo1@gmail.com | 2024-06-10T05:45:19 | 2024-06-10T05:45:19 | 1 |
| `1305` | Esclusa da una regola | 1305 | francescomandolfo1@gmail.com | 2024-06-10T05:45:23 | 2024-06-10T05:45:23 | 1 |
| `1312` | Esclusa da una regola | 1312 | francescomandolfo1@gmail.com | 2024-06-12T12:52:51 | 2024-06-12T12:52:51 | 1 |
| `1317` | Esclusa da una regola | 1317 | casciolimarica78@gmail.com | 2024-06-14T17:29:08 | 2024-06-14T17:29:08 | 1 |
| `1331` | Esclusa da una regola | 1331 | scilla.paini@ymail.com | 2024-06-17T06:43:35 | 2024-06-17T06:43:35 | 1 |
| `1339` | Esclusa da una regola | 1339 | info@dogbeachsolidago.it | 2024-06-19T10:59:19 | 2024-06-19T10:59:19 | 1 |
| `1340` | Esclusa da una regola | 1340 | bertucci1964@gmail.com | 2024-06-19T11:02:24 | 2024-06-19T11:02:24 | 1 |
| `1344` | Esclusa da una regola | 1344 | elib_fly@hotmail.com | 2024-06-20T11:17:40 | 2024-06-20T11:17:40 | 1 |
| `1346` | Esclusa da una regola | 1346 | yosibi8092@luravell.com | 2024-06-20T15:00:47 | 2024-06-20T15:00:47 | 1 |
| `1347` | Esclusa da una regola | 1347 | ralefey812@dovinou.com | 2024-06-20T15:03:42 | 2024-06-20T15:03:42 | 1 |
| `1348` | Esclusa da una regola | 1348 | yosibi8092@luravell.com | 2024-06-20T15:05:06 | 2024-06-20T15:05:06 | 1 |
| `1349` | Esclusa da una regola | 1349 | yosibi8092@luravell.com | 2024-06-20T15:05:51 | 2024-06-20T15:05:51 | 1 |
| `1350` | Esclusa da una regola | 1350 | ralefey812@dovinou.com | 2024-06-20T15:06:04 | 2024-06-20T15:06:04 | 1 |
| `1352` | Esclusa da una regola | 1352 | yosibi8092@luravell.com | 2024-06-20T15:08:41 | 2024-06-20T15:08:41 | 1 |
| `1353` | Esclusa da una regola | 1353 | yosibi8092@luravell.com | 2024-06-20T15:11:28 | 2024-06-20T15:11:28 | 1 |
| `1354` | Esclusa da una regola | 1354 | yosibi8092@luravell.com | 2024-06-20T15:12:48 | 2024-06-20T15:12:48 | 1 |
| `1356` | Esclusa da una regola | 1356 | elib_fly@hotmail.com | 2024-06-20T16:27:43 | 2024-06-20T16:27:43 | 1 |
| `1359` | Esclusa da una regola | 1359 | elib_fly@hotmail.com | 2024-06-21T11:44:06 | 2024-06-21T11:44:06 | 1 |
| `1369` | Esclusa da una regola | 1369 | josephfox59295@gmail.com | 2024-06-23T16:19:03 | 2024-06-23T16:19:03 | 1 |
| `1370` | Esclusa da una regola | 1370 | josephfox59295@gmail.com | 2024-06-23T16:19:04 | 2024-06-23T16:19:04 | 1 |
| `1373` | Esclusa da una regola | 1373 | carbonefilippo@libero.it | 2024-06-24T11:06:25 | 2024-06-24T11:06:25 | 1 |
| `1381` | Esclusa da una regola | 1381 | vicky2107.lv@gmail.com | 2024-06-25T16:39:18 | 2024-06-25T16:39:18 | 1 |
| `1408` | Esclusa da una regola | 1408 | krupp78@mail.ru | 2024-06-29T20:23:14 | 2024-06-29T20:23:14 | 1 |
| `1418` | Esclusa da una regola | 1418 | info@ricossrl.com | 2024-07-01T14:41:27 | 2024-07-01T14:41:27 | 1 |
| `1420` | Esclusa da una regola | 1420 | bagninosb@yopmail.com | 2024-07-01T16:15:10 | 2024-07-01T16:15:10 | 1 |
| `1421` | Esclusa da una regola | 1421 | cameriere@yopmail.com | 2024-07-01T16:16:00 | 2024-07-01T16:16:00 | 1 |
| `1433` | Esclusa da una regola | 1433 | danyred65@hotmail.it | 2024-07-04T15:52:53 | 2024-07-04T15:52:53 | 1 |
| `1436` | Esclusa da una regola | 1436 | quiricoassetti@outlook.it | 2024-07-05T14:50:09 | 2024-07-05T14:50:09 | 1 |
| `1447` | Esclusa da una regola | 1447 | cecio3279@hotmail.com | 2024-07-07T20:27:31 | 2024-07-07T20:27:31 | 1 |
| `1448` | Esclusa da una regola | 1448 | cecio3279@hotmail.com | 2024-07-07T20:28:53 | 2024-07-07T20:28:53 | 1 |
| `1449` | Esclusa da una regola | 1449 | fabrizio.baccarini@gmail.com | 2024-07-08T08:55:54 | 2024-07-08T08:55:54 | 1 |
| `1468` | Esclusa da una regola | 1468 | giuseppemarandino@gmail.com | 2024-07-12T08:47:03 | 2024-07-12T08:47:03 | 1 |
| `1470` | Esclusa da una regola | 1470 | cecio3279@hotmail.com | 2024-07-12T10:55:58 | 2024-07-12T10:55:58 | 1 |
| `1515` | Esclusa da una regola | 1515 | danda.sgr@libero.it | 2024-07-21T12:08:43 | 2024-07-21T12:08:43 | 1 |
| `1516` | Esclusa da una regola | 1516 | danda.sgr@libero.it | 2024-07-21T12:08:49 | 2024-07-21T12:08:49 | 1 |
| `1517` | Esclusa da una regola | 1517 | danda.sgr@libero.it | 2024-07-21T12:08:50 | 2024-07-21T12:08:50 | 1 |
| `1518` | Esclusa da una regola | 1518 | danda.sgr@libero.it | 2024-07-21T12:10:22 | 2024-07-21T12:10:22 | 1 |
| `1553` | Esclusa da una regola | 1553 | roccofoderaro22@gmail.com | 2024-07-30T06:19:16 | 2024-07-30T06:19:16 | 1 |
| `1554` | Esclusa da una regola | 1554 | roccofoderaro22@gmail.com | 2024-07-30T06:20:25 | 2024-07-30T06:20:25 | 1 |
| `1563` | Esclusa da una regola | 1563 | danila.basso@gmail.com | 2024-08-01T11:21:51 | 2024-08-01T11:21:51 | 1 |
| `1583` | Esclusa da una regola | 1583 | rossella_pignataro@virgilio.it | 2024-08-06T05:46:28 | 2024-08-06T05:46:28 | 1 |
| `1590` | Esclusa da una regola | 1590 | venditasoppress@gmail.com | 2024-08-08T11:21:39 | 2024-08-08T11:21:39 | 1 |
| `1639` | Esclusa da una regola | 1639 | laghiroom@gmail.com | 2024-11-11T10:42:43 | 2024-11-11T10:42:43 | 1 |
| `1641` | Esclusa da una regola | 1641 | segreteria@marinevillage.it | 2024-12-04T10:35:30 | 2024-12-04T10:35:30 | 1 |
| `1642` | Esclusa da una regola | 1642 | segreteria@marinevillage.it | 2024-12-04T10:36:22 | 2024-12-04T10:36:22 | 1 |
| `1644` | Esclusa da una regola | 1644 | bagnisporting@gmail.com | 2024-12-12T15:03:49 | 2024-12-12T15:03:49 | 1 |
| `1648` | Esclusa da una regola | 1648 | mazzarif@hotmail.it | 2024-12-30T11:02:54 | 2024-12-30T11:02:54 | 1 |

> Mostrate le prime **300** di **1010**. Le restanti 710 sono in [`dati/notification_marketing_newsletter_subscriptions.jsonl`](dati/notification_marketing_newsletter_subscriptions.jsonl), con tutte le colonne sorgente.
