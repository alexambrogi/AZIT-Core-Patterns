  ---
  Struttura generata

  samples/AzetaIT.Core.Patterns.Sample/
  ├── Entities/
  │   └── Ordine.cs            ← entity + enum StatoOrdine
  ├── Data/
  │   └── AppDbContext.cs      ← DbContext con mapping fluente
  ├── Services/
  │   ├── IOrderService.cs     ← contratto pubblico
  │   └── OrderService.cs      ← implementazione con IUnitOfWork
  ├── appsettings.json         ← connection string SQL Server
  └── Program.cs               ← DI bootstrap + demo CRUD

  ---
  Come leggere OrderService — i pattern chiave

  Read → usa sempre GetReadRepository (semantica no-tracking possibile):
  uow.GetReadRepository<Ordine>().GetAllAsync(ct)

  Write semplice → SaveChangesAsync esplicito dopo la modifica:
  await repo.AddAsync(ordine, ct);
  await uow.SaveChangesAsync(ct);      // ← tu decidi quando salvare

  Transazione esplicita → BeginTransaction + CommitAsync (che ingloba SaveChanges):
  await uow.BeginTransactionAsync(ct);
  try   { ordine.Stato = ...; repo.Update(ordine); await uow.CommitAsync(ct); }
  catch { await uow.RollbackAsync(ct); throw; }

  DI in Program.cs — tre righe:
  services.AddDbContext<AppDbContext>(...);
  services.AddAzetaUnitOfWork<AppDbContext>();  // ← extension della libreria
  services.AddScoped<IOrderService, OrderService>();

  Per eseguire: imposta la connection string in appsettings.json (o tramite variabile d'ambiente
  ConnectionStrings__Default) e lancia dotnet run dalla cartella del sample.




   ---
  Scelta del repository — regola pratica

  // ── Leggi senza mai modificare (report, proiezioni, liste) ────────────────
  var repo = uow.GetReadRepository<Ordine>();
  // → ReadOnlyRepository: AsNoTracking su tutto, change tracker non coinvolto

  // ── Leggi per poi modificare/cancellare ──────────────────────────────────
  var repo = uow.GetRepository<Ordine>();
  // → Repository: tracking attivo, EF rileva le modifiche automaticamente

  Differenza concreta:

  ┌───────────────────┬────────────────────────┬────────────────────────────────┐
  │                   │    GetRepository<T>    │      GetReadRepository<T>      │
  ├───────────────────┼────────────────────────┼────────────────────────────────┤
  │ Classe            │ Repository<T,TContext> │ ReadOnlyRepository<T,TContext> │
  ├───────────────────┼────────────────────────┼────────────────────────────────┤
  │ Tracking          │ attivo                 │ AsNoTracking su ogni query     │
  ├───────────────────┼────────────────────────┼────────────────────────────────┤
  │ Update(entity)    │ disponibile            │ non esposto (IReadRepository)  │
  ├───────────────────┼────────────────────────┼────────────────────────────────┤
  │ Cache UoW         │ _cache                 │ _readCache (separata)          │
  ├───────────────────┼────────────────────────┼────────────────────────────────┤
  │ Query()           │ queryable tracked      │ queryable no-tracking          │
  ├───────────────────┼────────────────────────┼────────────────────────────────┤
  │ QueryNoTracking() │ no-tracking esplicito  │ identico a Query()             │
  └───────────────────┴────────────────────────┴────────────────────────────────┘

  Nota su GetByIdAsync in ReadOnlyRepository: con AsNoTracking attivo, DbSet.FindAsync non funziona (cerca prima in
  cache). La classe usa FirstOrDefaultAsync con EF.Property<object>(e, "Id") — funziona per entità con chiave primaria
  chiamata Id. Se usi chiavi con nome diverso, passa da QueryNoTracking().FirstOrDefaultAsync(...) direttamente nel
  service.




   Fuori e dentro una transazione, il criterio è lo stesso — dipende da cosa fai dopo la lettura, non dalla transazione:

  ┌─────────────────────────────────────────────┬───────────────────────────────────────────────────────────────────┐
  │                  Scenario                   │                        Repository da usare                        │
  ├─────────────────────────────────────────────┼───────────────────────────────────────────────────────────────────┤
  │ Leggo e poi modifico/cancello               │ GetRepository<T>() — tracking attivo, EF rileva le modifiche      │
  │                                             │ automaticamente                                                   │
  ├─────────────────────────────────────────────┼───────────────────────────────────────────────────────────────────┤
  │ Leggo solo per validare, controllare o      │ GetReadRepository<T>() — no-tracking, più leggero                 │
  │ restituire                                  │                                                                   │
  └─────────────────────────────────────────────┴───────────────────────────────────────────────────────────────────┘

  La transazione aperta non cambia la scelta — entrambi i repository usano lo stesso DbContext, che è già enlistato
  nella transazione. La differenza è solo nel change tracker EF, non nel contesto di database.

  ---
  Esempio concreto dentro una transazione:

  await uow.BeginTransactionAsync(ct);
  try
  {
      // Devo modificare → tracking
      var ordine = await uow.GetRepository<Ordine>().GetByIdAsync(id, ct);

      // Solo validazione → no-tracking, non finirà nel SaveChanges
      var clienteEsiste = await uow.GetReadRepository<Cliente>().ExistsAsync(c => c.Id == ordine.ClienteId, ct);

      ordine.Stato = StatoOrdine.Confermato; // EF lo rileva perché è tracked
      await uow.CommitAsync(ct);
  }
  catch { await uow.RollbackAsync(ct); throw; }

  Se leggessi Ordine con GetReadRepository e poi modificassi il campo, EF non vedrebbe il cambiamento — dovresti
  chiamare esplicitamente GetRepository<Ordine>().Update(ordine) per ri-agganciarlo al change tracker. Più verboso e
  soggetto a errori.
  
  
  
  
  
  
  ---
  Cosa è stato aggiunto

  Entities/Product.cs          ← [Table("products")] + [Key] per Dapper.Contrib
  Services/IProductService.cs
  Services/ProductService.cs   ← Dapper puro (SQL raw)
  appsettings.json             ← SqlServer + Postgres connection strings
  Program.cs                   ← keyed DI + demo separati per i due DB

  ---
  Pattern chiave da leggere in ProductService

  Cast a IDapperRepository<T> — obbligatorio per accedere ai metodi SQL raw:
  private IDapperRepository<Product> Repo =>
      (IDapperRepository<Product>)uow.GetRepository<Product>();

  Read → GetReadRepository (senza overhead del cast):
  private IReadRepository<Product> ReadRepo => uow.GetReadRepository<Product>();

  INSERT PostgreSQL — RETURNING id invece di SCOPE_IDENTITY() di SQL Server:
  var id = await Repo.ExecuteScalarAsync<int>(
      "INSERT INTO products (...) VALUES (...) RETURNING id", new { ... });

  Query filtrata — nessun FindAsync(predicate), solo SQL:
  await Repo.QueryAsync("SELECT * FROM products WHERE scorta <= @soglia", new { soglia });

  ---
  Keyed DI in Program.cs — nessun conflitto su IUnitOfWork

  // SQL Server → chiave "sqlserver"
  services.AddKeyedScoped<IUnitOfWork>("sqlserver",
      (sp, _) => new EFUnitOfWork<AppDbContext>(...));

  // PostgreSQL → chiave "postgres"
  services.AddKeyedScoped<IUnitOfWork>("postgres",
      (_, _) => new DapperUnitOfWork(new NpgsqlConnection(pgConnStr)));

  Il service dichiara da quale chiave vuole ricevere la UoW tramite [FromKeyedServices] sul costruttore — nessuna
  ambiguità, nessun cast in DI:
  public class OrderService  ([FromKeyedServices("sqlserver")] IUnitOfWork uow) ...
  public class ProductService([FromKeyedServices("postgres")]  IUnitOfWork uow) ...




  PER GENERARE UN ASSEMLBY DI DOMAIN APPLICATION SEGUIRE GLI STEPS:

  # Piano: referenze per un assembly Domain/Application

