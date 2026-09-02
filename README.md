# Local Liquor

Marketing site for Local Liquor — small-batch Danish fruit wine. ASP.NET Core Razor
Pages (.NET 10), a Three.js hero, and an admin at `/admin` for the things that
change between deploys.

```bash
dotnet run --project local-liquor/local-liquor/local-liquor.csproj
```

Then open <http://localhost:5127>. The database is created and seeded on first
run; <http://localhost:5127/admin> will ask you to choose an administrator
password before it lets you in.

## What's where

| Path | What it is |
| --- | --- |
| `Pages/Index.cshtml` | The landing page: hero, figures, range, contact form |
| `Pages/Vin.cshtml` | One page per wine, routed at `/vin/{slug}` |
| `Pages/Admin/` | The admin: production board, batches, wines, photos, messages |
| `Data/Entities/` | The database model: wines, batches, steps, photos, messages, the account |
| `Data/Seed.cs` | First-run content, so an empty database looks like the site did |
| `Services/WineService.cs` | Reads the range, resolved to one language |
| `Data/Entities/Batch.cs` | One vessel of wine, from fruit to bottle |
| `Data/Entities/BatchStep.cs` | A dated job on a batch, plus the default schedule |
| `Services/MediaService.cs` | Photo uploads: decode, resize, strip EXIF, re-encode |
| `Resources/SharedResource.resx` | **Danish site copy** — nav, hero, range, contact, footer |
| `Resources/SharedResource.en.resx` | The English copy, same keys |
| `wwwroot/css/site.css` | The whole design system — tokens at the top |
| `redesign/` | **The brand manual and the master artwork.** Source of truth |
| `wwwroot/js/bottle.js` | The 3D bottle: profile, glass, wine, capsule, label |
| `wwwroot/js/label.js` | Draws the label artwork onto a canvas |
| `wwwroot/js/stage.js` | The scene: camera fitting, the slots, click-to-swap, drag-to-turn |
| `wwwroot/js/environment.js` | The studio it reflects, the backdrop it refracts, the shadow |
| `Pages/Shared/_AgeGate.cshtml` | The 18+ gate |

## Common changes

**Change a wine, add one, mark one sold out, start a batch, upload a photo.** All of
that is in the admin now — no deploy needed. The wine editor carries a live 3D
preview: the colour picker recolours the bottle and the label name redraws the label
as you type.

The hero sizes itself to the range: `buildSlots()` in `stage.js` lays out however many
published wines there are, and the middle one starts centre stage. Past five or so they
get tight, and the spacing constants are the thing to loosen.

**Change the site copy** — nav, hero, range, contact, footer. That still lives in the
two `.resx` files and needs a deploy. Keys are shared, so a string added to one needs
adding to the other, or the English page falls back to Danish. Per-wine copy is in the
database instead, because wines are created at runtime and `.resx` is compiled in.

**Change the bottle shape.** `outerProfile()` in `bottle.js` composes the silhouette
from `line()` and `curve()` segments in centimetres, which is then revolved. It is
modelled on the real bottle: 32 cm tall, 7.3 cm across, clear flint glass, one long
gentle shoulder, black capsule with two gold bands.

Do not be tempted to spline a sparse list of points instead. A spline run through a
long straight body followed by a shoulder overshoots at the junction, and the bulge
catches the light as a hard horizontal line across the middle of the bottle.

**Turn the age gate off, or change how long it is remembered.** It lives in
`_AgeGate.cshtml` and `setupAgeGate()` in `site.js`, and stores `ll-age` in
`localStorage`. Deleting the `<partial>` line in `_Layout.cshtml` removes it entirely.

**Change the label.** `label.js` draws the front and back artwork in millimetres,
transcribed straight from `redesign/SVG/label-jordbaer.svg` and its `-bagside` twin —
same numbers, same order. Open the SVG next to the file and they read line for line.
The label is 67.7 × 99.1 mm; that ratio is exported as `LABEL_ASPECT` and everything
else derives from it.

**Change the wordmark.** It is live type, not an image: `Pages/Shared/_Wordmark.cshtml`
plus the `.wordmark` block in `site.css`, following `redesign/SVG/local-liquor-primary.svg`.
Size it with `font-size` on the wrapping element — everything inside is in `em`. The
rules span 6.67em, wider than the two words, which is where the dot sits.

