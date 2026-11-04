import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { html, customElement } from "@umbraco-cms/backoffice/external/lit";

@customElement("shorthand-code-first-workspace")
export default class ShorthandCodeFirstWorkspaceElement extends UmbLitElement {
  override render() {
    return html`
      <umb-workspace-editor
        headline=${this.localize.term("codeFirst_pageTitle")}
        .enforceNoFooter=${true}>
      </umb-workspace-editor>
    `;
  }
}

declare global {
  interface HTMLElementTagNameMap {
    "shorthand-code-first-workspace": ShorthandCodeFirstWorkspaceElement;
  }
}
