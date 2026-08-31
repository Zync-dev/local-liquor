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
| `Pages/Index.cshtml` | The landing page: hero, figures, story, range, craft, contact |
| `Pages/Vin.cshtml` | One page per wine, routed at `/vin/{slug}` |
| `Pages/Admin/` | The admin: dashboard, wines, markets, photos |
| `Data/Entities/` | The database model: wines, notes, markets, photos, the account |
| `Data/Seed.cs` | First-run content, so an empty database looks like the site did |
| `Services/WineService.cs` | Reads the range, resolved to one language |
| `Services/MediaService.cs` | Photo uploads: decode, resize, strip EXIF, re-encode |
| `Resources/SharedResource.resx` | **Danish site copy** — nav, hero, story, craft, footer |
| `Resources/SharedResource.en.resx` | The English copy, same keys |
| `wwwroot/css/site.css` | The whole design system — tokens at the top |
| `wwwroot/js/bottle.js` | The 3D bottle: profile, glass, wine, capsule, label |
| `wwwroot/js/label.js` | Draws the label artwork onto a canvas |
| `wwwroot/js/stage.js` | The scene: camera fitting, the three slots, click-to-swap |
| `wwwroot/js/environment.js` | The studio it reflects, the backdrop it refracts, the shadow |
| `Pages/Shared/_AgeGate.cshtml` | The 18+ gate |

## Common changes

**Change a wine, add one, mark one sold out, add a market, upload a photo.** All of
that is in the admin now — no deploy needed. The wine editor carries a live 3D
preview: the colour picker recolours the bottle and the label name redraws the label
as you type.

Note the hero only has three slots. A fourth wine appears on the range cards and gets
its own page, but `SLOTS` in `stage.js` needs widening for it to reach the hero.

**Change the site copy** — nav, hero, story, craft, footer. That still lives in the
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

**Change the label.** The geometry constants at the top of `label.js` were measured
off `logoer/Etiket.png` in its own 2000 × 2539 pixel space, so they can be re-measured
from a new print file the same way.

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

- **The page ground is warm paper (`#faf7f2`), not white.** The label is white with a
  black frame; on `#fff` the bottle has nothing to sit against.
- **Fonts are self-hosted.** Poppins and Newsreader are served from `wwwroot/fonts`
  rather than Google's CDN, so no visitor IP is handed to a third party — the site
  sets no cookie until someone picks a language, and needs no consent banner.
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
  bend. It is a small shader rather than a texture so its colour can ease between
  wines, and it is deliberately **not tone mapped**: its outer colour has to land on
  exactly the page's paper or the edge of the canvas shows as a rectangle. `uEdge`
  is what keeps the falloff finished before the frame ends.
- **The studio is lit for glass, not for brightness.** A mid-grey surround with soft
  panels — the way you would actually photograph glass. A uniformly bright room
  mirrors white from every angle and every edge disappears.
- **Both stages are full bleed.** The backdrop is opaque, so a stage narrower than
  its section reads as a rectangle laid over the page. The bottles are moved off
  centre by shearing the camera frustum (`shift`), never by moving the bottles.
- **Dispersion, and roughness that varies.** Real glass splits light by wavelength
  and is never uniformly polished. Neither costs much and their absence is most of
  what "looks fake" actually means.
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

- **The two label files disagree on strength.** `logoer/Etiket.png` says `~13% VOL.`;
  the photographed bottle says `~8% VOL.`. The site follows the bottle — all three
  wines are set to 8% in `VariantCatalog`. Correct it there if that is wrong.
- Tasting notes, taglines, batch sizes and harvest months are still invented. Only the
  names, the strength and the contact details came from you.
- No photography yet. Upload some under `/admin/billeder` and tag them *Historien* or
  *Håndværket* and they appear on the front page; until then those sections are
  typographic by design and do not look like they are missing images.
- The privacy page describes what the site actually does, but has not been through a
  lawyer.
