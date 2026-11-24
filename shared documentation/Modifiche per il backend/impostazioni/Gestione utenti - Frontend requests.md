# Gestione utenti - Frontend requests

Le modifiche che il backend deve fare perché la pagina **Gestione utenti** del gestionale funzioni come la vuole il
frontend. Il documento è pensato per chi le implementa (anche un agente): ogni sezione dice cosa creare, con payload,
risposta, regole ed errori attesi.

La pagina è `/settings/user-management`: una tabella con gli utenti dello stabilimento (Email, Cellulare, Tipo di
account) e i popup per aggiungere, modificare ed eliminare un utente. Dalla pagina si apre anche la schermata del
registro, `/settings/user-management/logs`.

## Riepilogo

| # | Metodo | Rotta | Cosa fa |
|---|---|---|---|
| 1 | POST | `/api/identity/users-list` | Elenco degli utenti per la tabella, a pagine |
| 2 | POST | `/api/identity/create-user` | Crea un utente con i campi del popup "Aggiungi nuovo utente" |
| 3 | GET | `/api/identity/get-selected-user` | Legge l'utente da modificare |
| 4 | POST | `/api/identity/update-user` | Salva i campi del popup "Modifica utente" |
| 5 | POST | `/api/audit/entries-list` | Voci del registro per la tabella, a pagine |
| 6 | POST | `/api/identity/user-combobox` | La tendina degli utenti (qui: filtro del registro) |
| 7 | POST | `/api/audit/print-log` | File Excel (`xlsx`) da stampare con le voci del registro filtrate |
| 8 | POST | `/api/audit/export-log` | File Excel con le voci del registro filtrate |
| 10 | GET + POST | `/api/identity/get-selected-user` e `/api/identity/update-user` | Controllo di versione: `version` nella lettura, `If-Match` ed `ETag` nel salvataggio, 409 `resource.version_conflict` |

Oltre alle API, la sezione 9 dice quali codici di azione del registro usare per le azioni che nessun modulo registra ancora
(cassa, ordini, ingresso segnalato). La sezione 10 aggiunge il controllo di versione alle API
3 e 4.

## Regole comuni

- **Moduli**: le API `/api/identity/*` stanno in Identity (`Module.Identity`), accanto a `/api/identity/property-users`
  (`Endpoints/PropertyEndpoints.cs`, `StaffService`), con i dati di `StaffUserDto`; le API `/api/audit/*` stanno in Audit
  (`Module.Audit`), accanto a `/api/audit/entries` (`Endpoints/PropertyAuditEndpoints.cs`, `AuditEntryService`), con le
  voci di `AuditEntryDto`.
- **Le API esistenti non cambiano**: `/api/identity/property-users` con le sue sotto-rotte, `/api/audit/entries`,
  `/api/audit/entries/export`, `/api/audit/action-codes` e `/api/audit/actors` restano come sono: stesse rotte,
  richieste, risposte e comportamento. Quando un'API nuova riusa la loro logica (es. `StaffService.UpdateAsync`,
  `AuditExportWriter`), i comportamenti nuovi chiesti qui (caselle "Abilita" salvate, sessioni chiuse solo se cambiano
  password o ruolo, periodo letto come giorni, lingua e fuso dei file) valgono per le API nuove.
- **Le liste sono le tabelle del frontend**: ogni elenco mostrato in una tabella usa la richiesta e la risposta standard
  delle tabelle del frontend (sezioni 1 e 5), con pagine, ricerca e ordinamento fatti dal backend.
- **Richiesta delle tabelle** (`users-list`, `entries-list`; `print-log` ed `export-log` hanno gli stessi campi senza
  `page` e `pageSize`):
  - `sortDirection` conta solo con `sortField` non vuoto: con `sortField` `""` il frontend manda sempre `Ascending` e il
    backend usa il suo ordine.
  - I `null` della colonna ordinata stanno dove li mette PostgreSQL: in fondo con `Ascending`, in cima con
    `Descending`.
  - Una `page` oltre l'ultima risponde 200 con `rows` vuoto e il `total` vero: il frontend passa da solo all'ultima
    pagina che c'è.
  - Valori non validi: 400 `validation.failed` con un errore `validation.invalid` sul campo: `page` (meno di 1),
    `pageSize` (non 10, 20, 50 o 100), `sortField` (non tra quelli della sezione), `sortDirection` (non `Ascending` o
    `Descending`), `filters` (un `field` non previsto o ripetuto, un valore che non si legge). Il frontend non li manda.
  - Il frontend mostra ogni errore di `users-list` ed `entries-list` nel popup di errore della tabella (testo fisso,
    con "Riprova"), senza il testo del codice.
- **Stabilimento**: tutte le API lo prendono dal token, comprese le tabelle (l'elenco degli utenti, il registro, la sua
  stampa e la sua esportazione) e le tendine (`*-combobox`), che sono POST senza corpo: nessuna richiesta lo contiene.
- **Permessi**: le API degli utenti come `/api/identity/property-users` (`StaffManage`), quelle del registro come
  `GET /api/audit/entries` (`AuditRead`). `user-combobox` chiede `AuditRead`, come il registro che la usa (sezione 6).
  Senza permesso: 403 `auth.forbidden`.
- **Token senza stabilimento**: 403 `account.property_required` per le API `/api/identity/*`
  (`CurrentAccount.RequirePropertyId`), 403 `audit.property_required` per le API `/api/audit/*`
  (`AuditEntryService.RequirePropertyId`), come oggi.
- **JSON** in camelCase, con i nomi esatti di questo documento. I ruoli viaggiano con i codici di `RoleCodes`
  (es. `cashoperator`).
- **Prefisso del cellulare** (`phoneCountryCode`): in tutte le risposte senza `+` (es. `39`: il backend toglie il `+`
  iniziale, perché i valori salvati possono averlo) e `null` quando l'utente non ha il cellulare (`phone` `null`).
  Oggi `phone_country_code` di `identity.access_users` ha il valore predefinito `+39` (`AccountConfiguration`,
  `HasDefaultValue`): un account creato senza prefisso lo riceve con il `+`, anche se non ha il cellulare.
- **Errori** nel formato Problem Details del backend, con `code`. Senza token valido: 401 `auth.unauthenticated`.
- **`args` degli errori di campo**: `ValidatorExtensions.ToFieldError`
  (`BuildingBlocks.Application/Validation/ValidatorExtensions.cs`) copia sempre tutti i segnaposto che FluentValidation
  dà alla regola, quindi `validation.max_length` e `validation.password_too_long` hanno `args.max` e anche
  `args.min` = 0; `validation.min_length` e `validation.password_too_short` `args.min` e anche `args.max` = -1; una
  lunghezza esatta (`Length(2)`) `args.min` e `args.max` uguali; una regola di confronto (`GreaterThan`,
  `GreaterThanOrEqualTo`, anche con un altro `WithErrorCode`) `args.value` con il valore confrontato; `InclusiveBetween`
  `args.from` e `args.to` (provato con FluentValidation 12.1.1, la versione del backend). Il frontend usa solo gli args
  dei suoi testi, quelli scritti nelle tabelle di questo documento.

## 1. POST /api/identity/users-list

Tabella della pagina: il frontend la chiama all'apertura e ogni volta che cambiano pagina, numero di righe, ricerca o
ordinamento. Sostituisce, per il frontend, `GET /api/identity/property-users`, che restituisce tutti gli utenti insieme,
senza pagine né ricerca, in un ordine fisso (ruolo, poi email) e con 13 campi per utente.

### Richiesta

Il corpo è la richiesta standard delle tabelle:

```json
{
  "page": 1,
  "pageSize": 10,
  "search": "ferri",
  "filters": [],
  "sortField": "email",
  "sortDirection": "Ascending"
}
```

| Campo | Tipo | Regola |
|---|---|---|
| `page` | number | Pagina richiesta, da 1. |
| `pageSize` | number | Righe per pagina: 10, 20, 50 o 100. |
| `search` | string | Testo della barra di ricerca, `""` = nessuna ricerca. Il frontend lo manda senza spazi ai lati, da 3 caratteri in su e al massimo 100 (il campo non ne accetta di più). Trova gli utenti la cui email o il cui cellulare (il numero salvato, senza prefisso) contiene il testo, senza distinguere maiuscole e minuscole. |
| `filters` | array | Sempre `[]`: la tabella non ha filtri. |
| `sortField` | string | `email`, `phone` (il numero, senza prefisso) o `role`; `""` = ordine del backend: ruolo, poi email, come oggi `property-users` (`AccountRepository.ListByPropertyAsync`). |
| `sortDirection` | string | `Ascending` o `Descending`. |

