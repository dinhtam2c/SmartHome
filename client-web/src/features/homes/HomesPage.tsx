import { useEffect, useRef } from "react";
import { useTranslation } from "react-i18next";
import { useLocation, useNavigate } from "react-router-dom";
import { Button } from "@/components/Button";
import { PageHeader } from "@/components/PageHeader";
import { HomesOverviewSection } from "./components/HomesOverviewSection";
import { CreateHomeModal } from "./components/CreateHomeModal";
import { EditHomeModal } from "./components/EditHomeModal";
import { useHomesPageController } from "./hooks/useHomesPageController";
import styles from "@/features/shared/featurePage.module.css";

type HomesPageLocationState = {
  skipAutoEnter?: boolean;
};

export function HomesPage() {
  const { t } = useTranslation("homes");
  const navigate = useNavigate();
  const location = useLocation();
  const vm = useHomesPageController();
  const hasAutoEnteredSingleHomeRef = useRef(false);

  const skipAutoEnter = Boolean(
    (location.state as HomesPageLocationState | null)?.skipAutoEnter
  );

  useEffect(() => {
    if (hasAutoEnteredSingleHomeRef.current) return;
    if (vm.isLoading || vm.error) return;
    if (skipAutoEnter) return;
    if (vm.homes.length !== 1) return;

    hasAutoEnteredSingleHomeRef.current = true;
    navigate(`/homes/${vm.homes[0].id}`, { replace: true });
  }, [navigate, skipAutoEnter, vm.error, vm.homes, vm.isLoading]);

  if (vm.isLoading) {
    return <div className={styles.emptyState}>{t('loading')}</div>;
  }

  if (vm.error) {
    return <div className={styles.emptyState}>{t('failed')}</div>;
  }

  return (
    <div className={styles.pageStack}>
      <PageHeader
        title={t('title')}
        action={
          <Button size="sm" onClick={vm.openCreateModal}>
            {t('addHome')}
          </Button>
        }
      />

      <HomesOverviewSection
        homes={vm.homes}
        filteredHomes={vm.filteredHomes}
        query={vm.query}
        onQueryChange={vm.setQuery}
        onOpenHome={(selectedHomeId) => navigate(`/homes/${selectedHomeId}`)}
        onOpenHomeEdit={vm.openEditModal}
      />

      <CreateHomeModal
        open={vm.isCreateOpen}
        onClose={vm.closeCreateModal}
        onSubmit={vm.handleCreateHome}
        name={vm.newName}
        onNameChange={vm.setNewName}
        description={vm.newDescription}
        onDescriptionChange={vm.setNewDescription}
        isCreating={vm.isCreating}
        error={vm.createError}
      />

      <EditHomeModal
        open={vm.isEditOpen}
        title={t("detail.edit")}
        onClose={vm.closeEditModal}
        onSubmit={vm.handleSaveHome}
        name={vm.editName}
        onNameChange={vm.setEditName}
        description={vm.editDescription}
        onDescriptionChange={vm.setEditDescription}
        isSaving={vm.isEditing}
        error={vm.editError}
      />
    </div>
  );
}
