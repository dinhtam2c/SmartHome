import { Route, Routes } from "react-router-dom";
import MainLayout from "../layouts/MainLayout";
import HomePage from "../features/homes/HomePage";
import GatewayList from "../features/gateways/GatewayList";
import DeviceList from "../features/devices/DeviceList";
import Environment from "../features/environment/Environment";

export default function AppRoutes() {
  return (
    <Routes>
      <Route path="/" element={<MainLayout />}>
        <Route path="homes" element={<HomePage />} />
        <Route path="gateways" element={<GatewayList />} />
        <Route path="devices" element={<DeviceList />} />
        <Route path="environment" element={<Environment />} />
      </Route>
    </Routes>
  );
}
