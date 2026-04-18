import { NavLink } from "react-router-dom";
import { primaryNavigation } from "./navigation";
import styles from "./BottomNav.module.css";
import { useTranslation } from "react-i18next";

const linkClass = ({ isActive }: { isActive: boolean; }) =>
  isActive ? styles.active : undefined;

export default function BottomNav() {
  const { t } = useTranslation();

  return (
    <nav className={styles.bottomNav} aria-label={t("sidebar.primaryNavigation")}>
      {primaryNavigation.map((item) => (
        <NavLink
          key={item.to}
          className={linkClass}
          to={item.to}
          end={item.end}
        >
          <span className={styles.label}>{t(item.labelKey)}</span>
          <span className={styles.description}>{t(item.descriptionKey)}</span>
        </NavLink>
      ))}
    </nav>
  );
}
