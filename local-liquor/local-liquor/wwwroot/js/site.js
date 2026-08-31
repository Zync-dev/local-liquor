/**
 * Page glue: header state, mobile nav, scroll reveals, the label canvases on
 * the wine cards, and booting the 3D stage.
 *
 * Nothing here is required for the page to work — if this file fails to load,
 * or WebGL is unavailable, the markup still renders and the label artwork shows
 * as a flat image instead.
 */

import { drawLabel, loadLogo } from "./label.js";

const wines = readWineData();

/* -------------------------------------------------------------- header --- */

const header = document.querySelector("[data-header]");
if (header) {
  const setStuck = () => header.classList.toggle("is-stuck", window.scrollY > 24);
  setStuck();
  window.addEventListener("scroll", setStuck, { passive: true });
}

const navToggle = document.querySelector("[data-nav-toggle]");
if (navToggle) {
  navToggle.addEventListener("click", () => {
    const open = document.body.hasAttribute("data-nav-open");
    document.body.toggleAttribute("data-nav-open", !open);
    navToggle.setAttribute("aria-expanded", String(!open));
  });

  // Close on navigation and on Escape.
  document.querySelectorAll(".nav__link").forEach((link) =>
    link.addEventListener("click", () => {
      document.body.removeAttribute("data-nav-open");
      navToggle.setAttribute("aria-expanded", "false");
    }),
  );
  document.addEventListener("keydown", (event) => {
    if (event.key === "Escape" && document.body.hasAttribute("data-nav-open")) {
      document.body.removeAttribute("data-nav-open");
      navToggle.setAttribute("aria-expanded", "false");
      navToggle.focus();
    }
  });
}

/* ------------------------------------------------------------ age gate --- */

/**
 * The gate is already open by the time this runs — the partial opens it inline
 * during parse so the page never flashes behind it. All that is left is wiring
 * the two answers.
 */
function setupAgeGate() {
  const gate = document.querySelector("[data-age-gate]");
  if (!gate) return;

  // Escape must not dismiss it; the only ways out are the two buttons.
  gate.addEventListener("cancel", (event) => event.preventDefault());

  const reduced = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

  gate.querySelector("[data-age-yes]")?.addEventListener("click", () => {
    try {
      localStorage.setItem("ll-age", "ok");
    } catch {
      // Private browsing: they will simply be asked again next visit.
    }
    if (reduced) {
      gate.close();
      return;
    }
    gate.classList.add("is-leaving");
    setTimeout(() => gate.close(), 450);
  });

  gate.querySelector("[data-age-no]")?.addEventListener("click", () => {
    gate.querySelector("[data-age-ask]").hidden = true;
    gate.querySelector("[data-age-denied]").hidden = false;
  });
}

setupAgeGate();

/* ------------------------------------------------------------- reveals --- */

const revealTargets = document.querySelectorAll("[data-reveal]");
if (revealTargets.length) {
  const observer = new IntersectionObserver(
    (entries) => {
      for (const entry of entries) {
        if (!entry.isIntersecting) continue;
        entry.target.classList.add("is-visible");
        observer.unobserve(entry.target);
      }
    },
    { rootMargin: "0px 0px -12% 0px", threshold: 0.05 },
  );
  revealTargets.forEach((el) => observer.observe(el));
}

/* ------------------------------------------------------------ wine data -- */

function readWineData() {
  const node = document.getElementById("wine-data");
  if (!node) return [];
  try {
    return JSON.parse(node.textContent);
  } catch {
    return [];
  }
}

/* ------------------------------------------------------- label canvases -- */

/**
 * Wine cards show the label itself, drawn rather than photographed so every
 * variety is typeset identically.
 */
async function paintLabelCanvases(logo) {
  const canvases = document.querySelectorAll("[data-label-canvas]");
  if (!canvases.length) return;

  const dpr = Math.min(window.devicePixelRatio || 1, 2);
  const bySlug = new Map(wines.map((wine) => [wine.slug, wine]));

  for (const canvas of canvases) {
    const wine = bySlug.get(canvas.dataset.labelCanvas);
    if (!wine) continue;

    // Scale the backing store for the device but leave the display size to CSS —
    // the attributes only need to carry the right aspect ratio.
    canvas.width = Math.round(canvas.width * dpr);
    canvas.height = Math.round(canvas.height * dpr);

    drawLabel(canvas.getContext("2d"), canvas.width, wine.label, logo);
  }
}

/* ----------------------------------------------------------- the stage --- */

async function bootStage(logo) {
  const container = document.querySelector("[data-stage]");
  if (!container || !wines.length) return;

  let createStage;
  try {
    ({ createStage } = await import("./stage.js"));
  } catch (error) {
    console.warn("Local Liquor: 3D stage unavailable", error);
    return;
  }

  const soloSlug = container.dataset.stageSingle;
  const solo = Boolean(soloSlug);
  const hero = document.querySelector("[data-hero]");
  const initialIndex = solo
    ? Math.max(wines.findIndex((w) => w.slug === soloSlug), 0)
    : Number(hero?.dataset.heroIndex ?? 1);

  const pickerButtons = [...document.querySelectorAll("[data-picker-btn]")];

  const markCurrent = (index) => {
    for (const button of pickerButtons) {
      button.setAttribute("aria-current", String(Number(button.dataset.pickerBtn) === index));
    }
  };

  const stage = createStage(container, {
    wines,
    logo,
    solo,
    initialIndex,
    // The product page puts its copy on the right, so the bottle goes left.
    shift: solo ? -0.16 : undefined,
    onReady: () => document.body.classList.add("stage-ready"),
    onSelect: markCurrent,
  });

  if (!stage) return; // no WebGL; the fallback artwork stays visible

  for (const button of pickerButtons) {
    button.addEventListener("click", () => stage.select(Number(button.dataset.pickerBtn)));
  }
  markCurrent(initialIndex);
}

/* ----------------------------------------------------------------- go ---- */

(async () => {
  let logo = null;
  try {
    // Both the label text and the wordmark need to be present before we draw,
    // or the first paint uses a fallback face and looks wrong.
    [logo] = await Promise.all([loadLogo(), document.fonts?.ready]);
  } catch {
    // carry on without the wordmark rather than losing the labels entirely
  }

  await paintLabelCanvases(logo);
  await bootStage(logo);
})();
