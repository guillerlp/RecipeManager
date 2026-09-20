import styles from "./AppLayout.module.css";
import { BottomNav, Footer, Header } from "@/components";

interface AppLayoutProps {
  children: React.ReactNode;
}

export const AppLayout = ({ children }: AppLayoutProps) => {
  return (
    <div className={styles.appShell}>
      <Header />
      <main className={styles.main} role="main">
        {children}
      </main>
      <Footer />
      <BottomNav />
    </div>
  );
};

export default AppLayout;
