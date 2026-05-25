import { BrowserRouter } from "react-router-dom";
import { ToastProvider } from "@/shared/ui/Toast";
import AppRoutes from "./AppRoutes";

export default function App() {
  return (
    <ToastProvider>
      <BrowserRouter>
        <AppRoutes />
      </BrowserRouter>
    </ToastProvider>
  );
}
