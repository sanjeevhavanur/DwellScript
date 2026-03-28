# DwellScript — Claude Code Context Prompt
> Paste this entire file into the Claude VS Code plugin context window at the start of every coding session.

---

## 1. What Is DwellScript?

DwellScript is a **SaaS web application** that helps independent landlords write professional, platform-optimized rental listing copy using AI. A landlord enters their property details once, clicks a button, and receives four ready-to-post outputs: a Long-Term Rental (LTR) listing, a Short-Term Rental (STR) listing, a Social Media post, and headline variants — all generated in a single Claude API call and scanned for Fair Housing compliance before display.

A Pro-tier feature called the **Vacancy Gap Analyzer** diagnoses underperforming listings by comparing them against performance context (days live, inquiries received, rent) and returns ranked weaknesses with side-by-side diffs and one-click rewrite suggestions.

**Owner / operator:** Sanjeev Bajanihavanur, operating under BAJANIHAVANUR LLC, Chesapeake, Virginia.
**Solo developer:** Mac + VS Code. Timeline: 10–12 weeks at 5–10 hrs/week.
**Brand:** DwellScript — coined compound: "Dwell" (real estate) + "Script" (AI writing).

---

## 2. Tech Stack — Every Decision Is Final

### Frontend
- **Bootstrap 5** — layout, grid, utility classes only. No BS components (modals, dropdowns) — all UI is custom.
- **jQuery + Ajax** — all async calls to the Web API. No fetch() — use `$.ajax()` consistently.
- **Razor HTML shells** — `.cshtml` pages render the page skeleton only. Zero server-side business logic in views.
- **Toastr.js** — all user notifications (success, error, info, warning). Always top-right, 4000ms, closeButton + progressBar.
- **Bootstrap Icons** — icon library throughout. Class format: `bi bi-{icon-name}`.
- **Inter** — sole font family. Weights used: 300, 400, 500, 600, 700, 800.
- **No TypeScript.** Plain JavaScript only.
- **No React, Vue, or any JS framework.**

### Backend
- **ASP.NET Core 8 MVC** — Controllers render pages only (return `View()`). All data operations go through Web API controllers.
- **ASP.NET Core 8 Web API** — all business logic lives in API controllers at `/api/`. Returns JSON exclusively.
- **Entity Framework Core 8** — ORM. Code-first migrations. Never write raw SQL unless absolutely unavoidable.
- **Dependency Injection** — every service is registered in `Program.cs` and injected via constructor.
- **Service layer** — all logic lives in `/Services/`. Controllers never touch DbContext directly.

### Database
- **SQL Server 2022** — production on Railway (`mcr.microsoft.com/mssql/server:2022-latest`).
- **Local dev:** Docker Desktop with `--platform linux/amd64` flag (Rosetta 2 on Mac).
- **Connection string:** stored in `appsettings.Development.json`, never committed to Git.

### External Services
| Service | Purpose | Notes |
|---|---|---|
| **Anthropic Claude API** | AI generation + vacancy analysis | Server-side only. API key in env vars, never in frontend. Model: `claude-opus-4-5` or latest Sonnet. |
| **Stripe** | Subscriptions + billing | Checkout Sessions, Webhooks, Customer Portal. Webhook is source of truth — never grant access on redirect alone. |
| **Resend** | Transactional email | Magic link auth + dunning emails. 3K/mo free tier. |

### Auth
- **ASP.NET Core Identity** — foundation. Supports both login methods.
- **Magic Link** — email-based passwordless. Custom `MagicLinkTokens` table. Token hashed in DB, expires 15 min, single-use.
- **Google OAuth** — via `AddGoogle()`. Same-email account linking handled in `AuthService`.
- **No passwords — ever.**

### Hosting
- **Railway** — push-to-deploy from GitHub. Managed SQL Server + app container.
- **GitHub** — source control. `main` branch deploys to Railway automatically.

---

## 3. Project Structure

