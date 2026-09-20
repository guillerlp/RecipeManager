// src/components/ui/NavLink/NavLink.tsx

import { Link, useLocation } from 'react-router-dom';
import { isPathActive } from '@/utils/navPath';
import styles from "./NavLink.module.css";

interface NavLinkProps {
  to: string;
  children: React.ReactNode;
}

export const NavLink = ({ to, children }: NavLinkProps) => {
  const { pathname } = useLocation();
  const isActive = isPathActive(to, pathname);

  return (
    <Link 
      to={to} 
      className={`${styles.navLink} ${isActive ? styles.active : ''}`}
      aria-current={isActive ? 'page' : undefined}
      title={typeof children === 'string' ? children : undefined}
    >
      {children}
    </Link>
  );
};