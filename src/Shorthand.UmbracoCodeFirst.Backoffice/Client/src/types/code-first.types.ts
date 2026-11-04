export type LayoutItem = {
  alias: string;
  name: string;
  sortOrder: number;
};

export type StatusProperty = {
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

export type StatusItem = {
  kind: string;
  alias: string;
  name: string;
  folder: string;
  description?: string;
  icon?: string;
  allowAsRoot: boolean;
  allowInLibrary: boolean;
  defaultTemplateAlias?: string;
  allowedTemplateAliases: string[];
  allowedChildAliases: string[];
  variations?: string;
  isListView: boolean;
  compositionAliases: string[];
  groups: LayoutItem[];
  tabs: LayoutItem[];
  editorAlias?: string;
  editorUiAlias?: string;
  isElement: boolean;
  propertyCount: number;
  status: string;
  message?: string;
  backofficeUrl?: string;
  properties: StatusProperty[];
};

export type StatusResponse = {
  definitionCount: number;
  dataTypeCount: number;
  documentTypeCount: number;
  elementTypeCount: number;
  mediaTypeCount: number;
  memberTypeCount: number;
  createdCount: number;
  updatedCount: number;
  unchangedCount: number;
  skippedCount: number;
  failedCount: number;
  completedAt: string;
  dataTypes: StatusItem[];
  documentTypes: StatusItem[];
  elementTypes: StatusItem[];
  mediaTypes: StatusItem[];
  memberTypes: StatusItem[];
};

export type CleanupItem = {
  kind: string;
  alias: string;
  name: string;
  parentAlias?: string;
};

export type CleanupResponse = {
  items: CleanupItem[];
};

export type CleanupResult = {
  removedCount: number;
  removed: CleanupItem[];
  failed: { item: CleanupItem; message: string }[];
};
