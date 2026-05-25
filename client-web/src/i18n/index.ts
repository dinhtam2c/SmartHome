import i18n from "i18next";
import { initReactI18next } from "react-i18next";

import en from "./locales/en/common.json";
import vi from "./locales/vi/common.json";
import enHomes from "./locales/en/homes.json";
import viHomes from "./locales/vi/homes.json";
import enScenes from "./locales/en/scenes.json";
import viScenes from "./locales/vi/scenes.json";
import enAutomations from "./locales/en/automations.json";
import viAutomations from "./locales/vi/automations.json";
import enRooms from "./locales/en/rooms.json";
import viRooms from "./locales/vi/rooms.json";
import enDevices from "./locales/en/devices.json";
import viDevices from "./locales/vi/devices.json";
import enCapabilities from "./locales/en/capabilities.json";
import viCapabilities from "./locales/vi/capabilities.json";
import enFloors from "./locales/en/floors.json";
import viFloors from "./locales/vi/floors.json";
import enDeviceCategories from "./locales/en/deviceCategories.json";
import viDeviceCategories from "./locales/vi/deviceCategories.json";

i18n
  .use(initReactI18next)
  .init({
    resources: {
      en: {
        translation: en,
        homes: enHomes,
        scenes: enScenes,
        automations: enAutomations,
        rooms: enRooms,
        devices: enDevices,
        deviceCategories: enDeviceCategories,
        capabilities: enCapabilities,
        floors: enFloors
      },
      vi: {
        translation: vi,
        homes: viHomes,
        scenes: viScenes,
        automations: viAutomations,
        rooms: viRooms,
        devices: viDevices,
        deviceCategories: viDeviceCategories,
        capabilities: viCapabilities,
        floors: viFloors
      }
    },
    lng: "vi", // default
    fallbackLng: "en",
    interpolation: {
      escapeValue: false
    }
  });

export default i18n;
