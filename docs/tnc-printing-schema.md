# Terms & Conditions Printing — Data Structure & Table Schema

Insurance underwriting: allow underwriters to customise clauses printed on each policy document.

## Problem summary

| | |
|---|---|
| **Current** | Pre-defined clauses in one table with an `ordering` column. Print / skip logic for a few opinion variances is hard-coded in the app. |
| **Next** | Underwriters can add / remove clauses, edit content and order. Customisation is **per policy**. |

## Design principles

1. Keep a **shared clause template** library.
2. Each policy gets its own **copied clause set** (content + order). Editing a policy never changes the master templates or other policies.
3. Move opinion → default clauses into the database (`OpinionClauseDefault`) so new variances do not require code changes.
4. Prefer soft remove (`IsIncluded = 0`) for audit / undo.

## Conceptual model

```
ClauseTemplate ──┐
                 ├── OpinionClauseDefault  (opinion → which templates to seed)
                 │
Policy ── PolicyClauseSet ── PolicyClause  (what actually prints)
                 ▲
                 └── seeded from templates (or free-text add)
```

---

## Table schemas

### 1. `ClauseTemplate` — company standard clauses

| Column | Type | Null | Notes |
|--------|------|------|--------|
| `TemplateId` | `INT` / `BIGINT` identity | No | PK |
| `Code` | `VARCHAR(50)` | No | Unique business code, e.g. `TC_EXCLUSION_01` |
| `Title` | `NVARCHAR(200)` | No | Display / heading |
| `BodyHtmlOrText` | `NVARCHAR(MAX)` | No | Clause body |
| `DefaultOrder` | `INT` | No | Default print order when seeded |
| `IsActive` | `BIT` | No | Soft-retire templates |
| `Version` | `INT` | No | Bump when template text changes |
| `CreatedAt` | `DATETIME2` | No | |
| `UpdatedAt` | `DATETIME2` | No | |

**Keys / indexes**

- PK: `TemplateId`
- UNIQUE: `Code`

---

### 2. `OpinionClauseDefault` — opinion → default templates

Replaces hard-coded “3 variances” print logic.

| Column | Type | Null | Notes |
|--------|------|------|--------|
| `OpinionCode` | `VARCHAR(50)` | No | Insured opinion / product option, e.g. `STD`, `PLUS`, `PREMIUM` |
| `TemplateId` | `INT` / `BIGINT` | No | FK → `ClauseTemplate` |
| `DefaultOrder` | `INT` | No | Order when seeding a new policy |
| `Include` | `BIT` | No | `1` = seed as included; `0` = known but not printed by default |

**Keys / indexes**

- PK: `(OpinionCode, TemplateId)`
- FK: `TemplateId` → `ClauseTemplate(TemplateId)`

---

### 3. `PolicyClauseSet` — one customisable set per policy

| Column | Type | Null | Notes |
|--------|------|------|--------|
| `PolicyClauseSetId` | `INT` / `BIGINT` identity | No | PK |
| `PolicyId` | `INT` / `BIGINT` | No | FK → Policy; **one set per policy** |
| `SourceOpinionCode` | `VARCHAR(50)` | Yes | Opinion used to seed |
| `IsCustomized` | `BIT` | No | `1` after underwriter edits |
| `CreatedAt` | `DATETIME2` | No | |
| `UpdatedAt` | `DATETIME2` | No | |

**Keys / indexes**

- PK: `PolicyClauseSetId`
- UNIQUE: `PolicyId`

---

### 4. `PolicyClause` — rows that print (source of truth at print time)

| Column | Type | Null | Notes |
|--------|------|------|--------|
| `PolicyClauseId` | `INT` / `BIGINT` identity | No | PK |
| `PolicyClauseSetId` | `INT` / `BIGINT` | No | FK → `PolicyClauseSet` |
| `TemplateId` | `INT` / `BIGINT` | Yes | Source template; **NULL** if free-text clause added by UW |
| `Code` | `VARCHAR(50)` | Yes | Copied or custom code |
| `Title` | `NVARCHAR(200)` | No | May differ from template after edit |
| `BodyHtmlOrText` | `NVARCHAR(MAX)` | No | May differ from template after edit |
| `PrintOrder` | `INT` | No | Final print order |
| `IsIncluded` | `BIT` | No | `0` = removed / not printed (soft delete) |
| `CreatedAt` | `DATETIME2` | No | |
| `UpdatedAt` | `DATETIME2` | No | |

