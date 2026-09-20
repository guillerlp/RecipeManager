import { Link, useLocation } from 'react-router-dom';
import { AddCircleIcon, HomeIcon, MenuBookIcon, PersonIcon } from '@/components/ui/Icon';
import { isPathActive } from '@/utils/navPath';
import styles from './BottomNav.module.css';

const DESTINATIONS = [
  { to: '/', label: 'Home', Icon: HomeIcon },
  { to: '/recipes', label: 'Recipes', Icon: MenuBookIcon },
  { to: '/recipes/new', label: 'Add', Icon: AddCircleIcon },
  { to: '/profile', label: 'You', Icon: PersonIcon },
] as const;

export const BottomNav: React.FC = () => {
  const { pathname } = useLocation();

  // A nested path can match more than one destination (e.g. "/recipes/new" matches both
  // "/recipes" and "/recipes/new"). Only the longest (most specific) match is current, so
  // exactly one item ever carries aria-current="page".
  const activeTo = DESTINATIONS.filter(({ to }) => isPathActive(to, pathname)).reduce<string | null>(
    (longest, { to }) => (longest === null || to.length > longest.length ? to : longest),
    null,
  );

  return (
    // Header.tsx renders its own <nav aria-label="Primary navigation">. The duplicate label is
    // intentional and safe only because the two are mutually exclusive in CSS — see the
    // cross-referencing comments on the 768px breakpoint in this file's .module.css and in
    // Header.module.css. In a real browser exactly one is ever in the accessibility tree, since
    // `display: none` removes an element from it. Under Vitest/jsdom both render at once: CSS
    // Modules are stubbed out (vite.config.ts sets no `test.css`), so neither media query
    // applies and both <nav>s are present — a `getByRole('navigation', { name: ... })` against
    // the full app shell will throw "found multiple elements" (see App.test.tsx).
    <nav className={styles.bar} aria-label="Primary navigation">
      {DESTINATIONS.map(({ to, label, Icon }) => {
        const isActive = to === activeTo;
        return (
          <Link
            key={to}
            to={to}
            className={`${styles.item} ${isActive ? styles.active : ''}`}
            aria-current={isActive ? 'page' : undefined}
          >
            <Icon className={styles.icon} />
            <span className={styles.label}>{label}</span>
          </Link>
        );
      })}
    </nav>
  );
};
