import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { css, html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { client } from "../api/client.gen.js";

type StatusItem = {
  kind: string;
  alias: string;
  name: string;
  propertyCount: number;
  status: string;
  message?: string;
};

type StatusResponse = {
  definitionCount: number;
  createdCount: number;
  updatedCount: number;
  unchangedCount: number;
  failedCount: number;
  completedAt: string;
  definitions: StatusItem[];
};

@customElement("shorthand-code-first-view")
export default class ShorthandCodeFirstViewElement extends UmbLitElement {
  @state()
  private status?: StatusResponse;

  @state()
  private error?: string;

  connectedCallback(): void {
    super.connectedCallback();
    void this.loadStatus();
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
      <uui-box headline="Code-first status">
        <div class="summary">
          <strong>${this.status.definitionCount}</strong>
          <span>generated definitions</span>
          <strong>${this.status.createdCount}</strong>
          <span>created</span>
          <strong>${this.status.updatedCount}</strong>
          <span>updated</span>
          <strong>${this.status.failedCount}</strong>
          <span>failed</span>
        </div>
      </uui-box>
      <uui-box headline="Definitions">
        <uui-table>
          <uui-table-head>
            <uui-table-head-cell>Type</uui-table-head-cell>
            <uui-table-head-cell>Name</uui-table-head-cell>
            <uui-table-head-cell>Alias</uui-table-head-cell>
            <uui-table-head-cell>Properties</uui-table-head-cell>
            <uui-table-head-cell>Status</uui-table-head-cell>
          </uui-table-head>
          ${this.status.definitions.map(
            (definition) => html`
              <uui-table-row>
                <uui-table-cell>${definition.kind}</uui-table-cell>
                <uui-table-cell>${definition.name}</uui-table-cell>
                <uui-table-cell>${definition.alias}</uui-table-cell>
                <uui-table-cell>${definition.propertyCount}</uui-table-cell>
                <uui-table-cell>${definition.status}</uui-table-cell>
              </uui-table-row>
            `,
          )}
        </uui-table>
      </uui-box>
    `;
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
        throw new Error(`The status request failed with HTTP ${responseStatus}.`);
      }

      this.status = data;
    } catch (error) {
      this.error = error instanceof Error ? error.message : "The status request failed.";
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

    .summary {
      display: grid;
      grid-template-columns: repeat(4, auto);
      gap: var(--uui-size-2);
      align-items: baseline;
    }
  `;
}

declare global {
  interface HTMLElementTagNameMap {
    "shorthand-code-first-view": ShorthandCodeFirstViewElement;
  }
}
