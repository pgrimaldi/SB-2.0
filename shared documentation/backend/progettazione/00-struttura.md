PROMPT 01

Si vuole fare un refactoring di un'applicativo web. Attualmente scritto in nodejs da convertire in .NET 10.
Qui si vuole sviluppare il backend. Non sarà una semplice conversione da node a .NET ma verranno ristutturata la base dati e le chiamate API.
Ci sono le seguenti cartelle:
 - docs: documenti md vari
 - documenti: conterrà file word ed excel
 - back-pre: contiene i sorgenti nodejs del backend
 - front-pre: contiene i sorgenti nodejs del frontend
 - backend: contiene la solution del nuovo backend (parzialmente impostato)
 - stato-pre: per ora non utilizzata
 - infra: file relativi all'infrastruttura

Una copia parziale (solo una parte dei dati) della base dati si trova in:
LOCAL_HOST="127.0.0.1"
LOCAL_USER="root"
LOCAL_PASSWORD="root"
LOCAL_PORT=3306
LOCAL_DB="local_sb"

Tiene conto delle seguenti informazioni:
 - il cambio dal vecchio al nuovo è radicale: cambia la base dati e le api
 - ci sono tabelle in db inutilizzate


Ora analizza tutto il materiale (ci sono sottocartelle - leggi anche i file docx) e in particolare back-pre (front-pre serve principalmente perché c'è del codice frontend da spostare nel backend) e scrivi un file md (01-....md) in docs dove elenchi tutte le criticità di sicurezza, performance, mancanza di best practice, codice da spostare da front a back, .. Scrivi indicazioni per il backend.
Se c'è qualche chiarimento di cui hai bisogno, prima di procedere, chiedi.

-------------------------------

PROMPT 02

Basandoti sia sui dati presenti in db e in base al codice back-pre (e quando necessario front-pre) esegui il seguente compito.
Nella cartella analisi-pre/db crea un file md per ogni tabella del database (properties.ms, customers.md, ..).
In ciascuno di questi file fai un elenco di tutti i campi e per ogni campo indica il nome, il tipo, a cosa serve (descrizione dettagliata), vincoli, eventuali relazioni con altre tabelle, default (se presente), ecc.. . Alcuni campi sono JSON, descrivere ogni campo presente nel JSON. Ad esempio:
Tabella properties
Campi:
- id
    Tipo: int
    Vincoli: chiave primaria, campo autoicrementale
    Relazioni: 
    Descrizione: campo utilizzato per ..
- paymentOptions
    Tipo: JSON
    Vincoli: ..
    Relazioni: ..
    Default: ..
    Descrizione: ..
    Campi:
     - deposit_now_half_info
        Tipo: ..
        Vincoli: ..
        Relazioni: ..
        Descrizione: ..
     - pay_at_property_info
        ..

Per la descrizione dettagliata basati principalmente sul codice back-pre.
Se c'è qualche chiarimento di cui hai bisogno, prima di procedere, chiedi.

Inizia con la tabella properties e procedi con le altre solo dopo che ti dico di procedere.

--
Per ogni campo vedi a cosa serve e compila la descrizione tracciando l'utilizzo nel back, e nel front a ritroso

--
Procedi ora con le tabelle ... .
Per le descrizioni (ed il resto) non basarti sui nomi e NON fare supposizioni, NON inventare ma basati solo sul codice ed i valori presenti a db. Attraversa backe e front (tutti). Fai un lavoro preciso e dettagliato
Una volta scritti i file md, verifica in modo accurato tutto.

--

Procedi ora con le tabelle relative a Prenotazioni (feature).
Per le descrizioni (ed il resto) non basarti sui nomi e NON fare supposizioni, NON inventare ma basati solo sul codice ed i valori presenti a db. Attraversa backe e front (tutti). Fai un lavoro preciso e dettagliato
Una volta scritti i file md, verifica in modo accurato tutto.
Metti per ogni campo del db anche:"Nullabile" (sì o no), "Esempio di valori" (pochi di valori di esempio, se la stringa è lunga tagliane una parte; metti i valori anche nei sottovalori dei JSON; per gli autoincrementali scrivi "1, 2, 3, ..") e "Deprecato" (solo quando il valore è sì - quando il campo non è utilizzato dal codice)

Individua tutti campi che possono essere sostituiti con un enumeratore e aggiorna 03-gestione-dati.md sezione Enumerazioni.

Alla fine scrivi in chat quali sono i file md creati


-

quali sono le tabelle del db necessare per quella che sarà la feature di Catalogo & onfigurazione?

Analizza la situazione hotel. Valuta se fare come per booking: la parte di impostazione in configuration (C2) e la parte di prenotazione (C1). Sìì obiettivo.

- 

Verifica se ci sono tabelle che non sono state inserite delle tabelle nei vari domain (tabelle orfane). Per queste, verifica se sono utilizzate (dal codice) e scrivi in chat in quale domain metteresti ognuna di esse (o eventuale nuovo/ domain).
Ci sono le tabelle chatbot_* che fanno parte di una feature in un branch a parte (CX dove X è l'ultimo numero, se non ce ne sono altre, X = 8). beacharound_reservation non verrà utilizzata. Alla fine elencale tutte quelle che hai trovato che prima non eraano censite con di fianco il suggerimento su come usarle.



-

Inserisci in 03-gestione-dati.md una nota in cui è scritto che, anche se alcune tabelle sono state inserite in un dominio, alcuni loro campi vanno in altro dominio. Rivedi tutti i campi di tutte le tabelle e disegna una tabella nell'md con i seguenti campi: tabella, domain della tabella, campo, domain di destinazione del campo.

-------------------------------

PROMPT 03


Considerato il frontend, backend e db, in analisi-pre/business-logic scrivi un documento md (o più) per feature dove in ognuno di essi scriverai la logica di business di ogni azione dell'utente indicando quello che viene fatto: calcoli (es: operazioni su prezzi), logiche (condizioni, iterazioni) e eventi (invio notifiche, invio email, chiamata api esterne, ..). Scrivi tutto da un punto di vista logico come testo (no codice), ad esempio "se l'utente ha ruolo admin o superadmin ..": una logica per ogni interazione dell'utente. Dividi i vari file per tipo di utente (cliente finale, utente di stabilimento, systemadmin) e per voce di menù (tutte le funzionalità all'interno di una voce di menù vanno nello stesso file)
Esegui una voce di menù alla volta in modo dettagliato. Prenditi il tempo di cui hai bisogno.


Se c'è qualche chiarimento di cui hai bisogno, prima di procedere, chiedi.

-------------------------------

PROMPT 04

Verifica se base dati contiene tutte le info e quali no



FIGMA
https://www.figma.com/design/xMeMgSTTdp7aN4kPBfyYpH/Summer-Booking-e-Summer-Booking-Hub---mockups?node-id=0-1&p=f&t=cFBO8eYQf2PEnlAY-0

REPO
https://github.com/pgrimaldi/SB-2.0

Trello
https://trello.com/b/lXUII9ee/summer-booking

