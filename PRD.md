# Product Requirements Document
## TenantHub — Multitenant SaaS Platform with CShells

**Version:** 1.0  
**Stack:** .NET 10 · Blazor Server · Entity Framework Core · SQL Server · CShells  
**CShells source:** https://github.com/valence-works/cshells

---

## 1. Overview

TenantHub is a multitenant SaaS platform built on CShells' per-shell DI container and config-driven feature system. Each tenant is modelled as a **shell**; a **SuperAdmin** manages tenants and their feature flags from a central Blazor dashboard. All tenant state, feature configuration, and audit data is persisted in SQL Server via Entity Framework Core. Shell settings are loaded at runtime from the database using a custom `IShellSettingsProvider`, so changes take effect without restarting the application.

---

## 2. Goals

| # | Goal |
|---|------|
| G1 | SuperAdmin can create, edit, suspend and delete tenants from a Blazor dashboard |
| G2 | SuperAdmin can enable or disable named features per tenant with immediate effect |
| G3 | Each tenant gets an isolated shell with its own DI scope and configuration |
| G4 | Tenant users see only the UI sections backed by enabled features |
| G5 | All mutations are persisted in SQL Server and surfaced in an audit log |
| G6 | Shell configuration is loaded from the DB (custom `IShellSettingsProvider`) |

---

## 3. Users & Roles

| Role | Description |
|------|-------------|
| **SuperAdmin** | Platform operator. Full access to the admin dashboard. Manages tenants and feature flags. |
| **TenantAdmin** | Per-tenant administrator. Can manage tenant users and view tenant-level settings. |
| **TenantUser** | End user of a tenant. Sees only the features enabled for their tenant. |

---

## 4. Solution Structure

```
TenantHub/
├── src/
│   ├── TenantHub.Web/                  # Blazor Server host (references all projects below)
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   └── Pages/
│   │       ├── Admin/                  # SuperAdmin dashboard pages
│   │       └── Tenant/                 # Tenant-facing pages
│   ├── TenantHub.Features/             # CShells feature definitions
│   │   ├── CoreFeature.cs
│   │   ├── NotesFeature.cs
│   │   ├── TasksFeature.cs
│   │   ├── AnnouncementsFeature.cs
│   │   └── MediaFeature.cs
│   ├── TenantHub.Data/                 # EF Core DbContexts, models, migrations
│   │   ├── AdminDbContext.cs           # Platform-wide (tenants, features, audit)
│   │   ├── TenantDbContext.cs          # Per-tenant scoped data
│   │   └── Migrations/
│   └── TenantHub.Core/                 # Shared DTOs, interfaces, helpers
└── tests/
    └── TenantHub.Tests/
```

> `TenantHub.Features` only references `CShells.AspNetCore.Abstractions` to stay lightweight. All EF logic lives in `TenantHub.Data`.

---

## 5. Data Model

### 5.1 AdminDbContext (platform-wide)

```csharp
// Tenant
public class Tenant
{
    public Guid   Id            { get; set; }
    public string Name          { get; set; } = "";        // display name
    public string Slug          { get; set; } = "";        // URL path segment → shell Name
    public string? Description  { get; set; }
    public TenantStatus Status  { get; set; }              // Active | Suspended | Deleted
    public DateTime CreatedAt   { get; set; }
    public DateTime? SuspendedAt{ get; set; }

    public ICollection<TenantFeature> Features { get; set; } = [];
}

public enum TenantStatus { Active, Suspended, Deleted }

// Which features are enabled for a tenant
public class TenantFeature
{
    public Guid   Id         { get; set; }
    public Guid   TenantId   { get; set; }
    public string FeatureName{ get; set; } = "";   // matches [ShellFeature] name
    public bool   IsEnabled  { get; set; }
    public DateTime UpdatedAt{ get; set; }

    public Tenant Tenant { get; set; } = null!;
}

// Audit log
public class AuditLog
{
    public long     Id         { get; set; }
    public string   Actor      { get; set; } = "";   // email of SuperAdmin
    public string   Action     { get; set; } = "";   // e.g. "FeatureEnabled"
    public string   EntityType { get; set; } = "";
    public string   EntityId   { get; set; } = "";
    public string?  Detail     { get; set; }         // JSON diff / free text
    public DateTime OccurredAt { get; set; }
}
```

