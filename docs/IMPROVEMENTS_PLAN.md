# S3 Storage Manager — 10 Major Improvements Plan

A prioritized roadmap for evolving the app in security, UX, performance, and operations.

---

## 1. **Presigned URLs for Downloads & Previews**

**Why:** Today, every download/preview goes through the app: the server streams S3 bytes to the browser. That uses app CPU/memory and bandwidth and can become a bottleneck for large files or many users.

**Improvement:** Generate short-lived presigned URLs (e.g. 5–15 minutes) for Download and Preview. The browser fetches directly from S3; the app only issues the URL. Reduces server load and often improves perceived speed.

**Rough steps:** Add a small service that builds `GetPreSignedUrlRequest` for GetObject; Preview/Download pages return a redirect to that URL (or, for preview modal, set `img`/`iframe` `src` to the presigned URL). Keep existing stream-based path as fallback if needed (e.g. when presigning is disabled).

---

## 2. **Bulk Operations (Multi-Select + Batch Delete / Move)**

**Why:** Deleting or moving many files one-by-one is slow and error-prone.

**Improvement:**  
- Checkbox per row (file/folder); “Select all” in header.  
- Toolbar: “Delete selected”, “Move selected” (with optional “Copy” later).  
- For delete: confirm once, then delete in batches (reuse existing delete/delete-folder logic).  
- For move: pick target prefix, then copy + delete (or multipart copy for large objects).

**Rough steps:** Add client-side selection state; POST selection to new BulkDelete/BulkMove endpoints that iterate over keys and call existing S3 APIs; show progress or a single success/error summary.

---

## 3. **Search & Filter in Current Bucket**

**Why:** In buckets with many keys or deep hierarchies, finding a file by name or type is hard.

**Improvement:**  
- A search box that filters the **currently loaded** list by name (client-side) for instant feedback.  
- Optional **server-side search**: list objects with prefix derived from search (e.g. type-ahead by prefix) or use S3 ListObjectsV2 with prefix filter and optional key-marker for paging.  
- Filters: “Files only” / “Folders only”, “By type” (e.g. images, PDFs), “Modified after date”.

**Rough steps:** Add a search input and filter dropdowns above the table; for client-side, filter `Model.Items` in JS or re-render a filtered subset; for server-side, add a query param and pass prefix/continuation to the list API and optionally cache or limit depth.

---

## 4. **Folder Move / Rename (and Copy)**

**Why:** Users need to reorganize without re-uploading. Today only delete exists for folders.

**Improvement:**  
- **Move:** Choose target folder; for each object under the source prefix, copy to new key (new prefix) then delete original. Use multipart copy for large objects.  
- **Rename:** Treat as move within the same parent (change one path segment).  
- **Copy folder:** Same as move but without deleting originals.  
- Progress or background job for large folders so the UI doesn’t time out.

**Rough steps:** New “Move” / “Copy” actions on folder row and optionally in bulk; target selector (tree or path input); backend endpoint that lists under prefix, then CopyObject (and DeleteObject for move); consider a simple job queue or “run in background” with polling for big folders.

---

## 5. **Stronger Secret Storage (e.g. AWS Secrets Manager / Key Vault)**

**Why:** User S3 credentials are today encrypted with ASP.NET Core Data Protection, which is good for local/server keys but not ideal for multi-machine or audit requirements.

**Improvement:** Optionally store encrypted credentials in a dedicated secret store:  
- **AWS Secrets Manager**: one secret per user (e.g. key `s3-profile-{userId}`) with access key and secret; app retrieves at runtime and caches briefly.  
- Or **Azure Key Vault** / **Hashicorp Vault** if that fits the environment.  
- Keep a “simple” mode (current Data Protection) for single-server or dev; add a config switch and a `IUserS3CredentialStore` abstraction so the rest of the app stays unchanged.

**Rough steps:** Define interface (get/set encrypted profile by user); implement current DP as default; add Secrets Manager (or similar) implementation and config flag; migrate existing profiles on first read or via a one-off script.

---

## 6. **Activity / Audit Log**

**Why:** For shared or compliance-sensitive buckets, you need to know who did what and when.

**Improvement:**  
- Log key events: login/logout, S3 profile create/update, file upload, delete (file/folder), download/preview (optional, can be noisy), move/copy.  
- Store in DB (e.g. `AuditEntry`: UserId, Action, ResourceType, ResourceKey, Timestamp, IP, optional details JSON).  
- Optional: “Activity” page filtered by user, date, action, or key prefix; export CSV.  
- Optionally integrate with CloudTrail later for S3-side audit; app log remains the source for “who used the UI”.

