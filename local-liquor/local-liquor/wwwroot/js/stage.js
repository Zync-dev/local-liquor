/**
 * The WebGL stage: one bottle centre-forward, the others held back and to the
 * sides. Clicking a flanking bottle brings it in and sends the current one out
 * to the slot it came from.
 *
 * Movement between slots is tweened rather than damped. Damping is fine for
 * settling towards a target but it cannot be choreographed — and the swap wants
 * choreography: the arriving bottle swings forward past the one it is replacing,
 * both lean into the turn, the wine rocks a beat behind the glass, and the
 * backdrop warms towards the new colour. Interrupting is still free: a click
 * mid-flight just restarts the tween from wherever things currently are.
 */

import * as THREE from "three";
import { createBottle, BOTTLE_HEIGHT } from "./bottle.js";
import {
  createStudioEnvironment,
  createBackdrop,
  createContactShadow,
} from "./environment.js";
import { renderLabelCanvas } from "./label.js";

const PAPER = 0xf7f5f1;

/**
 * Where the bottles stand, built for however many there are. One is centred;
 * the rest fan out behind it, alternating left and right so the group stays
 * balanced. The range went from three to two in the 2026 redesign and the
 * manual is explicit that more fruits are expected, so this is derived rather
 * than written out.
 */
function buildSlots(count) {
  const centre = { x: 0, y: 0, z: 0, scale: 1, spin: 0 };
  if (count <= 1) return [centre];

  const slots = [centre];
  const step = count === 2 ? 12 : 13;
  for (let i = 1; i < count; i++) {
    const rank = Math.ceil(i / 2);
    const sideways = i % 2 === 1 ? -1 : 1;
    slots.push({
      x: sideways * step * rank,
      y: -1.4,
      z: -18,
      scale: 0.78,
      spin: sideways * -0.34,
    });
  }

  // Centre stage is the middle of the row, not the head of the list.
  slots.sort((a, b) => a.x - b.x);
  return slots;
}

/** Half the horizontal room the group needs, in world units. */
function groupHalfWidth(slots) {
  return Math.max(...slots.map((s) => Math.abs(s.x))) + 4;
}

/**
 * How far off centre the bottles sit on a wide screen, as a fraction of the
 * frame width, so the copy gets the other column to itself. Positive moves them
 * right (the hero); the product page passes a negative one.
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

const SWAP_SECONDS = 1.15;
const INTRO_SECONDS = 1.6;

const prefersReducedMotion = () =>
  window.matchMedia("(prefers-reduced-motion: reduce)").matches;

const easeInOut = (t) => (t < 0.5 ? 4 * t * t * t : 1 - Math.pow(-2 * t + 2, 3) / 2);
const easeOut = (t) => 1 - Math.pow(1 - t, 3);

/** Rises to 1 in the middle of a transition and back to 0 — the arc of a swing. */
const hump = (t) => Math.sin(Math.PI * THREE.MathUtils.clamp(t, 0, 1));

/**
 * @param {HTMLElement} container
 * @param {{wines: Array, initialIndex: number, solo: boolean, shift: number,
 *          onReady: Function, onSelect: Function}} options
 */
