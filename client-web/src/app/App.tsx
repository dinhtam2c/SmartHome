import { BrowserRouter } from "react-router-dom";
import { ToastProvider } from "@/components/Toast";
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
