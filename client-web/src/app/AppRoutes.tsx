import { Navigate, Route, Routes } from "react-router-dom";
import AppLayout from "./layouts/AppLayout";
import { HomesPage, HomeDetailPage } from "@/features/homes";
import { FloorPage } from "@/features/floors";
import { RoomDetailPage } from "@/features/rooms";
import { DeviceDetailPage } from "@/features/devices";

export default function AppRoutes() {
  return (
    <Routes>
      <Route path="/" element={<AppLayout />}>
        <Route index element={<Navigate to="homes" replace />} />

        <Route path="homes" element={<HomesPage />} />
        <Route path="homes/:homeId" element={<HomeDetailPage />} />
        <Route path="homes/:homeId/floors" element={<FloorPage />} />
        <Route path="homes/:homeId/floors/:floorId" element={<FloorPage />} />
        <Route
          path="homes/:homeId/rooms/:roomId"
          element={<RoomDetailPage />}
        />
        <Route
          path="homes/:homeId/rooms/:roomId/devices/:deviceId"
          element={<DeviceDetailPage />}
        />
        <Route
          path="homes/:homeId/devices/:deviceId"
          element={<DeviceDetailPage />}
        />
      </Route>
    </Routes>
  );
}