export function createStage(container, options) {
  const { wines, solo = false, onReady, onSelect } = options;
  const shift = options.shift ?? FRAME_SHIFT;
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

  const reduced = prefersReducedMotion();
  const SLOTS = buildSlots(wines.length);
  const GROUP_HALF_WIDTH = groupHalfWidth(SLOTS);
  const CENTRE_SLOT = Math.floor(SLOTS.length / 2);

  // Supersample on low-DPI screens. The label ends up roughly 120 physical
  // pixels wide from a 1024px texture — an eight-fold minification — so the GPU
  // samples a heavily blurred mip and the printed edges, which should be as
  // crisp as cut paper, go soft. Nothing about the material fixes that; drawing
  // more pixels is the only thing that does. If the machine cannot hold it,
  // degrade() below drops straight back to 1:1.
  renderer.setPixelRatio(THREE.MathUtils.clamp(window.devicePixelRatio, 1.5, 2));
  renderer.toneMapping = THREE.NeutralToneMapping ?? THREE.ACESFilmicToneMapping;
  renderer.toneMappingExposure = 1.0;
  // The transmission buffer is re-rendered every frame and dispersion samples it
  // three times over. At full resolution on a retina display that is several
  // megapixels of overdraw per frame and the page drops to a crawl; half is the
  // most this can afford, and on smooth glass the difference is hard to see.
  if ("transmissionResolutionScale" in renderer) renderer.transmissionResolutionScale = 0.5;
  container.appendChild(renderer.domElement);

  const scene = new THREE.Scene();
  // Range is set in resize(), once we know how far back the camera sits.
  scene.fog = new THREE.Fog(PAPER, 1, 2);

  const environment = createStudioEnvironment(renderer);
  scene.environment = environment.texture;
  scene.environmentIntensity = 1.0;

  const backdrop = createBackdrop();
  scene.add(backdrop.mesh);

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
  const bottles = wines.map((wine, index) => {
    const label = (side) => {
      const t = new THREE.CanvasTexture(renderLabelCanvas(wine.label, 1024, side));
      t.colorSpace = THREE.SRGBColorSpace;
      t.anisotropy = maxAnisotropy;
      t.needsUpdate = true;
      return t;
    };

    const texture = label("front");
    const backTexture = label("back");

    const bottle = createBottle({
      liquid: wine.liquid,
      labelTexture: texture,
      backTexture,
    });
    bottle.texture = texture;
    bottle.backTexture = backTexture;
    bottle.wine = wine;
    bottle.group.userData.bottle = bottle;

    const shadow = createContactShadow();
    shadow.setTint(wine.liquid);
    shadow.group.position.y = -BOTTLE_HEIGHT / 2 + 0.05;
    bottle.group.add(shadow.group);
    bottle.shadow = shadow;

    // Tween state. `from` and `to` are slot transforms; `t` walks between them.
    bottle.from = { ...SOLO_SLOT };
    bottle.to = { ...SOLO_SLOT };
    bottle.at = { ...SOLO_SLOT };
    bottle.t = 1;
    bottle.arc = 0;
    bottle.lean = 0;
    bottle.slosh = 0;
    bottle.sloshVelocity = 0;
    bottle.lastX = null;
    bottle.turn = 0;          // how far the visitor has spun this one
    bottle.turnVelocity = 0;
    bottle.phase = (index / wines.length) * Math.PI * 2;
    bottle.introDelay = index * 0.14;

    scene.add(bottle.group);
    return bottle;
  });

  // --- slot bookkeeping ----------------------------------------------------
  // `order` maps slot index -> bottle index. Centre is slot 1.
  let order = solo ? [options.initialIndex ?? 0] : bottles.map((_, i) => i);
  const centreBottle = solo ? (options.initialIndex ?? 0) : CENTRE_SLOT;

  function slotFor(bottleIndex) {
    if (solo) return SOLO_SLOT;
    const slot = order.indexOf(bottleIndex);
    return SLOTS[slot === -1 ? CENTRE_SLOT : slot];
  }

  /** Points every bottle at its slot, tweening from wherever it currently is. */
  function retarget(animate = true) {
    for (const [index, bottle] of bottles.entries()) {
      if (solo) bottle.group.visible = index === centreBottle;

      const target = slotFor(index);
      bottle.from = { ...bottle.at };
      bottle.to = { ...target };
      bottle.t = animate && !reduced ? 0 : 1;

      // Bottles moving towards the viewer swing forward, ones being replaced
      // swing back, so the two visibly pass each other rather than sliding
      // through the same plane.
      bottle.arc = Math.sign(bottle.to.z - bottle.from.z || 0) * -6;
    }
  }

  function select(wineIndex) {
    if (solo || wineIndex === order[CENTRE_SLOT]) return;
    const slot = order.indexOf(wineIndex);
    if (slot === -1) return;

    [order[CENTRE_SLOT], order[slot]] = [order[slot], order[CENTRE_SLOT]];
    retarget();

    onSelect?.(order[CENTRE_SLOT]);
  }

  if (!solo) {
    const start = options.initialIndex ?? CENTRE_SLOT;
    const slot = order.indexOf(start);
    if (slot !== -1) [order[CENTRE_SLOT], order[slot]] = [order[slot], order[CENTRE_SLOT]];
  }

  // Compose the first frame at rest, then let the intro lift it in.
  retarget(false);
  for (const bottle of bottles) bottle.at = { ...bottle.to };

  // --- pointer -------------------------------------------------------------
  const pointer = new THREE.Vector2();
  const parallax = { x: 0, y: 0, tx: 0, ty: 0 };
  const raycaster = new THREE.Raycaster();
  let hovering = "";

  // Dragging turns the bottle in front; a press that barely moves is a click.
  // Both start the same way, so the two are told apart by distance travelled
  // rather than by which handler fires first.
  const CLICK_SLOP = 6;
  const drag = { id: null, lastX: 0, travelled: 0, turning: false };

  function centreOf() {
    return bottles[solo ? centreBottle : order[CENTRE_SLOT]];
  }

  function pointerFromEvent(event) {
    const rect = renderer.domElement.getBoundingClientRect();
    pointer.set(
      ((event.clientX - rect.left) / rect.width) * 2 - 1,
      -((event.clientY - rect.top) / rect.height) * 2 + 1,
    );
  }

  function pick() {
    raycaster.setFromCamera(pointer, camera);
    const hits = raycaster.intersectObjects(bottles.map((b) => b.group), true);
    if (!hits.length) return -1;
    let node = hits[0].object;
    while (node && !node.userData.bottle) node = node.parent;
    return node ? bottles.indexOf(node.userData.bottle) : -1;
  }

  function onPointerDown(event) {
    pointerFromEvent(event);
    drag.id = event.pointerId;
    drag.lastX = event.clientX;
    drag.travelled = 0;
    drag.turning = false;
    container.setPointerCapture?.(event.pointerId);
  }

  function onPointerMove(event) {
    pointerFromEvent(event);

    if (drag.id === event.pointerId) {
      const dx = event.clientX - drag.lastX;
      drag.lastX = event.clientX;
      drag.travelled += Math.abs(dx);

      if (drag.travelled > CLICK_SLOP) {
        drag.turning = true;
        container.style.cursor = "grabbing";
        const bottle = centreOf();
        // A drag the width of the stage is a little over half a revolution,
        // which is enough to bring the back label round without feeling twitchy.
        const radians = (dx / container.clientWidth) * Math.PI * 1.6;
        bottle.turn += radians;
        bottle.turnVelocity = radians / Math.max(clockDelta, 0.008);
      }
      return;
    }

    parallax.tx = pointer.x;
    parallax.ty = pointer.y;

    const index = pick();
    const centre = solo ? centreBottle : order[CENTRE_SLOT];
    const cursor = index === -1 ? "" : index === centre ? "grab" : "pointer";
    if (cursor !== hovering) {
      hovering = cursor;
      container.style.cursor = cursor;
    }
  }

  function onPointerUp(event) {
    if (drag.id !== event.pointerId) return;
    container.releasePointerCapture?.(event.pointerId);
    drag.id = null;
    container.style.cursor = hovering || "";
    // A press that never really moved was a click on a flanking bottle.
    if (!drag.turning && !solo) {
      const index = pick();
      if (index !== -1) select(index);
    }
    drag.turning = false;
  }

  function onPointerLeave() {
    parallax.tx = 0;
    parallax.ty = 0;
    hovering = "";
    container.style.cursor = "";
  }

  container.addEventListener("pointerdown", onPointerDown);
  container.addEventListener("pointermove", onPointerMove);
  container.addEventListener("pointerup", onPointerUp);
  container.addEventListener("pointercancel", onPointerUp);
  container.addEventListener("pointerleave", onPointerLeave);

  // --- scroll --------------------------------------------------------------
  // How far the stage has travelled up the viewport, 0 at rest and 1 once it
  // has left. Drives a gentle drift so the hero has somewhere to go.
  let scrolled = 0;
  function onScroll() {
    const rect = container.getBoundingClientRect();
    const travel = Math.max(rect.height, 1);
    scrolled = THREE.MathUtils.clamp(-rect.top / travel, 0, 1);
  }
  window.addEventListener("scroll", onScroll, { passive: true });
  onScroll();

  // --- sizing --------------------------------------------------------------
  function resize() {
    const { clientWidth: w, clientHeight: h } = container;
    if (!w || !h) return;
    renderer.setSize(w, h, false);
    camera.aspect = w / h;

    const shifted = shift !== 0 && camera.aspect > 1.15;

    // Pull the camera back until the bottle fits the height *and* the trio fits
    // the width, whichever is the tighter constraint. Solving it rather than
    // guessing keeps the framing right from phone to ultrawide.
    //
    // A shifted frame eats into the width twice over — the group moves right
    // while the frame stays put — so the width it must fit into grows by twice
    // the shift.
    const halfFov = THREE.MathUtils.degToRad(camera.fov) / 2;
    const forHeight = (BOTTLE_HEIGHT / FILL / 2) / Math.tan(halfFov);
    const usable = shifted ? 1 - Math.abs(shift) * 2 : 1;
    const forWidth = solo
      ? 0
      : (GROUP_HALF_WIDTH / usable) / (Math.tan(halfFov) * camera.aspect);
    camera.position.z = Math.max(forHeight, forWidth);

    // Fog is what pushes the flanking bottles back into the backdrop. It has to
    // track the camera: fixed distances would swallow the hero bottle as soon
    // as the camera pulled back for a taller bottle or a narrower window.
    scene.fog.near = camera.position.z - 8;
    scene.fog.far = camera.position.z + 90;

    // Slide the rendered window left so the bottles sit right of centre, leaving
    // the left column to the copy. This shears the frustum rather than moving
    // anything, so the trio stays evenly spaced and square-on to the camera.
    if (shifted) {
      camera.setViewOffset(w, h, -w * shift, 0, w, h);
    } else {
      camera.clearViewOffset();
    }

    camera.updateProjectionMatrix();
    backdrop.fit(camera);
  }

  const resizeObserver = new ResizeObserver(resize);
  resizeObserver.observe(container);
  resize();

  // --- loop ----------------------------------------------------------------
  const timer = new THREE.Timer();
  // Last frame's delta, so a drag can turn pointer movement into a velocity.
  let clockDelta = 1 / 60;
  let running = false;
  let frame = 0;
  let ready = false;
  let intro = reduced ? 1 : 0;

  // --- adaptive quality ----------------------------------------------------
  // Refractive glass is the most expensive thing on this page by a wide margin,
  // and how expensive depends entirely on the visitor's GPU. Rather than pick a
  // setting that is either ugly everywhere or unusable on a laptop, start at the
  // good one, watch the first second of frames, and step down once if the
  // machine cannot hold a reasonable rate.
  let sampled = 0;
  let sampledTime = 0;
  let degraded = false;

  function degrade() {
    degraded = true;
    renderer.setPixelRatio(1);
    if ("transmissionResolutionScale" in renderer) renderer.transmissionResolutionScale = 0.35;
    for (const bottle of bottles) bottle.setQuality(false);
    resize();
  }

  let sampledFrames = 0;

  function sampleFrame(dt) {
    if (degraded || sampled > 140) return;
    // Ignore the first handful: shader compilation and texture upload land there.
    if (++sampled < 20) return;
    // And ignore outliers — a tab switch or a GC pause is not the GPU's fault.
    if (dt > 0.08) return;

    sampledTime += dt;
    sampledFrames++;
    if (sampledFrames >= 45 && sampledTime / sampledFrames > 0.034) degrade();
  }

  function tick() {
    if (!running) return;
    frame = requestAnimationFrame(tick);

    timer.update();
    const dt = Math.min(timer.getDelta(), 0.1);
    clockDelta = dt;
    const time = timer.getElapsed();

    sampleFrame(dt);
    intro = Math.min(intro + dt / INTRO_SECONDS, 1);

    for (const bottle of bottles) {
      // --- slot tween ------------------------------------------------------
      if (bottle.t < 1) {
        bottle.t = Math.min(bottle.t + dt / SWAP_SECONDS, 1);
      }
      const e = easeInOut(bottle.t);
      const swing = hump(bottle.t);

      const at = bottle.at;
      at.x = THREE.MathUtils.lerp(bottle.from.x, bottle.to.x, e);
      at.y = THREE.MathUtils.lerp(bottle.from.y, bottle.to.y, e);
      at.z = THREE.MathUtils.lerp(bottle.from.z, bottle.to.z, e) + swing * bottle.arc;
      at.scale = THREE.MathUtils.lerp(bottle.from.scale, bottle.to.scale, e);
      at.spin = THREE.MathUtils.lerp(bottle.from.spin, bottle.to.spin, e);

      // --- the wine lags the glass ----------------------------------------
      // Lateral acceleration drives a damped spring; the surface rocks a beat
      // behind the bottle and keeps rocking after it has stopped.
      const vx = bottle.lastX === null
        ? 0
        : THREE.MathUtils.clamp((at.x - bottle.lastX) / Math.max(dt, 0.0001), -60, 60);
      bottle.lastX = at.x;
      if (!reduced) {
        bottle.sloshVelocity += (-bottle.slosh * 26 - bottle.sloshVelocity * 3.4 + vx * 0.02) * dt;
        bottle.slosh = THREE.MathUtils.clamp(bottle.slosh + bottle.sloshVelocity * dt, -0.05, 0.05);
        bottle.setSlosh(bottle.slosh);
      }

      // Lean into the turn, like something being carried at speed.
      const leanTarget = THREE.MathUtils.clamp(vx * 0.0022, -0.09, 0.09);
      bottle.lean = THREE.MathUtils.damp(bottle.lean, leanTarget, 6, dt);

      // --- turning ---------------------------------------------------------
      const isCentre = bottle.to.scale === 1;

      if (!isCentre) {
        // Only the bottle in front holds the angle the visitor left it at; the
        // others ease back to facing front so they are presentable on arrival.
        bottle.turn = THREE.MathUtils.damp(bottle.turn, 0, 3, dt);
        bottle.turnVelocity = 0;
      } else if (drag.id === null && bottle.turnVelocity !== 0) {
        bottle.turn += bottle.turnVelocity * dt;
        bottle.turnVelocity *= Math.exp(-3.2 * dt);
        if (Math.abs(bottle.turnVelocity) < 0.02) bottle.turnVelocity = 0;
      }

      // --- idle life -------------------------------------------------------
      // The sway is there to keep an untouched bottle alive. Once someone has
      // taken hold of it, it stops fighting them for the angle.
      const settled = Math.exp(-Math.abs(bottle.turn) * 3);
      const sway = reduced
        ? 0
        : Math.sin(time * 0.42 + bottle.phase) * (isCentre ? 0.16 : 0.07) * settled;
      const bob = reduced ? 0 : Math.sin(time * 0.7 + bottle.phase) * 0.12;

      // --- intro -----------------------------------------------------------
      const lift = reduced
        ? 0
        : (1 - easeOut(THREE.MathUtils.clamp((intro - bottle.introDelay) / (1 - bottle.introDelay), 0, 1))) * -9;

      // --- scroll drift ----------------------------------------------------
      const drift = scrolled * scrolled;

      bottle.group.position.set(
        at.x,
        at.y + bob + lift + drift * 3.5,
        at.z - drift * 8,
      );
      bottle.group.rotation.set(0, at.spin + sway + bottle.turn, bottle.lean);
      bottle.group.scale.setScalar(at.scale * (1 - drift * 0.06));
      bottle.shadow.group.scale.setScalar(1 / Math.max(at.scale, 0.001));
    }

    // --- camera ------------------------------------------------------------
    if (!reduced) {
      parallax.x = THREE.MathUtils.damp(parallax.x, parallax.tx, 3, dt);
      parallax.y = THREE.MathUtils.damp(parallax.y, parallax.ty, 3, dt);

      // The key light drifts with the pointer, so the highlight travels down
      // the glass as you move — the single cheapest thing that makes a still
      // render feel like a lit object.
      key.position.set(-8 + parallax.x * 7, 14 + parallax.y * 4, 12);
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

  const onVisibilityChange = () => {
    if (document.hidden) stop();
    else if (container.getBoundingClientRect().bottom > 0) start();
  };
  document.addEventListener("visibilitychange", onVisibilityChange);

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

      if (patch.liquid) {
        bottle.setLiquid(patch.liquid);
        bottle.shadow.setTint(patch.liquid);
      }

      if (patch.label) {
        bottle.wine.label = { ...bottle.wine.label, ...patch.label };
        bottle.texture.image = renderLabelCanvas(bottle.wine.label, 1024, "front");
        bottle.backTexture.image = renderLabelCanvas(bottle.wine.label, 1024, "back");
        bottle.backTexture.needsUpdate = true;
        bottle.texture.needsUpdate = true;
      }
    },

    get current() {
      return solo ? centreBottle : order[CENTRE_SLOT];
    },

    destroy() {
      stop();
      visibility.disconnect();
      resizeObserver.disconnect();
      document.removeEventListener("visibilitychange", onVisibilityChange);
      window.removeEventListener("scroll", onScroll);
      container.removeEventListener("pointerdown", onPointerDown);
      container.removeEventListener("pointermove", onPointerMove);
      container.removeEventListener("pointerup", onPointerUp);
      container.removeEventListener("pointercancel", onPointerUp);
      container.removeEventListener("pointerleave", onPointerLeave);
      for (const bottle of bottles) {
        bottle.dispose();
        bottle.texture.dispose();
        bottle.backTexture.dispose();
        bottle.shadow.dispose();
      }
      backdrop.dispose();
      environment.dispose();
      renderer.dispose();
      renderer.domElement.remove();
    },
  };
}
