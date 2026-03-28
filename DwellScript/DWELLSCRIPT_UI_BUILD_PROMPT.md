# DwellScript — UI Implementation Prompt for Claude Code
> Use this prompt in VS Code Claude plugin when building each page's Razor view and CSS/JS.
> Reference the corresponding HTML mockup file for exact visual spec.

---

## HOW TO USE THIS PROMPT

1. Open VS Code with your DwellScript project
2. Open the Claude plugin
3. Paste the **Global Design Rules** section at the top of every conversation
4. Then paste the **specific page section** you are currently building
5. Attach the corresponding HTML mockup file as context
6. Tell Claude: *"Build this Razor view matching the mockup exactly"*

---

---

# PART 1 — GLOBAL DESIGN RULES
## Paste this in every session before any page-specific prompt

---

## Design System

You are building UI for **DwellScript** — an AI-powered rental listing SaaS. Every page must match the approved mockup files exactly. Do not invent new components, colors, or layouts.

### Font
- **Inter** only. Weights: 300, 400, 500, 600, 700, 800.
- Load via: `<link href="https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700;800&display=swap" rel="stylesheet">`
- Apply globally: `font-family: 'Inter', -apple-system, sans-serif;`

### Color Palette — CSS Variables (define in site.css :root)

```css
:root {
  /* Navy — primary brand */
  --ds-navy-900: #0F1F3D;
  --ds-navy-800: #1A3A5C;
  --ds-navy-700: #1E4D7B;
  --ds-navy-600: #2563EB;
  --ds-navy-500: #3B82F6;
  --ds-navy-400: #60A5FA;
  --ds-navy-100: #DBEAFE;
  --ds-navy-50:  #EFF6FF;

  /* Warm Orange — accent / CTA */
  --ds-warm-700: #C2410C;
  --ds-warm-600: #EA580C;
  --ds-warm-500: #F97316;
  --ds-warm-400: #FB923C;
  --ds-warm-100: #FFEDD5;
  --ds-warm-50:  #FFF7ED;

  /* Neutrals */
  --ds-neutral-900: #111827;
  --ds-neutral-700: #374151;
  --ds-neutral-600: #4B5563;
  --ds-neutral-500: #6B7280;
  --ds-neutral-400: #9CA3AF;
  --ds-neutral-300: #D1D5DB;
  --ds-neutral-200: #E5E7EB;
  --ds-neutral-100: #F3F4F6;
  --ds-neutral-50:  #F9FAFB;

  /* Semantic */
  --ds-success-bg: #DCFCE7;
  --ds-success:    #16A34A;
  --ds-warning-bg: #FEF9C3;
  --ds-warning:    #CA8A04;
  --ds-error-bg:   #FEE2E2;
  --ds-error:      #DC2626;

  /* Page surfaces */
  --bg-page:      #F8F7F4;
  --bg-surface:   #FFFFFF;
  --bg-surface-2: #F3F4F6;
  --bg-sidebar:   #0F1F3D;
  --border:       #E5E7EB;
  --border-strong:#D1D5DB;

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

  /* Shadows */
  --shadow-xs: 0 1px 2px rgba(0,0,0,.05);
  --shadow-sm: 0 1px 3px rgba(0,0,0,.08), 0 1px 2px rgba(0,0,0,.04);
  --shadow-md: 0 4px 6px -1px rgba(0,0,0,.08), 0 2px 4px -2px rgba(0,0,0,.04);
  --shadow-lg: 0 10px 15px -3px rgba(0,0,0,.1), 0 4px 6px -4px rgba(0,0,0,.05);
  --shadow-xl: 0 20px 25px -5px rgba(0,0,0,.12), 0 8px 10px -6px rgba(0,0,0,.05);
  --shadow-brand: 0 4px 14px rgba(37,99,235,.25);
  --shadow-warm:  0 4px 14px rgba(234,88,12,.22);

  /* Layout */
  --sidebar-width:  240px;
  --topbar-height:  64px;

  /* Transitions */
  --transition-base:   all .2s ease;
  --transition-spring: all .25s cubic-bezier(.34,1.56,.64,1);
}

/* Dark mode overrides */
[data-theme="dark"] {
  --bg-page:      #0A0F1E;
  --bg-surface:   #111827;
  --bg-surface-2: #1F2937;
  --bg-sidebar:   #060D1A;
  --border:       #1F2937;
  --border-strong:#374151;
  --text-primary: #F9FAFB;
  --text-secondary:#9CA3AF;
  --text-tertiary: #6B7280;
  --shadow-sm: 0 1px 3px rgba(0,0,0,.3), 0 1px 2px rgba(0,0,0,.2);
  --shadow-md: 0 4px 6px -1px rgba(0,0,0,.35), 0 2px 4px -2px rgba(0,0,0,.2);
  --shadow-lg: 0 10px 15px -3px rgba(0,0,0,.45), 0 4px 6px -4px rgba(0,0,0,.25);
}
```