### 5.2 TenantDbContext (per-tenant, scoped inside each shell)

Each tenant shares the same SQL Server instance but uses a **schema named after the tenant slug** (e.g. `acme.Notes`). EF Core's `HasDefaultSchema` is set at runtime from `ShellSettings`.

```csharp
// Notes feature
public class Note
{
    public Guid     Id        { get; set; }
    public string   Title     { get; set; } = "";
    public string   Body      { get; set; } = "";
    public string   CreatedBy { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt{ get; set; }
}

// Tasks feature
public class TaskItem
{
    public Guid     Id          { get; set; }
    public string   Title       { get; set; } = "";
    public bool     IsCompleted { get; set; }
    public string   AssignedTo  { get; set; } = "";
    public DateTime DueDate     { get; set; }
    public DateTime CreatedAt   { get; set; }
}

// Announcements feature
public class Announcement
{
    public Guid     Id          { get; set; }
    public string   Title       { get; set; } = "";
    public string   Body        { get; set; } = "";
    public bool     IsPinned    { get; set; }
    public DateTime PublishedAt { get; set; }
    public DateTime ExpiresAt   { get; set; }
}

// Media feature
public class FileRecord
{
    public Guid     Id           { get; set; }
    public string   FileName     { get; set; } = "";
    public string   ContentType  { get; set; } = "";
    public long     SizeBytes    { get; set; }
    public string   StoragePath  { get; set; } = "";   // relative path on disk/blob
    public string   UploadedBy   { get; set; } = "";
    public DateTime UploadedAt   { get; set; }
}
```

---

## 6. CShells Integration

### 6.1 Custom `IShellSettingsProvider`

A `DatabaseShellSettingsProvider` reads `Tenant` + `TenantFeature` rows from `AdminDbContext` and maps them to `ShellSettings` objects at startup and on-demand reload.

```csharp
public class DatabaseShellSettingsProvider : IShellSettingsProvider
{
    public async Task<IEnumerable<ShellSettings>> GetAllAsync()
    {
        // query AdminDbContext for Active tenants + their enabled features
        // map each Tenant → ShellSettings { Name = slug, Features = [...] }
        // always include "Core" feature
        // set WebRouting:Path = tenant.Slug
    }
}
```

Register in `Program.cs`:

```csharp
builder.AddShells(cshells =>
{
    cshells.WithProvider<DatabaseShellSettingsProvider>();
});
```

### 6.2 Feature Definitions (`TenantHub.Features`)

| Feature class | `[ShellFeature]` name | DependsOn | Registers |
|---------------|----------------------|-----------|-----------|
| `CoreFeature` | `Core` | — | `TenantDbContext`, `ICurrentTenantService` |
| `NotesFeature` | `Notes` | `Core` | `INoteService` |
| `TasksFeature` | `Tasks` | `Core` | `ITaskService` |
| `AnnouncementsFeature` | `Announcements` | `Core` | `IAnnouncementService` |
| `MediaFeature` | `Media` | `Core` | `IMediaService` |

Each feature implements `IWebShellFeature` and registers both its services and its Blazor component routes (via `MapRazorComponents` / endpoint groups).

### 6.3 Runtime Shell Reload

When the SuperAdmin toggles a feature, the application calls `IShellHost.UpdateShellAsync(slug)` (CShells runtime management API) after saving to the DB. This reloads the shell's DI container without a full app restart.

---

## 7. Features Catalogue

### F-01 · Core (always enabled)

