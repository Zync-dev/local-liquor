/**
 * Everything the bottles sit in: the studio they reflect, the backdrop they
 * refract, the shadow they sit in, and the micro-roughness that stops the glass
 * looking machined.
 *
 * The important idea is the backdrop. Glass has almost no appearance of its
 * own — it is a lens, and what sells it is the distortion of whatever is behind
 * it. With a transparent canvas there is nothing behind it, three.js falls back
 * to the environment map, and the empty neck of the bottle renders as a flat
 * panel of light. Putting a real surface back there is what turns it from a
 * white plastic shape into glass.
 */

import * as THREE from "three";

const PAPER = 0xf7f5f1;

/* --------------------------------------------------------------- studio --- */

/** A soft-edged light panel, so reflections fall off instead of ending in a line. */
function softPanelTexture(inner, outer, falloff = 0.55) {
  const size = 128;
  const canvas = document.createElement("canvas");
  canvas.width = canvas.height = size;
  const ctx = canvas.getContext("2d");

  const gradient = ctx.createRadialGradient(
    size / 2, size / 2, size * 0.05,
    size / 2, size / 2, size * falloff,
  );
  gradient.addColorStop(0, inner);
  gradient.addColorStop(1, outer);
  ctx.fillStyle = gradient;
  ctx.fillRect(0, 0, size, size);

  const texture = new THREE.CanvasTexture(canvas);
  texture.colorSpace = THREE.SRGBColorSpace;
  return texture;
}

/**
 * A small studio, baked into an environment map.
 *
 * Lit the way you would actually photograph glass: a dark surround so the edges
 * of the bottle stay dark and describe its form, with a few soft sources for it
 * to catch. The big bright panel that used to sit behind the bottle is gone —
 * the backdrop plane does that job now, and does it properly, by being a real
 * surface the glass can bend rather than a reflection.
 *
 * @returns {{texture: THREE.Texture, dispose: Function}}
 */
export function createStudioEnvironment(renderer) {
  const pmrem = new THREE.PMREMGenerator(renderer);
  pmrem.compileEquirectangularShader();

  const scene = new THREE.Scene();
  const parts = [];

  const panel = (texture, w, h, position, rotation) => {
    const geometry = new THREE.PlaneGeometry(w, h);
    const material = new THREE.MeshBasicMaterial({ map: texture, side: THREE.DoubleSide });
    const mesh = new THREE.Mesh(geometry, material);
    mesh.position.set(...position);
    if (rotation) mesh.rotation.set(...rotation);
    scene.add(mesh);
    parts.push(geometry, material, texture);
    return mesh;
  };

  // The surround. Dark enough that the bottle has edges.
  const roomGeometry = new THREE.BoxGeometry(24, 16, 24);
  const roomMaterial = new THREE.MeshBasicMaterial({ color: 0x6f6a61, side: THREE.BackSide });
  scene.add(new THREE.Mesh(roomGeometry, roomMaterial));
  parts.push(roomGeometry, roomMaterial);

  // Key softbox, high and to the left: the long highlight down the shoulder.
  panel(softPanelTexture("#fffdf8", "#6f6a61"), 9, 14, [-7.6, 4, 2], [0, Math.PI / 2.6, 0]);
  // Cooler, weaker fill on the right so the far edge separates from the ground.
  panel(softPanelTexture("#cdd6e0", "#6f6a61"), 7, 12, [7.6, 2, -0.5], [0, -Math.PI / 2.4, 0]);
  // Ceiling bounce.
  panel(softPanelTexture("#e6e0d6", "#6f6a61", 0.7), 14, 12, [0, 7.9, 0], [Math.PI / 2, 0, 0]);
  // Dark floor, for the heel of the bottle to sit against.
  panel(softPanelTexture("#3a352e", "#22201c", 0.8), 20, 20, [0, -7.9, 0], [-Math.PI / 2, 0, 0]);
  // Narrow strips in front: the crisp vertical glints on neck and shoulder.
  panel(softPanelTexture("#ffffff", "#8a8479", 0.9), 0.9, 13, [-3.4, 3, 8.5]);
  panel(softPanelTexture("#ffffff", "#8a8479", 0.9), 0.5, 10, [4.4, 2, 8.5]);
  // A dim warm bounce from behind, so the glass is not dead at the back.
  panel(softPanelTexture("#8d8375", "#5c574f", 0.8), 16, 12, [0, 1, -9]);

  const target = pmrem.fromScene(scene, 0.025);
  pmrem.dispose();
  for (const part of parts) part.dispose();

  return {
    texture: target.texture,
    dispose() {
      target.dispose();
    },
  };
}

