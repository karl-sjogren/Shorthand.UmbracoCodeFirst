# Explicit cleanup

Normal synchronization is intentionally non-destructive: removing a declaration from C# does not
delete the corresponding Umbraco definition. The Code First backoffice page provides an explicit
alternative when obsolete definitions should be removed.

## Workflow

1. Open **Settings > Advanced > Code First**.
2. Select **Clean up obsolete items**.
3. Review the preview table. It lists the kind, name, alias and parent of every candidate.
4. Select **Cancel** to leave everything untouched, or **Proceed with cleanup** to apply the list.
5. The result reports the number removed and any individual failures. Refreshing the status tables
   shows the remaining generated definitions.

The cleanup is never run automatically during synchronization. The preview and apply operations
both recalculate the candidates from the current registry, so the apply operation does not trust a
stale client-side list.

## Candidates

The service can remove:

- owned Data Types no longer present in the generated registry;
- owned Document Types no longer present in the generated registry;
- direct properties on owned Document Types that are no longer declared;
- empty obsolete direct groups and tabs on owned Document Types.

Properties, groups and tabs that come from a composition are excluded. An obsolete group is only
removed after its obsolete direct properties have been removed and it has no remaining properties.

## Safety rules

- Only items with the existing code-first ownership markers are candidates.
- Manual Data Types and Document Types are never included.
- Manual compositions and composition-owned properties/groups are preserved.
- Each deletion is attempted independently. A failure is displayed with its message and does not
  prevent other candidates from being attempted.
- Umbraco may reject deletion when the item is referenced or otherwise cannot be removed; that
  rejection is reported rather than hidden.

Direct properties and layout items on an owned Document Type are treated as code-first-managed
because the current persistence model does not yet store a separate ownership marker for each
property/group. Review the preview carefully before confirming. A future version may add more
granular ownership metadata.

## API

The backoffice endpoints are protected by Umbraco's backoffice access policy:

- `GET /umbraco/management/api/v1/shorthandumbracocodefirstbackoffice/cleanup-preview`
- `POST /umbraco/management/api/v1/shorthandumbracocodefirstbackoffice/cleanup`

The POST endpoint has no automatic or synchronization trigger; it is only called after the user
confirms in the view.
