/**
 * The Local Liquor bottle, built procedurally to match the real thing: tall and
 * slim, clear flint glass, a long gentle shoulder, and a black capsule with two
 * gold bands at its foot.
 *
 * Units are centimetres. The profile is composed from explicit straight and
 * curved segments rather than splined through a sparse point list — a spline
 * run through a long straight body followed by a shoulder overshoots at the
 * junction, and that bulge catches the light as a hard horizontal line across
 * the middle of the bottle.
 *
 * The whole thing is shifted so the bottle's middle sits on the origin, which
 * keeps the camera maths in the stage simple.
 */

import * as THREE from "three";
import { LABEL_ASPECT } from "./label.js";
import { createGlassRoughnessMap } from "./environment.js";

const HEIGHT = 32;
const BODY_RADIUS = 3.65;
const WALL = 0.4;

/** Height the wine reaches — a little way up into the shoulder. */
const LIQUID_TOP = 20.2;

const CAPSULE_BOTTOM = 26.5;

/**
 * Where the wine rocks about. Halfway up the fill, so the furthest any part of
 * it travels sideways is half its height times the tilt — which has to stay
 * inside WALL, or it pokes through the glass.
 */
const LIQUID_PIVOT_Y = LIQUID_TOP * 0.5;

/** Tilt at which the liquid would just reach the inside of the wall. */
const MAX_SLOSH = (WALL * 0.6) / LIQUID_PIVOT_Y;

const LABEL_ARC = THREE.MathUtils.degToRad(116);
const LABEL_RADIUS = BODY_RADIUS + 0.035;
const LABEL_HEIGHT = (2 * Math.PI * LABEL_RADIUS * (LABEL_ARC / (Math.PI * 2))) / LABEL_ASPECT;
const LABEL_CENTER_Y = 11.6;

const CENTER_OFFSET = HEIGHT / 2;

/* -------------------------------------------------------------- profile --- */

/** Straight run from a to b, excluding a so segments concatenate cleanly. */
function line(a, b, steps = 1) {
  const out = [];
  for (let i = 1; i <= steps; i++) {
    const t = i / steps;
    out.push([a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t]);
  }
  return out;
}

/** Quadratic bezier from a to b, bending around control point c. */
function curve(a, c, b, steps = 14) {
  const out = [];
  for (let i = 1; i <= steps; i++) {
    const t = i / steps;
    const u = 1 - t;
    out.push([
      u * u * a[0] + 2 * u * t * c[0] + t * t * b[0],
      u * u * a[1] + 2 * u * t * c[1] + t * t * b[1],
    ]);
  }
  return out;
}

/** Outer silhouette, from the punt apex up to the lip. */
function outerProfile() {
  const r = BODY_RADIUS;
  const punt = [0, 2.35];
  return [
    punt,
    // the punt sweeping down and out to the heel
    ...curve(punt, [1.9, 1.6], [3.05, 0.18], 12),
    ...curve([3.05, 0.18], [3.55, 0.0], [r, 0.5], 8),
    // the straight body
    ...line([r, 0.5], [r, 16.5], 2),
    // one long, gentle shoulder into the neck
    ...curve([r, 16.5], [r, 21.4], [1.62, 23.4], 22),
    ...curve([1.62, 23.4], [1.35, 24.4], [1.33, 25.6], 8),
    // neck
    ...line([1.33, 25.6], [1.31, 30.2], 2),
    // the finish
    ...curve([1.31, 30.2], [1.5, 30.6], [1.52, 31.2], 6),
    ...line([1.52, 31.2], [1.5, 31.7], 1),
    ...curve([1.5, 31.7], [1.35, 31.95], [0.95, 31.97], 5),
    // closed over the top; the capsule covers it
    [0, 31.98],
  ];
}

/** The wine: the inside of the glass, filled to LIQUID_TOP. */
function liquidProfile(outer) {
  const r = BODY_RADIUS - WALL;
  const body = [
    [0.02, 2.05],
    ...curve([0.02, 2.05], [1.85, 1.3], [2.85, 0.55], 10),
    ...curve([2.85, 0.55], [3.2, 0.45], [r, 0.75], 6),
    ...line([r, 0.75], [r, 16.5], 2),
  ];

  // Follow the shoulder inwards, inset from the glass, up to the fill line.
  const shoulder = outer
    .filter(([, y]) => y > 16.5 && y <= LIQUID_TOP)
    .map(([x, y]) => [Math.max(x - WALL, 0.05), y]);

  const top = shoulder.length ? shoulder[shoulder.length - 1][0] : r;
  return [...body, ...shoulder, [top, LIQUID_TOP], [0.02, LIQUID_TOP]];
}