## The production board

The admin's front page is not a CMS dashboard, it is a list of what has to happen
to the wine this week. Two entities carry it:

- **`Batch`** — one vessel, from fruit to bottle. Deliberately *not* the same thing
  as a `Wine`: a wine is what the site sells, a batch is what is standing in the
  room. Several batches of jordbær can exist at once, and a batch can be an
  experiment that never becomes a listed wine, which is why `WineId` is optional.
  Deleting a wine leaves its batches alone; they just stop pointing at a listing.
- **`BatchStep`** — one dated job on a batch: take the fruit off, rack it, filter
  it, bottle it. The title is free text on purpose. Every batch ends up with
  something the last one did not need, and a fixed list would be either too long
  to read or too short to use.

`StepTemplate.Default` in `BatchStep.cs` is the usual schedule, as day offsets from
the day the fruit went in. A new batch can be created with those steps already
dated; every one of them can then be moved, renamed or deleted. **That list is the
thing to edit if your method changes** — it is seven lines and it is the only place
the schedule is written down.

The board shows anything open and due within three weeks, late items first, with
the batch it belongs to and a button to tick it off without opening anything. It
ignores steps on batches past `Bottled`: those are history, not work.

Alcohol is estimated from the two hydrometer readings — `(start − end) × 131.25`,
the rule of thumb every home winemaker uses — and shown on the batch list. It is
deliberately *not* pushed onto the wine: what goes on the label is a decision, not
a calculation.

## Messages

The contact form writes to the database and the admin reads them at
`/admin/beskeder`. There is no mail server and no secret to configure, and nothing
is lost if a forwarding address quietly stops working.

Three things keep the spam down, none of which a visitor ever sees: a honeypot
field that only a bot fills in (a filled one is accepted and dropped, so the bot
gets no signal), a rate limit of five posts per ten minutes per IP that applies to
POSTs only — the front page itself is never limited — and the ordinary antiforgery
token. Message bodies are rendered as text with `white-space: pre-wrap`, so a
sender's line breaks survive and their markup does not.

## Deploying to Railway

Railway builds the `Dockerfile` at the repo root. Two things need setting up once:

1. **Attach a volume.** The container filesystem is thrown away on every deploy, so
   without one the database, the uploaded photos and the login keys all vanish each
   time you push. Mount it anywhere — Railway sets `RAILWAY_VOLUME_MOUNT_PATH` and the
   app reads it. `/data` is a sensible mount path.
2. Nothing else. Railway supplies `PORT`, and the app binds to it.

Without a volume the app still starts; it just keeps its data in `App_Data` inside
the container and loses it on the next deploy.

The volume holds `localliquor.db`, `uploads/` and `keys/`. Back it up by copying that
directory; it is the entire state of the site.

If you ever run it somewhere without a volume, set `Storage__DataPath` to a directory
that persists.

## Decisions worth knowing

- **The brand manual is the specification, and `redesign/` holds it.** Ink `#17150F`
  on paper `#F7F5F1`, Archivo 400/600/800 and JetBrains Mono in caps at `+0.2em`, and
  one rule that carries everything else: **the accent hue is the only variable.** A new
  fruit takes a free hue off the ladder — `oklch(0.58 0.14 H)`, at least 60° from the
  others — and nothing in the layout moves. Jordbær is H22 `#C0453C`, skovbær H320
  `#A34E93`; H60, H130 and H250 are free.
- **The accent is allowed in five places and no others.** The rule under the wordmark,
  the dot after LIQUOR, the fruit name, the divider on the back label, and the web
  address. Everything else is ink, secondary `#6E6A62`, light `#9A958C`, or a rule.
  This is why the backdrop behind the bottles is neutral: it used to take the
  wine's colour, and that was wrong.
- **No italics, no underlines, no justified text, and Archivo 800 never in body copy.**
  Danish sits above English wherever both appear. Tone: fruit, place, year, percent.
- **The page ground is paper (`#f7f5f1`), not white.** The label is white; on `#fff`
  it has nothing to sit against and the bottle floats.
