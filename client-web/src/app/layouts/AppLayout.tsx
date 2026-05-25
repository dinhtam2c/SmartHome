import { Outlet } from "react-router-dom";
import Header from "./Header";
import BottomNav from "./BottomNav";
import styles from "./AppLayout.module.css";

export default function AppLayout() {
  return (
    <div className={styles["app"]}>
      <Header />
      <div className={styles["contentArea"]}>
        <div className={styles["mainContent"]}>
          <div className={styles["surface"]}>
            <Outlet />
          </div>
        </div>
      </div>

      <div className={styles["mobileNavSlot"]}>
        <BottomNav />
      </div>
    </div>
  );
}