/* ------------------------------------------------------------- backdrop --- */

const PAPER_CSS = "#f7f5f1";

/**
 * The surface behind the bottles — opaque, so the glass has something real to
 * refract rather than falling back to the environment map.
 *
 * Painted into a 2D canvas and used as a plain texture rather than drawn by a
 * custom shader, and that is the whole point. A ShaderMaterial has to convert
 * its own linear colour to sRGB via `#include <colorspace_fragment>`, and that
 * conversion does not survive this scene: the backdrop is rendered twice a
 * frame, once into the linear transmission buffer and once to the canvas, and
 * the program compiled for the first was reused for the second. The result was
 * linear values written straight to an sRGB canvas — #faf7f2 came out #f4ede2,
 * and since the page around it really is #faf7f2, the canvas showed up as a
 * rectangle sitting on the page. A built-in material gets this right in both
 * passes without being asked.
 *
 * Paper all the way to the edge: the halo is well inside the texture, so no
 * amount of camera parallax can bring a seam into frame. Nothing here follows
 * the wine — the manual allows the accent in five places, and the ground behind
 * the bottle is not one of them.
 *
 * @returns {{mesh: THREE.Mesh, fit: Function, dispose: Function}}
 */
export function createBackdrop() {
  const size = 512;
  const canvas = document.createElement("canvas");
  canvas.width = canvas.height = size;
  const ctx = canvas.getContext("2d");

  function paint() {
    ctx.fillStyle = PAPER_CSS;
    ctx.fillRect(0, 0, size, size);

    // A breath of light where the bottle stands, so the paper is not dead flat
    // and the glass has a gradient to bend. Neutral on purpose: the manual
    // allows the accent in five places and a wash behind the bottle is not one
    // of them.
    const halo = ctx.createRadialGradient(
      size / 2, size * 0.46, 0,
      size / 2, size * 0.46, size * 0.32,
    );
    halo.addColorStop(0, "rgba(255,255,255,0.85)");
    halo.addColorStop(0.55, "rgba(255,255,255,0.35)");
    halo.addColorStop(1, "rgba(255,255,255,0)");
    ctx.fillStyle = halo;
    ctx.fillRect(0, 0, size, size);

    // The ground the bottles stand on. Refraction can only be seen where there
    // is something behind the glass to bend; an unbroken wash gives it nothing.
    ctx.save();
    ctx.translate(size / 2, size * 0.74);
    ctx.scale(1.7, 1);
    const ground = ctx.createRadialGradient(0, 0, 0, 0, 0, size * 0.15);
    ground.addColorStop(0, "rgba(120, 104, 86, 0.13)");
    ground.addColorStop(0.6, "rgba(120, 104, 86, 0.05)");
    ground.addColorStop(1, "rgba(120, 104, 86, 0)");
    ctx.fillStyle = ground;
    ctx.fillRect(-size, -size, size * 2, size * 2);
    ctx.restore();
  }

  paint();

  const texture = new THREE.CanvasTexture(canvas);
  texture.colorSpace = THREE.SRGBColorSpace;

  const geometry = new THREE.PlaneGeometry(1, 1);
  const material = new THREE.MeshBasicMaterial({
    map: texture,
    toneMapped: false,
    fog: false,
    depthWrite: false,
  });

  const mesh = new THREE.Mesh(geometry, material);
  mesh.renderOrder = -10;
  mesh.position.z = -55;

  return {
    mesh,

    /** Sized to fill the frustum at the plane's depth, with room to spare. */
    fit(camera) {
      const margin = 1.6;
      const distance = camera.position.z - mesh.position.z;
      const height = 2 * Math.tan(THREE.MathUtils.degToRad(camera.fov) / 2) * distance;
      mesh.scale.set(height * camera.aspect * margin, height * margin, 1);
    },

    dispose() {
      geometry.dispose();
      material.dispose();
      texture.dispose();
    },
  };
}

