/**
 * Draws the Local Liquor labels onto a canvas — front and back.
 *
 * Every coordinate below is millimetres, lifted directly from the artwork
 * (redesign/SVG/label-*.svg) on its 67.7 x 99.1 mm page, and scaled to whatever
 * canvas it is drawn into. Working in the artwork's own units means the label
 * on the 3D bottle and the label on the page are the same drawing as the one
 * that goes to the printer, and a change to the SVG is a change to two numbers
 * here rather than a re-guess.
 *
 * The accent is the only thing that varies between fruits. It appears in
 * exactly the places the manual allows: the rule under the wordmark, the dot
 * after LIQUOR, the fruit name, the divider on the back, and the web address.
 */

const MM_W = 67.7;
const MM_H = 99.1;

const INK = "#17150F";
const PAPER = "#FFFFFF";      // the label stock itself is white, not the page paper
const SECONDARY = "#6E6A62";
const LIGHT = "#9A958C";
const RULE = "#D5D2CB";
const RULE_SOFT = "#E2DFD9";
const BODY_INK = "#2E2A25";

const SANS = "Archivo, system-ui, sans-serif";
const MONO = "'JetBrains Mono', ui-monospace, monospace";

/** Ratio the label artwork is drawn at (width / height). */
export const LABEL_ASPECT = MM_W / MM_H;

/* ------------------------------------------------------------------ text --- */

/**
 * Draws text with tracking, glyph by glyph. Canvas letterSpacing exists but is
 * not universal, and drawing the glyphs gives exact control of the total width,
 * which is what the mono rows need to line up with the rules above them.
 */
function tracked(ctx, text, x, baseline, tracking, align = "left") {
  let width = 0;
  for (const ch of text) width += ctx.measureText(ch).width + tracking;
  width -= tracking;

  let cursor = x;
  if (align === "right") cursor = x - width;
  else if (align === "center") cursor = x - width / 2;

  for (const ch of text) {
    ctx.fillText(ch, cursor, baseline);
    cursor += ctx.measureText(ch).width + tracking;
  }
  return width;
}

/** Archivo, in mm-sized type. */
function sans(ctx, sizeMm, weight = 400) {
  ctx.font = `${weight} ${sizeMm}px ${SANS}`;
}

/** JetBrains Mono. The manual sets it in caps at +0.2em, always. */
function mono(ctx, sizeMm) {
  ctx.font = `400 ${sizeMm}px ${MONO}`;
  return sizeMm * 0.2;
}

function rule(ctx, x, y, w, h, colour) {
  ctx.fillStyle = colour;
  ctx.fillRect(x, y, w, h);
}

/* ----------------------------------------------------------------- front --- */

/**
 * @param {CanvasRenderingContext2D} ctx
 * @param {number} width  canvas width in pixels; height is width / LABEL_ASPECT
 * @param {{name: string, top: string, bottom: string, subtitle: string,
 *          volume: string, abv: string, accent: string}} label
 */
export function drawLabelFront(ctx, width, label) {
  const s = width / MM_W;
  const accent = label.accent || "#C0453C";

  ctx.save();
  ctx.clearRect(0, 0, width, width / LABEL_ASPECT);
  ctx.fillStyle = PAPER;
  ctx.fillRect(0, 0, width, width / LABEL_ASPECT);
  ctx.scale(s, s);
  ctx.textBaseline = "alphabetic";
  ctx.textAlign = "left";

  // --- wordmark block, 6 mm margin all round ------------------------------
  rule(ctx, 6, 6, 55.7, 0.45, INK);

  ctx.fillStyle = INK;
  sans(ctx, 7.6, 800);
  tracked(ctx, "LOCAL", 6, 14, -0.34);
  tracked(ctx, "LIQUOR", 6, 21.9, -0.34);

  ctx.fillStyle = accent;
  ctx.beginPath();
  ctx.arc(60.6, 20.1, 1.1, 0, Math.PI * 2);
  ctx.fill();

  rule(ctx, 6, 24.4, 55.7, 0.45, accent);

  // --- the fruit, in accent, hyphenated across two lines -------------------
  ctx.fillStyle = accent;
  sans(ctx, 11.4, 800);
  tracked(ctx, label.top, 6, 50, -0.57);
  if (label.bottom) tracked(ctx, label.bottom, 6, 60.5, -0.57);

  // --- english subtitle, mono ---------------------------------------------
  ctx.fillStyle = SECONDARY;
  tracked(ctx, label.subtitle, 6, 66.6, mono(ctx, 2.2));

  // --- year left, measure right, over a hairline ---------------------------
  rule(ctx, 6, 86, 55.7, 0.2, RULE);
  ctx.fillStyle = INK;
  const t = mono(ctx, 2.3);
  tracked(ctx, "EST. 2023", 6, 90.6, t);
  tracked(ctx, `${label.volume} · ${label.abv}`, 61.7, 90.6, t, "right");

  ctx.restore();
}

