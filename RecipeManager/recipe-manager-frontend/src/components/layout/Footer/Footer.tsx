import { Link } from "react-router-dom";
import styles from "./Footer.module.css";
import { useTheme } from "@/hooks";
import { BedtimeIcon, SunnyIcon } from "@/components/ui/Icon";

export const Footer: React.FC = () => {
  const { theme, toggleTheme } = useTheme();
  const nextTheme = theme === "light" ? "dark" : "light";

  return (
    <footer className={styles.footer}>
      <p className={styles.text}>© {new Date().getFullYear()} Recipe Manager</p>

      <div className={styles.actions}>
        <div className={styles.toggleWrapper}>
          <span className={styles.toggleLabel}>Theme</span>

          <button
            className={`${styles.toggle} ${theme === "dark" ? styles.dark : ""}`}
            onClick={toggleTheme}
            aria-label={`Switch to ${nextTheme} mode`}
            title={`Switch to ${nextTheme} mode`}
            type="button"
            role="switch"
            aria-checked={theme === 'dark'}
            aria-pressed={theme === 'dark'}
          >
            <span className={`${styles.icon} ${styles.iconSun}`} aria-hidden="true">
              <SunnyIcon className={styles.iconToggle}/>
            </span>
            <span className={`${styles.icon} ${styles.iconMoon}`} aria-hidden="true">
              <BedtimeIcon className={styles.iconToggle}/>
            </span>

            <span className={styles.knob} aria-hidden="true" />
          </button>
        </div>

        <Link to="/profile" className={styles.settingsLink}>
          Settings
        </Link>
      </div>
    </footer>
  );
};

export default Footer;