- **Fonts are self-hosted.** Archivo and JetBrains Mono are served from `wwwroot/fonts`
  rather than Google's CDN, so no visitor IP is handed to a third party — the site
  sets no cookie until someone picks a language, and needs no consent banner.
- **`color-scheme: light` is pinned.** The design has no dark variant, and without it
  form controls and the canvas behind the page follow the reader's system preference —
  which is how the admin ended up as ink text on a black ground.
- **The wine is an opaque material.** Three.js renders transmissive materials against
  a buffer that excludes other transmissive objects, so a see-through liquid would
  disappear behind the glass.
- **The label is drawn, not photographed.** One canvas routine typesets every variety
  identically and stays crisp at any size — it feeds the 3D texture and the range cards.
- **The glass refracts a real surface, not a reflection.** This is the single thing
  that decides whether it reads as glass or as painted plastic. With a transparent
  canvas there is nothing behind the bottle, three.js falls back to the environment
  map, and the empty neck renders as a flat panel of light — which is exactly what it
  looked like. `createBackdrop()` puts an opaque plane back there for the glass to
  bend. It is deliberately **not tone mapped**: its outer colour has to land on exactly
  the page's paper or the edge of the canvas shows as a rectangle. It is plain paper
  with a neutral halo and no accent anywhere — see the five-places rule above.
- **The backdrop is a painted texture on a built-in material, not a shader.** It
  was a ShaderMaterial, and a custom shader has to convert its own linear colour to
  sRGB via `#include <colorspace_fragment>` — a conversion that does not survive
  this scene. The backdrop is drawn twice a frame, once into the linear transmission
  buffer and once to the canvas, and the program compiled for the first gets reused
  for the second. Linear values went straight to an sRGB canvas: the paper came out
  visibly darker than the page around it, so the canvas showed as a rectangle sitting
  on it. A `MeshBasicMaterial` with a `CanvasTexture` gets this right in both passes.
  If you ever touch the backdrop, check the corners still sample `#f7f5f1` — that is
  the whole test.
- **`.hero__inner` is deliberately left unpositioned.** The stage's backdrop is
  opaque, so the canvas has to cover the hero *exactly* — any strip that falls short
  shows as a band laid over the page. `.hero__inner` is a positioned, max-width
  container, so while it was `position: relative` it became the stage's containing
  block and `inset: 0` gave the width of `.shell` and the height of the copy. The
  copy and picker carry their own `z-index`, so nothing needed it.
- **The scene renders above 1:1 on low-DPI screens.** The label lands at roughly 120
  physical pixels wide from a 1024px texture — an eight-fold minification — so the
  GPU samples a heavily blurred mip and its printed edges, which should be as crisp
  as cut paper, go soft. No material setting changes that; only drawing more pixels
  does. `degrade()` drops back to 1:1 if the machine cannot hold it, so this is the
  first thing to give if the hero ever feels heavy.
- **There is a faint ground under the bottles.** Refraction can only be seen where
  there is something behind the glass to bend: an unbroken gradient, bent evenly,
  still looks like an unbroken gradient. This is what makes the shoulder and neck
  visibly distort. It is squeezed vertically so it reaches nothing before the bottom
  of the canvas, for the same seam reason as everything else here.
- **The studio is lit for glass, not for brightness.** A mid-grey surround with soft
  panels — the way you would actually photograph glass. A uniformly bright room
  mirrors white from every angle and every edge disappears.
- **Both stages are full bleed.** The backdrop is opaque, so a stage narrower than
  its section reads as a rectangle laid over the page. The bottles are moved off
  centre by shearing the camera frustum (`shift`), never by moving the bottles.
- **Dispersion, and roughness that varies.** Real glass splits light by wavelength
  and is never uniformly polished. Neither costs much and their absence is most of
  what "looks fake" actually means. Roughness is kept low though — it blurs what the
  glass *transmits* as well as what it reflects, and blurring a pale backdrop is
  precisely what turns clear glass into frosted white.
- **`thickness` is 0.7, not a realistic 2.6.** The lathe is a solid of revolution,
  not a hollow vessel — there is no air cavity modelled inside it. At a true wall
  thickness the empty neck behaved like a block of glass and went milky. The body is
  full of opaque wine, so nothing is lost by treating the whole bottle as thin.