## Context
L'utente vuole creare un assembly applicativo (Application Layer / Domain Services) che contenga
tutti i service per la gestione delle entità del DB. Il dubbio è quali progetti della libreria
referenziare: solo le astrazioni, o anche le implementazioni specifiche (EF Core / Dapper).

---

## Struttura dipendenze esistente

```
AzetaIT.Core.Patterns               ← zero dipendenze esterne (solo SDK)
│   IUnitOfWork
│   IRepository<T>
│   IReadRepository<T>
│   IWriteRepository<T>
│
├── AzetaIT.Core.Patterns.EntityFrameworkCore  (dipende da Core + EF Core)
│       EFUnitOfWork<TContext>, Repository<T,TContext>, ReadOnlyRepository<T,TContext>
│
└── AzetaIT.Core.Patterns.Dapper              (dipende da Core + Dapper)
        DapperUnitOfWork
        IDapperRepository<T>   ← QueryAsync / ExecuteAsync / ExecuteScalarAsync
        DapperRepository<T>
        DapperReadOnlyRepository<T>
```

---

## Risposta

### Regola base: il Domain/Application Layer referenzia SOLO `AzetaIT.Core.Patterns`

Il livello applicativo dipende solo dalle astrazioni. Le implementazioni concrete (EF Core o
Dapper) vengono iniettate a runtime dal composition root (startup / Program.cs).

Questo mantiene il layer infrastruttura-agnostico: puoi sostituire EF con Dapper (o viceversa)
senza toccare i service.

```
MyApp.Application  →  AzetaIT.Core.Patterns
MyApp.API          →  AzetaIT.Core.Patterns.EntityFrameworkCore  (o .Dapper)
```

### Eccezione: serve IDapperRepository<T> nel service?

`IDapperRepository<T>` (con `QueryAsync`, `ExecuteAsync`, `ExecuteScalarAsync`) vive nel progetto
`AzetaIT.Core.Patterns.Dapper`, non nelle astrazioni core.

Se un service ha bisogno di SQL raw (filtri complessi, query con join, stored procedure),
ci sono due opzioni:

**Opzione A — Pragmatica** (consigliata per progetti non enterprise)
Referenzia anche `AzetaIT.Core.Patterns.Dapper` nel layer applicativo.
Il service casta a `IDapperRepository<T>` dove serve.
Pro: semplice. Contro: l'app layer conosce l'infrastruttura.

**Opzione B — Clean Architecture** (consigliata se si prevede di cambiare ORM)
Crea un'interfaccia di query custom nel Core (es. `IProductQueryService`) con i metodi
specifici, implementata nel layer infrastruttura. Il service chiama l'interfaccia senza
sapere che dietro c'è Dapper.

---

## Raccomandazione pratica

Per un progetto standard:
- Referenzia **sempre** `AzetaIT.Core.Patterns` nel Domain/Application
- Referenzia `AzetaIT.Core.Patterns.Dapper` **solo** se e quando hai servizi che fanno SQL raw
- Non referenziare mai le implementazioni infrastrutturali nel layer di dominio puro (entità, value objects)
- Non referenziare mai entrambi (EF Core + Dapper) nello stesso service assembly, a meno di
  scenari ibridi espliciti (es. writes EF, reads Dapper per CQRS)

---

## Verifica
Nessuna modifica al codice — risposta architetturale pura.
