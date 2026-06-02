# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Test

```powershell
# Build entire solution
dotnet build AZIT.Patterns.slnx

# Build release (generates NuGet packages)
dotnet build AZIT.Patterns.slnx -c Release
```

There is no test project — verification is done via the sample console app in `samples/AzetaIT.Core.Patterns.Sample/`.

## Package architecture

Four packages with a strict layering rule:

```
AzetaIT.Core.Patterns                          ← zero external dependencies
│   IUnitOfWork, IRepository<T>
│   IReadRepository<T>, IWriteRepository<T>
│
├── AzetaIT.Core.Patterns.EntityFrameworkCore  ← EF Core implementation
│       EFUnitOfWork<TContext>
│       Repository<T,TContext>          (tracking, non-sealed)
│       ReadOnlyRepository<T,TContext>  (AsNoTracking)
│
├── AzetaIT.Core.Patterns.Dapper               ← Dapper implementation
│       DapperUnitOfWork
│       IDapperRepository<T>  ← adds QueryAsync / ExecuteAsync / ExecuteScalarAsync
│       DapperRepository<T>, DapperReadOnlyRepository<T>
│
└── AzetaIT.Core.Patterns.MySql                ← thin Pomelo/EF Core wrapper
        AddAzetaMySqlUnitOfWork<TContext>()
```

**EF Core version split** — the `.csproj` uses conditional `ItemGroup`:

| TFM | EF Core |
|-----|---------|
| `net8.0` | 9.x |
| `net10.0` | 10.x |

`Microsoft.Extensions.DependencyInjection.Abstractions` is referenced explicitly only on `net8.0`; on `net10.0` it flows in from EF Core 10.

## IUnitOfWork contract

- `GetRepository<T>()` — full read/write, change tracking on, cached per UoW instance
- `GetReadRepository<T>()` — same cached instance cast to `IReadRepository<T>`, AsNoTracking
- `SaveChangesAsync` / `BeginTransactionAsync` / `CommitAsync` / `RollbackAsync`
- `HasActiveTransaction` — guard to avoid double-begin
- **Write methods (`Add/Update/Remove`) never call `SaveChanges`** — persistence is always explicit
- **`CommitAsync` wraps save + commit atomically** — auto-calls `RollbackAsync` on exception

## Repository selection rule

| Scenario | Use |
|----------|-----|
| Read to then mutate/delete | `GetRepository<T>()` — tracking active, EF detects changes automatically |
| Read for validation, projection, list | `GetReadRepository<T>()` — no-tracking, lighter |

The rule applies the same way inside or outside a transaction — both repositories share the same `DbContext`, already enlisted in the transaction. The difference is only in the EF change tracker.

`GetByIdAsync` in `ReadOnlyRepository` uses `FirstOrDefaultAsync` with `EF.Property<object>(e, "Id")` — works for entities with a PK named `Id`. For differently named PKs, use `QueryNoTracking().FirstOrDefaultAsync(...)` directly in the service.

## DI registration

**Single provider:**
```csharp
services.AddDbContext<AppDbContext>(o => o.UseSqlServer(connStr));
services.AddAzetaUnitOfWork<AppDbContext>();   // IUnitOfWork registered as Scoped
```

**Multiple providers in the same app (keyed DI):**
```csharp
services.AddKeyedScoped<IUnitOfWork>("sqlserver",
    (sp, _) => new EFUnitOfWork<AppDbContext>(sp.GetRequiredService<AppDbContext>()));
services.AddKeyedScoped<IUnitOfWork>("postgres",
    (_, _) => new DapperUnitOfWork(new NpgsqlConnection(pgConnStr)));

// Consumed via constructor parameter attribute:
public class OrderService([FromKeyedServices("sqlserver")] IUnitOfWork uow) ...
public class ProductService([FromKeyedServices("postgres")]  IUnitOfWork uow) ...
```

## Dapper-specific patterns

`IDapperRepository<T>` is only available by casting — `GetRepository<T>()` returns `IRepository<T>`:

```csharp
private IDapperRepository<Product> Repo =>
    (IDapperRepository<Product>)uow.GetRepository<Product>();
```

`DapperUnitOfWork.SaveChangesAsync` is a no-op (returns 0) — writes execute immediately. Transactions are the atomicity boundary.

`FindAsync(predicate)` and `ExistsAsync(predicate)` throw `NotSupportedException` in Dapper repositories — use raw SQL via `QueryAsync` instead.

PostgreSQL insert with generated id:
```csharp
var id = await Repo.ExecuteScalarAsync<int>(
    "INSERT INTO products (...) VALUES (...) RETURNING id", new { ... });
```

## Sample structure pattern

The `samples/` projects show the recommended layering for a consumer assembly:

```
Entities/
    Ordine.cs          ← EF entity class  (implements IOrdine)
    IOrdine.cs         ← read-only interface + OrdineRequest record (write input)
Services/
    IOrderService.cs   ← public contract  (returns IOrdine, accepts OrdineRequest)
    OrderService.cs    ← implementation   (depends on IUnitOfWork only)
Data/
    AppDbContext.cs    ← DbContext with fluent mapping
Extensions/
    ServiceCollectionExtensions.cs
```

Input records hold only caller-supplied fields (no `Id`, no server-set timestamps/defaults). The interface is getter-only on server-side fields (`Id`, `DataOrdine`, `Stato`).

## Domain / Application layer dependency rule

```
MyApp.Application  →  AzetaIT.Core.Patterns               (always)
MyApp.API          →  AzetaIT.Core.Patterns.EntityFrameworkCore  (or .Dapper)
```

Reference `AzetaIT.Core.Patterns.Dapper` in the application layer only when services need raw SQL via `IDapperRepository<T>`. Never reference both EF Core and Dapper implementations in the same service assembly unless doing explicit CQRS (writes EF, reads Dapper).

`Repository<T, TContext>` is non-sealed with `protected` fields (`Context`, `Set`). Subclass it to add domain-specific query methods, then register manually or override `GetRepository<T>` in a custom UoW subclass.