Ordinamento per `role`: secondo l'ordine dei ruoli del backend (`Role.SortOrder`), non in ordine alfabetico del codice.
A parità di valore nella colonna ordinata: email crescente.

### Risposta

200 nello stile delle tabelle: `total` conta tutti gli utenti dello stabilimento che rispettano la ricerca, `rows` è la
pagina richiesta (al massimo `pageSize` righe).

```json
{
  "total": 2,
  "rows": [
    {
      "idUser": "3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d01",
      "email": "giulia.ferri@example.com",
      "phoneCountryCode": "39",
      "phone": "3401112201",
      "role": "superadmin"
    },
    {
      "idUser": "3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d02",
      "email": "marco.ferri@example.com",
      "phoneCountryCode": "39",
      "phone": "3401112202",
      "role": "cashoperator"
    }
  ]
}
```

Solo questi cinque campi per riga:

| Campo | In `StaffUserDto` | Note |
|---|---|---|
| `idUser` | `id` | UUID pubblico dell'account. |
| `email` | `email` | |
| `phoneCountryCode` | `phoneCountryCode` | Prefisso senza `+` (es. `39`); `null` se `phone` è `null` (regole comuni). |
| `phone` | `phone` | Numero senza prefisso; `null` se non c'è. |
| `role` | `role` | Il codice di `RoleCodes`, come oggi: `superadmin`, `admin`, `lifeguard`, `manager`, `cashoperator`, `dailycashoperator`, `websiteadmin`, `user1`, `waiter`, `affiliate`, `advancedlifeguard`, `marketing`. |

Le righe sono tutti gli account dello stabilimento (quelli con il `property_id` del token, non eliminati), titolare
(`superadmin`) e utente collegato compresi. Il frontend non fa modificare né eliminare le righe che il backend non lascia
gestire dal pannello: il titolare (`superadmin`) e i Website admin (`websiteadmin`), riconosciuti dal ruolo, e
l'utente collegato, riconosciuto da `idUser` uguale all'`id` dell'utente del token (`AuthUserDto.Id`, lo stesso
`PublicId` dell'account, `SessionFactory.ToAuthUser`).

### Errori

| Status | `code` | Quando |
|---|---|---|
| 400 | `validation.failed` | Richiesta non valida (regole comuni, "Richiesta delle tabelle"). |
| 403 | `auth.forbidden` | Utente senza il permesso `StaffManage`. |
| 403 | `account.property_required` | Token senza stabilimento. |
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido. |

## 2. POST /api/identity/create-user

Bottone "Salva" del popup "Aggiungi nuovo utente". È un'**API nuova** con solo i campi del popup:
`POST /api/identity/property-users` resta com'è (ha in più `username`, `firstName`, `lastName`, `language`,
`isResourceScopeRestricted` e `resourceScopes`, che la pagina non ha). Può riusare la stessa logica
(`StaffService.CreateAsync`) con quei campi vuoti: nessun username né nome, la lingua di chi crea l'utente
(`language` `null`), nessuna restrizione (`isResourceScopeRestricted` `false`, `resourceScopes` vuoto).

### Richiesta

Lo stabilimento lo prende dal token.

```json
{
  "email": "nuovo.utente@example.com",
  "emailIsContact": true,
  "password": "Sabbia-2026",
  "phoneCountryCode": "39",
  "phone": "340 1112299",
  "phoneIsContact": false,
  "role": "waiter"
}
```

| Campo | Tipo | Regola |
|---|---|---|
| `email` | string | Obbligatoria, email valida, al massimo 255 caratteri, non già usata da un altro account. Si salva senza spazi ai lati. |
| `emailIsContact` | boolean | Casella "Abilita" accanto all'email: come `emailIsContact` di `property-users`, ma salvata (sotto, "Caselle Abilita"). |
| `password` | string | Obbligatoria, come per `property-users`: da 8 a 128 caratteri, non troppo comune (`IdentityRules.NewPassword`). Maiuscola, minuscola e numero li controlla solo il frontend. |
| `phoneCountryCode` | string \| null | Prefisso: da 1 a 6 cifre, con un `+` davanti facoltativo, al massimo 8 caratteri (`CommonRules.PhonePrefix`). Il frontend lo manda senza `+` (es. `39`) e `null` quando `phone` è `null`. |
| `phone` | string \| null | Numero senza prefisso: solo cifre e spazi, al massimo 32 caratteri (`CommonRules.PhoneNumber`). `null`, vuoto o di soli spazi = nessun cellulare: si salva `null` (come `StaffService.Clean`). |
| `phoneIsContact` | boolean | Casella "Abilita" accanto al cellulare: come `phoneIsContact` di `property-users`, ma salvata (sotto, "Caselle Abilita"). |
| `role` | string | Obbligatorio: un codice di `RoleCodes` assegnabile dal pannello (`admin`, `lifeguard`, `manager`, `cashoperator`, `dailycashoperator`, `user1`, `waiter`, `affiliate`, `advancedlifeguard`, `marketing`). |

### Risposta

201 Created con l'utente creato, solo con questi campi (gli stessi che il frontend legge quando apre la modifica di un
utente, senza `version`; mai la password). `phoneCountryCode` segue le regole comuni.

```json
{
  "idUser": "3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d15",
  "email": "nuovo.utente@example.com",
  "emailIsContact": true,
  "phoneCountryCode": "39",
  "phone": "340 1112299",
  "phoneIsContact": false,
  "role": "waiter"
}
```

Come oggi `StaffService.CreateAsync`, scrive nel registro `identity.user_created` (`StaffService.RecordAsync`).

### Errori

Come `POST /api/identity/property-users`:

| Status | `code` | Quando |
|---|---|---|
| 400 | `validation.failed` | `errors[]` sui nomi del corpo: `validation.required` (`email`, `password`, `role`), `validation.email` (`email`), `validation.max_length` con `args.max` (`email` oltre 255, `phone` oltre 32, `phoneCountryCode` oltre 8 caratteri), `validation.password_too_short` con `args.min` (8), `validation.password_too_long` con `args.max` (128), `validation.password_too_common` (`password`), `validation.phone` (`phone`), `validation.phone_prefix` (`phoneCountryCode`). |
| 422 | `account.role_not_assignable` | Ruolo che non si assegna dal pannello (es. `superadmin`). |
| 409 | `account.email_taken` | Esiste già un account non eliminato con questa email (di qualunque stabilimento, o di un cliente). |
| 403 | `auth.forbidden` | Utente senza il permesso `StaffManage`. |
| 403 | `account.property_required` | Token senza stabilimento. |
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido. |
| 401 | `auth.session_expired` | L'account dell'utente collegato è stato cancellato nel frattempo (`CurrentAccount.GetAsync`, come oggi in `StaffService`). |

Ordine dei controlli, come `StaffService.CreateAsync`: corpo (400, con tutti gli errori di campo insieme), stabilimento
del token (403), account dell'utente collegato (401), ruolo (422), email già usata (409).

Il frontend mostra gli errori di `errors[]` sotto i campi con lo stesso nome; 409 `account.email_taken` sotto Email e
422 `account.role_not_assignable` sotto Tipo di account (arrivano senza `errors[]` ma riguardano un campo solo); tutti
gli altri nel messaggio in fondo al popup.

Lunghezze e formati sono quelli di `CommonRules` (`BuildingBlocks.Application/Validation/CommonRules.cs`: email 255,
cellulare 32, prefisso 8) e di `CreateStaffUserRequestValidator`
(`Module.Identity/Application/Validators/AccountValidators.cs`).

L'unicità dell'email oggi la controlla solo il servizio (`IAccountRepository.EmailExistsAsync`, su tutti gli account non
eliminati, senza distinguere maiuscole e minuscole): `identity.access_users` non ha un indice unico sull'email, quindi
due creazioni o modifiche nello stesso momento con la stessa email possono passare tutte e due. Se aggiungere un indice
unico sull'email lo decide lo sviluppatore backend: l'agente glielo chiede prima di scrivere codice. Il contratto non
cambia: un'email già usata risponde 409 `account.email_taken`, che il frontend mostra sotto Email.

### Caselle "Abilita"