**Rough steps:** Add `AuditService` and table; call it from auth, S3 profile, and S3 operations; add Activity page with filters and pagination.

---

## 7. **Dark Mode**

**Why:** Matches the design system (Ovovex) and user preference; reduces eye strain.

**Improvement:**  
- Use design tokens for both themes (e.g. `--ds-background`, `--ds-foreground`, `--ds-muted` etc.) and a `.dark` or `[data-theme="dark"]` scope.  
- Toggle in nav or user menu; persist choice in localStorage (and optionally in user profile in DB).  
- Ensure preview modal, tables, cards, and alerts look correct in dark mode (no hardcoded light colors).

**Rough steps:** Add CSS variables for dark theme; add a small script that toggles `data-theme` and reads/saves preference; update design-system.css to use variables in both themes; test all main pages and the preview modal.

---

## 8. **Pagination or Virtual Scrolling for Large Directories**

**Why:** Listing thousands of keys in one request is slow and makes the page heavy; can hit timeouts or memory limits.

**Improvement:**  
- **Server-side pagination:** ListObjectsV2 with `MaxKeys` (e.g. 100–200) and `ContinuationToken`; “Next” / “Previous” (and optional page size selector).  
- **Infinite scroll:** Load next page when user scrolls near the bottom; append rows.  
- Or **virtual scrolling:** Only render visible rows (e.g. with a small library or custom logic) so the DOM stays small even with 10k keys.  
- Keep “folder first” ordering and show a clear “Loading…” or skeleton while fetching the next page.

**Rough steps:** Add `continuationToken` and `pageSize` to the list API and Index model; return next token to the view; add “Load more” or next/prev buttons (or infinite scroll); optionally virtualize the table body.

---

## 9. **Email Verification & Password Reset (Real Flow)**

**Why:** Today “Forgot password” and email verification are placeholders; no emails are sent. That blocks real use and secure self-service.

**Improvement:**  
- Integrate an email sender (e.g. SendGrid, AWS SES, or SMTP) via `IEmailSender`.  
- **Email verification:** Send a link with token on register; mark email confirmed only when the link is used.  
- **Password reset:** Forgot password → send link with token; reset page validates token and sets new password.  
- Optional: rate limit “forgot password” and “resend verification” by email/IP to avoid abuse.

**Rough steps:** Configure and register `IEmailSender`; use Identity’s `GenerateEmailConfirmationTokenAsync` / `ConfirmEmailAsync` and `GeneratePasswordResetTokenAsync` / `ResetPasswordAsync`; add ResetPassword page and wire ForgotPassword to send the email with a link.

---

## 10. **Health Check & Operational Readiness**

**Why:** For deployment behind a load balancer or in Kubernetes, you need a health endpoint; for debugging, a simple “status” view helps.

**Improvement:**  
- **Health endpoint:** e.g. `/health` or `/ready` that returns 200 if the app can start and (optionally) reach DB and S3 (e.g. list one bucket or run a no-op). Use `Microsoft.Extensions.Diagnostics.HealthChecks` and optionally AspNetCore.HealthChecks.UI for a dashboard.  
- **Readiness vs liveness:** Liveness = app running; readiness = DB + optional S3 check.  
- **Status page (optional):** A simple “System status” page (behind auth or admin) showing: app version, DB connectivity, last S3 check, and recent errors from a small in-memory or DB log.  
- **Structured logging:** Ensure logs are JSON or consistent so they can be shipped to a log aggregator.

**Rough steps:** Add health check packages; register DB and optional S3 health checks; map `/health` and `/ready`; add a minimal status page and optional error sink for “recent errors”.

---

## Summary Table

| # | Improvement              | Impact       | Effort (rough) |
|---|--------------------------|-------------|----------------|
| 1 | Presigned URLs           | Performance | Medium         |
| 2 | Bulk operations          | UX          | Medium–High    |
| 3 | Search & filter          | UX          | Medium         |
| 4 | Folder move/rename/copy  | UX          | High           |
| 5 | Stronger secret storage  | Security    | Medium         |
| 6 | Activity / audit log     | Security/Ops| Medium         |
| 7 | Dark mode                | UX          | Low–Medium     |
| 8 | Pagination / virtual list| Performance | Medium         |
| 9 | Email verification & reset | Security  | Medium         |
| 10| Health check & readiness | Ops         | Low            |

You can tackle them in order of impact (e.g. 10 → 7 → 1 → 3 → 2) or by theme (e.g. all security first: 5, 6, 9).
