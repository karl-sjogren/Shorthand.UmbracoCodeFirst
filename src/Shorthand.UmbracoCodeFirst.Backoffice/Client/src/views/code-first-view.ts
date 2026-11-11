import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { css, html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { client } from "../api/client.gen.js";
import type {
  CleanupResponse,
  CleanupResult,
  StatusItem,
  StatusResponse,
} from "../types/code-first.types.js";
import {
  formatCompletedAt,
  formatEditorAliases,
  getSynchronizationCssClass,
  getSynchronizationState,
  sortDefinitions,
} from "./code-first-view.helpers.js";

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
    return formatEditorAliases(definition.editorAlias, definition.editorUiAlias);
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
                class=${getSynchronizationCssClass(this.status.failedCount)}
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
          ${this.renderSummaryMetric(
            this.status.elementTypeCount,
            "codeFirst_elementTypes",
          )}
          ${this.renderSummaryMetric(
            this.status.mediaTypeCount,
            "codeFirst_mediaTypes",
          )}
          ${this.renderSummaryMetric(
            this.status.memberTypeCount,
            "codeFirst_memberTypes",
          )}
          ${this.renderSummaryMetric(this.status.createdCount, "codeFirst_created")}
          ${this.renderSummaryMetric(this.status.updatedCount, "codeFirst_updated")}
          ${this.renderSummaryMetric(this.status.skippedCount, "codeFirst_skipped")}
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
      ${this.renderDefinitionTable(
        this.localize.term("codeFirst_elementTypesHeadline"),
        this.status.elementTypes,
        true,
      )}
      ${this.renderDefinitionTable(
        this.localize.term("codeFirst_mediaTypesHeadline"),
        this.status.mediaTypes,
        true,
      )}
      ${this.renderDefinitionTable(
        this.localize.term("codeFirst_memberTypesHeadline"),
        this.status.memberTypes,
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
    const state = getSynchronizationState(
      this.status?.completedAt,
      this.status?.failedCount ?? 0,
    );
    if (state === "pending") {
      return this.localize.term("codeFirst_synchronizationPending");
    }

    return state === "failed"
      ? this.localize.term("codeFirst_synchronizationFailed")
      : this.localize.term("codeFirst_synchronizationComplete");
  }

  private formatCompletedAt(value: string): string {
    return formatCompletedAt(value);
  }

  private renderDefinitionTable(
    headline: string,
    definitions: StatusItem[],
    showProperties: boolean,
  ) {
    const sortedDefinitions = sortDefinitions(definitions);
    return html`
      <uui-box headline=${headline}>
        <table class="definition-table" aria-label=${headline}>
          <thead>
            <tr>
              <th scope="col">${this.localize.term("codeFirst_name")}</th>
              ${showProperties
                ? html`<th scope="col">${this.localize.term("codeFirst_alias")}</th>`
                : ""}
              ${showProperties
                ? html`<th scope="col">${this.localize.term("codeFirst_properties")}</th>`
                : html`<th scope="col">${this.localize.term("codeFirst_editor")}</th>`}
              <th scope="col">${this.localize.term("codeFirst_folder")}</th>
              <th scope="col">${this.localize.term("codeFirst_status")}</th>
            </tr>
          </thead>
          <tbody>
          ${sortedDefinitions.map(
            (definition) => html`
              <tr
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
                <td>
                  ${definition.backofficeUrl
                    ? html`<a
                        href=${definition.backofficeUrl}
                        @click=${(event: Event) => event.stopPropagation()}
                        >${definition.name}</a
                      >`
                    : definition.name}
                </td>
                ${showProperties
                  ? html`<td>${definition.alias}</td>`
                  : ""}
                <td>
                  ${showProperties
                    ? definition.propertyCount
                    : this.formatEditor(definition)}
                </td>
                <td>${definition.folder}</td>
                <td>
                  <span
                    class=${this.isFailed(definition) ? "status-error" : ""}
                  >
                    ${this.isFailed(definition)
                      ? html`<span class="failure-indicator" aria-hidden="true"
                          >!</span
                        >`
                      : ""}
                    ${definition.status}
                  </span>
                  ${definition.message
                    ? html`<small class=${this.isFailed(definition)
                        ? "status-message status-error"
                        : "status-message"}
                        >${definition.message}</small
                      >`
                    : ""}
                </td>
              </tr>
              ${this.expandedAlias === definition.alias
                ? html`
                    <tr class="details-row">
                      <td colspan=${showProperties ? 5 : 4}>
                        ${this.renderProperties(definition)}
                      </td>
                    </tr>
                  `
                : ""}
            `,
          )}
          </tbody>
        </table>
      </uui-box>
    `;
  }

  private renderProperties(definition: StatusItem) {
    return html`
      <div class="property-details">
        ${this.renderDefinitionSettings(definition)}
        ${definition.properties.length === 0
          ? html`<p>${this.localize.term("codeFirst_noProperties")}</p>`
          : ""}
        ${definition.properties.length > 0
          ? html`
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
          `
          : ""}
      </div>
    `;
  }

  private renderDefinitionSettings(definition: StatusItem) {
    const layout = [...definition.tabs, ...definition.groups];
    const hasSettings =
      Boolean(definition.description) ||
      Boolean(definition.icon) ||
      definition.allowAsRoot ||
      definition.allowInLibrary ||
      Boolean(definition.variations) ||
      definition.isListView ||
      Boolean(definition.defaultTemplateAlias) ||
      definition.allowedTemplateAliases.length > 0 ||
      definition.allowedChildAliases.length > 0 ||
      definition.compositionAliases.length > 0 ||
      layout.length > 0;

    if (!hasSettings) {
      return "";
    }

    return html`
      <div class="definition-settings">
        <strong>${this.localize.term("codeFirst_settings")}</strong>
        ${definition.description
          ? html`<p>${definition.description}</p>`
          : ""}
        <dl>
          ${definition.icon
            ? html`<dt>${this.localize.term("codeFirst_icon")}</dt>
                <dd class="definition-icon">
                  <umb-icon name=${definition.icon} aria-hidden="true"></umb-icon>
                  ${definition.icon}
                </dd>`
            : ""}
          ${definition.allowAsRoot
            ? html`<dt>${this.localize.term("codeFirst_allowedAsRoot")}</dt><dd>Yes</dd>`
            : ""}
          ${definition.allowInLibrary
            ? html`<dt>${this.localize.term("codeFirst_allowedInLibrary")}</dt><dd>Yes</dd>`
            : ""}
          ${definition.variations
            ? html`<dt>${this.localize.term("codeFirst_variations")}</dt><dd>${definition.variations}</dd>`
            : ""}
          ${definition.isListView
            ? html`<dt>${this.localize.term("codeFirst_listView")}</dt><dd>Yes</dd>`
            : ""}
          ${definition.defaultTemplateAlias
            ? html`<dt>${this.localize.term("codeFirst_defaultTemplate")}</dt><dd>${definition.defaultTemplateAlias}</dd>`
            : ""}
        </dl>
        ${this.renderAliasList(
          "codeFirst_allowedTemplates",
          definition.allowedTemplateAliases,
        )}
        ${this.renderAliasList(
          "codeFirst_allowedChildren",
          definition.allowedChildAliases,
        )}
        ${this.renderAliasList(
          "codeFirst_compositions",
          definition.compositionAliases,
        )}
        ${layout.length > 0
          ? html`<p>
              <strong>${this.localize.term("codeFirst_layout")}</strong>:
              ${layout.map((item) => `${item.name} (${item.alias})`).join(", ")}
            </p>`
          : ""}
      </div>
    `;
  }

  private renderAliasList(labelKey: string, aliases: string[]) {
    return aliases.length > 0
      ? html`<p><strong>${this.localize.term(labelKey)}</strong>: ${aliases.join(", ")}</p>`
      : "";
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

    .definition-table {
      width: 100%;
      border-spacing: 0;
      border: var(--uui-box-border-width, 1px) solid var(--uui-color-divider-standalone);
      border-radius: var(--uui-border-radius-3);
      background: var(--uui-color-surface);
    }

    .definition-table > thead > tr > th,
    .definition-table > tbody > tr > td {
      height: var(--uui-table-cell-height, var(--uui-size-12));
      padding: var(--uui-table-cell-padding, var(--uui-size-3) var(--uui-size-5));
      text-align: left;
      vertical-align: middle;
    }

    .definition-table > thead {
      position: sticky;
      top: 0;
      z-index: 1;
      background: var(--uui-color-surface);
    }

    .definition-table > tbody > tr > td {
      border-top: 1px solid var(--uui-color-border);
    }

    .definition-row:focus-visible {
      outline: calc(2px * var(--uui-show-focus-outline, 1)) solid var(--uui-color-focus);
      outline-offset: -4px;
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

    .definition-icon {
      display: flex;
      align-items: center;
      gap: var(--uui-size-2);
    }

    uui-table {
      width: 100%;
    }

    uui-table small,
    .definition-table small {
      display: block;
      color: var(--uui-color-danger);
      white-space: normal;
    }

    .definition-table small.status-message:not(.status-error) {
      color: inherit;
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
