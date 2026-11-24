import { UMB_HELP_MENU_ALIAS } from "@umbraco-cms/backoffice/help";
import { UMB_WORKSPACE_CONDITION_ALIAS } from "@umbraco-cms/backoffice/workspace";

const codeFirstEntityType = "shorthand-code-first";
const codeFirstWorkspaceAlias = "Shorthand.UmbracoCodeFirst.Workspace";

export const manifests: Array<UmbExtensionManifest> = [
  {
    type: "icons",
    alias: "Shorthand.UmbracoCodeFirst.Icons",
    name: "Shorthand Umbraco Code First Icons",
    js: () => import("../icons/icons.js"),
  },
  {
    type: "modal",
    alias: "Shorthand.UmbracoCodeFirst.Modal.GeneratedSource",
    name: "Code First Generated Source",
    element: () => import("../views/generated-source-modal.js"),
  },
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
    name: "Shorthand Umbraco Code First Swedish Localization",
    alias: "Shorthand.UmbracoCodeFirst.Localization.Sv",
    type: "localization",
    js: () => import("../localization/sv.js"),
    meta: {
      culture: "sv",
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
      icon: "shorthand-code-first",
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
      icon: "shorthand-code-first",
      entityType: codeFirstEntityType,
      menus: ["Umb.Menu.AdvancedSettings"],
    },
  },
  {
    name: "Shorthand Umbraco Code First GitHub Repository",
    alias: "Shorthand.UmbracoCodeFirst.MenuItem.Repository",
    type: "menuItem",
    kind: "link",
    meta: {
      label: "#codeFirst_repository",
      icon: "shorthand-code-first",
      href: "https://github.com/karl-sjogren/Shorthand.UmbracoCodeFirst",
      menus: [UMB_HELP_MENU_ALIAS],
    },
  },
];
