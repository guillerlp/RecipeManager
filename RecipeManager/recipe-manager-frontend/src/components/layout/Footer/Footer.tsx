import { useContext } from "react";
import styles from "./Footer.module.css";
import { ThemeContext } from "@/contexts";
import SunnyIcon from '@mui/icons-material/Sunny';
import DarkIcon from '@mui/icons-material/Bedtime';

export const Footer: React.FC = () => {
  const { theme, toggleTheme } = useContext(ThemeContext);
  const nextTheme = theme === "light" ? "dark" : "light";

  return (
    <footer className={styles.footer}>
      <p className={styles.text}>© {new Date().getFullYear()} Recipe Manager</p>

      <div className={styles.toggleWrapper}>
        <span className={styles.toggleLabel}>Theme</span>

        <button
          className={`${styles.toggle} ${theme === "dark" ? styles.dark : ""}`}
          onClick={toggleTheme}
          aria-label={`Switch to ${nextTheme} mode`}
          title={`Switch to ${nextTheme} mode`}
          type="button"
        >
          {/* Track icons */}
          <span className={`${styles.icon} ${styles.iconSun}`} aria-hidden="true">
            <SunnyIcon className={styles.iconToggle}/>
          </span>
          <span className={`${styles.icon} ${styles.iconMoon}`} aria-hidden="true">
            <DarkIcon className={styles.iconToggle}/>
          </span>

          {/* Sliding knob */}
          <span className={styles.knob} aria-hidden="true" />
        </button>
      </div>
    </footer>
  );
};

export default Footer;
