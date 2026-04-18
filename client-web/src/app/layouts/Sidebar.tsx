import { NavLink } from "react-router-dom";
import { primaryNavigation } from "./navigation";
import styles from "./Sidebar.module.css";
import { useTranslation } from "react-i18next";

const linkClass = ({ isActive }: { isActive: boolean; }) =>
  isActive ? styles.active : undefined;

export default function SideBar() {
  const { t } = useTranslation();
  return (
    <aside className={styles.sidebar}>
      <div className={styles.header}>
        <h2 className={styles.heading}>{t('sidebar.primaryNavigation')}</h2>
      </div>

      <nav>
        <ul>
          {primaryNavigation.map((item) => (
            <li key={item.to}>
              <NavLink className={linkClass} to={item.to} end={item.end}>
                <span className={styles.linkLabel}>{t(item.labelKey)}</span>
                <span className={styles.linkDescription}>{t(item.descriptionKey)}</span>
              </NavLink>
            </li>
          ))}
        </ul>
      </nav>
    </aside>
  );
}
