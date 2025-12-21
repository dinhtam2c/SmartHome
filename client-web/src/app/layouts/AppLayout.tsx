import { Outlet } from "react-router-dom";
import SideBar from "./Sidebar";
import Header from "./Header";
import styles from "./AppLayout.module.css";

export default function AppLayout() {
  return (
    <div className={styles["app"]}>
      <Header />
      <div className={styles["content-area"]}>
        <SideBar />
        <div className={styles["main-content"]}>
          <Outlet />
        </div>
      </div>
    </div>
  );
}
