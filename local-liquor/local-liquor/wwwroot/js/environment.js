/**
 * A small studio built out of flat colour, baked into an environment map.
 *
 * Glass is almost entirely reflection, so the bottle only reads if there is
 * something with contrast for it to reflect. This is lit the way you would
 * actually photograph glass: a big bright panel *behind* the bottle so the body
 * glows through, and a deliberately darker surround so the edges stay dark and
 * describe the form. A uniformly bright room mirrors white from every angle and
 * the bottle comes out looking like painted plastic.
 */

import * as THREE from "three";

/** @returns {{texture: THREE.Texture, dispose: Function}} */
export function createStudioEnvironment(renderer) {
  const pmrem = new THREE.PMREMGenerator(renderer);
  pmrem.compileEquirectangularShader();

  const scene = new THREE.Scene();
  const parts = [];

  const panel = (color, w, h, position, rotation) => {
    const geometry = new THREE.PlaneGeometry(w, h);
    const material = new THREE.MeshBasicMaterial({ color, side: THREE.DoubleSide });
    const mesh = new THREE.Mesh(geometry, material);
    mesh.position.set(...position);
    if (rotation) mesh.rotation.set(...rotation);
    scene.add(mesh);
    parts.push(geometry, material);
    return mesh;
  };

  // the surround: mid warm grey, not paper white — this is what gives the
  // bottle its dark edges
  const roomGeometry = new THREE.BoxGeometry(24, 16, 24);
  const roomMaterial = new THREE.MeshBasicMaterial({ color: 0x8f887c, side: THREE.BackSide });
  scene.add(new THREE.Mesh(roomGeometry, roomMaterial));
  parts.push(roomGeometry, roomMaterial);

  // the bright field behind the bottle: the wine glows through this
  panel(0xffffff, 17, 15, [0, 1, -9], [0, 0, 0]);
  // key softbox, high and to the left — the long highlight down the glass
  panel(0xfff8ef, 8, 13, [-7.6, 4, 2], [0, Math.PI / 2.6, 0]);
  // cooler, weaker fill on the right so the far edge separates
  panel(0xbcc6d0, 6, 11, [7.6, 2, -0.5], [0, -Math.PI / 2.4, 0]);
  // ceiling bounce
  panel(0xd6d0c6, 13, 11, [0, 7.9, 0], [Math.PI / 2, 0, 0]);
  // a dark floor for the heel of the bottle to sit against
  panel(0x4a453d, 20, 20, [0, -7.9, 0], [-Math.PI / 2, 0, 0]);
  // narrow strip lights in front, which become the crisp vertical glints
  panel(0xffffff, 0.8, 12, [-3.4, 3, 8.5], [0, 0, 0]);
  panel(0xffffff, 0.45, 10, [4.4, 2, 8.5], [0, 0, 0]);

  const target = pmrem.fromScene(scene, 0.02);
  pmrem.dispose();
  for (const part of parts) part.dispose();

  return {
    texture: target.texture,
    dispose() {
      target.dispose();
    },
  };
}

/**
 * A soft elliptical shadow to drop under a bottle. Real shadow maps look harsh
 * and noisy through transmissive glass; a painted blob is both cheaper and
 * closer to how a bottle actually sits on a bright surface.
 */
export function createContactShadow(width = 11, depth = 5.5) {
  const size = 256;
  const canvas = document.createElement("canvas");
  canvas.width = canvas.height = size;
  const ctx = canvas.getContext("2d");

  const gradient = ctx.createRadialGradient(size / 2, size / 2, 0, size / 2, size / 2, size / 2);
  gradient.addColorStop(0, "rgba(24, 20, 16, 0.46)");
  gradient.addColorStop(0.35, "rgba(24, 20, 16, 0.24)");
  gradient.addColorStop(0.7, "rgba(24, 20, 16, 0.06)");
  gradient.addColorStop(1, "rgba(24, 20, 16, 0)");
  ctx.fillStyle = gradient;
  ctx.fillRect(0, 0, size, size);

  const texture = new THREE.CanvasTexture(canvas);
  texture.colorSpace = THREE.SRGBColorSpace;

  const geometry = new THREE.PlaneGeometry(width, depth);
  const material = new THREE.MeshBasicMaterial({
    map: texture,
    transparent: true,
    depthWrite: false,
    toneMapped: false,
  });

  const mesh = new THREE.Mesh(geometry, material);
  mesh.rotation.x = -Math.PI / 2;
  mesh.renderOrder = -1;

  mesh.userData.dispose = () => {
    geometry.dispose();
    material.dispose();
    texture.dispose();
  };

  return mesh;
}
