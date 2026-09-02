/**
 * Page glue: header state, mobile nav, scroll reveals, the label canvases on
 * the wine cards, and booting the 3D stage.
 *
 * Nothing here is required for the page to work — if this file fails to load,
 * or WebGL is unavailable, the markup still renders and the label artwork shows
 * as a flat image instead.
 */

import { drawLabelFront, drawLabelBack } from "./label.js";

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

/* ------------------------------------------------------------- hero ------ */

/**
 * Publishes how far the hero has scrolled away, 0 to 1, as --hero-out. The type
 * reads it and pulls apart; everything else about the effect is in CSS, so this
 * stays one number and one write per frame.
 */
function setupHeroScroll(hero) {
  if (window.matchMedia("(prefers-reduced-motion: reduce)").matches) return;

  let queued = false;
  let last = -1;

  const measure = () => {
    queued = false;
    const rect = hero.getBoundingClientRect();
    const out = Math.min(Math.max(-rect.top / Math.max(rect.height, 1), 0), 1);
    // Two decimals is finer than a pixel at these distances and stops the write
    // firing on every sub-pixel of a trackpad scroll.
    const rounded = Math.round(out * 100) / 100;
    if (rounded === last) return;
    last = rounded;
    hero.style.setProperty("--hero-out", String(rounded));
  };

  const onScroll = () => {
    if (queued) return;
    queued = true;
    requestAnimationFrame(measure);
  };

  window.addEventListener("scroll", onScroll, { passive: true });
  window.addEventListener("resize", onScroll, { passive: true });
  measure();
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
function paintLabelCanvases() {
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

    const ctx = canvas.getContext("2d");
    if (canvas.dataset.labelSide === "back") drawLabelBack(ctx, canvas.width, wine.label);
    else drawLabelFront(ctx, canvas.width, wine.label);
  }
}

/* ----------------------------------------------------------- the stage --- */

async function bootStage() {
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

    // The accent is the only variable in the system, so when the bottle in front
    // changes the whole page changes with it — wordmark rule, dot, focus rings,
    // the fruit names in the ticker. --accent is a registered property, so this
    // cross-fades rather than snapping.
    const accent = wines[index]?.accent;
    if (accent) {
      document.documentElement.style.setProperty("--accent", accent);
      hero?.style.removeProperty("--accent");
    }
  };

  const stage = createStage(container, {
    wines,
    solo,
    initialIndex,
    // Fallback only — site.css owns this through --stage-shift.
    shift: solo ? -0.16 : 0.19,
    // The hero is the first thing on the site and earns a taller bottle than the
    // product page. Past this the contact shadow starts to meet the bottom of the
    // canvas, and the base reads as cut off rather than as bleeding.
    fill: solo ? 0.66 : 0.74,
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
  // The labels are typeset in Archivo and JetBrains Mono. Drawing before those
  // have loaded silently falls back to a system face and the whole label is
  // wrong, so wait — it is a few milliseconds and the canvases start blank.
  try {
    await document.fonts?.ready;
  } catch {
    // carry on and accept the fallback face rather than drawing nothing
  }

  paintLabelCanvases();

  const hero = document.querySelector("[data-hero]");
  if (hero) {
    setupHeroScroll(hero);
    // Two frames: one for the browser to lay the type out with the real faces,
    // one for the transition to have a from-state to run out of. The timer is
    // the safety net — the hero is hidden until this lands, and in a background
    // tab requestAnimationFrame does not run at all.
    const light = () => hero.classList.add("is-lit");
    requestAnimationFrame(() => requestAnimationFrame(light));
    setTimeout(light, 400);
  }

  await bootStage();
})();
