export type StatusSummary = {
  createdCount: number;
  updatedCount: number;
  skippedCount: number;
  failedCount: number;
};

export type DefinitionSummary = {
  alias: string;
  kind: string;
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

export function getSynchronizationState(
  completedAt: string | undefined,
  failedCount: number,
): "pending" | "failed" | "complete" {
  if (!completedAt) {
    return "pending";
  }

  return failedCount > 0 ? "failed" : "complete";
}

export function formatCompletedAt(
  value: string,
  locale?: string,
): string {
  const date = new Date(value);
  return Number.isNaN(date.valueOf())
    ? value
    : new Intl.DateTimeFormat(locale, {
        dateStyle: "medium",
        timeStyle: "short",
      }).format(date);
}

export function sortDefinitions<T extends DefinitionSummary>(
  definitions: readonly T[],
): T[] {
  return [...definitions].sort((left, right) =>
    left.alias.localeCompare(right.alias),
  );
}