```
DwellScript/
├── Controllers/
│   ├── HomeController.cs          # Renders pages (View() only)
│   ├── PropertyController.cs      # Renders property pages
│   ├── AuthController.cs          # Magic link + Google OAuth callbacks
│   └── Api/
│       ├── GenerationApiController.cs    # POST /api/generation/generate
│       ├── PropertyApiController.cs      # CRUD /api/properties
│       ├── AuthApiController.cs          # POST /api/auth/magic-link
│       ├── BillingApiController.cs       # POST /api/billing/checkout
│       └── AnalyzerApiController.cs      # POST /api/analyzer/analyze
├── Services/
│   ├── GenerationService.cs       # Calls Claude API, parses XML response
│   ├── VacancyAnalyzerService.cs  # Pro-only, no quota cost
│   ├── UsageService.cs            # Tracks monthly generation quota
│   ├── SubscriptionService.cs     # Stripe tier logic
│   ├── PropertyService.cs         # Property CRUD business logic
│   ├── AuthService.cs             # Magic link creation/validation, Google linking
│   └── FairHousingFilter.cs       # Scans output for prohibited phrases
├── Models/
│   ├── Property.cs
│   ├── Generation.cs
│   ├── GenerationOutput.cs
│   ├── MagicLinkToken.cs
│   ├── ApplicationUser.cs         # Extends IdentityUser
│   └── PromptTemplate.cs
├── Data/
│   └── AppDbContext.cs
├── Views/
│   ├── Shared/
│   │   └── _Layout.cshtml         # Sidebar + topbar shell
│   ├── Property/
│   │   ├── Index.cshtml           # Dashboard — property list
│   │   ├── Create.cshtml          # 4-step wizard
│   │   ├── Edit.cshtml            # Same wizard, pre-filled
│   │   └── Detail.cshtml          # Generate / History / Analyze tabs
│   ├── Auth/
│   │   └── Login.cshtml
│   └── Billing/
│       └── Index.cshtml
├── wwwroot/
│   ├── css/
│   │   └── site.css               # All CSS custom properties (design tokens)
│   ├── js/
│   │   └── site.js                # Shared JS utilities
│   └── lib/                       # Bootstrap 5, jQuery, Toastr (via LibMan or npm)
├── appsettings.json
├── appsettings.Development.json   # Local secrets (never commit)
└── Program.cs
```

---

## 4. Database Schema

### Users (extends IdentityUser)
```csharp
public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
    public string? StripeCustomerId { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public SubscriptionTier Tier { get; set; } = SubscriptionTier.Free;
    public DateTime CreatedAt { get; set; }
    public ICollection<Property> Properties { get; set; }
}

public enum SubscriptionTier { Free, Starter, Pro }
```

### Properties
```csharp
public class Property
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public ApplicationUser User { get; set; }
    public string Address { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string Zip { get; set; }
    public string PropertyType { get; set; }
    public PropertyStatus Status { get; set; }
    public int Bedrooms { get; set; }
    public decimal Bathrooms { get; set; }
    public int? SquareFootage { get; set; }
    public decimal? MonthlyRent { get; set; }
    public string PetPolicy { get; set; }
    public string Parking { get; set; }
    public string AmenitiesJson { get; set; }    // JSON array stored as string
    public string PlatformsJson { get; set; }    // JSON array: ["ltr","str","social"]
    public string? Notes { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<Generation> Generations { get; set; }
}

public enum PropertyStatus { Active, Vacant, Archived }
```

### Generations
```csharp
public class Generation
{
    public int Id { get; set; }
    public int PropertyId { get; set; }
    public Property Property { get; set; }
    public string UserId { get; set; }
    public GenerationType Type { get; set; }   // Full, SectionRegen, AnalyzerApplied
    public string? RefinementInstruction { get; set; }
    public string? RegeneratedSection { get; set; }  // "LTR","STR","Social","Headlines"
    public string? LtrOutput { get; set; }
    public string? StrOutput { get; set; }
    public string? SocialOutput { get; set; }
    public string? HeadlinesJson { get; set; }  // JSON array of 3 strings
    public bool IsFlaggedForAnalyzer { get; set; }
    public decimal UsageUnitsConsumed { get; set; }  // 1.0 full, 0.25 section regen
    public DateTime CreatedAt { get; set; }
}

public enum GenerationType { Full, SectionRegen, AnalyzerApplied }
```

