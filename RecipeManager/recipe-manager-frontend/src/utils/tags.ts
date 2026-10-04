// The normalisation the server applies to stored tags (spec 014): trim, collapse whitespace, lowercase. Shared by
// the ?tag= filter (a hand-typed ?tag=Roast still matches "roast") and the form's chip editor.
// An empty or all-whitespace value normalises to undefined: no tag.
export const normaliseTag = (value: string | null | undefined): string | undefined => {
  const tag = value?.trim().replace(/\s+/g, ' ').toLowerCase();
  return tag === '' ? undefined : tag;
};