### Icons
- **Bootstrap Icons** only. CDN: `https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css`
- Usage: `<i class="bi bi-{icon-name}"></i>`

### Notifications
- **Toastr.js** for all user feedback. Always configure:
```javascript
toastr.options = {
  positionClass: "toast-top-right",
  timeOut: 4000,
  closeButton: true,
  progressBar: true,
  newestOnTop: true
};
```
- `toastr.success()` — saved, generated, applied
- `toastr.error()` — failed, validation error
- `toastr.info()` — navigation, loading state
- `toastr.warning()` — quota warning, Pro-gated feature

### Layout Shell
Every authenticated page uses this two-part shell:
- **Fixed left sidebar** — 240px wide, dark navy (`#0F1F3D`), always visible
- **Main area** — fills remaining width, has sticky topbar (64px) + scrollable content

The sidebar contains: logo, nav items with active state indicator (3px warm orange left border), usage quota bar, Upgrade button (warm orange gradient), user avatar + name.

### Dark Mode
- Toggled by setting `data-theme="dark"` on `<html>`
- Auto-detect system preference on page load:
```javascript
if (window.matchMedia('(prefers-color-scheme: dark)').matches) {
  document.documentElement.setAttribute('data-theme', 'dark');
}
```
- Theme toggle button in topbar — swap icon between `bi-moon-stars-fill` and `bi-sun-fill`

### jQuery Ajax Pattern
All API calls use this exact pattern:
```javascript
$.ajax({
  url: '/api/endpoint',
  method: 'POST',
  contentType: 'application/json',
  data: JSON.stringify(payload),
  headers: {
    'RequestVerificationToken': $('[name="__RequestVerificationToken"]').val()
  },
  success: function(result) {
    toastr.success('Action completed', 'Success');
  },
  error: function(xhr) {
    const msg = xhr.responseJSON?.message || 'Something went wrong.';
    toastr.error(msg, 'Error');
  }
});
```

### Component Rules
- **Cards:** `border-radius: var(--radius-xl)`, `border: 1px solid var(--border)`, `box-shadow: var(--shadow-sm)`
- **Buttons:** Primary = navy 800 bg + white text + brand shadow. Accent = warm 600 bg. Ghost = transparent + border.
- **Inputs:** `border: 1.5px solid var(--border-strong)`, focus = navy 600 border + 3px rgba blue glow
- **Status pills:** Rounded full, colored background + matching text. Active = green, Vacant = amber, Archived = gray.
- **Hover transitions:** All interactive elements use `var(--transition-base)`. Buttons that elevate use `transform: translateY(-1px)` on hover.
- **Skeleton loading:** Shimmer animation using `background: linear-gradient(90deg, var(--ds-neutral-200) 25%, var(--ds-neutral-100) 50%, var(--ds-neutral-200) 75%)` with `background-size: 200%` and `animation: shimmer 1.5s infinite`.

---

---

# PART 2 — PAGE-SPECIFIC PROMPTS
## Paste only the section for the page you are currently building

---

## PAGE 1 — LOGIN / MAGIC LINK
**Mockup file:** `dwellscript-login.html`
**Route:** `/auth/login`
**Razor file:** `Views/Auth/Login.cshtml`

### Layout
Two-panel full-viewport layout. No sidebar. No topbar.
- **Left panel (52% width):** Dark navy background (`#0F1F3D`) with radial gradient mesh overlay. Contains logo, hero headline, feature list, floating animated preview card, social proof strip.
- **Right panel (48% width):** Light page background. Contains the auth form card. Theme toggle button fixed top-right.

