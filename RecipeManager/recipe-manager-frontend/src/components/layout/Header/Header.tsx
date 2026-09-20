// components/layout/header/Header.tsx
import { Link } from 'react-router-dom';
import { Logo, NavLink } from '@/components/ui';
import { AddIcon } from '@/components/ui/Icon';
import styles from './Header.module.css';

export const Header: React.FC = () => {
  return (
    <header className={styles.headerBar}>
      <div className={styles.brand}>
        <Logo />
      </div>

      <div className={styles.actions}>
        <nav aria-label="Primary navigation">
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

        <Link to="/recipes/new" className={styles.newRecipe}>
          <AddIcon className={styles.newRecipeIcon} />
          New recipe
        </Link>
      </div>
    </header>
  );
};

export default Header;
