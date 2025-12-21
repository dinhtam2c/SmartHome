import { Route, Routes } from "react-router-dom";
import AppLayout from "./layouts/AppLayout";
import HomePage from "@/features/homes/HomePage";
import GatewayList from "@/features/gateways/GatewayList";
import DeviceList from "@/features/devices/DeviceList";
import Environment from "@/features/environment/Environment";

export default function AppRoutes() {
  return (
    <Routes>
      <Route path="/" element={<AppLayout />}>
        <Route path="homes" element={<HomePage />} />
        <Route path="gateways" element={<GatewayList />} />
        <Route path="devices" element={<DeviceList />} />
        <Route path="environment" element={<Environment />} />
      </Route>
    </Routes>
  );
}
