// src/pages/Profile/ProfilePage.tsx

import { LockOpenIcon } from '@/components/ui/Icon';
import { ThemeControl } from '@/components/ui/ThemeControl';
import styles from './ProfilePage.module.css';

export const ProfilePage: React.FC = () => {
  return (
    <div className={styles.page}>
      <header className={styles.header}>
        <div className={styles.monogram} aria-hidden="true">
          G
        </div>
        <div>
          {/* Hard-coded until R-14 gives the app a real user account. */}
          <h1 className={styles.name}>Guillermo</h1>
          <p className={styles.since}>Cooking from this catalogue since March 2026</p>
        </div>
      </header>

      <section className={styles.section}>
        <h2 className={styles.sectionTitle}>Preferences</h2>
        <div className={styles.row}>
          <div>
            <p className={styles.rowTitle}>Theme</p>
            <p className={styles.rowDescription}>The only place this lives now</p>
          </div>
          <ThemeControl />
        </div>
      </section>

      <section className={styles.section}>
        <h2 className={styles.sectionTitle}>Account</h2>
        <div className={styles.accountBlock}>
          <LockOpenIcon className={styles.accountIcon} />
          <p className={styles.accountTitle}>No sign-in yet</p>
          <p className={styles.accountBody}>
            This catalogue lives on your own server and anyone who can reach it can edit it.
            Email, password and per-recipe ownership are still to come — this block is where
            they'll land.
          </p>
          <div className={styles.accountActions}>
            {/* Not disabled buttons: these controls do not exist yet, this is a picture of
                where they will land, so they must not be focusable. aria-hidden keeps a screen
                reader from announcing "Sign in" / "Create account" as real options — the prose
                above already explains that sign-in is not built. */}
            <span className={styles.accountButton} aria-hidden="true">
              Sign in
            </span>
            <span className={styles.accountButton} aria-hidden="true">
              Create account
            </span>
          </div>
        </div>
      </section>
    </div>
  );
};

export default ProfilePage;
