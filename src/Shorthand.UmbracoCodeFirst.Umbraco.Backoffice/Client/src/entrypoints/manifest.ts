export const manifests: Array<UmbExtensionManifest> = [
  {
    name: "Blog Index Collection View",
    alias: "Shorthand.UmbracoCodeFirst.CollectionView.BlogIndex",
    type: "collectionView",
    element: () => import("../views/blog-index-collection-view.js"),
    meta: {
      label: "Blog posts",
      icon: "icon-list",
      pathName: "blog-posts",
    },
    conditions: [
      {
        alias: "Umb.Condition.CollectionAlias",
        match: "Umb.Collection.Document",
      },
    ],
  },
];
