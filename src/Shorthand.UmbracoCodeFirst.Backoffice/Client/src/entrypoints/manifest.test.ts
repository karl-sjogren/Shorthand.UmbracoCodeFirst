import { describe, expect, it, vi } from "vitest";

vi.mock("@umbraco-cms/backoffice/help", () => ({ UMB_HELP_MENU_ALIAS: "Umb.Menu.Help" }));
vi.mock("@umbraco-cms/backoffice/workspace", () => ({
  UMB_WORKSPACE_CONDITION_ALIAS: "Umb.Condition.WorkspaceAlias",
}));

import { manifests } from "./manifest.js";

describe("repository help menu item", () => {
  it("links to the project repository with the Code First icon", () => {
    // Arrange
    const repository = manifests.find(
      (manifest) => manifest.alias === "Shorthand.UmbracoCodeFirst.MenuItem.Repository",
    );

    // Act & Assert
    expect(repository).toMatchObject({
      type: "menuItem",
      kind: "link",
      meta: {
        menus: ["Umb.Menu.Help"],
        label: "#codeFirst_repository",
        icon: "shorthand-code-first",
        href: "https://github.com/karl-sjogren/Shorthand.UmbracoCodeFirst",
      },
    });

  });
});

describe("Swedish localization", () => {
  it("registers the Swedish culture and loads its translations", async () => {
    // Arrange
    const manifest = manifests.find(
      (item) => item.alias === "Shorthand.UmbracoCodeFirst.Localization.Sv",
    );

    // Act & Assert
    expect(manifest).toMatchObject({ type: "localization", meta: { culture: "sv" } });
    if (!manifest || !("js" in manifest) || typeof manifest.js !== "function") {
      throw new Error("Swedish localization loader is unavailable");
    }
    const load = manifest.js as () => Promise<typeof import("../localization/sv.js")>;
    const localization = await load();
    expect(localization.default.codeFirst.menuLabel).toBe("Code First");
    expect(localization.default.codeFirst.dataTypesHeadline).toBe("Datatyper");
  });
});