/* --------------------------------------------------------------- shadow --- */

function radialTexture(stops, size = 256) {
  const canvas = document.createElement("canvas");
  canvas.width = canvas.height = size;
  const ctx = canvas.getContext("2d");
  const gradient = ctx.createRadialGradient(size / 2, size / 2, 0, size / 2, size / 2, size / 2);
  for (const [offset, colour] of stops) gradient.addColorStop(offset, colour);
  ctx.fillStyle = gradient;
  ctx.fillRect(0, 0, size, size);

  const texture = new THREE.CanvasTexture(canvas);
  texture.colorSpace = THREE.SRGBColorSpace;
  return texture;
}

/**
 * The soft pool the bottle stands in.
 *
 * Painted rather than ray-traced: real shadow maps go harsh and noisy through
 * transmissive glass. It is kept shallow front-to-back on purpose — the camera
 * sits almost level with the base, so a deep ground plane is seen nearly
 * edge-on and its far half smears upward behind the bottle instead of lying
 * flat. That is also why there is no coloured caustic here: at this camera
 * height it reads as a stain climbing the glass rather than light on a surface.
 *
 * @returns {{group: THREE.Group, setTint: Function, dispose: Function}}
 */
export function createContactShadow(width = 11, depth = 3.4) {
  const group = new THREE.Group();
  const parts = [];

  const layer = (texture, w, d, blending) => {
    const geometry = new THREE.PlaneGeometry(w, d);
    const material = new THREE.MeshBasicMaterial({
      map: texture,
      transparent: true,
      depthWrite: false,
      toneMapped: false,
      blending,
    });
    const mesh = new THREE.Mesh(geometry, material);
    mesh.rotation.x = -Math.PI / 2;
    group.add(mesh);
    parts.push(geometry, material, texture);
    return { mesh, material };
  };

  const shadow = layer(radialTexture([
    [0, "rgba(26, 22, 18, 0.46)"],
    [0.34, "rgba(26, 22, 18, 0.22)"],
    [0.7, "rgba(26, 22, 18, 0.05)"],
    [1, "rgba(26, 22, 18, 0)"],
  ]), width, depth, THREE.NormalBlending);
  shadow.mesh.renderOrder = -3;

  return {
    group,

    /**
     * Tints the pool very slightly towards the wine. Enough to warm the ground
     * under a red and cool it under a blue; not enough to read as a colour.
     */
    setTint(hex) {
      shadow.material.color.set(hex).lerp(new THREE.Color(0xffffff), 0.72);
    },

    dispose() {
      for (const part of parts) part.dispose();
    },
  };
}

/* ------------------------------------------------------------ roughness --- */

/**
 * Faint, large-scale variation in how polished the glass is.
 *
 * Perfectly uniform roughness is one of the loudest tells that something was
 * rendered rather than photographed: real bottles have wipe marks, mould lines
 * and handling. This only has to be strong enough to break the highlights up.
 */
export function createGlassRoughnessMap() {
  const size = 256;
  const canvas = document.createElement("canvas");
  canvas.width = canvas.height = size;
  const ctx = canvas.getContext("2d");

  ctx.fillStyle = "#3c3c3c";
  ctx.fillRect(0, 0, size, size);

  // Soft blotches, drawn large so they read as sheen rather than as noise.
  for (let i = 0; i < 90; i++) {
    const x = Math.random() * size;
    const y = Math.random() * size;
    const r = 12 + Math.random() * 46;
    const bright = Math.random() > 0.5;
    const gradient = ctx.createRadialGradient(x, y, 0, x, y, r);
    gradient.addColorStop(0, bright ? "rgba(255,255,255,0.09)" : "rgba(0,0,0,0.09)");
    gradient.addColorStop(1, "rgba(0,0,0,0)");
    ctx.fillStyle = gradient;
    ctx.fillRect(x - r, y - r, r * 2, r * 2);
  }

  // A couple of faint vertical seams, as a moulded bottle has.
  ctx.fillStyle = "rgba(255,255,255,0.07)";
  ctx.fillRect(size * 0.24, 0, 2, size);
  ctx.fillRect(size * 0.74, 0, 2, size);

  const texture = new THREE.CanvasTexture(canvas);
  texture.wrapS = texture.wrapT = THREE.RepeatWrapping;
  return texture;
}
