/**
 * Draws the Local Liquor label onto a canvas.
 *
 * The geometry below was measured off the supplied artwork (logoer/Etiket.png,
 * 2000 x 2539) so the rendered label matches the printed one. Everything is
 * expressed in that reference space and scaled to whatever canvas it is drawn
 * into. Text is drawn per glyph so we can control tracking exactly — the label
 * is all caps and widely letterspaced, where kerning loss is not noticeable.
 */

const REF_W = 2000;
const REF_H = 2539;

const FRAME = { x: 162, y: 137, w: 1662, h: 1661, stroke: 49 };
const LOGO = { x: 386, y: 606, w: 1230 };
const EST = { text: "EST. 2023", centerX: 994, capTop: 1631, cap: 72, width: 518 };
const NAME = { centerX: 999, capTop: 1992, cap: 204, tracking: 0.03 };
const VIN = { centerX: 996, capTop: 2197, cap: 101, width: 254 };
const FOOT = { capTop: 2442, cap: 61, left: 49, right: 1954, tracking: 0.11 };

const INK = "#000000";
const ORANGE = "#f89c1c";
const PAPER = "#ffffff";
const SANS = '"Poppins", system-ui, sans-serif';

let logoPromise = null;

/** The wordmark, loaded once and shared by every label on the page. */
export function loadLogo(src = "/img/logo.png") {
  if (!logoPromise) {
    logoPromise = new Promise((resolve, reject) => {
      const img = new Image();
      img.onload = () => resolve(img);
      img.onerror = reject;
      img.src = src;
    });
  }
  return logoPromise;
}

/** Font size at which a capital letter is exactly `cap` pixels tall. */
function sizeForCap(ctx, weight, cap) {
  ctx.font = `${weight} 100px ${SANS}`;
  const ascent = ctx.measureText("H").actualBoundingBoxAscent;
  return ascent > 0 ? (cap / ascent) * 100 : cap * 1.4;
}

function trackedWidth(ctx, text, tracking) {
  let width = 0;
  for (const ch of text) width += ctx.measureText(ch).width + tracking;
  return width - tracking;
}

/** Draws `text` with `tracking` px between glyphs, `x` being the left edge. */
function drawTracked(ctx, text, x, baseline, tracking) {
  let cursor = x;
  for (const ch of text) {
    ctx.fillText(ch, cursor, baseline);
    cursor += ctx.measureText(ch).width + tracking;
  }
}

/** Spreads `text` to exactly `target` px wide, then draws it centred on `centerX`. */
function drawToWidth(ctx, text, centerX, baseline, target) {
  const natural = trackedWidth(ctx, text, 0);
  const gaps = Math.max([...text].length - 1, 1);
  const tracking = (target - natural) / gaps;
  drawTracked(ctx, text, centerX - target / 2, baseline, tracking);
}

/**
 * @param {CanvasRenderingContext2D} ctx
 * @param {number} width  canvas width; height is assumed to be width / 0.7877
 * @param {{name: string, volume: string, abv: string}} label
 * @param {HTMLImageElement} logo
 */
export function drawLabel(ctx, width, label, logo) {
  const s = width / REF_W;
  const height = REF_H * s;

  ctx.save();
  ctx.clearRect(0, 0, width, height);
  ctx.fillStyle = PAPER;
  ctx.fillRect(0, 0, width, height);
  ctx.scale(s, s);
  ctx.textBaseline = "alphabetic";
  ctx.textAlign = "left";

  // --- the black square frame ---------------------------------------------
  ctx.strokeStyle = INK;
  ctx.lineWidth = FRAME.stroke;
  ctx.strokeRect(
    FRAME.x + FRAME.stroke / 2,
    FRAME.y + FRAME.stroke / 2,
    FRAME.w - FRAME.stroke,
    FRAME.h - FRAME.stroke,
  );

  // --- wordmark ------------------------------------------------------------
  if (logo) {
    const logoH = (LOGO.w / logo.naturalWidth) * logo.naturalHeight;
    ctx.drawImage(logo, LOGO.x, LOGO.y, LOGO.w, logoH);
  }

  // --- "EST. 2023" ---------------------------------------------------------
  ctx.fillStyle = INK;
  ctx.font = `400 ${sizeForCap(ctx, 400, EST.cap)}px ${SANS}`;
  drawToWidth(ctx, EST.text, EST.centerX, EST.capTop + EST.cap, EST.width);

  // --- the variety, below the frame ---------------------------------------
  let nameSize = sizeForCap(ctx, 400, NAME.cap);
  let nameTracking = nameSize * NAME.tracking;
  ctx.font = `400 ${nameSize}px ${SANS}`;
  let nameWidth = trackedWidth(ctx, label.name, nameTracking);

  // Long names (HYLDEBLOMST) shrink rather than run past the frame above them.
  // The baseline stays put, so every variety's name sits on the same line.
  const maxWidth = FRAME.w - FRAME.stroke * 2;
  if (nameWidth > maxWidth) {
    const shrink = maxWidth / nameWidth;
    nameSize *= shrink;
    nameTracking *= shrink;
    ctx.font = `400 ${nameSize}px ${SANS}`;
    nameWidth = trackedWidth(ctx, label.name, nameTracking);
  }
  drawTracked(
    ctx,
    label.name,
    NAME.centerX - nameWidth / 2,
    NAME.capTop + NAME.cap,
    nameTracking,
  );

  // --- "VIN", in the brand orange -----------------------------------------
  ctx.fillStyle = ORANGE;
  ctx.font = `500 ${sizeForCap(ctx, 500, VIN.cap)}px ${SANS}`;
  drawToWidth(ctx, "VIN", VIN.centerX, VIN.capTop + VIN.cap, VIN.width);

  // --- volume and strength, outside the frame ------------------------------
  ctx.fillStyle = INK;
  const footSize = sizeForCap(ctx, 400, FOOT.cap);
  ctx.font = `400 ${footSize}px ${SANS}`;
  const footTracking = footSize * FOOT.tracking;
  const footBaseline = FOOT.capTop + FOOT.cap;
  drawTracked(ctx, label.volume, FOOT.left, footBaseline, footTracking);
  const abvWidth = trackedWidth(ctx, label.abv, footTracking);
  drawTracked(ctx, label.abv, FOOT.right - abvWidth, footBaseline, footTracking);

  ctx.restore();
}

/** Ratio the label artwork is drawn at (width / height). */
export const LABEL_ASPECT = REF_W / REF_H;

/** Renders a label into an offscreen canvas, ready to become a texture. */
export function renderLabelCanvas(label, logo, width = 1024) {
  const canvas = document.createElement("canvas");
  canvas.width = width;
  canvas.height = Math.round(width / LABEL_ASPECT);
  drawLabel(canvas.getContext("2d"), width, label, logo);
  return canvas;
}
