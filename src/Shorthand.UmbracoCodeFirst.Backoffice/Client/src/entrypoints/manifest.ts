export const manifests: Array<UmbExtensionManifest> = [
  {
    name: "Shorthand Umbraco Code First Backoffice Entrypoint",
    alias: "Shorthand.UmbracoCodeFirst.Backoffice.Entrypoint",
    type: "backofficeEntryPoint",
    js: () => import("./entrypoint.js"),
  },
];