### Left Panel Features
- **Logo:** Orange gradient square mark with "D" + "DwellScript" wordmark (orange accent on "Script")
- **Hero eyebrow pill:** Semi-transparent white border pill — "AI-Powered Rental Listings"
- **Hero headline:** 38px, weight 800, white. Line breaks: "Fill vacancies / faster with smarter / listings." — "smarter" in warm orange
- **Feature list:** 4 items with icon boxes (semi-transparent white bg), each with bold feature name + description in muted white
- **Floating preview card:** Bottom center of panel, `backdrop-filter: blur(12px)`, white 7% bg, animated float loop (`translateY` keyframe, 3s infinite alternate). Shows a pulsing orange dot.
- **Social proof:** Row of 4 overlapping colored avatars + "Trusted by 500+ landlords" text

### Right Panel — Auth Form
- **Tab switcher:** Two tabs — "Magic Link" (envelope icon) and "Google" (Google SVG logo). Active tab has white bg + shadow inside a gray pill container.
- **Magic Link tab:** Email input with envelope icon prefix. Animated "Send Magic Link" button (navy gradient, send icon). On submit — show loading state (hourglass icon, "Sending…"), then transition to success state.
- **Success state:** Replaces the form. Animated pop-in green checkmark circle. "Check your inbox" title. Shows the email address submitted. "Use a different email" + "Resend link" text links.
- **Google tab:** Large Google button with official Google SVG colors. "Or sign in with Email" secondary button below.
- **Terms line:** Centered small text below form — "No password required — ever."

### Interactivity
- Email validation on submit — show red border + error message if invalid format
- Send button loading state — disabled + hourglass icon + "Sending…" text during API call
- Tab switching — smooth content swap, active tab gets white card bg
- Success state animation — checkmark circle scales in from 0 with spring easing
- Google button → `POST /api/auth/google` redirect
- Magic link → `POST /api/auth/magic-link` with `{ email }` payload
- Theme toggle → saves preference, swaps icon

### API Calls
```javascript
// Magic link
POST /api/auth/magic-link
Body: { email: "user@example.com" }
Success: show success state
Error 400: show validation error on input

// Google OAuth
GET /auth/google
(redirect, not Ajax)
```

---

## PAGE 2 — DASHBOARD (Property List)
**Mockup file:** `dwellscript-dashboard.html`
**Route:** `/properties`
**Razor file:** `Views/Property/Index.cshtml`

### Layout
Full sidebar + topbar shell. Content area has stats bar, toolbar, filter tabs, and property grid/list.

### Stats Bar
5 equal-width cards in a CSS grid row. Each card has:
- Colored icon box (top-left), large number value, label, delta note
- Card 1: Total Properties (navy icon)
- Card 2: Active / Vacant split (green icon, warn note if vacant > 0)
- Card 3: Generations This Month (purple icon)
- Card 4: Monthly Quota — shows mini gradient progress bar + "X remaining" in warning color if low
- Card 5: Subscription Tier — shows colored pill badge + upgrade link

### Toolbar
Two rows:
- Left: Search input (live filter, magnifier icon prefix) + Type dropdown + Status dropdown
- Right: Grid/List view toggle (two icon buttons, active = navy fill) + "Add Property" button (navy bg, plus icon)

### Filter Tabs
Four tabs: All / Active / Vacant / Archived — each with a count badge. Active tab has navy underline border. Clicking a tab syncs the status dropdown and re-filters cards.

### Grid View (default)
3-column CSS grid. Each property card:
- **Top accent strip** (5px height): navy gradient for Active, orange gradient for Vacant, gray for Archived
- **Card body:** Property type icon box, status pill (top-right), address (bold), city (muted), specs row (beds/baths/sqft with icons), platform tags (LTR/STR/Social colored badges)
- **Card footer:** Generation count (left) + action buttons (right, visible on hover only): "Edit" (ghost btn) + "Open" (navy btn)
- **Hover state:** `translateY(-3px)`, larger shadow, stronger border
- **Add New card:** Dashed border, centered plus icon box. Hover = navy tint bg + navy border.

