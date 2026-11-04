import { UMB_WORKSPACE_CONDITION_ALIAS } from "@umbraco-cms/backoffice/workspace";

const codeFirstEntityType = "shorthand-code-first";
const codeFirstWorkspaceAlias = "Shorthand.UmbracoCodeFirst.Workspace";

export const manifests: Array<UmbExtensionManifest> = [
  {
    name: "Shorthand Umbraco Code First English Localization",
    alias: "Shorthand.UmbracoCodeFirst.Localization.En",
    type: "localization",
    js: () => import("../localization/en.js"),
    meta: {
      culture: "en",
    },
  },
  {
    name: "Shorthand Umbraco Code First Backoffice Entrypoint",
    alias: "Shorthand.UmbracoCodeFirst.Backoffice.Entrypoint",
    type: "backofficeEntryPoint",
    js: () => import("./entrypoint.js"),
  },
  {
    name: "Shorthand Umbraco Code First Workspace",
    alias: codeFirstWorkspaceAlias,
    type: "workspace",
    element: () => import("../views/code-first-workspace.js"),
    meta: {
      entityType: codeFirstEntityType,
    },
  },
  {
    name: "Shorthand Umbraco Code First Workspace View",
    alias: "Shorthand.UmbracoCodeFirst.WorkspaceView",
    type: "workspaceView",
    element: () => import("../views/code-first-view.js"),
    meta: {
      label: "#codeFirst_viewLabel",
      pathname: "overview",
      icon: "icon-code",
    },
    conditions: [
      {
        alias: UMB_WORKSPACE_CONDITION_ALIAS,
        match: codeFirstWorkspaceAlias,
      },
    ],
  },
  {
    name: "Shorthand Umbraco Code First Menu Item",
    alias: "Shorthand.UmbracoCodeFirst.MenuItem",
    type: "menuItem",
    weight: 100,
    meta: {
      label: "#codeFirst_menuLabel",
      icon: "icon-code",
      entityType: codeFirstEntityType,
      menus: ["Umb.Menu.AdvancedSettings"],
    },
  },
];