Oggi il backend non salva `emailIsContact` e `phoneIsContact`: `StaffService.CreateAsync` e `StaffService.UpdateAsync`
(`Module.Identity/Application/Services/StaffService.cs`) le mandano solo nell'evento `StaffContactChannelsChanged`
(`PublishContactChannelsAsync`), che nessun modulo gestisce, e `identity.access_users` non ha colonne per loro. Senza
salvarle, `get-selected-user` non ha valori da restituire e il popup "Modifica utente" non può mostrarle.

Il backend deve:

- salvarle per l'account a ogni `create-user` e `update-user`, con il valore ricevuto (anche `false`);
- restituirle nelle risposte di `create-user`, `get-selected-user` e `update-user`;
- restituire `false` per gli account che non le hanno mai ricevute da `create-user` o `update-user`.

L'evento `StaffContactChannelsChanged` resta com'è.

**Dove salvarle lo decide lo sviluppatore backend.** Un agente che implementa questo punto, prima di scrivere codice, si
ferma e chiede allo sviluppatore backend quale delle due strade seguire, e implementa solo quella scelta:

1. due colonne `email_is_contact` e `phone_is_contact` di `identity.access_users` (`boolean NOT NULL DEFAULT false`),
   aggiunte con una migration di Identity;
2. nel modulo Notification, come prevede il commento `PROMEMORIA-85-5` in `StaffService.cs` ("le spunte «come
   contatto» vivono in Notification"): un gestore di `StaffContactChannelsChanged` che le salva, e una lettura che le
   restituisce a `create-user`, `get-selected-user` e `update-user`.

Qualunque sia la scelta, il contratto delle API resta quello di questo documento: stessi campi, stessi valori.

## 3. GET /api/identity/get-selected-user

Apertura del popup "Modifica utente" (matita di una riga): il frontend chiede l'utente e riempie il form. È un'**API
nuova**: oggi il backend non ha una lettura di un solo utente.

### Richiesta

È una GET senza corpo: l'utente va nella query string, lo stabilimento lo prende dal token.

```
GET /api/identity/get-selected-user?idUser=3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d03
```

| Parametro | Tipo | Regola |
|---|---|---|
| `idUser` (query string) | string (UUID) | L'utente da modificare (`idUser` di `users-list`). Mancante o non UUID: 404 `account.not_found`, come un utente che non c'è (mai un errore di campo). Per questo il backend lo riceve come testo e lo converte nel codice: con un parametro `Guid` il framework risponderebbe 400 prima dell'endpoint (in Development `validation.failed`, negli altri ambienti senza corpo), e un corpo senza `idUser` darebbe `Guid.Empty` (provato con .NET 10 e la gestione degli errori di `Boot/Program.cs`). |

### Risposta

200 con l'utente, con gli stessi campi della risposta di `create-user` più `version`, la versione che il Salva rimanda
(sezione 10); mai la password:

```json
{
  "idUser": "3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d03",
  "email": "sara.conti@example.com",
  "emailIsContact": true,
  "phoneCountryCode": "39",
  "phone": "340 1112203",
  "phoneIsContact": true,
  "role": "manager",
  "version": "a3f09c41"
}
```

`emailIsContact` e `phoneIsContact` sono i valori salvati (sezione 2, "Caselle Abilita"): quelli ricevuti l'ultima
volta da `create-user` o `update-user`, `false` se non sono mai arrivati. `phoneCountryCode` segue le regole comuni.

### Errori

Gli stessi di `update-user` per l'utente da leggere:

| Status | `code` | Quando |
|---|---|---|
| 404 | `account.not_found` | `idUser` non è un utente dello stabilimento. |
| 403 | `account.role_not_assignable` | L'utente ha un ruolo che non si gestisce dal pannello (il titolare `superadmin` o `websiteadmin`). |
| 422 | `account.cannot_modify_self` | L'utente collegato chiede se stesso. |
| 403 | `auth.forbidden` | Utente senza il permesso `StaffManage`. |
| 403 | `account.property_required` | Token senza stabilimento. |
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido. |
| 401 | `auth.session_expired` | L'account dell'utente collegato è stato cancellato nel frattempo (`CurrentAccount.GetAsync`, come oggi in `StaffService`). |

Ordine dei controlli, come `StaffService.GetManageableAsync`: stabilimento del token (403
`account.property_required`), account dell'utente collegato (401), utente nello stabilimento (404), se stesso (422),
ruolo gestibile dal pannello (403 `account.role_not_assignable`).

Con ogni errore il popup si chiude e compare l'avviso di errore con il testo del `code`, Chiudi e Riprova (per un 404
solo Chiudi); la pagina riceve l'errore e con un 404 ricarica la tabella.

## 4. POST /api/identity/update-user

Bottone "Salva" del popup "Modifica utente". È un'**API nuova** con solo i campi del popup:
`PATCH /api/identity/property-users/{userId}` resta com'è (ha in più `username`, `firstName`, `lastName`, `language`,
`isResourceScopeRestricted` e `resourceScopes`). Può riusare la stessa logica (`StaffService.UpdateAsync`), con le
sessioni di questa sezione.

### Richiesta

Tutti i campi del popup, cambiati o no; l'utente da modificare è `idUser`. Lo stabilimento lo prende dal token.
L'header `If-Match` porta la `version` letta con `get-selected-user` (sezione 10): `If-Match: "a3f09c41"`.

```json
{
  "idUser": "3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d03",
  "email": "sara.conti@example.com",
  "emailIsContact": true,
  "password": null,
  "phoneCountryCode": "41",
  "phone": "79 123 45 67",
  "phoneIsContact": false,
  "role": "manager"
}
```

| Campo | Tipo | Regola |
|---|---|---|
| `idUser` | string (UUID) | L'utente da modificare (`idUser` di `users-list`). Mancante o non UUID: 404 `account.not_found`, come un utente che non c'è (mai un errore di campo). Per questo il backend lo riceve come testo e lo converte nel codice: con un parametro `Guid` il framework risponderebbe 400 prima dell'endpoint (in Development `validation.failed`, negli altri ambienti senza corpo), e un corpo senza `idUser` darebbe `Guid.Empty` (provato con .NET 10 e la gestione degli errori di `Boot/Program.cs`). |
| `email` | string | Obbligatoria, email valida, al massimo 255 caratteri, non già usata da un altro account (l'utente stesso escluso). Si salva senza spazi ai lati. |
| `emailIsContact` | boolean | Come in `create-user`: salvata e restituita. |
| `password` | string \| null | `null` = la password resta quella salvata; altrimenti la nuova password, con le regole di `create-user`. Il frontend manda `null` quando il campo è vuoto, mai `""`. |
| `phoneCountryCode` | string \| null | Come in `create-user` (es. `41`); `null` quando `phone` è `null`. Il frontend manda il prefisso che mostra: per un utente letto con il cellulare e `phoneCountryCode` `null` la tendina mostra l'Italia e Salva manda `39`. |
| `phone` | string \| null | Come in `create-user`; `null`, vuoto o di soli spazi = nessun cellulare: numero e prefisso salvati diventano `null`. `StaffService.UpdateAsync` così com'è non lo fa: con `Phone` `null` lascia il numero com'era (`Merge`) e un prefisso `""` non passa `CommonRules.PhonePrefix` (`validation.phone_prefix`). Come togliere numero e prefisso lo decide lo sviluppatore backend: l'agente glielo chiede prima di scrivere codice. |
| `phoneIsContact` | boolean | Come in `create-user`: salvata e restituita. |
| `role` | string | Obbligatorio: un codice di `RoleCodes` assegnabile dal pannello, come in `create-user`. |

Diversamente dal `PATCH`, qui `null` non vuol dire "invariato" (tranne la password): il corpo è sempre tutto il popup,
quindi ogni campo del corpo prende il valore ricevuto.

Tutto quello che non è nel corpo resta com'era. In particolare `update-user` non cambia mai `username`, `first_name`,
`last_name`, `language`, `is_resource_scope_restricted` e `resource_scopes` di `identity.access_users`: la pagina non li
legge e non li mostra. Riusando `StaffService.UpdateAsync` si passano `null` in `Username`, `FirstName`, `LastName`,
`Language`, `IsResourceScopeRestricted` e `ResourceScopes` di `UpdateStaffUserRequest`, che lì vuol dire "invariato"
(`Merge` per nome e cognome, `if` sugli altri).

### Sessioni dell'utente modificato

Il backend chiude le sessioni dell'utente modificato (`SessionFactory.RevokeAllAsync`, che revoca i suoi refresh token
in `identity.access_refresh_tokens`) solo se:

