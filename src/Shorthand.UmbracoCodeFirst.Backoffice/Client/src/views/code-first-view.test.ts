import { describe, expect, it, vi } from "vitest";
import type { StatusItem } from "../types/code-first.types";

vi.mock("@umbraco-cms/backoffice/lit-element", () => ({
  UmbLitElement: class {
    localize = { term: (key: string) => key };
  },
}));

vi.mock("@umbraco-cms/backoffice/external/lit", () => {
  const template = (parts: TemplateStringsArray, ...values: unknown[]) =>
    parts.reduce(
      (result, part, index) =>
        result + part + (Array.isArray(values[index])
          ? values[index].join("")
          : String(values[index] ?? "")),
      "",
    );
  return {
    html: template,
    css: template,
    customElement: () => (target: unknown) => target,
    state: () => () => {},
  };
});

vi.mock("../api/client.gen.js", () => ({ client: {} }));

import CodeFirstView from "./code-first-view";

function renderStatus(status: string, message?: string): string {
  const view = new CodeFirstView() as unknown as {
    renderDefinitionTable: (headline: string, definitions: StatusItem[], showProperties: boolean) => string;
  };
  const definition = {
    alias: "article",
    kind: "DocumentType",
    name: "Article",
    folder: "/",
    status,
    message,
    propertyCount: 0,
  } as StatusItem;
  return view.renderDefinitionTable("Documents", [definition], true);
}

describe("code-first status messages", () => {
  it("renders a skipped reason without a failure indicator", () => {
    // Arrange
    const reason = "Not owned by code-first.";

    // Act
    const table = renderStatus("Skipped", reason);

    // Assert
    expect(table).toContain(reason);
    expect(table).toContain('class=status-message');
    expect(table).not.toContain("failure-indicator");
    expect(table).not.toContain("failed-row");
  });

  it("retains failure styling and its message", () => {
    // Arrange
    const message = "Synchronization failed.";

    // Act
    const table = renderStatus("Failed", message);

    // Assert
    expect(table).toContain(message);
    expect(table).toContain("status-message status-error");
    expect(table).toContain("failure-indicator");
    expect(table).toContain("failed-row");
  });

  it("does not render a message when none is provided", () => {
    // Arrange
    const status = "Unchanged";

    // Act
    const table = renderStatus(status);

    // Assert
    expect(table).not.toContain("status-message");
  });
});

describe("expanded definition details", () => {
  it.each([
    [true, 5],
    [false, 4],
  ])("spans every column when showProperties is %s", (showProperties, columns) => {
    // Arrange
    const view = new CodeFirstView() as unknown as {
      expandedAlias: string;
      renderDefinitionTable: (headline: string, definitions: StatusItem[], showProperties: boolean) => string;
    };
    view.expandedAlias = "article";
    const definition = {
      alias: "article",
      kind: "DocumentType",
      name: "Article",
      folder: "/",
      status: "Updated",
      propertyCount: 0,
      properties: [],
      allowedTemplateAliases: [],
      allowedChildAliases: [],
      compositionAliases: [],
      groups: [],
      tabs: [],
    } as unknown as StatusItem;

    // Act
    const table = view.renderDefinitionTable("Definitions", [definition], showProperties);

    // Assert
    expect(table).toContain(`<td colspan=${columns}>`);
    expect(table).not.toContain("<uui-table-cell colspan=");
  });

  it.each(["icon-barcode", "my-custom-icon"])(
    "renders the %s icon alongside its alias",
    (icon) => {
      // Arrange
      const view = new CodeFirstView() as unknown as {
        renderDefinitionSettings: (definition: StatusItem) => string;
      };
      const definition = {
        icon,
        allowedTemplateAliases: [],
        allowedChildAliases: [],
        compositionAliases: [],
        groups: [],
        tabs: [],
      } as unknown as StatusItem;

      // Act
      const settings = view.renderDefinitionSettings(definition);

      // Assert
      expect(settings).toContain(`<umb-icon name=${icon} aria-hidden="true">`);
      expect(settings).toMatch(new RegExp(`</umb-icon>\\s+${icon}\\s*</dd>`));
    },
  );

  it("omits the icon when no alias is specified", () => {
    // Arrange
    const view = new CodeFirstView() as unknown as {
      renderDefinitionSettings: (definition: StatusItem) => string;
    };
    const definition = {
      description: "Description",
      allowedTemplateAliases: [],
      allowedChildAliases: [],
      compositionAliases: [],
      groups: [],
      tabs: [],
    } as unknown as StatusItem;

    // Act
    const settings = view.renderDefinitionSettings(definition);

    // Assert
    expect(settings).toContain("Description");
    expect(settings).not.toContain("<umb-icon");
  });
});
