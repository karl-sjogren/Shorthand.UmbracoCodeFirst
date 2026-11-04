import { describe, expect, it } from "vitest";
import {
  formatCompletedAt,
  formatEditorAliases,
  getSynchronizationCssClass,
  getSynchronizationState,
  hasSynchronizationFailures,
  sortDefinitions,
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

  it("derives pending, failed, and complete synchronization states", () => {
    expect(getSynchronizationState(undefined, 0)).toBe("pending");
    expect(getSynchronizationState("2026-11-04T20:00:00Z", 1)).toBe("failed");
    expect(getSynchronizationState("2026-11-04T20:00:00Z", 0)).toBe("complete");
  });

  it("preserves invalid timestamps for diagnostics", () => {
    expect(formatCompletedAt("not-a-date")).toBe("not-a-date");
  });

  it("sorts definitions without mutating the source array", () => {
    const definitions = [
      { alias: "zeta", kind: "DocumentType" },
      { alias: "alpha", kind: "MediaType" },
    ];

    expect(sortDefinitions(definitions).map((definition) => definition.alias))
      .toEqual(["alpha", "zeta"]);
    expect(definitions.map((definition) => definition.alias))
      .toEqual(["zeta", "alpha"]);
  });
});
