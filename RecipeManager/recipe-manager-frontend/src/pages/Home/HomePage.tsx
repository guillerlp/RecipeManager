// src/pages/Home/HomePage.tsx
import { NavLink } from '@/components/ui/NavLink';
import styles from './HomePage.module.css';
import { Box } from '@mui/material';
import Logo from '../../assets/mainPhoto.png';

export const HomePage: React.FC = () => {
  return (
    <section className={styles.heroSection}>
      <section className={styles.contentSection}>
        <header className={styles.heroHeader}>
          <h1 className={styles.heroTitle}>
            Welcome to Recipe{' '}
            <span className={styles.titleBreak}>Manager</span>
          </h1>
          <p className={styles.heroSubtitle}>
            Organize and manage your recipes with ease
          </p>
        </header>
        
        <nav 
          className={styles.actionButtons} 
          aria-label="Main recipe management actions"
        >
          <Box className={styles.actionButton}>
            <NavLink 
              to='/recipes'
              aria-label="View all your saved recipes"
            >
              View Recipes
            </NavLink>
          </Box>
          <Box className={styles.actionButton}>
            <NavLink 
              to='/recipes/new'
              aria-label="Create a new recipe"
            >
              Add Recipe
            </NavLink>
          </Box>
        </nav>
      </section>
      
      <aside className={styles.heroImageContainer}>
        <img 
          src={Logo} 
          className={styles.heroImage} 
          alt="Recipe management illustration showing a document with cooking bowl and checklist"
          loading="lazy"
          decoding="async"
        />
      </aside>
    </section>
  );
};

export default HomePage;