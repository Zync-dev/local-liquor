/**
 * Admin wine editor: boots the same 3D stage the public site uses, in solo mode,
 * and keeps it in step with the form as it is typed into.
 *
 * If any of this fails the form still works — the preview is the only thing that
 * depends on it.
 */

import { createStage } from "./stage.js";

const container = document.querySelector("[data-stage]");
const dataNode = document.getElementById("wine-data");
if (!container || !dataNode) throw new Error("no preview on this page");

const wines = JSON.parse(dataNode.textContent);

const field = (name) => document.querySelector(`[data-preview-${name}]`);

const inputs = {
  label: field("label"),
  liquid: field("liquid"),
  liquidText: field("liquid-text"),
  accent: field("accent"),
  accentText: field("accent-text"),
  subtitle: field("subtitle"),
  batch: field("batch"),
  abv: field("abv"),
  volume: field("volume"),
};

const HEX = /^#[0-9a-fA-F]{6}$/;

/**
 * The same split WineView.SplitLabelName does on the server: a long fruit name
 * breaks across two lines with a hyphen, near the middle so the lines match in
 * width. Kept in step by hand — it is four lines, and shipping it twice beats
 * a round trip on every keystroke.
 */
function splitLabelName(name) {
  if (name.length <= 5) return [name, ""];
  const split = Math.ceil(name.length / 2);
  return [`${name.slice(0, split)}-`, name.slice(split)];
}

/** Reads the current form state into the shape the stage expects. */
function readForm() {
  const abv = Number(inputs.abv?.value ?? 0);
  const volume = Number(inputs.volume?.value ?? 750);
  const name = (inputs.label?.value ?? "").toUpperCase();
  const [top, bottom] = splitLabelName(name);
  const accent = inputs.accentText?.value ?? "";

  return {
    liquid: HEX.test(inputs.liquidText?.value ?? "") ? inputs.liquidText.value : null,
    label: {
      name,
      top,
      bottom,
      subtitle: (inputs.subtitle?.value ?? "").toUpperCase(),
      batch: inputs.batch?.value ?? "",
      volume: `${Number.isFinite(volume) ? volume : 750} ML`,
      // The printed label is Danish, so its decimal separator is a comma.
      abv: `${(Number.isFinite(abv) ? abv : 0).toString().replace(".", ",")} % VOL.`,
      ...(HEX.test(accent) ? { accent } : {}),
    },
  };
}

/** Keeps the colour swatch and the hex field showing the same value. */
function pairColour(swatch, text, onChange) {
  if (!swatch || !text) return;

  swatch.addEventListener("input", () => {
    text.value = swatch.value;
    onChange();
  });

  text.addEventListener("input", () => {
    if (HEX.test(text.value)) {
      swatch.value = text.value;
      onChange();
    }
  });
}

await document.fonts?.ready;

const stage = createStage(container, {
  wines,
  solo: true,
  initialIndex: 0,
  onReady: () => document.body.classList.add("stage-ready"),
});

if (stage) {
  // One update per frame at most: dragging a colour picker fires continuously,
  // and redrawing the label texture on every event is wasted work.
  let queued = false;
  const push = () => {
    if (queued) return;
    queued = true;
    requestAnimationFrame(() => {
      queued = false;
      stage.update(0, readForm());
    });
  };

  pairColour(inputs.liquid, inputs.liquidText, push);
  pairColour(inputs.accent, inputs.accentText, push);

  for (const input of [inputs.label, inputs.subtitle, inputs.batch, inputs.abv, inputs.volume]) {
    input?.addEventListener("input", push);
  }
}
