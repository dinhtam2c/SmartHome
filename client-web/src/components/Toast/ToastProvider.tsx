import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from "react";
import { createPortal } from "react-dom";
import { useTranslation } from "react-i18next";
import styles from "./ToastProvider.module.css";

type ToastTone = "info" | "success" | "error";

type ToastMessage = {
  id: string;
  message: string;
  tone: ToastTone;
};

type PushToastOptions = {
  message: string;
  tone?: ToastTone;
  durationMs?: number;
};

type ToastContextValue = {
  pushToast: (options: PushToastOptions) => void;
  dismissToast: (id: string) => void;
};

const DEFAULT_DURATION_MS = 4200;

const ToastContext = createContext<ToastContextValue | null>(null);

function createToastId() {
  return `${Date.now()}-${Math.random().toString(16).slice(2, 10)}`;
}

export function ToastProvider({ children }: { children: ReactNode; }) {
  const { t } = useTranslation();
  const [toasts, setToasts] = useState<ToastMessage[]>([]);
  const timerMapRef = useRef<Record<string, ReturnType<typeof setTimeout>>>({});

  const dismissToast = useCallback((id: string) => {
    const timer = timerMapRef.current[id];
    if (timer) {
      clearTimeout(timer);
      delete timerMapRef.current[id];
    }

    setToasts((current) => current.filter((toast) => toast.id !== id));
  }, []);

  const pushToast = useCallback(
    ({ message, tone = "info", durationMs = DEFAULT_DURATION_MS }: PushToastOptions) => {
      const normalizedMessage = message.trim();

      if (!normalizedMessage) {
        return;
      }

      const id = createToastId();
      const nextToast: ToastMessage = {
        id,
        message: normalizedMessage,
        tone,
      };

      setToasts((current) => [...current, nextToast]);
      timerMapRef.current[id] = setTimeout(() => {
        setToasts((current) => current.filter((toast) => toast.id !== id));
        delete timerMapRef.current[id];
      }, durationMs);
    },
    []
  );

  useEffect(
    () => () => {
      Object.values(timerMapRef.current).forEach((timer) => clearTimeout(timer));
      timerMapRef.current = {};
    },
    []
  );

  const contextValue = useMemo<ToastContextValue>(
    () => ({
      pushToast,
      dismissToast,
    }),
    [dismissToast, pushToast]
  );

  return (
    <ToastContext.Provider value={contextValue}>
      {children}
      {typeof document !== "undefined"
        ? createPortal(
          <div className={styles.viewport} aria-live="polite" aria-atomic="true">
            {toasts.map((toast) => (
              <div
                key={toast.id}
                className={`${styles.toast} ${styles[`tone${toast.tone[0].toUpperCase()}${toast.tone.slice(1)}`]}`}
                role="status"
              >
                <div className={styles.message}>{toast.message}</div>
                <button
                  type="button"
                  className={styles.closeButton}
                  onClick={() => dismissToast(toast.id)}
                  aria-label={t("close")}
                >
                  x
                </button>
              </div>
            ))}
          </div>,
          document.body
        )
        : null}
    </ToastContext.Provider>
  );
}

export function useToast() {
  const contextValue = useContext(ToastContext);

  if (!contextValue) {
    throw new Error("useToast must be used within ToastProvider");
  }

  return contextValue;
}