### List View
Table with columns: Property (icon + address + city/type), Type, Generations, Status, Actions. Row hover = light navy tint. Action buttons (Edit + arrow) appear on row hover.

### Modals
**Create / Edit Property Modal:**
- Centered overlay with backdrop blur
- Sections: Basic Info, Property Details, Listing Platforms (pill toggles), Amenities (checkbox pills), Pet/Parking selects, Notes textarea
- Footer: Archive button (danger ghost, edit mode only, left-aligned) + Cancel + Save
- Form validation on save — required fields highlighted in red

### Interactivity
- Live search filters both grid and list views simultaneously
- Grid ↔ List toggle persists state, smooth transition
- Filter tabs sync with status dropdown
- Card click → navigate to `/properties/{id}/generate`
- Edit button (stops propagation) → opens Edit modal pre-filled
- Add Property card / button → opens Create modal (blank)
- Archive → confirm dialog → Toastr warning + card fades out
- Empty state shown when no cards match filters

### API Calls
```javascript
GET    /api/properties              // Load all properties on page init
POST   /api/properties              // Create new property
PUT    /api/properties/{id}         // Save edits
DELETE /api/properties/{id}         // Archive (soft delete)
```

---

## PAGE 3 — GENERATE TAB (Property Detail)
**Mockup file:** `dwellscript-generate.html`
**Route:** `/properties/{id}/generate`
**Razor file:** `Views/Property/Detail.cshtml` (tab = generate)

### Layout
Sidebar + topbar + page tabs. Content splits into:
- **Left profile panel** (300px fixed): Property details sidebar
- **Right generate panel** (flex 1): Generate toolbar + output grid

### Page Tabs
Four tabs below topbar: Generate (active) | History (with count badge) | Analyze (Pro badge) | Edit Profile. Tab switching via query string `?tab=`.

### Left Profile Panel
Scrollable. Contains:
- Property icon box, address (bold 16px), city, status pill
- Divider
- "Property Details" section label
- Spec rows: Type, Bedrooms, Bathrooms, Sq Footage, Rent, Parking, Pets — each as key/value pair with icon
- Divider
- "Amenities" section — small chips with icon + label
- Divider
- "Listing Platforms" section — colored platform tags
- "Edit Profile" ghost button at bottom

**On load from wizard:** Read `sessionStorage.getItem('ds_new_property')`, populate all fields, delete from sessionStorage, show success Toastr.

### Generate Toolbar
Sticky bar between page tabs and output area:
- Left: "AI Listing Generator" label + subtitle
- Middle: Quota warning pill (amber) if 1 generation remaining — `bi-exclamation-triangle-fill` icon
- Right: "Generate All Outputs" button — navy gradient, stars icon, spring hover

**Button states:**
- Default: "Generate All Outputs" with `bi-stars`
- Loading: disabled + "Generating…" + `bi-hourglass-split` icon
- Done: turns green + "Generated" + `bi-check-circle-fill`

### Pre-Generate State
Shown before first generation. Centered in output area:
- Animated breathing icon box (navy tint, `bi-stars`, scale pulse 3s infinite)
- Title + body copy
- 4 platform feature chips (Zillow / Airbnb / Facebook / Headlines)
- Fair Housing note with green shield icon

### Output Grid — 2×2 Layout
CSS grid, 2 columns, 2 rows. Shown after generation. Each of the 4 cards:

**Card structure:**
- **Header:** Platform icon box + title + platform name | Action buttons (right-aligned)
- **Body:** Output text (pre-wrap, 13.5px, 1.75 line-height) OR skeleton shimmer during load
- **Refine drawer:** Slides open below body (max-height transition). Contains text input + send button + "0.25 usage units" hint.
- **Footer:** Word count (left) | "Refine" ghost btn + "Copy" navy btn (right)
- **Regen overlay:** Absolute positioned, semi-transparent white/dark, centered spinner — shown during section regen

**Card variants:**
- LTR: navy icon (`bi-house-door-fill`)
- STR: warm orange icon (`bi-sun-fill`)
- Social: green icon (`bi-chat-dots-fill`)
- Headlines: purple icon (`bi-fonts`) — body shows 3 numbered items each with individual copy button, not prose text

