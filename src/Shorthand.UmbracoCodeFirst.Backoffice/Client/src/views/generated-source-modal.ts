import "@umbraco-cms/backoffice/code-editor";
import "@umbraco-cms/backoffice/components";
import type { UmbInputDropdownListElement } from "@umbraco-cms/backoffice/components";
import { css, customElement, html, state } from "@umbraco-cms/backoffice/external/lit";
import type { PropertyValues } from "@umbraco-cms/backoffice/external/lit";
import { UmbModalBaseElement } from "@umbraco-cms/backoffice/modal";
import { client } from "../api/client.gen.js";
import type { GeneratedSource } from "../types/code-first.types.js";
import type { GeneratedSourceModalData } from "./generated-source-modal.token.js";

@customElement("shorthand-generated-source-modal")
export default class ShorthandGeneratedSourceModalElement
  extends UmbModalBaseElement<GeneratedSourceModalData> {
  @state()
  private sources?: GeneratedSource[];

  @state()
  private error?: string;

  @state()
  private selectedIndex = 0;

  protected override updated(changed: PropertyValues<this>): void {
    super.updated(changed);
    if (changed.has("data") && this.data) {
      void this.loadSources(this.data.id);
    }
  }

  private async loadSources(id: string): Promise<void> {
    this.sources = undefined;
    this.error = undefined;
    this.selectedIndex = 0;
    try {
      const result = await client.get<GeneratedSource[]>({
        security: [{ scheme: "bearer", type: "http" }],
        url: `/umbraco/management/api/v1/shorthandumbracocodefirstbackoffice/generated-source/${encodeURIComponent(id)}`,
        throwOnError: false,
      });
      if (!result.response?.ok || !result.data) {
        throw new Error(this.localize.term("codeFirst_generatedSourceRequestFailed"));
      }

      this.sources = result.data;
    } catch (error) {
      this.error = error instanceof Error
        ? error.message
        : this.localize.term("codeFirst_generatedSourceRequestFailed");
    }
  }

  private selectSource(event: Event): void {
    this.selectedIndex = Number((event.target as UmbInputDropdownListElement).value);
  }

  override render() {
    const source = this.sources?.[this.selectedIndex];
    return html`
      <umb-body-layout .headline=${this.localize.term("codeFirst_generatedSourceHeadline", this.data?.name ?? "")}>
        <div class="content">
          ${this.error
            ? html`<p role="alert">${this.error}</p>`
            : !this.sources
              ? html`<uui-loader></uui-loader>`
              : !source
                ? html`<p>${this.localize.term("codeFirst_generatedSourceEmpty")}</p>`
                : html`
                    <div class="file-picker">
                      <span>${this.localize.term("codeFirst_generatedSourceFile")}</span>
                      <umb-input-dropdown-list
                        .name=${this.localize.term("codeFirst_generatedSourceFile")}
                        .options=${this.sources.map((file, index) => ({
                          name: file.isOriginal
                            ? this.localize.term("codeFirst_originalSourceFile", file.fileName)
                            : file.fileName,
                          value: String(index),
                          selected: index === this.selectedIndex,
                        }))}
                        @change=${this.selectSource}
                      ></umb-input-dropdown-list>
                    </div>
                    <umb-code-editor
                      language=${source.isOriginal && source.fileName.toLowerCase().endsWith(".cshtml") ? "razor" : "csharp"}
                      .label=${this.localize.term("codeFirst_generatedSourceEditor")}
                      .code=${source.content}
                      ?readonly=${true}
                      ?disable-minimap=${true}
                    ></umb-code-editor>
                  `}
        </div>
        <uui-button
          slot="actions"
          label=${this.localize.term("codeFirst_generatedSourceClose")}
          @click=${this._rejectModal}
        ></uui-button>
      </umb-body-layout>
    `;
  }

  static override styles = css`
    :host {
      display: block;
      width: min(80vw, 1100px);
    }

    .content {
      box-sizing: border-box;
      display: flex;
      flex-direction: column;
      gap: var(--uui-size-3);
      height: 65vh;
      min-width: 0;
      padding: var(--uui-size-layout-1);
    }

    .file-picker {
      display: flex;
      align-items: center;
      gap: var(--uui-size-3);
      min-width: 0;
    }

    umb-input-dropdown-list {
      min-width: 0;
      max-width: 100%;
    }

    umb-code-editor {
      flex: 1;
      min-height: 0;
      min-width: 0;
    }
  `;
}