/* ------------------------------------------------------------------ back --- */

/**
 * @param {{name: string, bodyDa: string, bodyEn: string, ingredients: string,
 *          volume: string, abv: string, batch: string, accent: string}} label
 */
export function drawLabelBack(ctx, width, label) {
  const s = width / MM_W;
  const accent = label.accent || "#C0453C";

  ctx.save();
  ctx.clearRect(0, 0, width, width / LABEL_ASPECT);
  ctx.fillStyle = PAPER;
  ctx.fillRect(0, 0, width, width / LABEL_ASPECT);
  ctx.scale(s, s);
  ctx.textBaseline = "alphabetic";
  ctx.textAlign = "left";

  // --- header row ----------------------------------------------------------
  ctx.fillStyle = INK;
  sans(ctx, 3.6, 800);
  tracked(ctx, "LOCAL LIQUOR", 6, 10.2, -0.11);

  ctx.fillStyle = SECONDARY;
  tracked(ctx, label.name, 61.7, 10.2, mono(ctx, 2), "right");

  rule(ctx, 6, 12.6, 55.7, 0.3, accent);

  // --- danish above english, always ---------------------------------------
  ctx.fillStyle = BODY_INK;
  sans(ctx, 2.6, 400);
  let y = 20;
  for (const line of wrap(ctx, label.bodyDa, 55.7)) {
    ctx.fillText(line, 6, y);
    y += 3.7;
  }

  ctx.fillStyle = SECONDARY;
  sans(ctx, 2.3, 400);
  y += 1.2;
  for (const line of wrap(ctx, label.bodyEn, 55.7)) {
    ctx.fillText(line, 6, y);
    y += 3.3;
  }

  // --- ingredients ---------------------------------------------------------
  rule(ctx, 6, 41.4, 55.7, 0.25, RULE_SOFT);
  ctx.fillStyle = LIGHT;
  tracked(ctx, "INGREDIENSER · INGREDIENTS", 6, 45.9, mono(ctx, 1.9));

  ctx.fillStyle = BODY_INK;
  sans(ctx, 2.3, 400);
  ctx.fillText(label.ingredients, 6, 49.4);

  // --- the data row --------------------------------------------------------
  ctx.fillStyle = INK;
  const t = mono(ctx, 2.2);
  tracked(ctx, label.volume, 6, 80, t);
  tracked(ctx, label.abv, 33.8, 80, t, "center");
  tracked(ctx, `BATCH ${label.batch}`, 61.7, 80, t, "right");

  rule(ctx, 6, 82.2, 55.7, 0.25, RULE_SOFT);

  ctx.fillStyle = SECONDARY;
  const t2 = mono(ctx, 1.9);
  tracked(ctx, "FREMSTILLET OG AFTAPPET AF", 6, 86.2, t2);
  tracked(ctx, "LOCAL LIQUOR · DANMARK · EST. 2023", 6, 89.4, t2);

  ctx.fillStyle = accent;
  sans(ctx, 3, 800);
  tracked(ctx, "LOCALLIQUOR.DK", 6, 94.6, -0.03);

  ctx.fillStyle = LIGHT;
  tracked(ctx, "18+", 61.7, 94.6, mono(ctx, 1.8), "right");

  ctx.restore();
}

/** Greedy wrap at the current font, in the same mm units as everything else. */
function wrap(ctx, text, maxWidth) {
  const words = (text || "").split(/\s+/).filter(Boolean);
  const lines = [];
  let line = "";
  for (const word of words) {
    const next = line ? `${line} ${word}` : word;
    if (line && ctx.measureText(next).width > maxWidth) {
      lines.push(line);
      line = word;
    } else {
      line = next;
    }
  }
  if (line) lines.push(line);
  return lines;
}

/* ---------------------------------------------------------------- canvas --- */

/** Renders a label into an offscreen canvas, ready to become a texture. */
export function renderLabelCanvas(label, width = 1024, side = "front") {
  const canvas = document.createElement("canvas");
  canvas.width = width;
  canvas.height = Math.round(width / LABEL_ASPECT);
  const ctx = canvas.getContext("2d");
  if (side === "back") drawLabelBack(ctx, width, label);
  else drawLabelFront(ctx, width, label);
  return canvas;
}
