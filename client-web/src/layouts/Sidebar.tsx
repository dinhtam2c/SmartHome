import { Link } from "react-router-dom";

export default function SideBar() {
  return (
    <aside className="sidebar">
      <nav>
        <ul>
          <li><Link to="/homes">Homes</Link></li>
          <li><Link to="/gateways">Gateways</Link></li>
          <li><Link to="/devices">Devices</Link></li>
          <li><Link to="/environment">Environment</Link></li>
        </ul>
      </nav>
    </aside>
  );
}