- `password` non è `null`: motivo `PasswordChanged`, come oggi;
- altrimenti, se `role` è diverso dal ruolo salvato prima di questo Salva: motivo `Manual`, come oggi.

Con `password` `null` e lo stesso ruolo non le chiude, anche se cambiano email, cellulare o caselle "Abilita". Oggi
`StaffService.UpdateAsync` le chiude appena `Role` non è `null`, anche se è lo stesso ruolo: siccome `update-user` manda
sempre il ruolo, ogni Salva chiuderebbe le sessioni dell'utente anche quando cambiano solo il cellulare o una casella.

### Risposta

200 con l'utente aggiornato, con gli stessi campi della risposta di `create-user` (mai la password):

```json
{
  "idUser": "3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d03",
  "email": "sara.conti@example.com",
  "emailIsContact": true,
  "phoneCountryCode": "41",
  "phone": "79 123 45 67",
  "phoneIsContact": false,
  "role": "manager"
}
```

Con la risposta, l'header `ETag` porta la nuova versione dell'utente (sezione 10).

Come oggi `StaffService.UpdateAsync`, ogni Salva riuscito scrive nel registro `identity.user_updated`.

### Errori

Come `PATCH /api/identity/property-users/{userId}`:

| Status | `code` | Quando |
|---|---|---|
| 400 | `validation.failed` | `errors[]` sui nomi del corpo, come in `create-user` (con `validation.required` su `email` e `role`; le regole della password solo se `password` non è `null`). |
| 422 | `account.role_not_assignable` | Il nuovo ruolo non si assegna dal pannello. |
| 409 | `account.email_taken` | L'email è già di un altro account non eliminato. |
| 404 | `account.not_found` | `idUser` non è un utente dello stabilimento. |
| 409 | `resource.version_conflict` | La versione in `If-Match` non è più quella dell'utente, o manca `If-Match` (sezione 10). Niente cambia. |
| 403 | `account.role_not_assignable` | L'utente da modificare ha un ruolo che non si gestisce dal pannello (il titolare `superadmin` o `websiteadmin`). |
| 422 | `account.cannot_modify_self` | L'utente collegato prova a modificare se stesso. |
| 403 | `auth.forbidden` | Utente senza il permesso `StaffManage`. |
| 403 | `account.property_required` | Token senza stabilimento. |
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido. |
| 401 | `auth.session_expired` | L'account dell'utente collegato è stato cancellato nel frattempo (`CurrentAccount.GetAsync`, come oggi in `StaffService`). |

Ordine dei controlli: corpo (400), poi i controlli di `get-selected-user` sull'utente da modificare (sezione 3), poi
la versione (409 `resource.version_conflict`, sezione 10), poi email già usata (409), poi ruolo nuovo (422). A parte la
versione, è l'ordine di `StaffService.UpdateAsync`.

Il frontend mostra gli errori di `errors[]` sotto i campi con lo stesso nome; 409 `account.email_taken` sotto Email e
422 `account.role_not_assignable` sotto Tipo di account (arrivano senza `errors[]` ma riguardano un campo solo); tutti
gli altri nel messaggio in fondo al popup. Il 403 `account.role_not_assignable` (ruolo dell'utente da modificare) resta nel messaggio in fondo.
Il 409 `resource.version_conflict` non va nel messaggio: il popup mostra l'avviso e rilegge l'utente (sezione 10).

## 5. POST /api/audit/entries-list

Tabella della schermata del registro (`/settings/user-management/logs`): il frontend la chiama all'apertura e ogni volta
che cambiano pagina, numero di righe, ricerca, periodo, filtri o ordinamento. È un'**API nuova** per la tabella:
`GET /api/audit/entries` resta com'è. Rispetto a quella, la tabella ha bisogno di filtrare più utenti insieme, di
filtrare tutte le azioni del catalogo, di ordinare ogni colonna, del periodo letto come giorni nel fuso dell'utente e di
righe con solo i campi mostrati.

### Richiesta

Il corpo è la richiesta standard delle tabelle:

```json
{
  "page": 1,
  "pageSize": 10,
  "search": "ferri",
  "filters": [
    { "field": "datetimeFrom", "value": ["2026-10-17T00:00:00.000Z"] },
    { "field": "datetimeTo", "value": ["2026-10-23T23:59:59.999Z"] },
    { "field": "actionCode", "value": ["catalog.map_spots_created", "catalog.season_updated"] },
    { "field": "idUser", "value": ["3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d01", "3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d02"] }
  ],
  "sortField": "occurredAt",
  "sortDirection": "Descending"
}
```

| Campo | Tipo | Regola |
|---|---|---|
| `page` | number | Pagina richiesta, da 1. |
| `pageSize` | number | Righe per pagina: 10, 20, 50 o 100. |
| `search` | string | Testo della barra di ricerca, `""` = nessuna ricerca; al massimo 100 caratteri (sotto, "Limiti"). Trova le voci il cui nome dell'operatore, numero della prenotazione o postazione contiene il testo, senza distinguere maiuscole e minuscole, come oggi `GET /api/audit/entries` (`AuditEntryRepository.Filter`). |
| `filters` | array | Un elemento per campo, `value` sempre un array di stringhe; `[]` = nessun filtro. Campi qui sotto. |
| `sortField` | string | Una delle quattro colonne ordinabili: `bookingNumber`, `actorName`, `spotLabel`, `occurredAt`; `""` = le più recenti prima (`occurredAt`, poi `idAuditEntry`, decrescenti, come oggi `GET /api/audit/entries`). |
| `sortDirection` | string | `Ascending` o `Descending`. |

Il frontend non ordina per `actionCode`: la colonna Azione mostra il nome tradotto, che l'ordine per codice non
seguirebbe. A parità di valore nella colonna ordinata: le più recenti prima (`occurredAt`, poi `idAuditEntry`,
decrescenti).

I filtri (un campo può mancare; più valori dello stesso campo = uno qualsiasi, campi diversi = tutti insieme):

| `field` | Valori | Significato |
|---|---|---|
| `datetimeFrom` | un valore: il primo giorno del periodo, scritto come mezzanotte UTC di quel giorno (es. `2026-10-17T00:00:00.000Z`) | Solo le voci dal primo giorno, compreso (il periodo scelto nella tendina delle date; sotto, "Periodo"). |
| `datetimeTo` | un valore: l'ultimo giorno del periodo, scritto come 23:59:59.999 UTC di quel giorno (es. `2026-10-23T23:59:59.999Z`) | Solo le voci fino all'ultimo giorno, compreso. |
| `actionCode` | uno o più codici di azione di `AuditActionCodes` (es. `catalog.map_spot_updated`), fino a tutti quelli del filtro | Le azioni scelte nel popup Filtri (sotto, "Limiti"). |
| `idUser` | uno o più id di utenti (UUID) | Gli utenti scelti nel popup Filtri, o l'utente del bottone "Log" di una riga di Gestione utenti. Sono gli `actorUserId` delle voci. |

Il frontend manda `datetimeFrom` e `datetimeTo` sempre insieme (un periodo scelto) o nessuno dei due ("Tutte le date").

### Periodo

`datetimeFrom` e `datetimeTo` sono **giorni**, non istanti: il frontend manda il giorno scelto nel calendario del
dispositivo con un'ora fissa e la `Z`, senza conversione di fuso (`utcDayStart` e `utcDayEnd`, usate da
`dateRangeFilters` in `behaviours/dates/date-periods.ts`). Il backend:

1. di ogni valore prende solo la data, i primi 10 caratteri (`2026-10-17`, `2026-10-23`): l'ora non conta;
2. prende il fuso dall'header `X-Time-Zone`, il nome IANA del fuso del dispositivo (es. `Europe/Rome`), letto con
   `TimeZoneInfo.FindSystemTimeZoneById`; se l'header manca o il fuso non esiste, `Europe/Rome`
   (`ItalianTime.TimeZoneId`);
3. calcola l'inizio, l'istante UTC della mezzanotte del primo giorno in quel fuso, e la fine, l'istante UTC della
   mezzanotte del giorno dopo l'ultimo in quel fuso; se in quel fuso un giorno non ha la mezzanotte (cambio dell'ora
   alle 00:00), vale il primo istante di quel giorno;