Always included in every shell. Provides:
- `TenantDbContext` scoped to the tenant's DB schema
- `ICurrentTenantService` (resolves tenant from `ShellSettings`)
- Tenant-level user authentication via ASP.NET Core Identity (shared Identity tables, filtered by `TenantId`)

### F-02 · Notes

Simple rich-text note taking per tenant.

**Pages (Blazor):** `/notes` list, `/notes/new`, `/notes/{id}`

**Persistence:** `Note` table in tenant schema.

**Behaviour:**
- Create, read, update, soft-delete notes
- Notes are listed newest-first
- Full-text search by title

### F-03 · Tasks

Lightweight task tracker.

**Pages (Blazor):** `/tasks` board (To Do / In Progress / Done columns), `/tasks/{id}`

**Persistence:** `TaskItem` table in tenant schema.

**Behaviour:**
- Create tasks with title, due date, assignee
- Drag-and-drop status columns (Blazor + JS interop)
- Filter by assignee or due-date range

### F-04 · Announcements

Broadcast messages to all tenant users.

**Pages (Blazor):** `/announcements` feed (users), `/announcements/manage` (TenantAdmin only)

**Persistence:** `Announcement` table in tenant schema.

**Behaviour:**
- TenantAdmin creates/pins/expires announcements
- Pinned announcements appear at the top of every page (via shared layout)
- Announcements auto-hide past `ExpiresAt`

### F-05 · File Vault

Upload and share files within the tenant.

**Pages (Blazor):** `/files` list, upload dialog

**Persistence:** `FileRecord` metadata in tenant schema; binary stored on local disk (configurable path per shell via `ShellSettings`).

**Behaviour:**
- Upload files up to 50 MB
- Download by authenticated tenant users
- Delete by uploader or TenantAdmin
- List shows name, size, uploader, date

---

## 8. Admin Dashboard (SuperAdmin)

All pages under the `/admin` route prefix, protected by the `SuperAdmin` role.

### 8.1 Tenant List (`/admin/tenants`)

- Paginated table: Name, Slug, Status, Features enabled (badge count), Created date
- Actions: **Create**, **Edit**, **Suspend/Activate**, **Delete**
- Search by name or slug

### 8.2 Create / Edit Tenant (`/admin/tenants/new`, `/admin/tenants/{id}/edit`)

**Fields:**

| Field | Type | Notes |
|-------|------|-------|
| Name | string | Display name |
| Slug | string | URL segment, unique, lowercase, alphanumeric + hyphens |
| Description | string? | Optional |
| Status | enum | Active / Suspended |

On save:
1. Persist `Tenant` row via EF Core
2. Write `TenantFeature` rows for each feature toggle
3. Call `IShellHost.UpdateShellAsync` (or `CreateShellAsync` for new tenants)
4. Write `AuditLog` entry

### 8.3 Feature Management (`/admin/tenants/{id}/features`)

Grid of all available features with a toggle switch per feature.

- Toggling a feature: saves `TenantFeature.IsEnabled`, reloads the shell, writes audit log
- Feature cards show: name, description, dependency badges (e.g. "Requires Core")
- Disabled state shown for features whose dependencies are not met

### 8.4 Audit Log (`/admin/audit`)

- Paginated, filterable log of all SuperAdmin actions
- Columns: Timestamp, Actor, Action, Entity Type, Entity, Detail
- Filter by actor, action type, date range
- Read-only; no delete

---

## 9. Tenant Portal

Each tenant is accessed at `/{slug}/*`. The shell middleware resolves the shell from the URL path segment.

**Shared layout:**
- Top navigation shows only links to enabled features
- Pinned announcements banner (if `Announcements` feature is enabled)
- User menu (profile, sign out)

**Feature pages** are registered by each feature's `MapEndpoints` / component registration and are only reachable when the feature is active in the shell.

---

## 10. Technical Architecture

