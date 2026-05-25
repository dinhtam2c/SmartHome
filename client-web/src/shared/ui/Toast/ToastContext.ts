import { createContext } from "react";

export type ToastTone = "info" | "success" | "error";

export type ToastMessage = {
  id: string;
  message: string;
  tone: ToastTone;
};

export type PushToastOptions = {
  message: string;
  tone?: ToastTone;
  durationMs?: number;
};

export type ToastContextValue = {
  pushToast: (options: PushToastOptions) => void;
  dismissToast: (id: string) => void;
};

export const ToastContext = createContext<ToastContextValue | null>(null);
