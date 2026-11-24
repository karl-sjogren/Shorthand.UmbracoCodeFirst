import { UMB_COLLECTION_CONTEXT, UmbCollectionViewElementBase } from "@umbraco-cms/backoffice/collection";
import type { UmbCollectionColumnConfiguration, UmbDefaultCollectionContext } from "@umbraco-cms/backoffice/collection";
import "@umbraco-cms/backoffice/components";
import type { UmbTableColumn, UmbTableElement, UmbTableItem } from "@umbraco-cms/backoffice/components";
import type { UmbDocumentCollectionItemModel } from "@umbraco-cms/backoffice/document";
import { UmbAncestorsEntityContext, UmbEntityContext } from "@umbraco-cms/backoffice/entity";
import { customElement, html, property, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { blogColumnValue, formatPublishDate } from "./blog-index-collection-view.helpers.js";

@customElement("shorthand-blog-index-actions")
export class BlogIndexActions extends UmbLitElement {
  #entity = new UmbEntityContext(this);
  #ancestors = new UmbAncestorsEntityContext(this);

  @property({ attribute: false })
  set value(item: UmbDocumentCollectionItemModel | undefined) {
    this.#entity.setEntityType(item?.entityType);
    this.#entity.setUnique(item?.unique ?? null);
    this.#ancestors.setAncestors(item?.ancestors ?? []);
  }

  override render() {
    return html`<umb-entity-actions-bundle></umb-entity-actions-bundle>`;
  }
}

@customElement("shorthand-blog-index-collection-view")
export class BlogIndexCollectionView extends UmbCollectionViewElementBase<UmbDocumentCollectionItemModel> {
  @state() private columns: UmbCollectionColumnConfiguration[] = [];
  @state() private orderBy = "";
  @state() private orderDirection = "";
  #context?: UmbDefaultCollectionContext;

  constructor() {
    super();
    this.consumeContext(UMB_COLLECTION_CONTEXT, (context) => {
      this.#context = context;
      context?.setupView(this);
      this.observe(context?.userDefinedProperties, (columns) => { this.columns = columns ?? []; });
      this.observe(context?.filter, (filter) => {
        const order = filter as { orderBy?: string; orderDirection?: string } | undefined;
        this.orderBy = order?.orderBy ?? "";
        this.orderDirection = order?.orderDirection ?? "";
      });
    });
  }

  override render() {
    const columns: UmbTableColumn[] = this.columns.map((column) => ({
      name: this.localize.string(column.header),
      alias: column.alias,
      allowSorting: true,
    }));
    if (!this._hideItemActions) {
      columns.push({ name: "", alias: "actions", elementName: "shorthand-blog-index-actions", align: "right" });
    }

    const items: UmbTableItem[] = this._items.map((item) => ({
      id: item.unique,
      icon: item.contentType?.icon ?? item.documentType.icon,
      entityType: item.entityType,
      selectable: this._isSelectableItem(item),
      data: [
        ...this.columns.map((column, index) => {
          const value = blogColumnValue(item, column.alias);
          const text = column.alias === "publishDate" ? formatPublishDate(value) : value;
          const href = this._itemHrefs.get(item.unique);
          return {
            columnAlias: column.alias,
            value: index === 0 && href && !this._selectOnly
              ? html`<a href=${href}>${text || item.variants[0]?.name || ""}</a>`
              : text,
          };
        }),
        { columnAlias: "actions", value: item },
      ],
    }));

    return html`
      <umb-table
        .columns=${columns}
        .items=${items}
        .config=${{ allowSelection: this._selectable, allowSelectAll: this._multiple, selectOnly: this._selectOnly }}
        .selection=${this._selection}
        .orderingColumn=${this.orderBy}
        .orderingDesc=${this.orderDirection === "desc"}
        @selected=${this.#selectionChanged}
        @deselected=${this.#selectionChanged}
        @ordered=${this.#orderingChanged}>
      </umb-table>
    `;
  }

  #selectionChanged(event: Event) {
    event.stopPropagation();
    this._setSelection((event.target as UmbTableElement).selection);
  }

  #orderingChanged(event: Event) {
    const table = event.target as UmbTableElement;
    const filter = {
      skip: 0,
      orderBy: table.orderingColumn,
      orderDirection: table.orderingDesc ? "desc" : "asc",
    };
    this.#context?.setFilter(filter);
  }
}

export default BlogIndexCollectionView;
