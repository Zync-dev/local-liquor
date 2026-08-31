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

const PAPER = 0xfaf7f2;

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

const BACKDROP_VERTEX = /* glsl */ `
  varying vec2 vUv;
  void main() {
    vUv = uv;
    gl_Position = projectionMatrix * modelViewMatrix * vec4(position, 1.0);
  }
`;

const BACKDROP_FRAGMENT = /* glsl */ `
  varying vec2 vUv;
  uniform vec3 uPaper;
  uniform vec3 uTint;
  uniform float uEdge;

  void main() {
    float d = length(vUv - 0.5);

    // The wash has to be back to exactly uPaper by the time it reaches the edge
    // of the frame, or the canvas would meet the page on a slightly different
    // colour and the join would show as a rectangle. uEdge is where that is.
    float core = smoothstep(uEdge * 0.62, 0.0, d);
    float wash = smoothstep(uEdge, uEdge * 0.08, d);
    vec3 colour = mix(uPaper, uTint, core * 0.8 + wash * 0.28);

    // The floor sits a shade deeper than the air above it.
    colour *= 1.0 - smoothstep(0.42, 0.0, vUv.y) * 0.05;

    gl_FragColor = vec4(colour, 1.0);
    #include <colorspace_fragment>
  }
`;

/**
 * The surface behind the bottles. Opaque, so the glass has something real to
 * refract, and deliberately not tone mapped — its outer colour has to land on
 * exactly the page's paper or the edge of the canvas would show as a seam.
 *
 * @returns {{mesh: THREE.Mesh, setTint: Function, fit: Function, dispose: Function}}
 */
export function createBackdrop() {
  const uniforms = {
    uPaper: { value: new THREE.Color(PAPER).convertSRGBToLinear() },
    uTint: { value: new THREE.Color(0xfdf0e2).convertSRGBToLinear() },
    uEdge: { value: 0.33 },
  };

  const geometry = new THREE.PlaneGeometry(1, 1);
  const material = new THREE.ShaderMaterial({
    uniforms,
    vertexShader: BACKDROP_VERTEX,
    fragmentShader: BACKDROP_FRAGMENT,
    toneMapped: false,
    fog: false,
    depthWrite: false,
  });

  const mesh = new THREE.Mesh(geometry, material);
  mesh.renderOrder = -10;
  mesh.position.z = -55;

  const target = new THREE.Color();

  return {
    mesh,

    /** Eases towards the selected wine's tint rather than snapping to it. */
    setTint(hex) {
      target.set(hex).convertSRGBToLinear();
    },

    update(dt) {
      uniforms.uTint.value.lerp(target, Math.min(dt * 2.2, 1));
    },

    /** Sized to fill the frustum at the plane's depth, with room for parallax. */
    fit(camera) {
      const margin = 1.5;
      const distance = camera.position.z - mesh.position.z;
      const height = 2 * Math.tan(THREE.MathUtils.degToRad(camera.fov) / 2) * distance;
      const width = height * camera.aspect;
      mesh.scale.set(width * margin, height * margin, 1);
      // Where the visible frame falls on the plane, in UV distance from centre.
      uniforms.uEdge.value = 0.5 / margin;
    },

    dispose() {
      geometry.dispose();
      material.dispose();
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

  ctx.fillStyle = "#2a2a2a";
  ctx.fillRect(0, 0, size, size);

  // Soft blotches, drawn large so they read as sheen rather than as noise.
  for (let i = 0; i < 90; i++) {
    const x = Math.random() * size;
    const y = Math.random() * size;
    const r = 12 + Math.random() * 46;
    const bright = Math.random() > 0.5;
    const gradient = ctx.createRadialGradient(x, y, 0, x, y, r);
    gradient.addColorStop(0, bright ? "rgba(255,255,255,0.16)" : "rgba(0,0,0,0.16)");
    gradient.addColorStop(1, "rgba(0,0,0,0)");
    ctx.fillStyle = gradient;
    ctx.fillRect(x - r, y - r, r * 2, r * 2);
  }

  // A couple of faint vertical seams, as a moulded bottle has.
  ctx.fillStyle = "rgba(255,255,255,0.1)";
  ctx.fillRect(size * 0.24, 0, 2, size);
  ctx.fillRect(size * 0.74, 0, 2, size);

  const texture = new THREE.CanvasTexture(canvas);
  texture.wrapS = texture.wrapT = THREE.RepeatWrapping;
  return texture;
}
