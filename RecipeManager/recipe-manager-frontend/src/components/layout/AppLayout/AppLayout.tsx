import { Box } from "@mui/material";
import styles from "./AppLayout.module.css";
import { Footer, Header } from "@/components";

interface AppLayoutProps {
  children: React.ReactNode;
}

export const AppLayout = ({ children }: AppLayoutProps) => {
  return (
    <div className={styles.appShell}>
      <Header />
      <Box component="main" className={styles.main} role="main">
        {children}
      </Box>
      <Footer />
    </div>
  );
};

export default AppLayout;
