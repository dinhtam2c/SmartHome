import { NavLink } from "react-router-dom";
import styles from "./Sidebar.module.css";

const linkClass = ({ isActive }: { isActive: boolean }) =>
  isActive ? styles.active : undefined;

export default function SideBar() {
  return (
    <aside className={styles.sidebar}>
      <nav>
        <ul>
          <li>
            <NavLink className={linkClass} to="/dashboard">
              Dashboard
            </NavLink>
          </li>
          <li>
            <NavLink className={linkClass} to="/homes">
              Homes
            </NavLink>
          </li>
          <li>
            <NavLink className={linkClass} to="/gateways">
              Gateways
            </NavLink>
          </li>
          <li>
            <NavLink className={linkClass} to="/devices">
              Devices
            </NavLink>
          </li>
        </ul>
      </nav>
    </aside>
  );
}