**Header action buttons (4 per card):**
- `bi-arrow-clockwise` — Regenerate section (0.25 units)
- `bi-clipboard` — Copy to clipboard
- `bi-download` — Download as .txt
- `bi-flag` — Flag for Vacancy Analyzer (Pro gate)

### Copied Tooltip
Fixed bottom-center pill. `opacity: 0` by default, transitions to `opacity: 1` for 2.2 seconds then fades. Shows "✓ {section} copied to clipboard".

### Interactivity
- Generate button → skeleton shimmer appears in all 4 cards → cards populate sequentially with stagger delays
- Regen button → spinner overlay on that card → repopulates after 1.6s
- Refine drawer → `max-height` slide transition on toggle → submit on Enter or send button → spinner overlay → repopulates
- Copy → writes to clipboard → shows copied tooltip
- Download → creates `.txt` Blob → triggers browser download
- Flag → Toastr warning if Free/Starter, calls analyzer if Pro
- Pro feature gate: flag button shows `toastr.warning` with upgrade message for non-Pro users

### API Calls
```javascript
POST /api/generation/generate
Body: { propertyId, platforms: ["ltr","str","social","headlines"] }
Response: { ltr, str, social, headlines: ["h1","h2","h3"] }

POST /api/generation/regen-section
Body: { generationId, section: "LTR", instruction: "make it more formal" }
Response: { output }
```

---

## PAGE 4 — GENERATION HISTORY TAB
**Mockup file:** `dwellscript-history.html`
**Route:** `/properties/{id}/history`
**Razor file:** `Views/Property/Detail.cshtml` (tab = history)

### Layout
Sidebar + topbar + page tabs. Content has stats strip + toolbar + history table. Slide-out drawer on the right (480px) for viewing full output.

### Stats Strip
4 chips in a row: Total Generations | This Month | Section Regens | Sections Copied. Each with colored icon box + large number + label.

