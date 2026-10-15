<template>
  <VcBlade
    :title="isNew ? $t('EXPORT.PAGES.FILTER.NEW') : origin?.name"
    :subtitle="typeLabel(options?.typeName)"
    :toolbar-items="bladeToolbar"
    width="40%"
  >
    <VcContainer>
      <VcForm class="export-form tw-space-y-4">
        <VcInput
          v-model="draft.name"
          :label="$t('EXPORT.PAGES.FILTER.NAME')"
          :placeholder="$t('EXPORT.PAGES.FILTER.NAME_PLACEHOLDER')"
          required
        />
        <!-- the data type's filter fields, contributed by the module that owns the type -->
        <ExtensionPoint :name="`export-filter:${options?.typeName}`">
          <template #default="{ components, hasComponents }">
            <component
              :is="field.component"
              v-for="field in components"
              :key="field.id"
              v-bind="field.props ?? {}"
              :filter="draft"
            />
            <VcBanner
              v-if="!hasComponents"
              variant="info"
              icon="lucide-info"
            >
              {{ $t("EXPORT.PAGES.FILTER.NO_FIELDS") }}
            </VcBanner>
          </template>
        </ExtensionPoint>
      </VcForm>
    </VcContainer>
  </VcBlade>
</template>

<script setup lang="ts">
/**
 * A search filter of an export data type (AngularJS export-generic-viewer-filter + metafields "<type>ExportFilter").
 * Owners of a data type add its filter fields to the extension point `export-filter:<exportTypeName>`:
 *   useExtensionPoint("export-filter:VirtoCommerce.PricingModule.Data.ExportImport.ExportablePricelist")
 *     .add({ id: "currencies", component: CurrencyFilterField, priority: 10 });
 * Each field gets a `filter` prop (the reactive filter being edited) and writes its criteria on it, e.g.
 * `filter.currencies = ["USD"]`; the criteria are sent as-is in the data query of that type.
 */
import { ExtensionPoint, generateId, useBlade, usePopup, type IBladeToolbar } from "@vc-shell/framework";
import { VcBanner, VcBlade, VcContainer, VcForm, VcInput } from "@vc-shell/framework/ui";
import { computed, reactive, ref } from "vue";
import { useI18n } from "vue-i18n";
import { loadFilters, storeFilters, typeLabel, type SavedFilter } from "../utils";

defineBlade({
  url: "/generic-export-filter",
  name: "ExportFilterDetails",
  // the type and the filter come from the data step (options): nothing to restore after a reload
  routable: false,
});

const { t } = useI18n({ useScope: "global" });
const { options, callParent, closeSelf } = useBlade<{ typeName?: string; filter?: SavedFilter }>();
const typeName = options.value?.typeName ?? "";
const { showConfirmation } = usePopup();

const origin = ref<SavedFilter | undefined>(options.value?.filter);
const isNew = computed(() => !origin.value);
const blank = (): SavedFilter => ({ id: generateId(), name: "" });
// plain JSON copies: criteria are data, and nested arrays the fields set are reactive proxies
const copy = <T,>(value: T): T => JSON.parse(JSON.stringify(value));
const draft = reactive<SavedFilter>(copy(origin.value ?? blank()));
const isSaved = ref(loadFilters(typeName).some((x) => x.id === draft.id));

function reset() {
  for (const key of Object.keys(draft)) delete draft[key];
  Object.assign(draft, copy(origin.value ?? blank()));
}

async function apply(save: boolean) {
  const filter = copy(draft);
  if (save) {
    storeFilters(typeName, [...loadFilters(typeName).filter((x) => x.id !== filter.id), filter]);
    isSaved.value = true;
  }
  origin.value = filter;
  await callParent("applyFilter", filter);
}

const bladeToolbar = ref<IBladeToolbar[]>([
  {
    id: "apply",
    title: t("EXPORT.PAGES.FILTER.TOOLBAR.APPLY"),
    icon: "lucide-filter",
    clickHandler: () => apply(false),
  },
  {
    id: "save",
    title: t("EXPORT.PAGES.FILTER.TOOLBAR.SAVE"),
    icon: "lucide-save",
    disabled: computed(() => !draft.name?.trim()),
    clickHandler: () => apply(true),
  },
  {
    id: "reset",
    title: t("EXPORT.PAGES.FILTER.TOOLBAR.RESET"),
    icon: "lucide-undo-2",
    clickHandler: () => reset(),
  },
  {
    id: "delete",
    title: t("EXPORT.PAGES.FILTER.TOOLBAR.DELETE"),
    icon: "lucide-trash-2",
    disabled: computed(() => !isSaved.value),
    async clickHandler() {
      if (!(await showConfirmation(t("EXPORT.PAGES.FILTER.DELETE_CONFIRMATION", { name: draft.name })))) return;
      storeFilters(typeName, loadFilters(typeName).filter((x) => x.id !== draft.id));
      await callParent("deleteFilter", draft.id);
      await closeSelf();
    },
  },
]);
</script>
