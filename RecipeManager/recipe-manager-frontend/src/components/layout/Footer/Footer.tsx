import { Link } from "react-router-dom";
import styles from "./Footer.module.css";

export const Footer: React.FC = () => {
  return (
    <footer className={styles.footer}>
      <p className={styles.text}>© {new Date().getFullYear()} Recipe Manager</p>

      <Link to="/profile" className={styles.settingsLink}>
        Settings
      </Link>
    </footer>
  );
};

export default Footer;