4. restituisce le voci con `inizio <= occurredAt < fine`: tutto il primo giorno e tutto l'ultimo, nell'ora del
   dispositivo.

Lo stesso giorno in `datetimeFrom` e `datetimeTo` (es. "Oggi") è un periodo di un giorno intero. Un campo da solo limita
solo da quel lato. Un ultimo giorno prima del primo dà 400 (sotto, "Errori").

Esempio: con `X-Time-Zone: Europe/Rome`, `datetimeFrom` `2026-10-17T00:00:00.000Z` e `datetimeTo`
`2026-10-23T23:59:59.999Z` restituisce le voci con `occurredAt` da `2026-10-16T22:00:00Z` (compreso) a
`2026-10-23T22:00:00Z` (escluso).

Oggi `GET /api/audit/entries` tratta `from` e `to` come istanti, con la fine esclusa (`AuditEntryRepository.Filter`:
`OccurredAt >= from`, `OccurredAt < to`): con questi valori, a Roma con l'ora legale, perderebbe le voci tra mezzanotte
e le 2 del primo giorno e mostrerebbe quelle tra mezzanotte e le 2 del giorno dopo l'ultimo.

### Limiti

- `actionCode`: il popup Filtri permette di scegliere tutte le azioni del catalogo che il frontend conosce (oggi 102 di quelle di
  `GET /api/audit/action-codes`), quindi qui non vale il tetto di 50 codici di `GET /api/audit/entries`
  (`AuditQueryRules.MaxActionCodes` in `Module.Audit/Application/Validators/AuditValidators.cs`, 400 oltre 50). Il
  massimo è il numero di codici di `AuditActionCodes.All` (oggi 149, cresce con il catalogo); oltre, o con un codice
  vuoto o più lungo di 100 caratteri, 400 `validation.failed` con `errors[]`
  `{ "field": "filters", "code": "validation.invalid" }`. Un codice che non è nel catalogo non è un errore e non trova
  voci, come oggi.
- `search`: al massimo 100 caratteri (`AuditQueryRules.SearchMaxLength`, come `GET /api/audit/entries`); oltre, 400
  `validation.failed` con `errors[]` `{ "field": "search", "code": "validation.max_length", "args": { "max": 100, "min": 0 } }`.
  Il campo di ricerca del frontend, come in tutte le tabelle, non accetta più di 100 caratteri e la manda senza spazi
  ai lati, da 3 caratteri in su.
- I validatori di `GET /api/audit/entries` (`AuditEntryListQueryValidator` e `AuditFilterValidator` in
  `Module.Audit/Application/Validators/AuditValidators.cs`) non si riusano così come sono: accettano `pageSize` 25
  (`AuditQueryRules.PageSizes`), danno `validation.out_of_range` su `page` minore di 1 e `validation.out_of_range` su
  `to` quando `to` non è dopo `from`. Questa API vuole invece `validation.invalid` su `page` e su `pageSize` (regole
  comuni) e `validation.end_before_start` su `filters` (sotto, "Errori").

### Risposta

200 nello stile delle tabelle: `total` conta tutte le voci che rispettano ricerca e filtri, `rows` è la pagina
richiesta (al massimo `pageSize` righe).

```json
{
  "total": 2,
  "rows": [
    {
      "idAuditEntry": "1287",
      "bookingNumber": "1234",
      "actorName": "Giulia Ferri",
      "spotLabel": "A1",
      "actionCode": "catalog.map_spots_created",
      "occurredAt": "2026-10-23T11:11:00.000Z"
    },
    {
      "idAuditEntry": "1265",
      "bookingNumber": "2741",
      "actorName": "Giulia Ferri",
      "spotLabel": "D2",
      "actionCode": "catalog.season_updated",
      "occurredAt": "2026-10-17T14:05:00.000Z"
    }
  ]
}
```

Le voci sono le stesse di `GET /api/audit/entries` (categorie `Operator` e `Security` dello stabilimento,
`AuditEntryService.VisibleCategories`), con solo questi sei campi per riga:

| Campo | In `AuditEntryDto` | Note |
|---|---|---|
| `idAuditEntry` | `id` | Identificativo della voce (il numero `bigint` di `audit.trail_entries`), come testo. Non si mostra. |
| `bookingNumber` | `bookingNumber` | Colonna "ID prenotazione"; `null` se la voce non riguarda una prenotazione. |
| `actorName` | `actorName` | Colonna "Nome completo": il nome dell'operatore salvato nella voce; `null` se la voce non ce l'ha. |
| `spotLabel` | `spotLabel` | Colonna "Postazione"; `null` se la voce non riguarda una postazione. |
| `actionCode` | `actionCode` | Colonna "Azione": il codice (`<modulo>.<azione>`), che il frontend traduce. |
| `occurredAt` | `occurredAt` | Colonna "Data e ora", ISO 8601 UTC con la `Z`; il frontend la mostra nell'ora del dispositivo. |

### Errori

| Status | `code` | Quando |
|---|---|---|
| 400 | `validation.failed` | Con `errors[]`: `search` oltre 100 caratteri (`validation.max_length` su `search`); giorno di `datetimeTo` prima di quello di `datetimeFrom` (`validation.end_before_start` su `filters`); i limiti di `actionCode` e le altre regole delle tabelle (`validation.invalid`, regole comuni). |
| 403 | `auth.forbidden` | Utente senza il permesso `AuditRead`. |
| 403 | `audit.property_required` | Token senza stabilimento. |
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido. |

## 6. POST /api/identity/user-combobox

La tendina degli utenti del frontend: **una sola per tutta l'app**, usata ovunque serva scegliere un utente (salvo
diversa indicazione). In questa pagina è la tendina "Utente" del popup Filtri della schermata del registro, chiamata
ogni volta che il popup si apre (tranne quando la schermata è aperta dal bottone "Log" di una riga e quell'utente è
ancora nel filtro: allora la tendina contiene solo lui). La schermata aperta dal bottone "Log" di una riga la chiama
anche una volta all'apertura, per mostrare il nome dell'utente al posto della sua email. È un'**API nuova** nello
standard delle tendine del frontend: `GET /api/audit/actors` resta com'è.

Quali utenti restituire lo decide il backend. Gli `id` devono essere quelli che filtrano il registro: il frontend li
rimanda nel filtro `idUser` di `entries-list`, `print-log` ed `export-log`.

Permesso: `AuditRead`, come la schermata del registro che la usa: chi apre il registro deve poter scegliere i suoi
utenti. Oggi `AuditRead` e `StaffManage` li hanno gli stessi ruoli, `superadmin` e `admin`
(`Shared/Security/RolePermissions.cs`).

### Richiesta

Come tutte le tendine del frontend, POST senza corpo: lo stabilimento lo prende il backend dal token.

### Risposta

200 con un array (senza un oggetto che lo contiene). L'ordine non conta: il frontend mette le voci in ordine alfabetico
di `value` nella lingua dell'app.

```json
[
  { "id": "3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d01", "value": "Giulia Ferri" },
  { "id": "3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d02", "value": "Marco Bianchi" }
]
```

| Campo | Tipo | Note |
|---|---|---|
| `id` | string (UUID) | L'utente: il suo UUID pubblico, lo stesso di `idUser` nelle altre API della pagina e di `actorUserId` nelle voci del registro. |
| `value` | string | Il nome da mostrare: quello con cui l'utente compare come operatore nel registro (`UserDirectory.FindAsync`: nome e cognome, altrimenti username, altrimenti email). |

### Errori

| Status | `code` | Quando |
|---|---|---|
| 403 | `auth.forbidden` | Utente senza il permesso `AuditRead`. |
| 403 | `account.property_required` | Token senza stabilimento. |
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido. |

Nel popup Filtri il frontend mostra l'errore sotto il campo Utente e rilegge la lista quando l'utente riapre la
tendina; nella chiamata all'apertura dal bottone "Log" ignora l'errore e lascia l'email.

## 7. POST /api/audit/print-log

Bottone "Stampa" della schermata del registro (`/settings/user-management/logs`). È un'**API nuova**: oggi il backend
ha l'esportazione (`GET /api/audit/entries/export`, csv o xlsx) ma non un documento da stampare. Il documento da
stampare è un file Excel (`xlsx`), il formato che il backend sa già scrivere (`TabularExportWriter`, ClosedXML): lo
stesso file di `export-log` (sezione 8).

