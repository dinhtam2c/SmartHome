import { Navigate, Route, Routes } from "react-router-dom";
import AppLayout from "./layouts/AppLayout";
import {
  DashboardHomeListPage,
  DashboardHomePage,
  DashboardLocationPage,
} from "@/features/dashboard";
import { HomesPage } from "@/features/homes";
import { GatewaysPage } from "@/features/gateways";
import { DevicesPage } from "@/features/devices";

export default function AppRoutes() {
  return (
    <Routes>
      <Route path="/" element={<AppLayout />}>
        <Route index element={<Navigate to="dashboard" replace />} />

        <Route path="dashboard">
          <Route index element={<DashboardHomeListPage />} />
          <Route path="home/:homeId" element={<DashboardHomePage />} />
          <Route
            path="location/:locationId"
            element={<DashboardLocationPage />}
          />
        </Route>

        <Route path="homes" element={<HomesPage />} />
        <Route path="gateways" element={<GatewaysPage />} />
        <Route path="devices" element={<DevicesPage />} />
      </Route>
    </Routes>
  );
}
