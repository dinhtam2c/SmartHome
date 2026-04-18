import i18n from "i18next";
import { initReactI18next } from "react-i18next";

import en from "./locales/en/common.json";
import vi from "./locales/vi/common.json";
import enHomes from "./locales/en/homes.json";
import viHomes from "./locales/vi/homes.json";
import enRooms from "./locales/en/rooms.json";
import viRooms from "./locales/vi/rooms.json";
import enDevices from "./locales/en/devices.json";
import viDevices from "./locales/vi/devices.json";
import enCapabilities from "./locales/en/capabilities.json";
import viCapabilities from "./locales/vi/capabilities.json";

i18n
  .use(initReactI18next)
  .init({
    resources: {
      en: {
        translation: en,
        homes: enHomes,
        rooms: enRooms,
        devices: enDevices,
        capabilities: enCapabilities
      },
      vi: {
        translation: vi,
        homes: viHomes,
        rooms: viRooms,
        devices: viDevices,
        capabilities: viCapabilities
      }
    },
    lng: "vi", // default
    fallbackLng: "en",
    interpolation: {
      escapeValue: false
    }
  });

export default i18n;
