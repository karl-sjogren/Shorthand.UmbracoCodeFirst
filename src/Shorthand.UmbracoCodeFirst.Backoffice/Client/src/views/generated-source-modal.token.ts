import { UmbModalToken } from "@umbraco-cms/backoffice/modal";

export type GeneratedSourceModalData = {
  id: string;
  name: string;
};

export const GENERATED_SOURCE_MODAL = new UmbModalToken<GeneratedSourceModalData>(
  "Shorthand.UmbracoCodeFirst.Modal.GeneratedSource",
  { modal: { type: "dialog", size: "large" } },
);
