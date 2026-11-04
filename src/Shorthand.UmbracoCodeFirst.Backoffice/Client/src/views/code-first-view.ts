import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { css, html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { client } from "../api/client.gen.js";

type StatusItem = {
  kind: string;
  alias: string;
  name: string;
  editorAlias?: string;
  editorUiAlias?: string;
  propertyCount: number;
  status: string;
  message?: string;
  backofficeUrl?: string;
  properties: StatusProperty[];
};

type StatusProperty = {
  alias: string;
  name: string;
  dataTypeAlias: string;
  description?: string;
  groupAlias?: string;
  groupName?: string;
  tabAlias?: string;
  tabName?: string;
  sortOrder: number;
  mandatory: boolean;
  validationExpression?: string;
  validationMessage?: string;
  labelOnTop: boolean;
};

type StatusResponse = {
  definitionCount: number;
  dataTypeCount: number;
  documentTypeCount: number;
  createdCount: number;
  updatedCount: number;
  unchangedCount: number;
  failedCount: number;
  completedAt: string;
  dataTypes: StatusItem[];
  documentTypes: StatusItem[];
};

type CleanupItem = {
  kind: string;
  alias: string;
  name: string;
  parentAlias?: string;
};

type CleanupResponse = {
  items: CleanupItem[];
};

type CleanupResult = {
  removedCount: number;
  removed: CleanupItem[];
  failed: { item: CleanupItem; message: string }[];
};

@customElement("shorthand-code-first-view")
export default class ShorthandCodeFirstViewElement extends UmbLitElement {
  @state()
  private status?: StatusResponse;

  @state()
  private error?: string;

  @state()
  private cleanup?: CleanupResponse;

  @state()
  private cleanupResult?: CleanupResult;

  @state()
  private cleanupError?: string;

  @state()
  private cleanupRunning = false;

  connectedCallback(): void {
    super.connectedCallback();
    void this.loadStatus();
  }

  private isFailed(definition: StatusItem): boolean {
    return definition.status.toLowerCase() === "failed";
  }

  private formatEditor(definition: StatusItem): string {
    return [definition.editorUiAlias, definition.editorAlias]
      .filter((alias): alias is string => Boolean(alias))
      .join(" | ");
  }

  override render() {
    if (this.error) {
      return html`<uui-box headline="Code-first status">
        <p>${this.error}</p>
      </uui-box>`;
    }

    if (!this.status) {
      return html`<uui-loader></uui-loader>`;
    }

    return html`
      <uui-box class="page-header">
        <div class="header-content">
          <div class="header-icon">
            <uui-icon name="icon-code"></uui-icon>
          </div>
          <div class="header-copy">
            <h1>${this.localize.term("codeFirst_pageTitle")}</h1>
            <p>${this.localize.term("codeFirst_pageDescription")}</p>
            <div class="header-meta">
              <span
                class=${this.status.failedCount > 0
                  ? "sync-status sync-status-error"
                  : "sync-status"}
              >
                <span class="status-dot"></span>
                ${this.getSynchronizationLabel()}
              </span>
              ${this.status.completedAt
                ? html`
                    <span class="last-synchronized">
                      ${this.localize.term("codeFirst_lastSynchronized")}:
                      ${this.formatCompletedAt(this.status.completedAt)}
                    </span>
                  `
                : ""}
            </div>
          </div>
          <div class="header-action">
            <uui-button
              look="secondary"
              label=${this.localize.term("codeFirst_cleanupButton")}
              @click=${this.openCleanup}
            ></uui-button>
          </div>
        </div>
        <div class="summary">
          ${this.renderSummaryMetric(
            this.status.definitionCount,
            "codeFirst_generatedDefinitions",
          )}
          ${this.renderSummaryMetric(
            this.status.dataTypeCount,
            "codeFirst_dataTypes",
          )}
          ${this.renderSummaryMetric(
            this.status.documentTypeCount,
            "codeFirst_documentTypes",
          )}
          ${this.renderSummaryMetric(this.status.createdCount, "codeFirst_created")}
          ${this.renderSummaryMetric(this.status.updatedCount, "codeFirst_updated")}
          ${this.renderSummaryMetric(
            this.status.failedCount,
            "codeFirst_failed",
            this.status.failedCount > 0,
          )}
        </div>
      </uui-box>
      ${this.cleanup ? this.renderCleanup() : ""}
      ${this.renderDefinitionTable(
        this.localize.term("codeFirst_dataTypesHeadline"),
        this.status.dataTypes,
        false,
      )}
      ${this.renderDefinitionTable(
        this.localize.term("codeFirst_documentTypesHeadline"),
        this.status.documentTypes,
        true,
      )}
    `;
  }

  private renderCleanup() {
    const items = this.cleanup?.items ?? [];
    return html`
      <uui-box class="cleanup-box" headline=${this.localize.term("codeFirst_cleanupHeadline")}>
        <p>${this.localize.term("codeFirst_cleanupDescription")}</p>
        ${this.cleanupError
          ? html`<p class="cleanup-error">${this.cleanupError}</p>`
          : ""}
        ${this.cleanupResult
          ? html`
              <p class="cleanup-result">
                ${this.localize.term(
                  "codeFirst_cleanupCompleted",
                  this.cleanupResult.removedCount,
                )}
              </p>
              ${this.cleanupResult.failed.length > 0
                ? html`
                    <p class="cleanup-error">
                      ${this.localize.term(
                        "codeFirst_cleanupFailed",
                        this.cleanupResult.failed.length,
                      )}
                    </p>
                    <ul>
                      ${this.cleanupResult.failed.map(
                        (failure) =>
                          html`<li>
                            ${failure.item.name}: ${failure.message}
                          </li>`,
                      )}
                    </ul>
                  `
                : ""}
              <uui-button
                look="secondary"
                label=${this.localize.term("codeFirst_cleanupClose")}
                @click=${this.closeCleanup}
              ></uui-button>
            `
          : html`
              <p class="cleanup-count">
                ${this.localize.term("codeFirst_cleanupItemCount", items.length)}
              </p>
              ${items.length > 0
                ? html`
                    <uui-table>
                      <uui-table-head>
                        <uui-table-head-cell
                          >${this.localize.term("codeFirst_kind")}</uui-table-head-cell
                        >
                        <uui-table-head-cell
                          >${this.localize.term("codeFirst_name")}</uui-table-head-cell
                        >
                        <uui-table-head-cell
                          >${this.localize.term("codeFirst_alias")}</uui-table-head-cell
                        >
                        <uui-table-head-cell
                          >${this.localize.term("codeFirst_parent")}</uui-table-head-cell
                        >
                      </uui-table-head>
                      ${items.map(
                        (item) => html`
                          <uui-table-row>
                            <uui-table-cell>${item.kind}</uui-table-cell>
                            <uui-table-cell>${item.name}</uui-table-cell>
                            <uui-table-cell>${item.alias}</uui-table-cell>
                            <uui-table-cell>${item.parentAlias ?? ""}</uui-table-cell>
                          </uui-table-row>
                        `,
                      )}
                    </uui-table>
                  `
                : html`<p>${this.localize.term("codeFirst_cleanupNothing")}</p>`}
              <div class="cleanup-buttons">
                <uui-button
                  look="secondary"
                  label=${this.localize.term("codeFirst_cleanupCancel")}
                  @click=${this.closeCleanup}
                ></uui-button>
                <uui-button
                  look="primary"
                  color="danger"
                  ?disabled=${items.length === 0 || this.cleanupRunning}
                  label=${this.cleanupRunning
                    ? this.localize.term("codeFirst_cleanupRunning")
                    : this.localize.term("codeFirst_cleanupProceed")}
                  @click=${this.applyCleanup}
                ></uui-button>
              </div>
            `}
      </uui-box>
    `;
  }

  private renderSummaryMetric(
    value: number,
    labelKey: string,
    isFailure = false,
  ) {
    return html`
      <div class=${isFailure ? "summary-metric summary-metric-failure" : "summary-metric"}>
        <strong>${value}</strong>
        <span>${this.localize.term(labelKey)}</span>
      </div>
    `;
  }

  private getSynchronizationLabel(): string {
    if (!this.status?.completedAt) {
      return this.localize.term("codeFirst_synchronizationPending");
    }

    return this.status.failedCount > 0
      ? this.localize.term("codeFirst_synchronizationFailed")
      : this.localize.term("codeFirst_synchronizationComplete");
  }

  private formatCompletedAt(value: string): string {
    const date = new Date(value);
    return Number.isNaN(date.valueOf())
      ? value
      : new Intl.DateTimeFormat(undefined, {
          dateStyle: "medium",
          timeStyle: "short",
        }).format(date);
  }

  private renderDefinitionTable(
    headline: string,
    definitions: StatusItem[],
    showProperties: boolean,
  ) {
    return html`
      <uui-box headline=${headline}>
        <uui-table>
          <uui-table-head>
            <uui-table-head-cell
              >${this.localize.term("codeFirst_name")}</uui-table-head-cell
            >
            <uui-table-head-cell
              >${this.localize.term("codeFirst_alias")}</uui-table-head-cell
            >
            ${showProperties
              ? html`<uui-table-head-cell
                  >${this.localize.term("codeFirst_properties")}</uui-table-head-cell
                >`
              : html`<uui-table-head-cell
                  >${this.localize.term("codeFirst_editor")}</uui-table-head-cell
                >`}
            <uui-table-head-cell
              >${this.localize.term("codeFirst_status")}</uui-table-head-cell
            >
          </uui-table-head>
          ${definitions.map(
            (definition) => html`
              <uui-table-row
                class=${this.isFailed(definition)
                  ? "definition-row failed-row"
                  : "definition-row"}
                tabindex="0"
                @click=${() => this.toggleDefinition(definition.alias)}
                @keydown=${(event: KeyboardEvent) => {
                  if (event.key === "Enter" || event.key === " ") {
                    event.preventDefault();
                    this.toggleDefinition(definition.alias);
                  }
                }}
              >
                <uui-table-cell>
                  ${definition.backofficeUrl
                    ? html`<a
                        href=${definition.backofficeUrl}
                        @click=${(event: Event) => event.stopPropagation()}
                        >${definition.name}</a
                      >`
                    : definition.name}
                </uui-table-cell>
                <uui-table-cell>${definition.alias}</uui-table-cell>
                <uui-table-cell>
                  ${showProperties
                    ? definition.propertyCount
                    : this.formatEditor(definition)}
                </uui-table-cell>
                <uui-table-cell>
                  <span
                    class=${this.isFailed(definition) || definition.message
                      ? "status-error"
                      : ""}
                  >
                    ${this.isFailed(definition)
                      ? html`<span class="failure-indicator" aria-hidden="true"
                          >!</span
                        >`
                      : ""}
                    ${definition.status}
                  </span>
                  ${definition.message
                    ? html`<small class="error-message"
                        >${definition.message}</small
                      >`
                    : ""}
                </uui-table-cell>
              </uui-table-row>
              ${this.expandedAlias === definition.alias
                ? html`
                    <uui-table-row class="details-row">
                      <uui-table-cell colspan="4">
                        ${this.renderProperties(definition)}
                      </uui-table-cell>
                    </uui-table-row>
                  `
                : ""}
            `,
          )}
        </uui-table>
      </uui-box>
    `;
  }

  private renderProperties(definition: StatusItem) {
    if (definition.properties.length === 0) {
      return html`<p>${this.localize.term("codeFirst_noProperties")}</p>`;
    }

    return html`
      <div class="property-details">
        <strong
          >${this.localize.term("codeFirst_propertiesFor", definition.name)}</strong
        >
        <uui-table>
          <uui-table-head>
            <uui-table-head-cell
              >${this.localize.term("codeFirst_name")}</uui-table-head-cell
            >
            <uui-table-head-cell
              >${this.localize.term("codeFirst_alias")}</uui-table-head-cell
            >
            <uui-table-head-cell
              >${this.localize.term("codeFirst_dataType")}</uui-table-head-cell
            >
            <uui-table-head-cell
              >${this.localize.term("codeFirst_layout")}</uui-table-head-cell
            >
            <uui-table-head-cell
              >${this.localize.term("codeFirst_validation")}</uui-table-head-cell
            >
          </uui-table-head>
          ${definition.properties.map(
            (property) => html`
              <uui-table-row>
                <uui-table-cell>
                  <strong>${property.name}</strong>
                  ${property.description
                    ? html`<small>${property.description}</small>`
                    : ""}
                </uui-table-cell>
                <uui-table-cell>${property.alias}</uui-table-cell>
                <uui-table-cell>${property.dataTypeAlias}</uui-table-cell>
                <uui-table-cell>
                  ${property.tabName ?? property.tabAlias ?? ""}
                  ${property.groupName ?? property.groupAlias
                    ? html` / ${property.groupName ?? property.groupAlias}`
                    : ""}
                </uui-table-cell>
                <uui-table-cell>
                  ${property.mandatory
                    ? this.localize.term("codeFirst_mandatory")
                    : ""}
                  ${property.validationExpression
                    ? html`<small
                        >${property.validationExpression}${property
                          .validationMessage
                          ? ` - ${property.validationMessage}`
                          : ""}</small
                      >`
                    : ""}
                </uui-table-cell>
              </uui-table-row>
            `,
          )}
        </uui-table>
      </div>
    `;
  }

  @state()
  private expandedAlias?: string;

  private toggleDefinition(alias: string): void {
    this.expandedAlias = this.expandedAlias === alias ? undefined : alias;
    this.requestUpdate();
  }

  private async loadStatus(): Promise<void> {
    try {
      const result = await client.get<StatusResponse>({
        security: [{ scheme: "bearer", type: "http" }],
        url: "/umbraco/management/api/v1/shorthandumbracocodefirstbackoffice/status",
        throwOnError: false,
      });

      const data = result.data as StatusResponse | undefined;
      const responseStatus = result.response?.status ?? 0;
      if (!result.response?.ok || !data) {
        throw new Error(
          this.localize.term("codeFirst_statusRequestFailed", responseStatus),
        );
      }

      this.status = data;
    } catch (error) {
      this.error =
        error instanceof Error
          ? error.message
          : this.localize.term("codeFirst_statusRequestFailedGeneric");
    }
  }

  private async openCleanup(): Promise<void> {
    this.cleanupError = undefined;
    this.cleanupResult = undefined;
    try {
      const result = await client.get<CleanupResponse>({
        security: [{ scheme: "bearer", type: "http" }],
        url: "/umbraco/management/api/v1/shorthandumbracocodefirstbackoffice/cleanup-preview",
        throwOnError: false,
      });
      if (!result.response?.ok || !result.data) {
        throw new Error(
          this.localize.term(
            "codeFirst_cleanupRequestFailed",
            result.response?.status ?? 0,
          ),
        );
      }

      this.cleanup = result.data as unknown as CleanupResponse;
    } catch (error) {
      this.cleanupError =
        error instanceof Error
          ? error.message
          : this.localize.term("codeFirst_cleanupRequestFailedGeneric");
      this.cleanup = { items: [] };
    }
  }

  private closeCleanup(): void {
    this.cleanup = undefined;
    this.cleanupResult = undefined;
    this.cleanupError = undefined;
  }

  private async applyCleanup(): Promise<void> {
    this.cleanupRunning = true;
    this.cleanupError = undefined;
    try {
      const result = await client.post<CleanupResult>({
        security: [{ scheme: "bearer", type: "http" }],
        url: "/umbraco/management/api/v1/shorthandumbracocodefirstbackoffice/cleanup",
        throwOnError: false,
      });
      if (!result.response?.ok || !result.data) {
        throw new Error(
          this.localize.term(
            "codeFirst_cleanupRequestFailed",
            result.response?.status ?? 0,
          ),
        );
      }

      this.cleanupResult = result.data as unknown as CleanupResult;
      await this.loadStatus();
    } catch (error) {
      this.cleanupError =
        error instanceof Error
          ? error.message
          : this.localize.term("codeFirst_cleanupRequestFailedGeneric");
    } finally {
      this.cleanupRunning = false;
    }
  }

  static override styles = css`
    :host {
      display: block;
      padding: var(--uui-size-layout-1);
    }

    uui-box {
      margin-bottom: var(--uui-size-layout-1);
    }

    .page-header {
      display: block;
      padding: var(--uui-size-layout-1);
    }

    .header-content {
      display: flex;
      align-items: flex-start;
      gap: var(--uui-size-4);
      min-width: 0;
    }

    .header-action {
      display: flex;
      flex: 0 0 auto;
      align-items: flex-start;
      margin-left: auto;
    }

    @media (max-width: 600px) {
      .header-content {
        flex-wrap: wrap;
      }

      .header-action {
        width: 100%;
        margin-left: 0;
      }
    }

    .cleanup-box {
      border: 2px solid var(--uui-color-danger);
    }

    .cleanup-buttons {
      display: flex;
      justify-content: flex-end;
      gap: var(--uui-size-3);
      margin-top: var(--uui-size-layout-1);
    }

    .cleanup-error {
      color: var(--uui-color-danger);
    }

    .cleanup-result {
      color: var(--uui-color-positive);
      font-weight: 700;
    }

    .header-icon {
      display: grid;
      flex: 0 0 auto;
      place-items: center;
      width: 3rem;
      height: 3rem;
      border-radius: 50%;
      color: var(--uui-color-interactive);
      background: var(--uui-color-surface-alt);
      font-size: 1.5rem;
    }

    .header-copy h1 {
      margin: 0;
      font-size: var(--uui-type-h3-size);
      line-height: 1.2;
    }

    .header-copy p {
      max-width: 42rem;
      margin: var(--uui-size-2) 0;
      color: var(--uui-color-text-alt);
    }

    .header-meta {
      display: flex;
      flex-wrap: wrap;
      gap: var(--uui-size-3);
      align-items: center;
      color: var(--uui-color-text-alt);
      font-size: 0.875rem;
    }

    .sync-status {
      display: inline-flex;
      gap: var(--uui-size-2);
      align-items: center;
      color: var(--uui-color-positive);
      font-weight: 700;
    }

    .sync-status-error,
    .summary-metric-failure,
    .status-error {
      color: var(--uui-color-danger);
    }

    .status-dot {
      width: 0.5rem;
      height: 0.5rem;
      border-radius: 50%;
      background: currentColor;
    }

    .summary {
      display: grid;
      grid-template-columns: repeat(6, minmax(5rem, 1fr));
      gap: 0;
      margin-top: var(--uui-size-layout-1);
      padding-top: var(--uui-size-1);
    }

    .summary-metric {
      display: flex;
      flex-direction: column;
      justify-content: center;
      padding: var(--uui-size-3);
    }

    .summary-metric + .summary-metric {
      border-left: 1px solid var(--uui-color-border);
    }

    .summary-metric strong {
      font-size: 1.5rem;
      line-height: 1;
    }

    .summary-metric span {
      margin-top: var(--uui-size-1);
      color: var(--uui-color-text-alt);
      font-size: 0.75rem;
    }

    .summary-metric-failure span {
      color: inherit;
    }

    @media (max-width: 900px) {
      .summary {
        grid-template-columns: repeat(3, minmax(4rem, 1fr));
      }

      .summary-metric {
        padding: var(--uui-size-2);
      }
    }

    @media (max-width: 600px) {
      .summary {
        grid-template-columns: repeat(3, minmax(4rem, 1fr));
      }
    }

    .definition-row {
      cursor: pointer;
    }

    .failed-row {
      background: color-mix(
        in srgb,
        var(--uui-color-danger) 6%,
        transparent
      );
    }

    .details-row {
      background: var(--uui-color-surface-alt);
    }

    .property-details {
      padding: var(--uui-size-2);
    }

    uui-table {
      width: 100%;
    }

    uui-table small,
    .error-message {
      display: block;
      color: var(--uui-color-danger);
      white-space: normal;
    }

    .failure-indicator {
      display: inline-grid;
      place-items: center;
      width: 1rem;
      height: 1rem;
      margin-right: var(--uui-size-1);
      border-radius: 50%;
      color: var(--uui-color-surface);
      background: var(--uui-color-danger);
      font-size: 0.75rem;
      font-weight: 700;
    }
  `;
}

declare global {
  interface HTMLElementTagNameMap {
    "shorthand-code-first-view": ShorthandCodeFirstViewElement;
  }
}