### Toolbar
- Search input (filters table rows live)
- Platform filter dropdown (All / LTR / STR / Social / Full)
- Property filter dropdown (all user's properties)
- Sort buttons: "Newest first" | "Oldest" | "By property" — toggle active state

### History Table
Table with columns: Generation | Platforms | Word count | Date | Actions

**Row types (each has distinct left border + icon):**
- **Full Generation:** Navy stars icon, navy left border. Shows all 4 platform tags.
- **Section Regen:** Orange refresh icon, orange left border. Shows refinement instruction as subtitle.
- **Flagged:** Same as Full but with purple `bi-flag-fill` in date column + "Flagged for analysis" text in purple.
- **Analyzer Applied:** Purple graph icon, purple left border. Shows "Analyzer" badge tag.

**Row behavior:**
- Hover → light navy tint background
- Hover → action buttons appear (opacity 0 → 1): View (eye) | Re-generate (refresh) | Flag (flag) | Delete (trash)
- Active row (drawer open) → navy left border + navy tint bg
- Click anywhere on row → opens slide-out drawer

### By Property View
When "By property" sort selected — groups rows under property headers. Each header: property icon + address + city + dividing line + generation count.

### Slide-Out Drawer (480px)
Slides in from right, pushes main content left (`margin-right: 480px` on main area).

**Drawer sections:**
- **Topbar:** "← Back to history" button | title | ✕ close button
- **Meta strip:** Property name with icon | platform tags | date (right-aligned)
- **Actions bar:** "Copy All" (navy) | "Re-generate" (ghost) | "Flag for Analyzer" (purple ghost, Pro-gated) | "Delete" (red)
- **Scrollable body:** One section per platform generated:
  - Section header: icon box + label + platform name + individual "Copy" button
  - LTR/STR/Social: gray bg text box + word count below
  - Headlines: 3 numbered items, each with individual copy button

### Interactivity
- Sort buttons → toggle active state → re-sort table rows (newest/oldest = DOM reorder, grouped = show grouped view div)
- Search → hides non-matching rows, shows empty state if all hidden
- Row click → open drawer, highlight row, push main area right
- Drawer close (back button, ✕, overlay click) → slide out, un-highlight row, restore main area
- Delete → `confirm()` dialog → row fades out + `toastr.success`
- Copy individual section → clipboard + copied tooltip
- Copy All → joins all sections → clipboard + tooltip

### API Calls
```javascript
GET    /api/generation/history/{propertyId}   // Load history on tab open
DELETE /api/generation/{id}                   // Delete a generation
POST   /api/generation/regen-section          // Re-generate from history entry
```

---

## PAGE 5 — VACANCY ANALYZER
**Mockup file:** `dwellscript-analyzer.html`
**Route:** `/properties/{id}/analyze`
**Razor file:** `Views/Property/Detail.cshtml` (tab = analyze)
**Access:** Pro tier only. Show upgrade prompt for Free/Starter users.

### Layout
Sidebar + topbar + page tabs. Two states: Pre-analyze form and Results view.

### Pre-Analyze State
Centered full-height. Contains:
- Animated breathing icon (purple tint, `bi-graph-down-arrow`, scale pulse 3s)
- Title + description copy
- **Context form card:** Platform dropdown, Days Live + Inquiries (2-column row), Monthly Rent input
- **"Run Diagnosis" button:** Purple gradient, full width, hover elevates

**Button states:**
- Default: "Run Diagnosis" + graph icon
- Loading: disabled + "Analyzing listing…" + hourglass icon

### Results Header
Replaces pre-analyze state. Contains:
- Property name + platform as title
- Meta strip: days live, inquiries, rent, performance verdict (red warning if below expected)
- **Score row** (4 cards):
  - Card 1: Animated SVG ring gauge (50/100) with "Needs Work" label — ring stroke animates from full to correct offset on load
  - Card 2: High-impact issues count (red icon + red number)
  - Card 3: Medium issues count (amber)
  - Card 4: Estimated improvement % (green, `bi-graph-up-arrow`)

### Two-Panel Body
- **Left panel (42%):** Original listing with color-coded inline highlights. Clicking any highlighted span jumps to + expands the corresponding weakness in the right panel. Highlight colors: red underline = High, amber underline = Medium, purple underline = Low.
- **Right panel (flex 1):** Three diagnosis tabs.

### Right Panel — Diagnosis Tabs

**Weaknesses tab (default):**
5 expandable accordion items. Each weakness card has:
- Left colored border (red/amber/purple by severity)
- Header (click to expand/collapse): numbered circle + title + subtitle + severity badge + impact badge + chevron
- Body (hidden until expanded):
  - Diagnosis text paragraph
  - **Side-by-side diff view:** Left column (red tint) = current text with `<del>` styling. Right column (green tint) = improved text with `<ins>` styling. Column headers "Current" (red) / "Improved" (green).
  - **Impact bar:** Gradient green fill, label showing "Estimated lead impact" + % on right
  - **Action row:** "Apply This Fix" (purple btn) + "Copy" (ghost btn) + "Applied & saved" success note (hidden, shown after apply)

**Rewrites tab:**
2 full rewrite cards. Each has:
- Header: title + platform + estimated impact (green)
- Body: full rewrite text in pre-wrap div
- Footer: "Apply This Rewrite" (purple) + "Copy" (ghost) + "Applied & saved" note

**Fix Order tab:**
Numbered priority list. Each item: rank circle (colored by priority) + title + description + "Fix now" button (purple ghost → jumps to that weakness in Weaknesses tab). Bottom CTA button to jump to Rewrites tab.

### Applied Banner
Slides down from below page tabs after any fix/rewrite applied:
- Green background, checkmark icon, confirmation text + "View in Generate" navy button

### Interactivity
- Inline highlights → click → switch to Weaknesses tab + scroll to + expand that weakness
- Accordion expand/collapse → max-height + chevron rotation transition
- Apply Fix → button turns green "Applied", note appears, banner slides in
- Apply Rewrite → same behavior
- "Fix now" in priority tab → switches to Weaknesses tab + expands correct item
- "View Full Rewrites" → switches to Rewrites tab

### API Calls
```javascript
POST /api/analyzer/analyze
Body: { propertyId, generationId, platform, daysLive, inquiries, monthlyRent }
Response: { score, issues: [...], rewrites: [...] }
```

---

## PAGE 6 — BILLING
**Mockup file:** `dwellscript-billing.html`
**Route:** `/billing`
**Razor file:** `Views/Billing/Index.cshtml`

### Layout
Sidebar + topbar (no page tabs). Scrollable single-column content, max-width 1000px.

### Current Plan Banner
Full-width card. Left: plan icon box + plan name (bold 18px) + activation date. Middle divider. Two usage bars side-by-side: Generations (with reset date, danger gradient if >66%) and Properties (navy gradient). Right divider (hidden on mobile).

### Billing Cycle Toggle
Centered row: "Monthly" label | animated pill toggle | "Annual" label | green "Save 20%" badge.
Toggle animation: white circle slides right (`translateX(24px)`) when annual selected.
On toggle: update all three price values live (`$9→$7`, `$19→$15`), show/hide "Billed as $X/year" notes.

### Pricing Cards — 3-Column Grid

**Free card (current):**
- Gray top strip
- "Current" badge (top-right)
- Gray icon, $0 price
- Feature list: 3 generations/mo, 1 property — green checks for included, gray X for excluded
- Grayed out "Your current plan" button (disabled)

**Starter card:**
- Orange top strip
- Warm orange upgrade button with hover elevation + warm shadow
- Features: unlimited generations, 10 properties, history, refinements — no Vacancy Analyzer

**Pro card (featured):**
- Navy top strip
- **Navy border** + brand shadow glow
- "Most Popular" navy badge (top-right)
- Navy upgrade button with hover elevation + brand shadow
- All features including Vacancy Analyzer (highlighted in bold)
- "Secured by Stripe · Cancel anytime" note under button

### Payment Method Card
Row layout: Visa card icon (navy bg SVG) + card number (••••4242) + expiry | "Default" green badge | "Update" + "Add card" ghost buttons → redirect to Stripe Customer Portal.

### Invoice Table
Columns: Description (with billing period subtitle) | Amount | Date | Status | Download.
Status pills: green "Paid" / amber "Pending" / red "Failed".
Download button (download icon) on each row.
"Load older invoices" text button at bottom.

### Danger Zone
Separate card with red border. Red icon box + "Cancel Subscription" title + explanation copy + red ghost cancel button.

**Cancel modal (on button click):**
Centered overlay with blur. Shows plan name, access end date, warning box listing what's lost. "Keep my plan" (ghost) + "Yes, cancel" (red) buttons.

### Interactivity
- Cycle toggle → live price updates + annual note show/hide
- Upgrade buttons → `POST /api/billing/checkout` → redirect to Stripe Checkout
- Update/Add card → `GET /api/billing/portal` → redirect to Stripe Customer Portal
- Invoice download → `GET /api/billing/invoice/{id}`
- Cancel button → opens modal → confirm → `DELETE /api/billing/subscription` → Toastr warning

### API Calls
```javascript
GET    /api/billing/status           // Load current plan + usage
POST   /api/billing/checkout         // Body: { plan, cycle } → returns { url }
GET    /api/billing/portal           // Returns { url } → redirect
DELETE /api/billing/subscription     // Cancel subscription
GET    /api/billing/invoices         // Returns invoice list
```

---

## PAGE 7 — PROPERTY WIZARD (Create / Edit)
**Mockup file:** `dwellscript-property-wizard.html`
**Route:** `/properties/create` and `/properties/{id}/edit`
**Razor file:** `Views/Property/Create.cshtml` and `Views/Property/Edit.cshtml`

### Layout
Sidebar + topbar (breadcrumb + Discard button). Content splits:
- **Left progress rail** (260px): Step list + AI tip box
- **Right wizard body**: Step content + sticky footer nav

### Left Progress Rail
4 step items connected by a vertical line. Each step item:
- **Circle:** 32px, shows step number. Active = navy filled. Done = navy outline + checkmark icon. Future = gray outline.
- **Connector line:** Gray by default, navy when step is done
- **Label + description:** Active step = full opacity. Others = muted.
- **AI tip box** at bottom of rail: navy tint bg, `bi-stars` icon, tip text about notes improving output quality.

### Step 1 — Basic Info
- Street address input with `bi-geo-alt` prefix icon
- City + State/ZIP in a 2-column row
- Property type dropdown
- **Status selector:** 3 large radio cards in a grid — Active (green icon), Vacant (amber icon), Archived (gray icon). Selected card gets navy border + navy tint bg.

### Step 2 — Details
- **Stepper inputs** for beds and baths: custom +/− buttons flanking a display value. Baths increment by 0.5.
- Square footage input with `bi-aspect-ratio` prefix icon
- Monthly rent input with `$` prefix
- **Amenity pills grid:** 12 pills (A/C, Heat, Washer/Dryer, WiFi, Backyard, Security, Dishwasher, Furnished, Pool, Gym, EV Charging, Storage). Toggle selected state — navy border + navy tint bg + checkmark appears.
- Pet policy + Parking dropdowns in 2-column row.

### Step 3 — Platforms & AI Context
- **Platform cards:** 3 large checkbox cards — LTR (navy icon, Zillow), STR (orange icon, Airbnb), Social (green icon, Facebook). Selected = navy border + navy tint + navy checkmark circle appears top-right.
- **Notes textarea:** 5 rows, 500 char limit with live counter. Blue AI tip box below explaining impact on generation quality.

### Step 4 — Review
3 summary cards (Basic Info, Details, Platforms & Notes). Each card:
- Section header with title + icon + "Edit" button → jumps back to that step
- All values shown as key/value grid, dynamically populated from form state
- Amenity tags shown as gray pills
- Platform tags shown as colored badges
- Status shown as colored pill

### Wizard Footer (sticky)
Left: "Back" button (hidden on step 1). Center: "Step X of 4" indicator. Right: "Cancel" ghost + "Continue" navy → on step 4 becomes "Save Property" green button.

### Success State
Replaces wizard content after save:
- Animated pop-in green circle checkmark
- "Property saved!" title
- Property address + "is ready to generate" subtitle
- Two CTAs: **"Generate Listing Now"** (navy, primary) → navigates to generate page with property data | **"Add Another Property"** (ghost) → resets wizard to step 1
- "View all properties" text link

### Data Handoff to Generate Page
On "Generate Listing Now" click: write full property object to `sessionStorage` under key `ds_new_property`, then navigate to `/properties/{newId}/generate`. Generate page reads and removes this key on load.

### Interactivity
- Step navigation → shows/hides step content divs, updates rail state
- Validation on "Continue" from step 1 — required fields: address, city, type. Show red border + error message. Block progression.
- Stepper +/− → update displayed value, update review card live
- Amenity pill toggle → toggle `.selected` class, update review card live
- All form inputs → sync review card values live via `oninput` / `onchange`
- Char counter on notes → live count, hard cap at 500
- Discard button → `confirm()` dialog → navigate to `/properties`
- "Add Another" → full form reset to blank state

### API Calls
```javascript
POST   /api/properties              // Create — returns { id, ...property }
PUT    /api/properties/{id}         // Edit — returns { id, ...property }
GET    /api/properties/{id}         // Pre-fill for edit mode
```

---

---

# PART 3 — IMPLEMENTATION CHECKLIST
## Run through this for every page you build

- [ ] CSS custom properties from design system applied (no hardcoded hex values)
- [ ] Bootstrap Icons loaded via CDN
- [ ] Inter font loaded and applied
- [ ] Toastr configured with correct options
- [ ] Dark mode toggle wired up — reads system preference on load, toggles `data-theme` on `<html>`
- [ ] Sidebar rendered with correct active nav item
- [ ] All buttons have correct hover states (`translateY`, shadow change)
- [ ] All API calls use `$.ajax()` — not `fetch()`
- [ ] Anti-forgery token included in all POST/PUT/DELETE headers
- [ ] Loading states on all async actions (buttons disabled, spinner/text change)
- [ ] Empty states shown when lists have no data
- [ ] Form validation — required fields show red border + error message
- [ ] Toastr used for all user feedback (no `alert()`)
- [ ] Responsive — sidebar hides on mobile (`display:none` below 768px), content goes full width
- [ ] Page matches mockup at 1280px viewport width

---

*DwellScript UI Prompt v1.0 — March 2026*
*Reference mockup files for exact visual spec on every component.*
