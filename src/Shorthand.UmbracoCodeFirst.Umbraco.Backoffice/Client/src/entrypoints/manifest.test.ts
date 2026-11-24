import { describe, expect, it } from "vitest";
import { manifests } from "./manifest.js";

describe("sample backoffice manifests", () => {
  it("registers the blog index view for document collections", () => {
    // Arrange
    const alias = "Shorthand.UmbracoCodeFirst.CollectionView.BlogIndex";

    // Act
    const views = manifests.filter((manifest) => manifest.alias === alias);

    // Assert
    expect(views).toHaveLength(1);
    expect(views[0]).toMatchObject({
      type: "collectionView",
      conditions: [{ alias: "Umb.Condition.CollectionAlias", match: "Umb.Collection.Document" }],
    });
  });
});
