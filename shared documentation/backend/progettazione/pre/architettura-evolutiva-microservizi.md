# Architettura evolutiva: monolite modulare → microservizi

> Documento di indirizzo architetturale per la riscrittura in **.NET 10 (Web API) + EF Core**.
> Obiettivo: costruire oggi un sistema che, un domani, possa essere spacchettato in microservizi
> **senza riscritture**, senza pagarne ora i costi operativi.
> Complemento a [refactoring-analisi-A-vs-B.md](refactoring-analisi-A-vs-B.md).

---

## 1. Principio guida

Nel contesto attuale (**1 tenant, ~500 clienti, 2-3 dev**) i microservizi darebbero soprattutto costi
(deploy distribuiti, transazioni distribuite, osservabilità, consistenza eventuale) senza i benefici
(non serve scalare team o traffico in modo indipendente). La regola consolidata *"monolith first"*
(Fowler / Sam Newman) qui vale a maggior ragione.

**Ma "monolite" non significa ricreare il big ball of mud attuale.** Si costruisce un **monolite
modulare**: un solo deployabile, diviso internamente in moduli con confini veri. Rispettando tre regole,
l'estrazione futura di un modulo in microservizio diventa quasi meccanica.

### Le tre regole di confine (non negoziabili)

1. **Ogni modulo possiede i suoi dati** — le sue tabelle/schema. Nessuna FK e nessuna JOIN che
   attraversa il confine del modulo.
2. **I moduli comunicano solo tramite interfacce** — in-process oggi, chiamata di rete domani. Mai
   leggere le tabelle di un altro modulo.
3. **Nessuno stato mutabile condiviso** — i riferimenti fra domini sono per **ID**, risolti chiedendo
   al modulo proprietario tramite la sua interfaccia.

È **questa disciplina**, non la topologia di deploy, a rendere possibile lo split. Il monolite attuale
è inestraibile proprio perché fa JOIN su tutto: la riscrittura è l'occasione per non ripetere l'errore.

---

## 2. Divisione in moduli (bounded context)

### 2.1 Valutazione di una prima ipotesi di divisione

| Blocco proposto | Verdetto | Nota |
|---|---|---|
| **Fiskaly** | ✅ Ottimo confine | Contratto esterno, compliance di legge, ciclo di vita proprio. Candidato n.1 all'estrazione. |
| **Switchboard** | ✅ Ottimo confine | Runtime completamente diverso (SIP/real-time), marginale al core, deve poter fallire senza fermare le prenotazioni. Candidato n.1 all'isolamento. |
| **Payment (Stripe)** | ✅ Buono | Accoppiato a Booking (il pagamento è di una prenotazione) e a Fiskaly (scontrino dopo incasso): l'accoppiamento va gestito **a eventi**, non a JOIN. |
| **Notification (email/WhatsApp)** | ✅ Buono | Naturalmente asincrono/event-driven. ⚠️ WhatsApp è usato anche dallo Switchboard (`whatsapp.helper`): va deciso chi possiede il gateway di messaggistica. |
| **API per esterni (ofsale, transaction, teamsystem)** | ⚠️ Da ripensare | Non è un dominio, è una **preoccupazione di integrazione**. `teamsystem` = export contabile, `transactionApi` = transazioni (vicino a Payment/Revenue), `ofsale` = POS. Raggrupparli solo perché "esterni" mescola domini diversi. Meglio: ognuno vicino al suo dominio + un **gateway API sottile** verso i partner. |
| **Core** | ⚠️ Troppo grande | Come primo servizio va bene, ma **dentro** va tenuto diviso in moduli distinti (Auth, Config, Booking): sono bounded context diversi che un domani si vorranno separare. |

### 2.2 Divisione target consigliata (moduli oggi → potenziali servizi domani)

| Modulo | Dominio (rif. analisi) | Responsabilità | Estraibilità |
|---|---|---|---|
| **Identity/Auth** | C5 | Autenticazione, ruoli, RBAC | Alta (trasversale, riusabile) |
| **Configuration/Catalog** | C2 | Pannello manager: settori, stagioni, listini, tipi servizio, mappe, sconti | Media (dati di riferimento letti dal booking) |
| **Booking/Reservation** | C1 | Prenotazioni, disponibilità, il cuore transazionale | Bassa (è il centro) |
| **Payment** | C3 | Stripe, incassi, webhook | Alta |
| **Fiskaly** | C4 | Scontrino fiscale elettronico | Alta |
| **Notification** | C6 | Email, WhatsApp, notifiche, marketing | Alta |
| **Switchboard** | C7 | Centralino/IVR, SIP, telefonia | Alta |
| **Revenue/Reporting** | (da C3) | Read model, reportistica, letture pesanti | Alta (si stacca facilmente) |
| **Partner/Integration gateway** | ofsale, teamsystem, transactionApi | Anti-corruption layer verso sistemi esterni | Media |