### Richiesta

Gli stessi filtri che la tabella del registro sta mostrando, senza pagina e righe per pagina: il documento contiene
**tutte** le voci che li rispettano, nell'ordine della tabella.

```json
{
  "search": "ferri",
  "filters": [
    { "field": "datetimeFrom", "value": ["2026-10-20T00:00:00.000Z"] },
    { "field": "datetimeTo", "value": ["2026-10-26T23:59:59.999Z"] },
    { "field": "actionCode", "value": ["catalog.sector_updated", "catalog.map_spot_updated"] },
    { "field": "idUser", "value": ["3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d02"] }
  ],
  "sortField": "occurredAt",
  "sortDirection": "Descending"
}
```

| Campo | Tipo | Regola |
|---|---|---|
| `search` | string | Come in `entries-list` (sezione 5): `""` = nessuna ricerca, al massimo 100 caratteri; si cerca in nome dell'operatore, numero della prenotazione e postazione. |
| `filters` | array | Come in `entries-list` (sezione 5): un elemento per campo, `value` sempre un array di stringhe, `[]` se non ci sono filtri. `datetimeFrom` / `datetimeTo`: il periodo, giorni nel fuso di `X-Time-Zone` ("Periodo"). `actionCode`: i codici delle azioni scelte (una qualsiasi), fino a tutti quelli del filtro ("Limiti"). `idUser`: gli utenti scelti (uno qualsiasi). |
| `sortField` | string | Come in `entries-list`: `bookingNumber`, `actorName`, `spotLabel`, `actionCode` o `occurredAt`; `""` = le più recenti prima. |
| `sortDirection` | string | `Ascending` o `Descending`; conta solo con `sortField` non vuoto. |

### Risposta

200 con il documento da stampare come file **Excel** (`xlsx`, binario, non JSON): una riga di titoli e una riga per
voce, con le colonne di "Colonne" (sotto), scritto con `TabularExportWriter` come l'esportazione. Il frontend non apre il
file, lo salva sul dispositivo con il nome di `Content-Disposition` (`FileDownloadBehaviour`).

| Header | Esempio | Note |
|---|---|---|
| `Content-Type` | `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet` | Sempre questo: il documento è un file Excel. |
| `Content-Disposition` | `attachment; filename="log-2026-10-26.xlsx"` | `log-` più la data di oggi nel fuso di `X-Time-Zone` (`yyyy-MM-dd`) ed estensione `.xlsx`; il frontend lo usa per salvarlo. |

Frontend e API stanno sulla stessa origine (`/api`), quindi il browser legge `Content-Disposition` senza altre modifiche: il
CORS (`Boot/Web/SecuritySetup.cs`, `AddCors`) non c'entra.

### Lingua, fuso, colonne, troppe voci e registro

Valgono per la stampa e per l'esportazione (sezione 8):

- **Lingua**: titoli delle colonne e nomi delle azioni nella lingua dell'header `Accept-Language`, che il frontend
  manda con la lingua scelta nell'app (un codice solo: `it`, `en`, `fr`, `es`, `de`, `zh` o `ar`), letta con
  `IRequestLanguage.Language` (`Boot/Web/HttpRequestUser.cs`; `it` se manca o non è gestita). I nomi delle azioni
  vengono dalle risorse `AuditExport.<lingua>.resx` come fa `AuditExportWriter`: se manca la traduzione l'italiano, poi
  il codice dell'azione. Oggi `GET /api/audit/entries/export` usa invece la lingua dell'account di chi esporta
  (`AuditExport.LanguageAsync`).
- **Fuso**: "Data e ora" nel fuso dell'header `X-Time-Zone` (`Europe/Rome` se manca o non esiste), nel formato di oggi
  `yyyy-MM-dd HH:mm:ss`, come la tabella che la mostra nell'ora del dispositivo. Oggi l'esportazione scrive sempre l'ora
  italiana (`AuditExportWriter.Time`, `ItalianTime`) e il titolo della colonna lo dice ("Data e ora (ora italiana)").
  Il periodo dei filtri segue la sezione 5.
- **Colonne**: le cinque colonne della tabella, in quest'ordine, con i valori `bookingNumber`, `actorName`, `spotLabel`,
  il nome dell'azione e `occurredAt` (cella vuota per i `null`). I titoli sono quelli della tabella del frontend:

  | Lingua | ID prenotazione | Nome completo | Postazione | Azione | Data e ora |
  |---|---|---|---|---|---|
  | `it` | ID prenotazione | Nome completo | Postazione | Azione | Data e ora |
  | `en` | Booking ID | Full name | Spot | Action | Date and time |
  | `fr` | ID réservation | Nom complet | Emplacement | Action | Date et heure |
  | `es` | ID reserva | Nombre completo | Puesto | Acción | Fecha y hora |
  | `de` | Buchungs-ID | Vollständiger Name | Platz | Aktion | Datum und Uhrzeit |
  | `zh` | 预订编号 | 全名 | 位置 | 操作 | 日期和时间 |
  | `ar` | رقم الحجز | الاسم الكامل | الموقع | الإجراء | التاريخ والوقت |

- **Troppe voci**: oltre 50.000 voci (`AuditQueryRules.ExportMaxRows`) nessun file e 422 `audit.export_too_large` con
  `args.max` (50000) e `args.total` (le voci che rispettano i filtri), contate prima di leggerle, come
  `AuditExport.LoadAsync`.
