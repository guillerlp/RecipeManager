// src/pages/NotFound/NotFoundPage.tsx

import { Link } from 'react-router-dom';
import styles from './NotFoundPage.module.css';

export const NotFoundPage: React.FC = () => {
  return (
    <section className={styles.wrapper}>
      <h1 className={styles.title}>Page not found</h1>
      <p className={styles.body}>
        The page you asked for does not exist. It may be a screen that is still to come.
      </p>
      <Link to="/" className={styles.link}>
        Back to home
      </Link>
    </section>
  );
};

export default NotFoundPage;