```
Browser
  │
  ▼
Blazor Server (SignalR)
  │
  ├── CShells Middleware  (resolves shell from URL path)
  │       │
  │       ▼
  │   Per-Shell DI Container
  │       ├── CoreFeature services   (always)
  │       ├── NotesFeature services  (if enabled)
  │       ├── TasksFeature services  (if enabled)
  │       └── ...
  │
  ├── AdminDbContext  ──► SQL Server  (dbo schema — tenants, features, audit)
  └── TenantDbContext ──► SQL Server  (per-tenant schema — notes, tasks, etc.)
```

### 10.1 EF Core Configuration

- **AdminDbContext** — `dbo` schema, registered as singleton-like scoped service in the host container
- **TenantDbContext** — registered inside each shell's DI container by `CoreFeature`; schema set from `ShellSettings["TenantSchema"]`
- Migrations split: `AdminDbContext` migrations in `TenantHub.Data/Migrations/Admin/`; `TenantDbContext` migrations in `TenantHub.Data/Migrations/Tenant/`
- `TenantDbContext` migrations applied on-demand at shell activation via `dbContext.Database.MigrateAsync()`

### 10.2 Authentication

- ASP.NET Core Identity tables in `dbo` schema (shared across tenants)
- Users carry a `TenantId` claim; `ICurrentTenantService` validates the claim matches the active shell
- SuperAdmin role stored in Identity, no tenant affiliation

### 10.3 Shell Settings Provider Flow

```
App startup
  └─► DatabaseShellSettingsProvider.GetAllAsync()
        └─► SELECT active Tenants + TenantFeatures
              └─► Build ShellSettings[]
                    └─► CShells activates one shell per tenant
```

On feature toggle:
```
SuperAdmin clicks toggle
  └─► TenantFeature row updated (EF Core)
        └─► AuditLog written
              └─► IShellHost.UpdateShellAsync(slug)
                    └─► Shell DI container rebuilt with new feature set
```

---

## 11. Non-Functional Requirements

| NFR | Requirement |
|-----|-------------|
| Security | All `/admin` routes require `SuperAdmin` role; tenant routes require valid `TenantId` claim matching the shell |
| Isolation | Tenants must not be able to read each other's data; enforced by per-shell DI + schema separation |
| Performance | Admin dashboard tenant list must paginate ≤ 50 rows; feature toggles must reload shell within 2 s |
| Auditability | Every SuperAdmin mutation writes an `AuditLog` row with actor, timestamp, and detail |
| Migrations | Tenant schema migrations run automatically on first shell activation |
| Resilience | Suspended tenants return HTTP 503 from the shell middleware; Active shells unaffected |

---

## 12. Project Dependencies (NuGet)

| Package | Used in |
|---------|---------|
| `CShells` | `TenantHub.Web` |
| `CShells.AspNetCore` | `TenantHub.Web` |
| `CShells.Abstractions` | `TenantHub.Features` |
| `CShells.AspNetCore.Abstractions` | `TenantHub.Features` |
| `Microsoft.EntityFrameworkCore.SqlServer` | `TenantHub.Data` |
| `Microsoft.EntityFrameworkCore.Tools` | `TenantHub.Data` |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | `TenantHub.Data` |
| `Microsoft.AspNetCore.Components.Server` | `TenantHub.Web` |

---

## 13. Milestones

| Milestone | Scope |
|-----------|-------|
| **M1 — Foundation** | Solution scaffold, AdminDbContext + migrations, DatabaseShellSettingsProvider, CoreFeature, Identity, SuperAdmin auth |
| **M2 — Admin Dashboard** | Tenant CRUD, Feature toggles, Audit Log pages (Blazor) |
| **M3 — Tenant Features** | Notes, Tasks, Announcements, Media features + Blazor pages |
| **M4 — Polish** | Shell suspension middleware, pinned announcements layout, search/filter, E2E tests |

---

## 14. Out of Scope (v1)

- Billing / subscription management
- Per-tenant custom domains (host-based shell resolution)
- Email notifications
- Cross-tenant reporting
- White-label theming per tenant
