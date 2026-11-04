import { describe, expect, it } from "vitest";
import {
  formatEditorAliases,
  getSynchronizationCssClass,
  hasSynchronizationFailures,
} from "./code-first-view.helpers";

describe("code-first view helpers", () => {
  it("formats UI and schema aliases in display order", () => {
    expect(formatEditorAliases("Umbraco.TextBox", "Umb.PropertyEditorUi.TextBox"))
      .toBe("Umb.PropertyEditorUi.TextBox | Umbraco.TextBox");
  });

  it("omits missing editor aliases", () => {
    expect(formatEditorAliases(undefined, "Umb.PropertyEditorUi.TextBox"))
      .toBe("Umb.PropertyEditorUi.TextBox");
    expect(formatEditorAliases()).toBe("");
  });

  it("marks failed synchronization summaries", () => {
    expect(getSynchronizationCssClass(0)).toBe("sync-status");
    expect(getSynchronizationCssClass(1)).toBe("sync-status sync-status-error");
    expect(hasSynchronizationFailures({ createdCount: 1, updatedCount: 0, skippedCount: 0, failedCount: 0 }))
      .toBe(false);
    expect(hasSynchronizationFailures({ createdCount: 0, updatedCount: 0, skippedCount: 0, failedCount: 1 }))
      .toBe(true);
  });
});
