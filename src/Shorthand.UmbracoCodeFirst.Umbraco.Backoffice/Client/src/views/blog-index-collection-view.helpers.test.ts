import { describe, expect, it } from "vitest";
import type { UmbDocumentCollectionItemModel } from "@umbraco-cms/backoffice/document";
import { blogColumnValue, formatPublishDate } from "./blog-index-collection-view.helpers";

describe("blog index columns", () => {
  it("uses the invariant property value and handles missing values", () => {
    // Arrange
    const item = {
      values: [
        { alias: "mainHeading", culture: "en-US", value: "Translated" },
        { alias: "mainHeading", value: "Title" },
      ],
    } as UmbDocumentCollectionItemModel;

    // Act & Assert
    expect(blogColumnValue(item, "mainHeading")).toBe("Title");
    expect(blogColumnValue(item, "publishDate")).toBe("");
  });

  it("formats dates and preserves invalid values", () => {
    // Arrange
    const date = "2025-01-02T10:30:00";

    // Act & Assert
    expect(formatPublishDate(date)).toBe(new Intl.DateTimeFormat(undefined, {
      dateStyle: "medium", timeStyle: "short",
    }).format(new Date(date)));
    expect(formatPublishDate("invalid")).toBe("invalid");
    expect(formatPublishDate("")).toBe("");
  });
});
