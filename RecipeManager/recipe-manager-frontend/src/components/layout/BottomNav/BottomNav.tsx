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
