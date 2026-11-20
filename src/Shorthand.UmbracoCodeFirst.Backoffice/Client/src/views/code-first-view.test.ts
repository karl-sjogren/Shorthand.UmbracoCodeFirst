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

vi.mock("@umbraco-cms/backoffice/modal", () => ({
  UmbModalToken: class {},
  umbOpenModal: vi.fn().mockResolvedValue(undefined),
}));

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
  it("places available navigation and source actions in the row popover", () => {
    // Arrange
    const view = new CodeFirstView() as unknown as {
      renderDefinitionTable: (headline: string, definitions: StatusItem[], showProperties: boolean) => string;
    };
    const definition = {
      alias: "article",
      kind: "DocumentType",
      name: "Article",
      folder: "/",
      status: "Unchanged",
      backofficeUrl: "/umbraco/section/settings/workspace/document-type/edit/123",
      generatedSourceId: "source-id",
    } as StatusItem;

    // Act
    const output = view.renderDefinitionTable("Documents", [definition], true);
    const noActions = view.renderDefinitionTable("Documents", [{
      ...definition, backofficeUrl: undefined, generatedSourceId: undefined,
    }], true);

    // Assert
    expect(output).toContain("<td>Article</td>");
    expect(output).toContain("<uui-popover-container");
    expect(output).toContain(`href=${definition.backofficeUrl}`);
    expect(output).toContain("codeFirst_openInUmbraco");
    expect(output).toContain("codeFirst_viewGeneratedSource");
    expect(noActions).not.toContain("<uui-popover-container");
  });

  it("only offers generated code when the definition has a source ID", () => {
    // Arrange
    const view = new CodeFirstView() as unknown as {
      renderDefinitionTable: (headline: string, definitions: StatusItem[], showProperties: boolean, isTemplate: boolean) => string;
    };
    const template = {
      alias: "landingPage",
      kind: "Template",
      name: "Landing Page",
      folder: "/",
      status: "Unchanged",
    } as StatusItem;

    // Act
    const unavailable = view.renderDefinitionTable("Templates", [template], false, true);
    const available = view.renderDefinitionTable("Templates", [{ ...template, generatedSourceId: "source-id" }], false, true);

    // Assert
    expect(unavailable).not.toContain("codeFirst_viewGeneratedSource");
    expect(available).toContain("codeFirst_viewGeneratedSource");
  });

  it("renders template aliases and synchronization outcomes", () => {
    // Arrange
    const view = new CodeFirstView() as unknown as {
      renderDefinitionTable: (headline: string, definitions: StatusItem[], showProperties: boolean, isTemplate: boolean) => string;
    };
    const template = {
      alias: "landingPage",
      kind: "Template",
      name: "Landing Page",
      folder: "/",
      status: "Skipped",
      message: "Not owned.",
    } as StatusItem;

    // Act
    const output = view.renderDefinitionTable("Templates", [template], false, true);

    // Assert
    expect(output).toContain("landingPage");
    expect(output).toContain("Not owned.");
    expect(output).not.toContain("codeFirst_editor");
    expect(output).not.toContain("codeFirst_properties");
  });

  it("shows a pending header without a completion time when no report exists", () => {
    // Arrange
    const view = new CodeFirstView() as unknown as {
      status: unknown;
      render: () => string;
    };
    view.status = {
      hasReport: false,
      completedAt: "0001-01-01T00:00:00+00:00",
      failedCount: 0,
      dataTypes: [],
      documentTypes: [],
      elementTypes: [],
      mediaTypes: [],
      memberTypes: [],
      templates: [],
    };

    // Act
    const output = view.render();

    // Assert
    expect(output).toContain("codeFirst_synchronizationPending");
    expect(output).toContain("sync-status-pending");
    expect(output).not.toContain("codeFirst_synchronizationComplete");
    expect(output).not.toContain("codeFirst_lastSynchronized");
  });

  it("shows an orchestration failure even when no definitions exist", () => {
    // Arrange
    const view = new CodeFirstView() as unknown as {
      status: unknown;
      render: () => string;
    };
    view.status = {
      hasReport: true,
      completedAt: "2026-11-11T00:00:00Z",
      synchronizationError: "Coordination unavailable.",
      failedCount: 0,
      dataTypes: [],
      documentTypes: [],
      elementTypes: [],
      mediaTypes: [],
      memberTypes: [],
      templates: [],
    };

    // Act
    const output = view.render();

    // Assert
    expect(output).toContain("codeFirst_synchronizationFailed");
    expect(output).toContain("Coordination unavailable.");
    expect(output).toContain("sync-status-error");
  });

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
  it("spans every column of an expanded content type", () => {
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
    const table = view.renderDefinitionTable("Definitions", [definition], true);

    // Assert
    expect(table).toContain("<td colspan=6>");
    expect(table).toContain("tabindex=0");
    expect(table).not.toContain("<uui-table-cell colspan=");
  });

  it("does not expand or keyboard-focus data type rows, even if an alias matches an expanded type", () => {
    // Arrange
    const view = new CodeFirstView() as unknown as {
      expandedAlias: string;
      renderDefinitionTable: (headline: string, definitions: StatusItem[], showProperties: boolean) => string;
    };
    view.expandedAlias = "shared";
    const definition = {
      alias: "shared",
      kind: "DataType",
      name: "Shared",
      folder: "/",
      status: "Unchanged",
      backofficeUrl: "/umbraco/section/settings/workspace/data-type/edit/123",
      generatedSourceId: "source-id",
    } as StatusItem;

    // Act
    const table = view.renderDefinitionTable("Data types", [definition], false);

    // Assert
    expect(table).toContain("tabindex=-1");
    expect(table).not.toContain("expandable-row");
    expect(table).not.toContain("details-row");
    expect(table).toContain("codeFirst_openInUmbraco");
    expect(table).toContain("codeFirst_viewGeneratedSource");
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

  it("renders a collection summary", () => {
    // Arrange
    const view = new CodeFirstView() as unknown as {
      renderDefinitionSettings: (definition: StatusItem) => string;
    };
    const definition = {
      collection: { list: true, grid: false, pageSize: 25 },
      allowedTemplateAliases: [],
      allowedChildAliases: [],
      compositionAliases: [],
      groups: [],
      tabs: [],
    } as unknown as StatusItem;

    // Act
    const settings = view.renderDefinitionSettings(definition);

    // Assert
    expect(settings).toContain("codeFirst_collection");
    expect(settings).toContain("codeFirst_collectionList");
    expect(settings).not.toContain("codeFirst_collectionGrid");
    expect(settings).toContain("codeFirst_collectionPageSize");
  });
});
