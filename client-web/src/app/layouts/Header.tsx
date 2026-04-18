import { Link } from "react-router-dom";
import styles from "./Header.module.css";
import { useTranslation } from "react-i18next";
import i18n from "@/i18n";

export default function Header() {
  const { t } = useTranslation();

  const changeLanguage = (lang: string) => {
    i18n.changeLanguage(lang);
    localStorage.setItem("lang", lang);
  };

  return (
    <header className={styles.header}>
      <div className={styles.brand}>
        <Link className={styles.title} to="/homes">
          {t('header.title')}
        </Link>
      </div>

      <div className={styles.spacer}>
        <button
          onClick={() => changeLanguage("en")}
          id="langBtnEn"
          className={styles.langBtnEn}
        >
          EN
        </button>
        <button
          onClick={() => changeLanguage("vi")}
          id="langBtnVi"
          className={styles.langBtnVi}
        >
          VI
        </button>
      </div>
    </header>
  );
}