- **Registro**: ogni stampa e ogni esportazione che restituisce il file scrive nel registro dello stabilimento
  `audit.entries_exported` (categoria `Operator`, come `AuditEntryService.ExportAsync`), con i metadati di
  `AuditExport.Metadata` per questi filtri: `rows` (le voci del file), `format` (`xlsx`, per la stampa e per
  l'esportazione), `from` e `to` (gli istanti UTC del periodo calcolati come nella
  sezione 5, `null` senza periodo), `actionCodes`, `actorUserIds` (gli `idUser` del filtro, al posto di `actorUserId`)
  e `propertyId`. Una richiesta che finisce con un errore non scrive niente. Il frontend ha già testo ("Registro
  esportato") e icona di questo codice: un codice diverso per la stampa va concordato prima, perché il frontend deve
  aggiungerne testi e icona.

### Errori

Errori in JSON (Problem Details), anche se la risposta giusta è un file: il frontend legge come JSON il corpo di ogni
errore fino a 64 KB (`readBlobError`) e mostra il messaggio del `code` nel popup "Operazione non riuscita" (per
`audit.export_too_large` con il numero di voci e il massimo).

| Status | `code` | Quando |
|---|---|---|
| 400 | `validation.failed` | Con `errors[]`, come in `entries-list`: `search` oltre 100 caratteri, giorno di `datetimeTo` prima di quello di `datetimeFrom`, i limiti di `actionCode`, `sortField`, `sortDirection` o `filters` non validi. |
| 422 | `audit.export_too_large` | Più di 50.000 voci: nessun file; `args.max` e `args.total`. |
| 403 | `auth.forbidden` | Utente senza il permesso di leggere il registro (`AuditRead`, come `GET /api/audit/entries`). |
| 403 | `audit.property_required` | Token senza stabilimento. |
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido. |

## 8. POST /api/audit/export-log

Bottone "Esporta" della schermata del registro. È un'**API nuova**: `GET /api/audit/entries/export` resta com'è, ma
filtra un solo utente, ordina solo per data e riceve i filtri in un altro formato. Questa usa gli stessi filtri della
tabella (`entries-list`) e della stampa (`print-log`).

### Richiesta

Lo stesso corpo di `print-log` (vedi la sezione 7 per le regole di ogni campo): gli stessi filtri che la tabella sta
mostrando, senza pagina e righe per pagina, quindi **tutte** le voci che li rispettano, nell'ordine della tabella.

```json
{
  "search": "ferri",
  "filters": [
    { "field": "datetimeFrom", "value": ["2026-10-20T00:00:00.000Z"] },
    { "field": "datetimeTo", "value": ["2026-10-26T23:59:59.999Z"] },
    { "field": "actionCode", "value": ["catalog.sector_updated"] },
    { "field": "idUser", "value": ["3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d01", "3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d02"] }
  ],
  "sortField": "actorName",
  "sortDirection": "Ascending"
}
```

### Risposta

200 con un file Excel (`xlsx`), binario, non JSON: una riga di titoli e una riga per voce, con le colonne della
tabella (ID prenotazione, Nome completo, Postazione, Azione, Data e ora). Lingua, fuso, colonne, limite di voci e
registro come per la stampa (sezione 7, "Lingua, fuso, colonne, troppe voci e registro"). `TabularExportWriter`
(`BuildingBlocks.Infrastructure/Export`) scrive già il formato xlsx.

| Header | Esempio | Note |
|---|---|---|
| `Content-Type` | `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet` | File Excel. |
| `Content-Disposition` | `attachment; filename="log-2026-10-26.xlsx"` | `log-` più la data di oggi nel fuso di `X-Time-Zone` (`yyyy-MM-dd`) ed estensione `.xlsx`, come la stampa; il frontend lo usa per salvarlo. |

### Errori

Errori in JSON (Problem Details), come per `print-log`, mostrati nello stesso popup:

| Status | `code` | Quando |
|---|---|---|
| 400 | `validation.failed` | Con `errors[]`, come in `entries-list`: `search` oltre 100 caratteri, giorno di `datetimeTo` prima di quello di `datetimeFrom`, i limiti di `actionCode`, `sortField`, `sortDirection` o `filters` non validi. |
| 422 | `audit.export_too_large` | Più di 50.000 voci: nessun file; `args.max` e `args.total`. |
| 403 | `auth.forbidden` | Utente senza il permesso `AuditRead`. |
| 403 | `audit.property_required` | Token senza stabilimento. |
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido. |

## 9. Codici di azione del registro

La schermata del registro (`/settings/user-management/logs`) traduce e filtra le azioni con i codici di `AuditActionCodes`
(`Shared/Contracts/Audit/AuditActionCodes.cs`). Di un codice che non conosce mostra il codice, e non lo ha nel filtro.

- **I codici che ci sono non si rinominano** e non se ne aggiungono altri per le stesse azioni. Prenotazioni e pagamenti hanno già i loro
  (34 `booking.*`, per esempio `booking.reservation_checked_in`, `booking.reservations_cleaned_up`,
  `booking.sharing_settings_updated`, e 4 `payment.*`): le pagine Database e Sharing usano quelle API così come sono, con quelle voci. Il
  frontend aggiungerà al suo elenco quelli che non conosce.
- **Le azioni che oggi nessuno registra**, quando i loro moduli le scriveranno, usano **questi codici** (categoria `Operator`, come le
  azioni degli operatori di oggi), che il frontend ha già tradotto:

| Codice | Testo del frontend (italiano) | Modulo |
|---|---|---|
| `booking.arrival_reported` | Ingresso segnalato | Prenotazioni (`Module.Booking`) |
| `cash.pre_bill_printed` | Preconto stampato | Cassa e stampa fiscale (`Module.FiscalPrint` / `Module.Fiskaly`) |
| `cash.receipt_printed` | Scontrino stampato | Cassa e stampa fiscale (`Module.FiscalPrint` / `Module.Fiskaly`) |
| `order.marked_done` | Ordine segnalato come evaso | Ordini |
| `order.marked_not_done` | Ordine segnalato come non evaso | Ordini |

Se un'azione non serve o il backend ne vuole un'altra, va deciso insieme: il frontend allinea il suo elenco. Per ogni voce,
`bookingNumber` e `spotLabel` sono valorizzati quando l'azione riguarda una prenotazione o una postazione, come per le altre voci (sezione
5). **Oggi non lo sono per i 4 `payment.*`**: `ReservationPayments` non passa il numero della prenotazione, come fa Booking
(`BookingSupport.cs`), e la colonna "ID prenotazione" del registro resta vuota. Va aggiunto.

## 10. Controllo di versione sui salvataggi di modifica

Oggi, se due utenti del pannello modificano lo stesso utente, vince chi salva per ultimo: il popup "Modifica utente"
rimanda tutti i campi letti all'apertura (sezione 4) e cancella, senza che nessuno se ne accorga, quello che un altro
utente ha cambiato nel frattempo (per esempio con `PATCH /api/identity/property-users/{userId}` o con un altro popup
aperto sullo stesso utente). Il backend non ha un controllo di concorrenza sugli aggiornamenti: nessun token di
concorrenza in `AccountConfiguration` (`identity.access_users`), nessun `If-Match`. Il frontend chiede la **concorrenza
ottimistica** degli altri documenti, con un **codice di errore unico per tutte le API**: `resource.version_conflict`, anche qui al
posto di un codice dell'area `account.*`.

**Non è quello che Booking ha già per le prenotazioni** (`Endpoints.cs:253-255`, `ReservationService.cs:114-115`): lì
l'`If-Match` è un numero, se manca o non si legge non si controlla niente, e il conflitto è 412 o 409
`booking.version_conflict`. Qui valgono le regole sotto: con un altro codice il frontend non rileggerebbe i dati.

### Cosa ci serve

- **Versione nella lettura**: `get-selected-user` (sezione 3) restituisce, insieme ai dati, il campo `version` (stringa
  opaca). Cambia ogni volta che cambiano i dati che `update-user` scrive (tabella sotto), anche quando li cambia
  un'altra API (per esempio `PATCH /api/identity/property-users/{userId}` o `PUT /api/identity/me` per prefisso e
  cellulare), e solo allora.
- **La password non conta**: il popup non la legge mai e la manda solo se l'utente ne scrive una nuova, quindi un
  cambio di password (per esempio `POST /api/identity/me/password`) non cambia la versione e non fa fallire il Salva
  di un amministratore che sta modificando l'utente.
- **Non cambia** per i dati che `update-user` non scrive: per esempio `last_login_at`, che `Account.RecordLogin`
  aggiorna a ogni accesso dell'utente (`AuthService`), o `username`, `first_name`, `last_name`, `language` di
  `identity.access_users`. Per questo un token di tutta la riga (come `xmin` di `identity.access_users`) non va bene da
  solo: darebbe un conflitto ogni volta che l'utente modificato accede, e non vedrebbe le caselle "Abilita", che oggi
  `StaffService` non salva in quella tabella (le pubblica con l'evento `StaffContactChannelsChanged`; sezione 2,
  "Caselle Abilita").
- **`If-Match` nel salvataggio**: il frontend manda la versione letta nell'header `If-Match: "<version>"`. Se non è più
  quella attuale, il backend non salva niente (nemmeno la password, e non chiude sessioni) e risponde
  **409 `resource.version_conflict`**, senza `errors[]`. Senza `If-Match` risponde allo stesso modo: il frontend lo
  manda sempre.
- **`ETag` nella risposta**: ogni `update-user` riuscito restituisce la nuova versione nell'header `ETag: "<version>"`;
  il corpo resta quello della sezione 4.
- Il 409 `resource.version_conflict` si controlla dopo i controlli sull'utente da modificare (404, 422, 403 della
  sezione 3) e prima degli altri 409 (`account.email_taken`) e del 422 sul ruolo nuovo.
- **Controllo e salvataggio sono una cosa sola**: di due salvataggi dello stesso utente nello stesso momento con lo stesso
  `If-Match` uno riesce e l'altro risponde 409 `resource.version_conflict`; mai i dati di tutti e due mescolati, mai un 500.
- **Come farlo lo sceglie lo sviluppatore del backend** (un hash dei dati coperti dalla versione, un contatore
  aggiornato da ogni API che li cambia...), nei limiti dei due punti sopra: l'agente glielo chiede prima di scrivere
  codice. Il frontend tratta la versione come una stringa opaca e non la confronta mai.

### API coinvolte

| Lettura (restituisce `version`) | Salvataggio (manda `If-Match`, risponde `ETag`) | Dati coperti dalla versione |
|---|---|---|
| `GET /api/identity/get-selected-user` (sezione 3) | `POST /api/identity/update-user` (sezione 4) | I dati dell'utente che il popup legge e rimanda: email, caselle "Abilita" (`emailIsContact`, `phoneIsContact`), prefisso, cellulare e ruolo; non la password |

Le altre API di questo documento non ne hanno bisogno: `create-user` crea un utente nuovo, le tabelle, la tendina, la
stampa e l'esportazione leggono soltanto, e `DELETE /api/identity/property-users/{id}` non rimanda dati letti.

### Cosa fa il frontend

