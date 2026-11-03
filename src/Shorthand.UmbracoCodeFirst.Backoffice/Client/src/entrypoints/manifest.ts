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
    name: "Shorthand Umbraco Code First View Route",
    alias: "Shorthand.UmbracoCodeFirst.ViewRoute",
    type: "sectionView",
    js: () => import("../views/code-first-view.js"),
    meta: {
      label: "#codeFirst_viewLabel",
      pathname: "code-first",
      icon: "icon-code",
    },
    conditions: [
      {
        alias: "Umb.Condition.SectionAlias",
        match: "Umb.Section.Settings",
      },
    ],
  },
  {
    name: "Shorthand Umbraco Code First Menu Item",
    alias: "Shorthand.UmbracoCodeFirst.MenuItem",
    type: "menuItem",
    kind: "link",
    weight: 100,
    meta: {
      label: "#codeFirst_menuLabel",
      icon: "icon-code",
      href: "/umbraco/section/settings/view/code-first",
      menus: ["Umb.Menu.AdvancedSettings"],
    },
  },
];
