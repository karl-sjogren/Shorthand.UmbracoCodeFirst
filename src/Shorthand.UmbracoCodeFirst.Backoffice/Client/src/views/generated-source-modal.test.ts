import { describe, expect, it, vi } from "vitest";

vi.mock("@umbraco-cms/backoffice/code-editor", () => ({}));
vi.mock("@umbraco-cms/backoffice/components", () => ({}));
vi.mock("../api/client.gen.js", () => ({ client: {} }));
vi.mock("@umbraco-cms/backoffice/modal", () => ({
  UmbModalBaseElement: class {
    localize = { term: (key: string) => key };
  },
}));
vi.mock("@umbraco-cms/backoffice/external/lit", () => {
  const template = (parts: TemplateStringsArray, ...values: unknown[]) =>
    parts.reduce((result, part, index) => result + part + String(values[index] ?? ""), "");
  return {
    html: template,
    css: template,
    customElement: () => (target: unknown) => target,
    state: () => () => {},
  };
});

import GeneratedSourceModal from "./generated-source-modal";

describe("generated source file selection", () => {
  it("uses the Umbraco dropdown and switches the displayed source", () => {
    // Arrange
    const modal = new GeneratedSourceModal() as unknown as {
      sources: { fileName: string; content: string; isOriginal?: boolean }[];
      selectedIndex: number;
      selectSource: (event: Event) => void;
      render: () => string;
    };
    modal.sources = [
      { fileName: "Model.cs", content: "model source" },
      { fileName: "page.cshtml", content: "original template", isOriginal: true },
    ];

    // Act
    const initial = modal.render();
    modal.selectSource({ target: { value: "1" } } as unknown as Event);
    const selected = modal.render();

    // Assert
    expect(initial).toContain("<umb-input-dropdown-list");
    expect(initial).toContain("model source");
    expect(modal.selectedIndex).toBe(1);
    expect(selected).toContain("original template");
    expect(selected).toContain("language=html");
    expect(selected).not.toContain("model source");
  });
});
