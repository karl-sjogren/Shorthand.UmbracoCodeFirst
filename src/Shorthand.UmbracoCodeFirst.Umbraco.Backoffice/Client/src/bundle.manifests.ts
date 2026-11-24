import { manifests as entrypoints } from "./entrypoints/manifest.js";

export const manifests: Array<UmbExtensionManifest> = [
  ...entrypoints,
];