### MagicLinkTokens
```csharp
public class MagicLinkToken
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public ApplicationUser User { get; set; }
    public string TokenHash { get; set; }   // SHA-256 of the raw token
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### PromptTemplates
```csharp
public class PromptTemplate
{
    public int Id { get; set; }
    public string Key { get; set; }       // "FULL_GENERATION", "SECTION_REGEN", etc.
    public string PromptText { get; set; }
    public string? SystemPrompt { get; set; }
    public bool IsActive { get; set; }
    public int Version { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

---

## 5. Subscription Tiers & Quota Rules

| Feature | Free | Starter ($9/mo) | Pro ($19/mo) |
|---|---|---|---|
| Generations/month | 3 | Unlimited | Unlimited |
| Properties | 1 | 10 | Unlimited |
| Generation history | ✗ | ✓ | ✓ |
| Section refinements | ✗ | ✓ | ✓ |
| Vacancy Analyzer | ✗ | ✗ | ✓ |

**Annual pricing:** Starter $7/mo (billed $84/yr), Pro $15/mo (billed $180/yr).

**Usage units:** Full generation = 1.0 unit. Section regen or refinement = 0.25 units. Vacancy Analyzer = 0 units (no quota cost).

**Quota check:** Always check in the API controller BEFORE calling `GenerationService`. Return HTTP 402 if quota exceeded. Grace period of 3 days on failed payments before downgrading.

---

## 6. AI Generation Flow

### Single API Call → 4 Outputs
One call to Claude returns all four outputs as structured XML. Parse the XML server-side in `GenerationService`. Never call Claude from the frontend.

**Expected Claude response format:**
```xml
<outputs>
  <ltr>Long-term rental listing text here...</ltr>
  <str>Short-term rental listing text here...</str>
  <social>Social media post here...</social>
  <headlines>
    <headline>Headline variant 1</headline>
    <headline>Headline variant 2</headline>
    <headline>Headline variant 3</headline>
  </headlines>
</outputs>
```

### Section Regen
Separate lightweight call. Pass only the section key + refinement instruction. Counts as 0.25 usage units.

### Fair Housing Filter
Run `FairHousingFilter.ScanAsync(output)` on every output before returning to client. If violations found, flag the output but still return it with a warning flag — do not suppress the output silently.

### Prompt Templates
Retrieve active template by key from `PromptTemplates` table. Never hardcode prompts in C# code. This allows prompt iteration without redeployment.

---

## 7. Stripe Integration — Critical Rules

1. **Webhook is the source of truth.** Never grant plan access based on a Checkout redirect.
2. **Always verify webhook signature** using `StripeClient.ConstructEvent()`.
3. **Handle idempotency** — webhooks can fire multiple times.
4. **Events to handle:**

| Event | Action |
|---|---|
| `checkout.session.completed` | Create/update subscription, set `Tier` |
| `customer.subscription.updated` | Update `Tier` and `StripeSubscriptionId` |
| `customer.subscription.deleted` | Downgrade to Free |
| `invoice.payment_failed` | Start grace period, send dunning email via Resend |
| `invoice.payment_succeeded` | Clear grace period |

5. **Price ID → Tier mapping** lives in `appsettings.json`, not in code constants.
6. **Customer Portal** — use Stripe-hosted portal for all payment method updates and cancellations. Never build your own.

---

## 8. MVC Pattern — Strict Separation

```
Browser → MVC Controller (renders View) → Razor page loads
Browser → jQuery Ajax → Web API Controller → Service → DB/Claude
```

**MVC Controllers:** Only call `return View()` or `return RedirectToAction()`. Never return JSON. Never touch `DbContext`.

**API Controllers:** Never return `View()`. Always return `IActionResult` with JSON. Attribute routed: `[Route("api/[controller]")]`.

**Services:** All business logic. Injected via constructor. Return domain objects, not HTTP responses.

**Views/Razor:** HTML shell only. All dynamic data loaded via jQuery Ajax after page load. Minimal `@Model` usage — prefer API calls.

---

## 9. Auth Flow Details

### Magic Link
1. User enters email → POST `/api/auth/magic-link`
2. `AuthService` creates user if not exists, generates raw token (GUID), hashes it (SHA-256), saves to `MagicLinkTokens` with 15-min expiry
3. Resend sends email with link: `https://dwellscript.com/auth/verify?token={rawToken}&email={email}`
4. User clicks link → GET `/auth/verify` → `AuthService.ValidateTokenAsync()` → signs in via `SignInManager`
5. Token marked as used (`UsedAt = DateTime.UtcNow`)

### Google OAuth
1. `challenge = new AuthenticationProperties { RedirectUri = "/auth/google-callback" }`
2. On callback: check if email already exists in Identity
3. If exists → link Google login to existing account
4. If new → create `ApplicationUser`, link Google login

### Same-Email Account Linking
If user signs in with Google using an email that already has a magic-link account — link the external login to the existing account. Never create a duplicate user.

---

## 10. API Endpoints Reference

```
POST   /api/auth/magic-link              Send magic link email
GET    /auth/verify                      Validate token, sign in (MVC)
GET    /auth/google                      Initiate Google OAuth (MVC)
GET    /auth/google-callback             Handle Google callback (MVC)

GET    /api/properties                   List user's properties
POST   /api/properties                   Create property
GET    /api/properties/{id}              Get single property
PUT    /api/properties/{id}              Update property
DELETE /api/properties/{id}              Archive property (soft delete)

POST   /api/generation/generate          Full generation (all 4 outputs)
POST   /api/generation/regen-section     Section regen + optional refinement
GET    /api/generation/history/{propertyId}  Generation history for property
DELETE /api/generation/{id}              Delete a generation

POST   /api/analyzer/analyze             Run vacancy analysis (Pro only)

POST   /api/billing/checkout             Create Stripe Checkout Session
POST   /api/billing/webhook              Stripe webhook receiver
GET    /api/billing/portal               Create Stripe Customer Portal session
GET    /api/billing/status               Current plan + quota for user
```

---

## 11. Frontend Conventions

### jQuery Ajax pattern (use this everywhere)
```javascript
$.ajax({
    url: '/api/properties',
    method: 'POST',
    contentType: 'application/json',
    data: JSON.stringify(payload),
    headers: { 'RequestVerificationToken': $('[name="__RequestVerificationToken"]').val() },
    success: function(result) {
        toastr.success('Property saved!', 'Success');
    },
    error: function(xhr) {
        const msg = xhr.responseJSON?.message || 'Something went wrong.';
        toastr.error(msg, 'Error');
    }
});
```

### Toastr — always use these options
```javascript
toastr.options = {
    positionClass: "toast-top-right",
    timeOut: 4000,
    closeButton: true,
    progressBar: true,
    newestOnTop: true
};
```

### Anti-forgery tokens
All POST/PUT/DELETE Ajax calls must include `RequestVerificationToken`. Add `@Html.AntiForgeryToken()` to every Razor form/page. Add `[ValidateAntiForgeryToken]` to all non-webhook API endpoints.

### CSS custom properties
All design tokens live in `site.css` under `:root {}`. Never use hardcoded hex colors in component CSS — always reference variables:
```css
/* Correct */
color: var(--text-primary);
background: var(--ds-navy-800);

/* Wrong */
color: #111827;
```

---

## 12. Design System Tokens (Key Values)

```css
:root {
  /* Navy (primary brand) */
  --ds-navy-900: #0F1F3D;
  --ds-navy-800: #1A3A5C;
  --ds-navy-600: #2563EB;
  --ds-navy-50:  #EFF6FF;

  /* Warm orange (accent / CTA) */
  --ds-warm-600: #EA580C;
  --ds-warm-500: #F97316;
  --ds-warm-400: #FB923C;

  /* Semantic */
  --ds-success: #16A34A;
  --ds-warning: #CA8A04;
  --ds-error:   #DC2626;

  /* Surfaces */
  --bg-page:     #F8F7F4;
  --bg-surface:  #FFFFFF;
  --bg-surface-2:#F3F4F6;
  --bg-sidebar:  #0F1F3D;

  /* Text */
  --text-primary:   #111827;
  --text-secondary: #4B5563;
  --text-tertiary:  #9CA3AF;

  /* Border radius */
  --radius-sm:   6px;
  --radius-md:   10px;
  --radius-lg:   14px;
  --radius-xl:   20px;
  --radius-2xl:  28px;
  --radius-full: 9999px;

  /* Layout */
  --sidebar-width:  240px;
  --topbar-height:  64px;
}
```

**Dark mode:** Toggled via `[data-theme="dark"]` on `<html>`. Auto-detects `prefers-color-scheme`. Key dark overrides:
```css
[data-theme="dark"] {
  --bg-page:    #0A0F1E;
  --bg-surface: #111827;
  --border:     #1F2937;
  --text-primary: #F9FAFB;
}
```

---

## 13. Page → File Reference

All 8 screens have been fully designed as self-contained HTML mockups. These are your visual reference and interaction spec — match them exactly:

| Screen | Mockup File | Route |
|---|---|---|
| Design System | `dwellscript-design-system.html` | (reference only) |
| Login / Magic Link | `dwellscript-login.html` | `/auth/login` |
| Dashboard | `dwellscript-dashboard.html` | `/properties` |
| Generate (property detail) | `dwellscript-generate.html` | `/properties/{id}/generate` |
| Generation History | `dwellscript-history.html` | `/properties/{id}/history` |
| Vacancy Analyzer | `dwellscript-analyzer.html` | `/properties/{id}/analyze` |
| Billing | `dwellscript-billing.html` | `/billing` |
| Property Wizard | `dwellscript-property-wizard.html` | `/properties/create` and `/properties/{id}/edit` |

The mockups contain all CSS, interactions, and component states. Extract CSS into `site.css` and component-specific files. Extract JS into `site.js` or page-specific script files.

---

## 14. Environment Variables Required

```
# Anthropic
ANTHROPIC_API_KEY=sk-ant-...

# Stripe
STRIPE_SECRET_KEY=sk_live_...
STRIPE_WEBHOOK_SECRET=whsec_...
STRIPE_PRICE_STARTER_MONTHLY=price_...
STRIPE_PRICE_STARTER_ANNUAL=price_...
STRIPE_PRICE_PRO_MONTHLY=price_...
STRIPE_PRICE_PRO_ANNUAL=price_...

# Resend
RESEND_API_KEY=re_...
RESEND_FROM_EMAIL=noreply@dwellscript.com

# Google OAuth
GOOGLE_CLIENT_ID=...
GOOGLE_CLIENT_SECRET=...

# App
APP_BASE_URL=https://dwellscript.com
CONNECTION_STRING=Server=...;Database=DwellScript;...
```

---

## 15. Sprint Plan (For Reference)

| Sprint | Focus | Gate |
|---|---|---|
| 1 (Wks 1–2) | Foundation — auth, Railway deploy pipeline | Login works on Railway URL |
| 2 (Wks 3–4) | Property CRUD — wizard, dashboard, list | Create/edit/archive/duplicate works |
| 3 (Wks 5–7) | AI generation — core engine, quota, history | Full generation flow end-to-end |
| 4 (Wks 8–9) | Billing — Stripe Checkout + webhooks + portal | Free→paid upgrade flow works |
| 5 (Wks 9–10) | Vacancy Analyzer — Pro feature | Pro user can diagnose + apply |
| 6 (Wks 11–12) | Polish, Fair Housing filter, launch prep | All BRD v1.0 requirements met |

---

## 16. Key Decisions — Do Not Revisit

- **No passwords.** Magic link + Google only.
- **No raw SQL.** EF Core only.
- **No frontend framework.** Bootstrap + jQuery only.
- **No server-side logic in Views.** API calls only.
- **Webhook is billing source of truth.** Never trust redirect.
- **Prompts live in DB.** `PromptTemplates` table, not C# constants.
- **One Claude call per full generation.** Four outputs from one request, parsed from XML.
- **Section regen = 0.25 units.** Enforced server-side in `UsageService`.
- **Fair Housing filter runs on every output** before it reaches the client.
- **Soft deletes only.** Properties and generations are archived, never hard-deleted.

---

## 17. Coding Instructions for Claude

When writing code for this project, always:

1. **Follow the MVC separation strictly** — controllers render, APIs return JSON, services do work.
2. **Use async/await everywhere** — `async Task<IActionResult>` on all controller actions.
3. **Return consistent API responses** — always wrap in `{ success: bool, data: ..., message: string }`.
4. **Validate server-side** — never trust client input. Use FluentValidation or DataAnnotations.
5. **Check subscription tier before Pro features** — `SubscriptionService.HasAccess(user, Feature.VacancyAnalyzer)`.
6. **Check quota before generation** — `UsageService.HasQuotaAsync(userId)` returns bool.
7. **Log errors** — use `ILogger<T>` injected via constructor. Never `Console.WriteLine`.
8. **Never expose the Claude API key** — it lives in env vars, used only in `GenerationService`.
9. **Use `[Authorize]` on all pages and API endpoints** except Login and webhook receiver.
10. **Webhook endpoint must be exempt from CSRF** — add `[IgnoreAntiforgeryToken]` to the Stripe webhook action.
11. **Match the mockup designs exactly** — spacing, colors, radius, component states, and dark mode are all specified in the HTML mockups.
12. **Write migrations** after every model change — `dotnet ef migrations add {Name}`.

---

*DwellScript — Built by BAJANIHAVANUR LLC, Chesapeake VA.*
*Context version: 1.0 — March 2026.*
