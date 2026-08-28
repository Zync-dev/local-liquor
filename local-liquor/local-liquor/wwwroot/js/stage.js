/**
 * The WebGL stage: one bottle centre-forward, the others held back and to the
 * sides. Clicking a flanking bottle brings it in and sends the current one out
 * to the slot it came from.
 *
 * Everything animates by easing towards a target each frame rather than through
 * a tween library — there are only three objects and it keeps interruption
 * (clicking mid-animation) free.
 */

import * as THREE from "three";
import { createBottle, BOTTLE_HEIGHT } from "./bottle.js";
import { createStudioEnvironment, createContactShadow } from "./environment.js";
import { renderLabelCanvas } from "./label.js";

const PAPER = 0xfaf7f2;

/** Where the three bottles stand. Index 1 is centre stage. */
const SLOTS = [
  { x: -13, y: -1.4, z: -18, scale: 0.78, spin: 0.34 },
  { x: 0, y: 0, z: 0, scale: 1, spin: 0 },
  { x: 13, y: -1.4, z: -18, scale: 0.78, spin: -0.34 },
];

/** Half the horizontal room the trio needs, in world units. */
const GROUP_HALF_WIDTH = 13 + 4;

/**
 * How far right of centre the trio sits on a wide screen, as a fraction of the
 * frame width, so the copy gets the left column to itself.
 *
 * Applied by shifting the camera's frustum, not by moving the bottles. Moving
 * them puts the group off the camera axis, and since the flanking bottles sit
 * further back than the centre one, the perspective divide differs between them
 * — the three stop looking evenly spaced.
 */
const FRAME_SHIFT = 0.15;

/**
 * Share of the viewport height the centre bottle should occupy. The contact
 * shadow lies flat and stretches towards the camera, so it projects a fair way
 * below the base — leaving room for it is what keeps the bottle from looking
 * cropped.
 */
const FILL = 0.62;

const SOLO_SLOT = { x: 0, y: 0, z: 0, scale: 1, spin: 0 };

const prefersReducedMotion = () =>
  window.matchMedia("(prefers-reduced-motion: reduce)").matches;

function damp(current, target, lambda, dt) {
  return THREE.MathUtils.damp(current, target, lambda, dt);
}

/**
 * @param {HTMLElement} container
 * @param {{wines: Array, initialIndex: number, solo: boolean, logo: HTMLImageElement,
 *          onReady: Function, onSelect: Function}} options
 */
