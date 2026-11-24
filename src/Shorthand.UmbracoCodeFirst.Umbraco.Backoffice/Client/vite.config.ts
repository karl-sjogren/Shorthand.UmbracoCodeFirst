import { defineConfig } from "vite";

export default defineConfig({
  build: {
    lib: {
      entry: "src/bundle.manifests.ts",
      formats: ["es"],
      fileName: "shorthand-umbraco-code-first-umbraco-backoffice",
    },
    outDir: "../wwwroot/App_Plugins/ShorthandUmbracoCodeFirstUmbracoBackoffice",
    emptyOutDir: true,
    sourcemap: true,
    rollupOptions: {
      external: [/^@umbraco/],
    },
  },
});
