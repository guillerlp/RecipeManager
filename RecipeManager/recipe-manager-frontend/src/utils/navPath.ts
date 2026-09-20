// Shared by NavLink (header) and BottomNav (mobile) so both treat a path as "current" under
// the same rule: a trailing slash doesn't matter, and a nested child path counts, but a
// same-prefix sibling (e.g. "/recipes-archive" against target "/recipes") does not.
const normalize = (path: string): string =>
  path.endsWith('/') && path !== '/' ? path.slice(0, -1) : path;

export const isPathActive = (target: string, pathname: string): boolean => {
  const normalizedTarget = normalize(target);
  const normalizedPathname = normalize(pathname);

  return (
    normalizedPathname === normalizedTarget ||
    (normalizedTarget !== '/' && normalizedPathname.startsWith(`${normalizedTarget}/`))
  );
};
