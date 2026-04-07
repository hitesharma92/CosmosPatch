# CosmosPatch

A generic, interactive console utility for **bulk-patching Azure Cosmos DB documents** using an Excel workbook as input/output. Supports five patch operations, automatic multi-database/container discovery, JSON backup before every change, and progress reporting.

---

## Features

| # | Operation | Description |
|---|-----------|-------------|
| 1 | **Patch Property** | Set one or more top-level or nested properties on matching documents |
| 2 | **Update Partition Key** | Move documents to a new partition key value (delete + re-insert) |
| 3 | **Remove Child Array Item** | Remove an element from an array property matched by WHERE criteria |
| 4 | **Patch Child Array Item** | Update field(s) inside an array element matched by WHERE criteria |
| 5 | **Update Id** | Replace the `id` of a document (delete + re-insert) |

- **Multi-database support** — interactively selects the target database and container via arrow-key menus (auto-selects when only one exists)
- **JSON backup** — every document is backed up to `./Backup - {name}/` before mutation
- **Structured logging** — timestamped log file written to `./Logs - {name}/`
- **Progress bar** — colored console bar with elapsed/remaining time
- **Environment detection** — URL-based detection of DEV / QA / STAGING / PROD with confirmation before PROD writes

---

## Prerequisites

| Requirement | Version |
|-------------|---------|
| .NET SDK    | 8.0+    |
| Azure Cosmos DB account | Any (SQL / NoSQL API) |

---

## Getting Started

### Clone and build

```bash
git clone https://github.com/<your-org>/CosmosPatch.git
cd CosmosPatch
dotnet build
```

### Run

```bash
dotnet run --project src/CosmosPatch.Console
```

You will be prompted for:
1. **Cosmos DB URL** — e.g. `https://my-account.documents.azure.com:443/`
2. **Primary key** (master key)
3. **Excel file path** — absolute path to the `.xlsx` workbook
4. **Operation** — choose 1–5 from the menu
5. **Database** — if multiple databases exist, select from the arrow-key menu
6. **Container** — same picker for containers

---

## Excel Workbook Format

All operations read from the workbook's **first worksheet**. Row 1 is the header row. Data starts at row 2.

**👉 Quick start:** Use the ready-made templates in the `Sample-Input-Templates/` folder and adapt them to your data. See **[Sample Files](#sample-files)** section below for details.

### Common columns (all operations)

| Column | Header | Description |
|--------|--------|-------------|
| A | `id` | Document `id` value |
| B | `partition_key` | Partition key value |

### Operation-specific columns

#### 1 — Patch Property

Columns C onward each become a patch target. The header is the **JSON property path** (e.g. `status`, `address/city`).

```
id | partition_key | status | address/city
```

#### 2 — Update Partition Key

Exactly 3 data columns required. Column C header must match the container's partition key property name.

```
id | partition_key | <new_partition_key_property>
```

#### 3 — Remove Child Array Item

Column headers from C onward act as **WHERE** filters to identify the array element to remove. You are prompted for the array property name at runtime.

```
id | partition_key | <match_field_1> | <match_field_2> ...
```

#### 4 — Patch Child Array Item

Uses prefixed headers: `where:<field>` to identify the array element and `patch:<field>` to specify values to set.

```
id | partition_key | where:type | patch:value | patch:label
```

#### 5 — Update Id

Exactly 3 data columns. Column C header must be `new_id`.

```
id | partition_key | new_id
```

After each run, result columns (`patch_status`, `delete_status`) are written back to the workbook.

---

## Sample Files

The `Sample-Input-Templates/` folder contains ready-to-use Excel files for each operation. Download any file, fill in your own `id` and `partition_key` values, and pass the path to CosmosPatch.

| File | Operation | Required Columns | Example Property Columns | Notes |
|------|-----------|---------|-------|-------|
| `op1-patch-property.xlsx` | **Patch Property** | `id`, `partition_key` | Custom (see file) | Extend with your own property columns; supports nested paths like `address/city` |
| `op2-update-partition-key.xlsx` | **Update Partition Key** | `id`, `partition_key`, `<new_pk_property>` | — | Column C name matches your partition key property |
| `op3-remove-child-array-item.xlsx` | **Remove Child Array Item** | `id`, `partition_key`, `where:<field>` | — | Array property name (e.g., `roles`) is entered interactively |
| `op4-patch-child-array-item.xlsx` | **Patch Child Array Item** | `id`, `partition_key`, `where:<field>`, `patch:<field>` | — | Use `where:` prefix to identify array element, `patch:` prefix for values to set |
| `op5-update-id.xlsx` | **Update Id** | `id`, `partition_key`, `new_id` | — | Simple 3-column format for ID migration |

**Note:** The utility automatically appends `PatchStatus` and `DeleteStatus` columns to your Excel file after execution, showing the result of each operation.

---

## Project Structure

```
CosmosPatch/
├── src/
│   ├── CosmosPatch.Domain/               # Entities, interfaces, enums — no external deps
│   ├── CosmosPatch.Application/          # Business logic, patch strategies
│   ├── CosmosPatch.Infrastructure.Cosmos/ # Cosmos DB client + repository
│   ├── CosmosPatch.Infrastructure.Storage/ # Excel (ClosedXML) + JSON backup
│   └── CosmosPatch.Console/              # Entry point, DI composition root
└── tests/
    └── CosmosPatch.Tests/                # xUnit unit tests (26 tests)
```

### Dependency flow

```
Console → Application → Domain ← Infrastructure.*
```

Infrastructure projects implement Domain interfaces; Application references only Domain.

---

## Configuration

No `appsettings.json` is required. All configuration is entered interactively at startup:

| Prompt | Notes |
|--------|-------|
| Cosmos DB URL | Used for environment detection (dev/qa/staging/prod) |
| Primary key | Stored only in memory for the lifetime of the process |
| Excel file path | Must be an accessible `.xlsx` file |

---

## Running Tests

```bash
dotnet test
```

Tests cover `EnvironmentDetector`, `ArrayItemMatcher`, `PatchValueParser`, and `ExcelDataStore` with 26 test cases.

---

## Dependencies

| Package | Version | License |
|---------|---------|---------|
| Microsoft.Azure.Cosmos | 3.58.0 | MIT |
| ClosedXML | 0.105.0 | MIT |
| Newtonsoft.Json | 13.0.4 | MIT |
| Microsoft.Extensions.DependencyInjection | 10.0.5 | MIT |
| xunit | 2.5.3 | Apache 2.0 |
| Moq | 4.20.72 | BSD-3-Clause |

---

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).

---

## License

[MIT](LICENSE)