function lathe(points, segments = 128) {
  return new THREE.LatheGeometry(
    points.map(([x, y]) => new THREE.Vector2(Math.max(x, 0.0001), y)),
    segments,
  );
}

/* ---------------------------------------------------------------- build --- */

/**
 * @param {{liquid: string, labelTexture: THREE.Texture|null,
 *          backTexture: THREE.Texture|null}} options
 * @returns {{group: THREE.Group, setLiquid: Function, setSlosh: Function,
 *            setQuality: Function, setLabelTexture: Function, dispose: Function}}
 */
export function createBottle({
  liquid = "#db8a4c",
  labelTexture = null,
  backTexture = null,
} = {}) {
  const group = new THREE.Group();
  const disposables = [];
  const track = (thing) => {
    disposables.push(thing);
    return thing;
  };

  const outer = outerProfile();

  // --- glass ---------------------------------------------------------------
  const glassGeometry = track(lathe(outer));

  const roughnessMap = track(createGlassRoughnessMap());
  roughnessMap.repeat.set(2, 1);

  const glassMaterial = track(new THREE.MeshPhysicalMaterial({
    color: 0xffffff,
    metalness: 0,
    // Multiplied by the map. Kept low on purpose: roughness blurs what the glass
    // transmits as well as what it reflects, and blurring a pale backdrop is
    // exactly what turns clear glass into frosted white.
    roughness: 0.12,
    roughnessMap,
    transmission: 1,
    // The lathe is a solid of revolution, not a hollow vessel — there is no air
    // cavity modelled inside it. At a realistic 2.6 cm this made the empty neck
    // behave like a block of glass and go milky. The body is full of opaque wine
    // so nothing is lost by treating the whole thing as a thin wall.
    thickness: 0.7,
    ior: 1.52,
    specularIntensity: 1,
    // Glass splits light by wavelength. The coloured fringing this puts on the
    // edges is subtle, but its absence is one of the loudest tells that a
    // render is a render.
    dispersion: 1.4,
    // Flint glass, like the real bottle: near colourless, with just enough
    // absorption that the silhouette darkens where you see through the most.
    attenuationColor: new THREE.Color(0xdfeee6),
    attenuationDistance: 6,
    side: THREE.DoubleSide,
  }));
  const glass = new THREE.Mesh(glassGeometry, glassMaterial);
  glass.renderOrder = 2;
  group.add(glass);

  // --- wine ----------------------------------------------------------------
  // Deliberately opaque: three.js renders transmissive materials against a
  // buffer that excludes other transmissive objects, so a see-through liquid
  // would vanish behind the glass.
  const liquidGeometry = track(lathe(liquidProfile(outer), 96));
  const liquidMaterial = track(new THREE.MeshPhysicalMaterial({
    color: new THREE.Color(liquid),
    roughness: 0.42,
    metalness: 0,
    // A bright studio reflects hard off an opaque surface and washes the colour
    // straight out of it. Dialling the environment down on the wine alone lets
    // the pigment read, while the key light still gives it a highlight.
    envMapIntensity: 0.55,
    clearcoat: 0.2,
    clearcoatRoughness: 0.35,
    side: THREE.DoubleSide,
  }));
  const liquidMesh = new THREE.Mesh(liquidGeometry, liquidMaterial);
  liquidMesh.renderOrder = 1;

  // Pivoted at the middle of the wine, not at its surface. Pivoting at the top
  // gives the base a 20 cm lever, and even a couple of degrees then swings the
  // body of the liquid clean through the wall of the bottle.
  const liquidPivot = new THREE.Group();
  liquidPivot.position.y = LIQUID_PIVOT_Y;
  liquidMesh.position.y = -LIQUID_PIVOT_Y;
  liquidPivot.add(liquidMesh);
  group.add(liquidPivot);

  // --- capsule over the screw cap ------------------------------------------
  const capsuleGeometry = track(lathe([
    [0, 32.25],
    ...curve([0, 32.25], [1.3, 32.24], [1.62, 31.75], 8),
    ...line([1.62, 31.75], [1.6, 30.4], 1),
    ...curve([1.6, 30.4], [1.5, 29.0], [1.47, 27.6], 8),
    ...line([1.47, 27.6], [1.45, CAPSULE_BOTTOM], 1),
  ], 72));
  const capsuleMaterial = track(new THREE.MeshPhysicalMaterial({
    color: 0x14120f,
    roughness: 0.34,
    metalness: 0.2,
    clearcoat: 0.6,
    side: THREE.DoubleSide,
  }));
  group.add(new THREE.Mesh(capsuleGeometry, capsuleMaterial));

  // the two gold bands near the foot of the capsule, as on the real bottle
  const bandGeometry = track(new THREE.CylinderGeometry(1.465, 1.465, 0.13, 72, 1, true));
  const bandMaterial = track(new THREE.MeshStandardMaterial({
    color: 0xd8a534,
    roughness: 0.32,
    metalness: 0.65,
    side: THREE.DoubleSide,
  }));
  for (const y of [27.15, 27.62]) {
    const band = new THREE.Mesh(bandGeometry, bandMaterial);
    band.position.y = y;
    group.add(band);
  }

  // --- labels, front and back ----------------------------------------------
  // theta 0 faces +Z, so starting half an arc back centres a label on the
  // camera; the back label is the same patch turned half a revolution.
  function labelMesh(texture, thetaStart) {
    const geometry = track(new THREE.CylinderGeometry(
      LABEL_RADIUS, LABEL_RADIUS, LABEL_HEIGHT, 96, 1, true,
      thetaStart, LABEL_ARC,
    ));
    const material = track(new THREE.MeshStandardMaterial({
      map: texture,
      color: 0xffffff,
      roughness: 0.84,
      metalness: 0,
      // The studio is a mid-grey room by design, so the wall a label faces is
      // grey and paper stock renders grey with it. Labels get their own,
      // stronger helping of the environment — but not so much that they clip:
      // a blown-out white loses the contrast at the edge of the rules, and the
      // label then reads as soft-cornered rather than as a piece of paper.
      envMapIntensity: 1.35,
      // Front faces only. DoubleSide draws the reverse of each label too, and
      // it shows through the glass as a mirrored ghost of itself.
      side: THREE.FrontSide,
    }));
    const mesh = new THREE.Mesh(geometry, material);
    mesh.position.y = LABEL_CENTER_Y;
    mesh.renderOrder = 3;
    group.add(mesh);
    return { mesh, material };
  }

  const frontLabel = labelMesh(labelTexture, -LABEL_ARC / 2);
  const backLabel = labelMesh(backTexture, Math.PI - LABEL_ARC / 2);

  group.position.y = -CENTER_OFFSET;

  // The bottle lives in a wrapper so callers can spin and scale it without
  // undoing the vertical centring.
  const wrapper = new THREE.Group();
  wrapper.add(group);

  return {
    group: wrapper,

    /**
     * Drops the expensive extras when the stage decides this machine cannot
     * afford them. Dispersion is the costly one: it samples the transmission
     * buffer once per colour channel.
     */
    setQuality(high) {
      glassMaterial.dispersion = high ? 1.6 : 0;
      glassMaterial.roughnessMap = high ? roughnessMap : null;
      glassMaterial.needsUpdate = true;
    },

    /** Used by the admin preview, which recolours the wine as you drag. */
    setLiquid(color) {
      liquidMaterial.color.set(color);
    },

    /**
     * Rocks the wine. Kept to small angles: the liquid is only inset 4 mm from
     * the wall, so a real tilt would push it through the glass.
     */
    setSlosh(radians) {
      liquidPivot.rotation.z = THREE.MathUtils.clamp(radians, -MAX_SLOSH, MAX_SLOSH);
    },

    /** Swap in a freshly drawn label without rebuilding the bottle. */
    setLabelTexture(texture, side = "front") {
      const target = side === "back" ? backLabel : frontLabel;
      target.material.map = texture;
      target.material.needsUpdate = true;
    },

    dispose() {
      for (const thing of disposables) thing.dispose();
    },
  };
}

export const BOTTLE_HEIGHT = HEIGHT;
