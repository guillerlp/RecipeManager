// components/layout/header/Header.tsx
import { Logo } from '@/components/ui';
import styles from './Header.module.css';
import { NavLink } from '@/components/ui/NavLink/NavLink.js';

export const Header: React.FC = () => {
  return (
    <header className={styles.headerBar}>
      <div className={styles.brand}>
        <Logo />
      </div>

      <nav aria-label="Primary navigation" className={styles.nav}>
        <ul className={styles.navList}>
          <li>
            <NavLink to="/">
              Home
            </NavLink>
          </li>
          <li>
            <NavLink to="/recipes">
              Recipes
            </NavLink>
          </li>
          <li>
            <NavLink to="/profile">
              Profile
            </NavLink>
          </li>
        </ul>
      </nav>
    </header>
  );
};

export default Header;