- **There is no caustic and the shadow is shallow.** The camera sits nearly level
  with the base, so a ground plane is seen almost edge-on and its far half smears
  upward behind the bottle. A coloured pool there reads as a stain climbing the
  glass, not as light on a surface.
- **The wine rocks about its middle, not its surface.** Pivoting at the fill line
  gives the base a 20 cm lever, and two degrees of tilt then swings the body of the
  liquid clean through the wall of the bottle. `MAX_SLOSH` is derived from the wall
  inset so it cannot.
- **Quality steps down by itself.** Refractive glass is by far the most expensive
  thing on the page and how expensive depends entirely on the visitor's GPU. The
  stage watches its first second of frames and, if it cannot hold roughly 30fps,
  drops dispersion, the roughness map, the pixel ratio and the transmission
  resolution. Better than picking a setting that is either ugly everywhere or
  unusable on a laptop.
- **The fog tracks the camera.** It is what pushes the flanking bottles back into the
  paper. Fixed distances would swallow the hero bottle the moment the camera pulled
  back for a taller bottle or a narrower window.
- **The age gate opens from an inline script**, not from `site.js`, so the page never
  flashes behind it. The buttons *are* wired in `site.js`: if that fails to load the
  gate stays shut, which is the safe direction for this particular control. It is
  client-side only, so the markup is always there for crawlers and for anyone
  browsing without JavaScript.
- **The bottle turns, and the back label is real.** Drag it and it spins with friction;
  a drag under six pixels still counts as a click, so tapping a flanking bottle swaps it
  to the centre as before. Only the centre bottle keeps its angle — the others ease back
  to facing front, because a row of bottles at random angles just looks untidy.
- **Everything degrades.** No JavaScript, or no WebGL, and the page still renders with
  the flat label artwork in place of the bottles.
- **The admin is Danish only, and deliberately not localised.** It has one user;
  carrying every label through the resource files would be a hundred keys of upkeep
  for nobody's benefit. The public site stays bilingual.
- **There is no default admin password and no email reset.** The account does not
  exist until someone visits `/admin` and creates it, so an un-set-up deployment
  cannot be signed into at all. Sign-in is rate limited to 8 attempts per 5 minutes
  per IP.
- **Data protection keys live on the volume.** They sign the login cookie and the
  antiforgery tokens, and default to a directory inside the container — which Railway
  discards on deploy. Left alone, every deploy would log you out and break every open
  form.
- **The admin runs on the invariant culture.** `<input type="number">` always posts
  `8.5` and `<input type="date">` always posts `2026-09-12`, whatever the page
  language; under `da-DK` model binding rejects the first and misreads the second.
  Views that want Danish month names ask for that culture explicitly.
- **The container runs as root, deliberately.** Railway mounts volumes owned by
  root, so a non-root process cannot create the database, the uploads directory or
  the key ring inside one — it would start and then fail on its first write. The base
  image ships a non-root `app` user (`APP_UID`); the Dockerfile says how to switch to
  it if this ever moves somewhere that mounts storage writable.
- **ImageSharp is pinned to 3.1.x on purpose.** Version 4 requires a paid Six Labors
  licence; 3.1 is the split licence, free under Apache 2.0 below $1M revenue. Bumping
  the major version is a commercial decision, not a routine update.
- **Uploads are decoded before they are trusted.** A file is re-encoded rather than
  passed through, saved under a name we generate, flattened onto white (JPEG has no
  alpha) and stripped of EXIF — phone photos carry GPS coordinates, and those would
  otherwise go public with the picture.

## Still to do

- **Both wines are set to 13 % vol., which is what the artwork says.** You said the
  strength differs per wine — set the real numbers per wine in `/admin/vine`. The
  figure is stored on the wine, so the label, the spec grid and the range card all
  follow it.
- Taglines, batch numbers, batch sizes and harvest months are still invented. The
  names, the label copy, the ingredients and the contact details came from you.
- No photography yet. Upload some under `/admin/billeder`, mark them *På forsiden*,
  and they appear as a full-bleed strip between the range and the contact block.
- The privacy page describes what the site actually does, but has not been through a
  lawyer.