**Keys / indexes**

- PK: `PolicyClauseId`
- FK: `PolicyClauseSetId` → `PolicyClauseSet`
- FK: `TemplateId` → `ClauseTemplate` (nullable)
- INDEX: `(PolicyClauseSetId, PrintOrder)`

**Print query**

```sql
SELECT Title, BodyHtmlOrText
FROM PolicyClause
WHERE PolicyClauseSetId = @SetId
  AND IsIncluded = 1
ORDER BY PrintOrder;
```

---

### 5. (Optional) `PolicyClauseHistory` — audit

| Column | Type | Null | Notes |
|--------|------|------|--------|
| `HistoryId` | `BIGINT` identity | No | PK |
| `PolicyClauseId` | `BIGINT` | No | |
| `ChangeType` | `VARCHAR(20)` | No | `ADD` / `EDIT` / `REORDER` / `EXCLUDE` / `INCLUDE` |
| `BeforeBody` | `NVARCHAR(MAX)` | Yes | |
| `AfterBody` | `NVARCHAR(MAX)` | Yes | |
| `BeforeOrder` | `INT` | Yes | |
| `AfterOrder` | `INT` | Yes | |
| `ChangedBy` | `VARCHAR(100)` | No | Underwriter user id |
| `ChangedAt` | `DATETIME2` | No | |

---

## Workflow

| Step | What happens |
|------|----------------|
| 1. Issue / open print set | Look up `OpinionClauseDefault` by `OpinionCode`. Insert `PolicyClauseSet`. Copy matching templates into `PolicyClause` (`IsIncluded` from `Include`, `PrintOrder` from `DefaultOrder`). `IsCustomized = 0`. |
| 2. Customise | Edit `Body` / `Title` / `PrintOrder`; set `IsIncluded = 0` to remove; insert new row with `TemplateId = NULL` to add. Set `IsCustomized = 1`. |
| 3. Print | Read included `PolicyClause` ordered by `PrintOrder`. |
| 4. Template library change | Affects **new** policies only. Existing `PolicyClause` rows keep their copied text. |

---

## Example data

Assume three templates and three opinions: `STD`, `PLUS`, `PREMIUM`.

### `ClauseTemplate`

| TemplateId | Code | Title | BodyHtmlOrText (short) | DefaultOrder | IsActive | Version |
|------------|------|-------|------------------------|--------------|----------|---------|
| 1 | `TC_GENERAL` | General Conditions | All parties agree to … | 10 | 1 | 1 |
| 2 | `TC_EXCLUSION` | Exclusions | War, nuclear, … | 20 | 1 | 1 |
| 3 | `TC_FPS_NOTE` | FPS Settlement Note | FPS payments settle T+0 … | 30 | 1 | 1 |
| 4 | `TC_PREMIUM_DISC` | Premium Discount Terms | Discount applies if … | 40 | 1 | 1 |

### `OpinionClauseDefault`

| OpinionCode | TemplateId | DefaultOrder | Include |
|-------------|------------|--------------|---------|
| `STD` | 1 | 10 | 1 |
| `STD` | 2 | 20 | 1 |
| `STD` | 3 | 30 | 0 |
| `PLUS` | 1 | 10 | 1 |
| `PLUS` | 2 | 20 | 1 |
| `PLUS` | 3 | 30 | 1 |
| `PREMIUM` | 1 | 10 | 1 |
| `PREMIUM` | 2 | 20 | 1 |
| `PREMIUM` | 3 | 30 | 1 |
| `PREMIUM` | 4 | 40 | 1 |

---

### Scenario A — New policy, opinion `STD`, no customisation

**`PolicyClauseSet`**

| PolicyClauseSetId | PolicyId | SourceOpinionCode | IsCustomized |
|-------------------|----------|-------------------|--------------|
| 100 | 5001 | `STD` | 0 |

**`PolicyClause`** (seeded; TemplateId 3 not included because `Include = 0`)

| PolicyClauseId | PolicyClauseSetId | TemplateId | Code | Title | Body (short) | PrintOrder | IsIncluded |
|----------------|-------------------|------------|------|-------|--------------|------------|------------|
| 1001 | 100 | 1 | `TC_GENERAL` | General Conditions | All parties agree to … | 10 | 1 |
| 1002 | 100 | 2 | `TC_EXCLUSION` | Exclusions | War, nuclear, … | 20 | 1 |

