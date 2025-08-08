// src/components/ui/NavLink/NavLink.tsx

import { Link, useLocation } from 'react-router-dom';
import styles from "./NavLink.module.css";

interface NavLinkProps {
  to: string;
  children: React.ReactNode;
}

export const NavLink = ({ to, children }: NavLinkProps) => {
  const location = useLocation();
  const isActive = location.pathname === to;
  
  return (
    <Link 
      to={to} 
      className={`${styles.navLink} ${isActive ? styles.active : ''}`}
    >
      {children}
    </Link>
  );
};