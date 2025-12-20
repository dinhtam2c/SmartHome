import { Outlet } from "react-router-dom";
import SideBar from "./Sidebar";
import Header from "./Header";

export default function MainLayout() {
  return (
    <div className="app">
      <Header />
      <div className="content-area">
        <SideBar />
        <div className="main-content">
          <Outlet />
        </div>
      </div>
    </div>
  );
}