**Prints:** General → Exclusions.

---

### Scenario B — Same opinion, underwriter customises Policy 5002

Seeded like A, then UW:

1. Edits exclusion text.
2. Removes nothing, but **adds** a free-text clause.
3. Reorders so custom clause prints first.

**`PolicyClauseSet`**

| PolicyClauseSetId | PolicyId | SourceOpinionCode | IsCustomized |
|-------------------|----------|-------------------|--------------|
| 101 | 5002 | `STD` | 1 |

**`PolicyClause`**

| PolicyClauseId | PolicyClauseSetId | TemplateId | Code | Title | Body (short) | PrintOrder | IsIncluded |
|----------------|-------------------|------------|------|-------|--------------|------------|------------|
| 1101 | 101 | NULL | `UW_CUSTOM_01` | Special Endorsement | Subject to survey by 15 Nov … | 5 | 1 |
| 1102 | 101 | 1 | `TC_GENERAL` | General Conditions | All parties agree to … | 10 | 1 |
| 1103 | 101 | 2 | `TC_EXCLUSION` | Exclusions | War, nuclear, **and flood in Zone A** … | 20 | 1 |

**Prints:** Special Endorsement → General → Exclusions (edited).  
Policy 5001 (Scenario A) is **unchanged**.

---

### Scenario C — Opinion `PLUS`, then UW removes a clause

**`PolicyClauseSet`**

| PolicyClauseSetId | PolicyId | SourceOpinionCode | IsCustomized |
|-------------------|----------|-------------------|--------------|
| 102 | 5003 | `PLUS` | 1 |

**`PolicyClause` after remove of FPS note**

| PolicyClauseId | PolicyClauseSetId | TemplateId | Code | Title | PrintOrder | IsIncluded |
|----------------|-------------------|------------|------|-------|------------|------------|
| 1201 | 102 | 1 | `TC_GENERAL` | General Conditions | 10 | 1 |
| 1202 | 102 | 2 | `TC_EXCLUSION` | Exclusions | 20 | 1 |
| 1203 | 102 | 3 | `TC_FPS_NOTE` | FPS Settlement Note | 30 | **0** |

**Prints:** General → Exclusions. Row 1203 kept for audit / restore.

---

### Scenario D — Opinion `PREMIUM`, default (no edit)

**`PolicyClauseSet`**

| PolicyClauseSetId | PolicyId | SourceOpinionCode | IsCustomized |
|-------------------|----------|-------------------|--------------|
| 103 | 5004 | `PREMIUM` | 0 |

**`PolicyClause`**

| PolicyClauseId | TemplateId | Code | PrintOrder | IsIncluded |
|----------------|------------|------|------------|------------|
| 1301 | 1 | `TC_GENERAL` | 10 | 1 |
| 1302 | 2 | `TC_EXCLUSION` | 20 | 1 |
| 1303 | 3 | `TC_FPS_NOTE` | 30 | 1 |
| 1304 | 4 | `TC_PREMIUM_DISC` | 40 | 1 |

**Prints:** all four in order 10 → 40.

---

### Scenario E — Master template updated later

Admin sets `ClauseTemplate` TemplateId=2 Version=2, new exclusion wording.

| Policy | Effect |
|--------|--------|
| 5001–5004 already issued | Still print **copied** body in `PolicyClause` |
| New Policy 5005 seeded after change | Gets new template body |

---

## Decision summary

| Decision | Recommendation |
|----------|----------------|
| Edit policy clause vs master | Always copy into `PolicyClause`; print from there |
| Remove clause | `IsIncluded = 0` (soft) |
| Add clause | New `PolicyClause` with `TemplateId = NULL` |
| Opinion variances | Drive via `OpinionClauseDefault`, not application `if/else` |
| Template wording change | New policies only, unless you add an explicit “refresh from template” action |

---

## Optional next steps

- Add `PolicyClauseHistory` if compliance needs who changed what.
- If clauses are shared multi-language, add `Locale` on template / policy clause or a child `*_Text` table.
- If several products share templates, add `ProductCode` filter on `OpinionClauseDefault` or a bridge table.
