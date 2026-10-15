<template>
  <VcBlade
    :title="$t('EXPORT.PAGES.PROPERTIES.TITLE')"
    :toolbar-items="bladeToolbar"
    width="50%"
  >
    <VcDataTable
      v-model:search-value="searchValue"
      v-model:selection="selection"
      selection-mode="multiple"
      :items="visible"
      :searchable="true"
      state-key="EXPORT_PROPERTIES"
    >
      <VcColumn
        id="title"
        :title="$t('EXPORT.PAGES.PROPERTIES.COLUMNS.NAME')"
        body-class="export-list__name"
        always-visible
      />
      <VcColumn
        id="group"
        :title="$t('EXPORT.PAGES.PROPERTIES.COLUMNS.GROUP')"
      />
    </VcDataTable>
  </VcBlade>
</template>

<script setup lang="ts">
import { useBlade, type IBladeToolbar } from "@vc-shell/framework";
import { VcBlade, VcColumn, VcDataTable } from "@vc-shell/framework/ui";
import { computed, ref } from "vue";
import { useI18n } from "vue-i18n";
import type { ExportedTypePropertyInfo } from "../../api_client/virtocommerce.export";

defineBlade({
  url: "/generic-export-properties",
  name: "ExportPropertiesSelector",
});

type Row = ExportedTypePropertyInfo & { id: string; title: string };

const { t } = useI18n({ useScope: "global" });
const { options, callParent, closeSelf } = useBlade<{ all: ExportedTypePropertyInfo[]; included: ExportedTypePropertyInfo[] }>();

// sorted by group, then name, as the AngularJS selector grouped them
const rows = computed<Row[]>(() =>
  (options.value?.all ?? [])
    .map((p) => ({ ...p, id: p.fullName!, title: p.displayName || p.fullName! }))
    .sort((a, b) => (a.group ?? "").localeCompare(b.group ?? "") || a.title.localeCompare(b.title)),
);
const searchValue = ref<string>();
const visible = computed(() => rows.value.filter((r) => !searchValue.value || r.title.toLowerCase().includes(searchValue.value.toLowerCase())));
const included = new Set((options.value?.included ?? []).map((x) => x.fullName));
const selection = ref<Row[]>(rows.value.filter((r) => included.has(r.fullName)));

const bladeToolbar = ref<IBladeToolbar[]>([
  {
    id: "apply",
    title: t("EXPORT.PAGES.PROPERTIES.TOOLBAR.APPLY"),
    icon: "lucide-check",
    disabled: computed(() => !selection.value.length),
    async clickHandler() {
      // the original property objects, without the row helpers
      const ids = new Set(selection.value.map((x) => x.id));
      await callParent(
        "applyProperties",
        (options.value?.all ?? []).filter((p) => ids.has(p.fullName!)),
      );
      await closeSelf();
    },
  },
  {
    id: "all",
    title: t("EXPORT.PAGES.PROPERTIES.TOOLBAR.ALL"),
    icon: "lucide-square-check",
    clickHandler: () => (selection.value = [...rows.value]),
  },
]);
</script>
