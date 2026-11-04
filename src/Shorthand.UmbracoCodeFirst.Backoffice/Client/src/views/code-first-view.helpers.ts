export type StatusSummary = {
  createdCount: number;
  updatedCount: number;
  skippedCount: number;
  failedCount: number;
};

export function formatEditorAliases(
  editorAlias?: string,
  editorUiAlias?: string,
): string {
  return [editorUiAlias, editorAlias]
    .filter((alias): alias is string => Boolean(alias))
    .join(" | ");
}

export function getSynchronizationCssClass(failedCount: number): string {
  return failedCount > 0
    ? "sync-status sync-status-error"
    : "sync-status";
}

export function hasSynchronizationFailures(summary: StatusSummary): boolean {
  return summary.failedCount > 0;
}