Con 409 `resource.version_conflict` il popup "Modifica utente" si chiude, compare l'avviso "Nel frattempo un altro
utente ha modificato questi dati: li abbiamo ricaricati. Rifai le tue modifiche." e la tabella si ricarica; le modifiche
scritte, password compresa, si perdono. Riaprendo la matita il popup rilegge l'utente con `get-selected-user`.

## API esistenti usate così come sono

Questa API del backend la usa già il frontend e **non va cambiata**: è qui solo perché chi lavora sulla pagina sappia
cosa il frontend si aspetta.

| Metodo | Rotta | Uso nella pagina |
|---|---|---|
| DELETE | `/api/identity/property-users/{id}` | Cestino di una riga, dopo la conferma: `id` = `idUser` di `users-list`; nessun corpo, lo stabilimento viene dal token. |

Il frontend si aspetta 204 senza corpo e questi errori, che mostra nel popup "Operazione non riuscita":

| Status | `code` | Quando |
|---|---|---|
| 404 | `account.not_found` | L'utente non è dello stabilimento. |
| 403 | `account.role_not_assignable` | L'utente ha un ruolo che non si gestisce dal pannello (il titolare `superadmin` o `websiteadmin`). |
| 422 | `account.cannot_modify_self` | L'utente collegato prova a eliminare se stesso. |
| 403 | `auth.forbidden` | Utente senza il permesso `StaffManage`. |
| 403 | `account.property_required` | Token senza stabilimento. |
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido. |
| 401 | `auth.session_expired` | L'account dell'utente collegato è stato cancellato nel frattempo (`CurrentAccount.GetAsync`, come oggi in `StaffService`). |

## Verifica

- `users-list`: `rows` ha al massimo `pageSize` righe e `total` conta tutte quelle che rispettano la ricerca; la ricerca
  trova un utente da una parte della sua email o del suo cellulare, con maiuscole o minuscole; i tre ordinamenti
  funzionano nei due versi (`role` nell'ordine di `Role.SortOrder`) e `sortField` vuoto dà ruolo, poi email; ogni riga
  ha solo i cinque campi, con `role` come codice di `RoleCodes`; un account salvato con prefisso `+39` ha
  `phoneCountryCode` `"39"`, uno senza cellulare `null`; una `page` oltre l'ultima dà 200 con `rows` vuoto e il `total`
  vero; `pageSize` 25 dà 400 `validation.invalid` su `pageSize`.
- `create-user` crea l'utente nello stabilimento del token, risponde 201 con i sette campi e scrive
  `identity.user_created`; ha gli stessi controlli ed errori di `property-users`; senza `role` dà 400
  `validation.required` sul campo `role`; un'email di 256 caratteri dà 400 `validation.max_length` su `email`; una
  password di 7 caratteri dà 400 `validation.password_too_short` con `args.min` 8.
- Caselle "Abilita": dopo `create-user` con `emailIsContact` `true` e `phoneIsContact` `false`, `get-selected-user`
  restituisce `true` e `false`; dopo `update-user` con i valori opposti restituisce quelli nuovi; un account che non le
  ha mai ricevute restituisce `false` e `false`.
- `get-selected-user` restituisce l'utente `idUser` dello stabilimento del token con i sette campi, gli stessi che
  `update-user` ha appena salvato; con `idUser` non UUID risponde 404 `account.not_found`; per il titolare 403
  `account.role_not_assignable`; per l'utente collegato 422 `account.cannot_modify_self`.
- `update-user` cambia solo l'utente `idUser` dello stabilimento del token, con tutti i campi ricevuti (`password`
  `null` la lascia com'è, `phone` `null` toglie numero e prefisso anche su un account che li aveva), risponde 200 con
  i sette campi e scrive
  `identity.user_updated`; su un account con `username`, `first_name`, `last_name`, `language`,
  `is_resource_scope_restricted` e `resource_scopes` già valorizzati, dopo `update-user` queste colonne di
  `identity.access_users` hanno gli stessi valori di prima.
- Controllo di versione (sezione 10): `get-selected-user` risponde con `version` (per esempio `v1`); `update-user` con
  `If-Match: "v1"` risponde 200 con l'header `ETag` di una versione nuova, diversa da `v1`, e una nuova
  `get-selected-user` risponde con quella versione. Lo stesso `update-user` con `If-Match: "v1"` dopo il primo, o senza
  `If-Match`, risponde 409 `resource.version_conflict` senza `errors[]` e l'utente non cambia (nemmeno la password, e
  le sue sessioni restano). Dopo un `PATCH /api/identity/property-users/{userId}` che cambia il cellulare,
  `update-user` con la versione letta prima risponde 409 `resource.version_conflict`. Dopo un accesso dell'utente
  (`last_login_at` cambia) la versione resta la stessa e `update-user` con quella versione riesce.
- Sessioni: dopo `update-user` con lo stesso ruolo e `password` `null` (cambia solo il cellulare), i refresh token
  dell'utente modificato in `identity.access_refresh_tokens` restano con `revoked_at` `null` e `POST
  /api/identity/auth/refresh` continua a funzionare per lui; con un ruolo diverso si revocano con `revoked_reason`
  `manual`; con una password nuova con `revoked_reason` `password_changed`.
- `entries-list` filtra più utenti e più azioni insieme, accetta `actionCode` con tutti i 102 codici del filtro, ordina
  le quattro colonne ordinabili nei due versi e restituisce righe con solo i sei campi; `search` di 101 caratteri dà
  400 `validation.max_length` su `search`; `datetimeTo` di un giorno prima di `datetimeFrom` dà 400
  `validation.end_before_start` su `filters`; `pageSize` 25 dà 400 `validation.invalid` su `pageSize` e `page` 0 dà
  400 `validation.invalid` su `page`.
- Periodo di `entries-list`: con `X-Time-Zone: Europe/Rome`, `datetimeFrom` `2026-10-24T00:00:00.000Z` e `datetimeTo`
  `2026-10-25T23:59:59.999Z` (il 25/10/2026 finisce l'ora legale) restituisce le voci con `occurredAt` da
  `2026-10-23T22:00:00Z` (compreso) a `2026-10-25T23:00:00Z` (escluso): una voce delle `2026-10-25T22:30:00Z` c'è, una
  delle `2026-10-23T21:59:59Z` no; con lo stesso giorno nei due filtri restituisce tutto quel giorno, da mezzanotte a
  mezzanotte ora italiana; senza header `X-Time-Zone` lo stesso risultato che con `Europe/Rome`.
- `user-combobox` restituisce `{ id, value }` per ogni utente da mostrare; gli `id` filtrano `entries-list`; per un
  utente creato da `create-user` (senza nome né username) `value` è la sua email; senza
  `AuditRead` (es. ruolo `manager`) risponde 403 `auth.forbidden`.
- `print-log` restituisce un file `xlsx` (nome `log-<data di oggi>.xlsx` in `Content-Disposition`) con la riga dei
  titoli e tutte le voci che rispettano ricerca e filtri, nell'ordine chiesto: lo stesso file di `export-log` con lo
  stesso corpo; con zero voci ha solo la riga dei titoli.
- `export-log` accetta lo stesso corpo di `print-log` e restituisce un file `xlsx` con la riga dei titoli e tutte le
  voci filtrate, nell'ordine chiesto, e il nome in `Content-Disposition`.
- `print-log` ed `export-log`: con `Accept-Language: en` i titoli sono `Booking ID`, `Full name`, `Spot`, `Action`,
  `Date and time` e le azioni in inglese; con `X-Time-Zone: America/New_York` una voce delle `2026-10-23T11:11:00Z` ha
  "Data e ora" `2026-10-23 07:11:00`, con `Europe/Rome` `2026-10-23 13:11:00`; con più di 50.000 voci rispondono 422
  `audit.export_too_large` con `args.max` 50000 e `args.total`, senza file e senza scrivere nel registro; ogni file
  scrive `audit.entries_exported` nel registro, con `format` `xlsx` per la stampa e per l'esportazione.
- I codici della sezione 9, quando i loro moduli registrano le azioni, sono quelli della tabella, con la categoria
  `Operator`, e `GET /api/audit/action-codes` li restituisce. Un pagamento registrato su una prenotazione ha il suo numero nella voce
  `payment.payment_recorded`, e la ricerca per numero la trova.
- Ovunque: lo stabilimento è quello del token, nessuna richiesta lo contiene; un token senza stabilimento dà 403
  `account.property_required` (`/api/identity/*`) o `audit.property_required` (`/api/audit/*`).
