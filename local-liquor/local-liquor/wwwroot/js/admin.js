/**
 * Admin wine editor: boots the same 3D stage the public site uses, in solo mode,
 * and keeps it in step with the form as it is typed into.
 *
 * If any of this fails the form still works — the preview is the only thing that
 * depends on it.
 */

import { loadLogo } from "./label.js";
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
  tint: field("tint"),
  tintText: field("tint-text"),
  abv: field("abv"),
  volume: field("volume"),
};

const HEX = /^#[0-9a-fA-F]{6}$/;

/** Reads the current form state into the shape the stage expects. */
function readForm() {
  const abv = Number(inputs.abv?.value ?? 8);
  const volume = Number(inputs.volume?.value ?? 750);
  return {
    liquid: HEX.test(inputs.liquidText?.value ?? "") ? inputs.liquidText.value : null,
    label: {
      name: (inputs.label?.value ?? "").toUpperCase(),
      volume: `${Number.isFinite(volume) ? volume : 750} ML`,
      // The printed label is Danish, so its decimal separator is a comma.
      abv: `~${(Number.isFinite(abv) ? abv : 0).toString().replace(".", ",")}% VOL.`,
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

const logo = await loadLogo().catch(() => null);
await document.fonts?.ready;

const stage = createStage(container, {
  wines,
  logo,
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
  pairColour(inputs.tint, inputs.tintText, () => {
    document.querySelector("[data-preview]")?.style.setProperty("--tint", inputs.tintText.value);
  });

  for (const input of [inputs.label, inputs.abv, inputs.volume]) {
    input?.addEventListener("input", push);
  }

  const tint = inputs.tintText?.value;
  if (tint && HEX.test(tint)) {
    document.querySelector("[data-preview]")?.style.setProperty("--tint", tint);
  }
}
