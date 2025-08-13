// src/components/ui/Logo/Logo.tsx

import styles from './Logo.module.css';
import { Link } from 'react-router-dom';
import BlenderOutlinedIcon from '@mui/icons-material/BlenderOutlined';

export const Logo: React.FC = () => {
  return (
    <Link to="/" className={styles.logo}>
      <BlenderOutlinedIcon className={styles.icon} />
      <span className={styles.text}>Recipe Manager</span>
    </Link>
  );
};
