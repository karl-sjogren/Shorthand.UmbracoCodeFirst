import type { UmbDocumentCollectionItemModel } from "@umbraco-cms/backoffice/document";

export function blogColumnValue(item: UmbDocumentCollectionItemModel, alias: string): string {
  const values = item.values.filter((value) => value.alias === alias);
  const value = values.find((entry) => !entry.culture && !entry.segment)?.value ?? values[0]?.value;
  return value == null ? "" : String(value);
}

export function formatPublishDate(value: string): string {
  if (!value) return "";
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? value : new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(date);
}
