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
    // Arrange
    const schema = "Umbraco.TextBox";
    const ui = "Umb.PropertyEditorUi.TextBox";

    // Act
    const result = formatEditorAliases(schema, ui);

    // Assert
    expect(result).toBe("Umb.PropertyEditorUi.TextBox | Umbraco.TextBox");
  });

  it("omits missing editor aliases", () => {
    // Arrange
    const ui = "Umb.PropertyEditorUi.TextBox";

    // Act
    const uiOnly = formatEditorAliases(undefined, ui);
    const neither = formatEditorAliases();

    // Assert
    expect(uiOnly).toBe(ui);
    expect(neither).toBe("");
  });

  it("marks failed synchronization summaries", () => {
    // Arrange
    const success = { createdCount: 1, updatedCount: 0, skippedCount: 0, failedCount: 0 };
    const failure = { createdCount: 0, updatedCount: 0, skippedCount: 0, failedCount: 1 };

    // Act
    const successClass = getSynchronizationCssClass(success.failedCount);
    const failureClass = getSynchronizationCssClass(failure.failedCount);
    const successHasFailures = hasSynchronizationFailures(success);
    const failureHasFailures = hasSynchronizationFailures(failure);

    // Assert
    expect(successClass).toBe("sync-status");
    expect(failureClass).toBe("sync-status sync-status-error");
    expect(successHasFailures).toBe(false);
    expect(failureHasFailures).toBe(true);
  });

  it("derives pending, failed, and complete synchronization states", () => {
    // Arrange
    const completedAt = "2026-11-04T20:00:00Z";

    // Act
    const pending = getSynchronizationState(undefined, 0);
    const failed = getSynchronizationState(completedAt, 1);
    const complete = getSynchronizationState(completedAt, 0);

    // Assert
    expect(pending).toBe("pending");
    expect(failed).toBe("failed");
    expect(complete).toBe("complete");
  });

  it("preserves invalid timestamps for diagnostics", () => {
    // Arrange
    const invalidTimestamp = "not-a-date";

    // Act
    const result = formatCompletedAt(invalidTimestamp);

    // Assert
    expect(result).toBe(invalidTimestamp);
  });

  it("sorts definitions without mutating the source array", () => {
    // Arrange
    const definitions = [
      { alias: "zeta", kind: "DocumentType" },
      { alias: "alpha", kind: "MediaType" },
    ];

    // Act
    const sorted = sortDefinitions(definitions);

    // Assert
    expect(sorted.map((definition) => definition.alias))
      .toEqual(["alpha", "zeta"]);
    expect(definitions.map((definition) => definition.alias))
      .toEqual(["zeta", "alpha"]);
  });
});
