const DEFAULT_API_BASE_URL = import.meta.env.PROD
  ? "/api/v1"
  : "http://localhost:5151/api/v1";

export const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL ?? DEFAULT_API_BASE_URL;
