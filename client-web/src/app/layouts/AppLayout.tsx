import { Outlet } from "react-router-dom";
import Header from "./Header";
import BottomNav from "./BottomNav";
import { primaryNavigation } from "./navigation";
import styles from "./AppLayout.module.css";

export default function AppLayout() {
  const hasMobileNavigation = primaryNavigation.length > 1;

  return (
    <div
      className={`${styles["app"]} ${hasMobileNavigation ? styles["hasMobileNavigation"] : ""}`}
    >
      <Header />
      <div className={styles["contentArea"]}>
        <div className={styles["mainContent"]}>
          <div className={styles["surface"]}>
            <Outlet />
          </div>
        </div>
      </div>

      {hasMobileNavigation && (
        <div className={styles["mobileNavSlot"]}>
          <BottomNav />
        </div>
      )}
    </div>
  );
}