---

## 3. Strategia dati: dividere il DB in più DB

**Sì, è possibile** — l'abilitatore è la Regola 1: ogni modulo possiede le sue tabelle e non fa JOIN
fuori dal confine. Strategia a due tempi:

### Fase monolite — un solo DB fisico, N schemi logici
- Uno **schema per modulo**: `identity`, `config`, `booking`, `payment`, `fiskaly`, `notification`,
  `switchboard`, `revenue`.
- **Nessuna FK cross-schema**; i riferimenti fra domini sono per **ID**.
- In EF Core: **un `DbContext` per modulo**, ciascuno mappato sul suo schema.

### Fase estrazione — schema → DB fisico separato
- Quando un servizio viene estratto, il suo schema si sposta in un DB dedicato: si cambia solo la
  connection string del suo `DbContext`.
- Poiché non c'erano JOIN/FK cross-schema, l'operazione è **meccanica**.

Il punto difficile **non è lo split fisico**, è la **disciplina del "niente JOIN/transazioni
cross-modulo"** mantenuta fin dal monolite.

### Prezzo da mettere in conto (solo quando si separano davvero i DB)
Persi ACID e JOIN tra servizi, la consistenza si gestisce con:
- **Eventi / consistenza eventuale** — es. Booking emette `BookingConfirmed` → Payment/Fiskaly/Notification reagiscono.
- **Saga** — per i flussi multi-step: prenota → paga → scontrino → notifica.
- **Duplicazione dati / read model** — dove un servizio ha bisogno dei dati di un altro per interrogarli.

È complessità reale: per questo la si rinvia finché non c'è un driver concreto.

---

## 4. Quando estrarre davvero un microservizio

Non estrarre finché non esiste un **driver concreto** (scaling indipendente, isolamento dei guasti,
isolamento compliance, team separato). Con 1 tenant e 2-3 dev, un monolite ben modularizzato sarà
probabilmente lo stato finale giusto per parecchio tempo. Candidati con driver più forte, in ordine:

1. **Switchboard** — isolamento dei guasti + runtime diverso (real-time).
2. **Fiskaly** — isolamento compliance (obbligo di legge).
3. **Notification** — scaling asincrono, fire-and-forget.
4. **Payment** — sicurezza/PCI + disponibilità dei webhook.

---

## 5. Come si impone in .NET (monolite "microservices-ready")

- **Un progetto/assembly per modulo** nella stessa solution; un solo processo deployato.
- Ogni modulo espone una **interfaccia pubblica** (application service) e nasconde l'implementazione;
  gli altri moduli vedono solo l'interfaccia.
- **Comunicazione sincrona** fra moduli tramite quelle interfacce (in-process → rete domani).
- **Comunicazione asincrona** tramite un **event bus in-memory** che si potrà sostituire con un broker
  reale (RabbitMQ / Azure Service Bus) senza toccare i moduli. (MediatR è un'opzione per il dispatch.)
- **Un `DbContext` + uno schema per modulo**; riferimenti cross-modulo per ID, mai per navigazione EF.
- **Test di architettura** (NetArchTest / ArchUnitNET) che falliscono la build se un modulo dipende
  dalle interne di un altro: è ciò che impedisce al confine di degradare nel tempo.

---

## 6. Sintesi

- Costruire un **monolite modulare in .NET** con confini imposti (progetti separati, `DbContext` per
  modulo, schema per modulo, test di architettura). Fattibile e corretto per il contesto.
- Divisione buona su **Fiskaly / Switchboard / Payment / Notification**; **spacchettare "Core"** in
  Auth + Config + Booking; **non** raggruppare "API esterne" per dominio ma trattarle come gateway di
  integrazione.
- Il **DB si potrà dividere dopo** se e solo se oggi si impone *uno schema per modulo, zero JOIN
  cross-confine, riferimenti per ID*.
- **Non estrarre** microservizi senza un driver reale: nel medio termine, plausibilmente solo
  Switchboard / Fiskaly / Notification.