export function createStage(container, options) {
  const { wines, logo, solo = false, onReady, onSelect } = options;
  if (!wines.length) return null;

  let renderer;
  try {
    renderer = new THREE.WebGLRenderer({
      antialias: true,
      alpha: true,
      powerPreference: "high-performance",
    });
  } catch {
    return null; // no WebGL — the CSS fallback stays put
  }

  renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
  renderer.toneMapping = THREE.NeutralToneMapping ?? THREE.ACESFilmicToneMapping;
  renderer.toneMappingExposure = 1.0;
  // The transmission pass is the expensive part; half resolution is invisible
  // on glass this smooth.
  if ("transmissionResolutionScale" in renderer) renderer.transmissionResolutionScale = 0.5;
  container.appendChild(renderer.domElement);

  const scene = new THREE.Scene();
  // Range is set in resize(), once we know how far back the camera sits.
  scene.fog = new THREE.Fog(PAPER, 1, 2);

  const environment = createStudioEnvironment(renderer);
  scene.environment = environment.texture;
  scene.environmentIntensity = 1.0;

  const camera = new THREE.PerspectiveCamera(32, 1, 0.5, 400);
  camera.position.set(0, 1.2, 70);
  camera.lookAt(0, 0, 0);

  // A key light on top of the environment, for the specular streak down the glass.
  const key = new THREE.DirectionalLight(0xfff6ea, 1.6);
  key.position.set(-8, 14, 12);
  scene.add(key);

  const rim = new THREE.DirectionalLight(0xe8f0ff, 0.7);
  rim.position.set(9, 4, -8);
  scene.add(rim);

  // --- build a bottle per wine ---------------------------------------------
  const maxAnisotropy = renderer.capabilities.getMaxAnisotropy();
  const bottles = wines.map((wine) => {
    const texture = new THREE.CanvasTexture(renderLabelCanvas(wine.label, logo, 1024));
    texture.colorSpace = THREE.SRGBColorSpace;
    texture.anisotropy = maxAnisotropy;
    texture.needsUpdate = true;

    const bottle = createBottle({ liquid: wine.liquid, labelTexture: texture });
    bottle.texture = texture;
    bottle.wine = wine;
    bottle.group.userData.bottle = bottle;

    const shadow = createContactShadow();
    shadow.position.y = -BOTTLE_HEIGHT / 2 + 0.05;
    bottle.group.add(shadow);
    bottle.shadow = shadow;

    // animation state
    bottle.current = { ...SOLO_SLOT };
    bottle.target = { ...SOLO_SLOT };
    bottle.phase = Math.random() * Math.PI * 2;

    scene.add(bottle.group);
    return bottle;
  });

  // --- slot bookkeeping ----------------------------------------------------
  // `order` maps slot index -> bottle index. Centre is slot 1.
  let order = solo
    ? [options.initialIndex ?? 0]
    : bottles.map((_, i) => i);

  let centreBottle = solo ? (options.initialIndex ?? 0) : 1;

  function applyTargets() {
    if (solo) {
      bottles.forEach((bottle, i) => {
        bottle.group.visible = i === centreBottle;
        bottle.target = { ...SOLO_SLOT };
      });
      return;
    }
    order.forEach((bottleIndex, slotIndex) => {
      bottles[bottleIndex].target = { ...SLOTS[slotIndex] };
    });
  }

  function select(wineIndex) {
    if (solo || wineIndex === order[1]) return;
    const slot = order.indexOf(wineIndex);
    if (slot === -1) return;
    [order[1], order[slot]] = [order[slot], order[1]];
    applyTargets();
    onSelect?.(order[1]);
  }

  if (!solo) {
    const start = options.initialIndex ?? 1;
    const slot = order.indexOf(start);
    if (slot !== -1) [order[1], order[slot]] = [order[slot], order[1]];
  }
  applyTargets();
  // start each bottle at its target so the first frame is already composed
  for (const bottle of bottles) bottle.current = { ...bottle.target };

  // --- pointer -------------------------------------------------------------
  const pointer = new THREE.Vector2();
  const parallax = { x: 0, y: 0, tx: 0, ty: 0 };
  const raycaster = new THREE.Raycaster();
  let hovering = false;

  function pointerFromEvent(event) {
    const rect = renderer.domElement.getBoundingClientRect();
    pointer.set(
      ((event.clientX - rect.left) / rect.width) * 2 - 1,
      -((event.clientY - rect.top) / rect.height) * 2 + 1,
    );
    return rect;
  }

  function pick() {
    raycaster.setFromCamera(pointer, camera);
    const hits = raycaster.intersectObjects(bottles.map((b) => b.group), true);
    if (!hits.length) return -1;
    let node = hits[0].object;
    while (node && !node.userData.bottle) node = node.parent;
    return node ? bottles.indexOf(node.userData.bottle) : -1;
  }

  function onPointerMove(event) {
    pointerFromEvent(event);
    parallax.tx = pointer.x;
    parallax.ty = pointer.y;
    if (solo) return;
    const index = pick();
    const canClick = index !== -1 && index !== order[1];
    if (canClick !== hovering) {
      hovering = canClick;
      container.style.cursor = canClick ? "pointer" : "";
    }
  }

  function onPointerLeave() {
    parallax.tx = 0;
    parallax.ty = 0;
    hovering = false;
    container.style.cursor = "";
  }

  function onClick(event) {
    if (solo) return;
    pointerFromEvent(event);
    const index = pick();
    if (index !== -1) select(index);
  }

  container.addEventListener("pointermove", onPointerMove);
  container.addEventListener("pointerleave", onPointerLeave);
  container.addEventListener("click", onClick);

  // --- sizing --------------------------------------------------------------
  function resize() {
    const { clientWidth: w, clientHeight: h } = container;
    if (!w || !h) return;
    renderer.setSize(w, h, false);
    camera.aspect = w / h;

    const shifted = !solo && camera.aspect > 1.15;

    // Pull the camera back until the bottle fits the height *and* the trio fits
    // the width, whichever is the tighter constraint. Solving it rather than
    // guessing keeps the framing right from phone to ultrawide.
    //
    // A shifted frame eats into the width twice over — the group moves right
    // while the frame stays put — so the width it must fit into grows by twice
    // the shift.
    const halfFov = THREE.MathUtils.degToRad(camera.fov) / 2;
    const forHeight = (BOTTLE_HEIGHT / FILL / 2) / Math.tan(halfFov);
    const usable = shifted ? 1 - FRAME_SHIFT * 2 : 1;
    const forWidth = solo
      ? 0
      : (GROUP_HALF_WIDTH / usable) / (Math.tan(halfFov) * camera.aspect);
    camera.position.z = Math.max(forHeight, forWidth);

    // Fog is what pushes the flanking bottles back into the paper. It has to
    // track the camera: fixed distances would swallow the hero bottle as soon
    // as the camera pulled back for a taller bottle or a narrower window.
    scene.fog.near = camera.position.z - 8;
    scene.fog.far = camera.position.z + 90;

    // Slide the rendered window left so the bottles sit right of centre, leaving
    // the left column to the copy. This shears the frustum rather than moving
    // anything, so the trio stays evenly spaced and square-on to the camera.
    if (shifted) {
      camera.setViewOffset(w, h, -w * FRAME_SHIFT, 0, w, h);
    } else {
      camera.clearViewOffset();
    }

    camera.updateProjectionMatrix();
  }

  const resizeObserver = new ResizeObserver(resize);
  resizeObserver.observe(container);
  resize();

  // --- loop ----------------------------------------------------------------
  const timer = new THREE.Timer();
  let running = false;
  let frame = 0;
  let ready = false;
  const reduced = prefersReducedMotion();

  function tick() {
    if (!running) return;
    frame = requestAnimationFrame(tick);

    timer.update();
    const dt = Math.min(timer.getDelta(), 0.1);
    const time = timer.getElapsed();

    for (const bottle of bottles) {
      const { current, target } = bottle;
      current.x = damp(current.x, target.x, 4, dt);
      current.y = damp(current.y, target.y, 4, dt);
      current.z = damp(current.z, target.z, 4, dt);
      current.scale = damp(current.scale, target.scale, 4, dt);

      // A slow sway rather than a full spin, so the label never turns away.
      const sway = reduced ? 0 : Math.sin(time * 0.42 + bottle.phase) * 0.16;
      const bob = reduced ? 0 : Math.sin(time * 0.7 + bottle.phase) * 0.12;
      current.spin = damp(current.spin, target.spin, 4, dt);

      bottle.group.position.set(current.x, current.y + bob, current.z);
      bottle.group.rotation.y = current.spin + sway * (target.scale === 1 ? 1 : 0.4);
      bottle.group.scale.setScalar(current.scale);
      bottle.shadow.scale.setScalar(1 / Math.max(current.scale, 0.001));
    }

    if (!reduced) {
      parallax.x = damp(parallax.x, parallax.tx, 3, dt);
      parallax.y = damp(parallax.y, parallax.ty, 3, dt);
    }
    camera.position.x = parallax.x * 3.2;
    camera.position.y = 1.2 + parallax.y * 1.6;
    camera.lookAt(0, parallax.y * 0.4, 0);

    renderer.render(scene, camera);

    if (!ready) {
      ready = true;
      onReady?.();
    }
  }

  function start() {
    if (running) return;
    running = true;
    // Swallow the gap since the last frame so a paused stage does not jump.
    timer.update();
    frame = requestAnimationFrame(tick);
  }

  function stop() {
    running = false;
    cancelAnimationFrame(frame);
  }

  // Only burn frames while the stage is actually on screen.
  const visibility = new IntersectionObserver(
    ([entry]) => (entry.isIntersecting ? start() : stop()),
    { rootMargin: "120px" },
  );
  visibility.observe(container);

  document.addEventListener("visibilitychange", () => {
    if (document.hidden) stop();
    else if (container.getBoundingClientRect().bottom > 0) start();
  });

  return {
    select,

    /**
     * Live edit of a bottle, for the admin preview. The label texture is redrawn
     * into the canvas the existing texture already points at, so repeated edits
     * do not leak a texture per keystroke.
     */
    update(index, patch) {
      const bottle = bottles[index];
      if (!bottle) return;

      if (patch.liquid) bottle.setLiquid(patch.liquid);

      if (patch.label) {
        bottle.wine.label = { ...bottle.wine.label, ...patch.label };
        bottle.texture.image = renderLabelCanvas(bottle.wine.label, logo, 1024);
        bottle.texture.needsUpdate = true;
      }
    },

    get current() {
      return solo ? centreBottle : order[1];
    },
    destroy() {
      stop();
      visibility.disconnect();
      resizeObserver.disconnect();
      container.removeEventListener("pointermove", onPointerMove);
      container.removeEventListener("pointerleave", onPointerLeave);
      container.removeEventListener("click", onClick);
      for (const bottle of bottles) {
        bottle.dispose();
        bottle.texture.dispose();
        bottle.shadow.userData.dispose();
      }
      environment.dispose();
      renderer.dispose();
      renderer.domElement.remove();
    },
  };
}
