import { NavLink } from "react-router-dom";

export default function SideBar() {
  return (
    <aside className="sidebar">
      <nav>
        <ul>
          <li><NavLink to="/homes">Homes</NavLink></li>
          <li><NavLink to="/gateways">Gateways</NavLink></li>
          <li><NavLink to="/devices">Devices</NavLink></li>
          <li><NavLink to="/environment">Environment</NavLink></li>
        </ul>
      </nav>
    </aside>
  );
}
