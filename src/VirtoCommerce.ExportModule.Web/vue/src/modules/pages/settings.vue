<template>
  <VcBlade
    :title="options?.title ? $t(options.title) : $t('EXPORT.PAGES.SETTINGS.TITLE')"
    :subtitle="options?.subtitle ? $t(options.subtitle) : undefined"
    :toolbar-items="bladeToolbar"
    :loading="loading"
    width="50%"
  >
    <VcContainer>
      <VcForm class="export-form tw-space-y-4">
        <VcSelect
          v-model="typeName"
          :label="$t('EXPORT.PAGES.SETTINGS.TYPE')"
          :hint="$t(locked ? 'EXPORT.PAGES.SETTINGS.TYPE_LOCKED' : 'EXPORT.PAGES.SETTINGS.TYPE_HINT')"
          :options="typeOptions"
          :disabled="locked"
          searchable
          required
          :clearable="false"
        />
        <VcSelect
          v-model="providerName"
          :label="$t('EXPORT.PAGES.SETTINGS.PROVIDER')"
          :hint="$t('EXPORT.PAGES.SETTINGS.PROVIDER_HINT')"
          :options="providerOptions"
          required
          :clearable="false"
        />
        <div class="export-steps">
          <div
            v-for="step in steps"
            :key="step.id"
            class="export-steps__row"
          >
            <VcIcon
              :icon="step.icon"
              size="m"
              class="export-steps__icon"
            />
            <div class="export-steps__text">
              <div class="export-steps__title">{{ step.title }}</div>
              <div class="export-steps__value">{{ $t("EXPORT.PAGES.SETTINGS.SELECTED", { count: step.count, total: step.total }) }}</div>
              <div
                v-if="step.note"
                class="export-steps__note"
              >
                {{ step.note }}
              </div>
            </div>
            <VcButton
              variant="outline"
              size="sm"
              :disabled="!type || !provider"
              @click="step.open"
            >
              {{ $t("EXPORT.PAGES.SETTINGS.CHOOSE") }}
            </VcButton>
          </div>
        </div>
      </VcForm>
    </VcContainer>
  </VcBlade>
</template>

<script setup lang="ts">
import { useApiClient, useBlade, usePopup, type IBladeToolbar } from "@vc-shell/framework";
import { VcBlade, VcButton, VcContainer, VcForm, VcIcon, VcSelect } from "@vc-shell/framework/ui";
import { computed, ref, watch } from "vue";
import { useI18n } from "vue-i18n";
import {
  ExportClient,
  type ExportDataRequest,
  type ExportedTypeDefinition,
  type ExportedTypePropertyInfo,
  type IExportProvider,
} from "../../api_client/virtocommerce.export";
import {
  criteriaOf,
  errorText,
  filterCriteria,
  loadLegacyNames,
  providerLabel,
  storageGet,
  storageSet,
  typeLabel,
  type DataSelection,
} from "../utils";

defineBlade({
  url: "/generic-export",
  name: "ExportSettings",
  isWorkspace: true,
  menuItem: {
    title: "EXPORT.MENU.TITLE",
    icon: "lucide-database",
    priority: 90,
  },
});

/**
 * Other modules open this blade pre-filled, as their AngularJS "Export" buttons did:
 *   openBlade({ name: "ExportSettings", options: {
 *     exportDataRequest: { exportTypeName, dataQuery: { exportTypeName: <query type>, objectIds?, isAllSelected?, keyword?, ...search criteria } },
 *     totalItemsCount?, title?, subtitle? } })
 * The data type is then fixed; the caller's selection and search criteria are kept; format and properties stay open.
 */
type Prefill = {
  exportDataRequest?: { exportTypeName?: string; dataQuery?: Record<string, unknown> };
  totalItemsCount?: number;
  title?: string;
  subtitle?: string;
};

const PROVIDER_KEY = "vc-generic-export-provider";
const { t } = useI18n({ useScope: "global" });
const { options, openBlade, exposeToChildren } = useBlade<Prefill>();
const { showError } = usePopup();
const { getApiClient } = useApiClient(ExportClient);

const prefill = options.value?.exportDataRequest;
const locked = !!prefill?.exportTypeName;
const seedQuery = prefill?.dataQuery ?? {};
const seedIds = (seedQuery.objectIds as string[] | undefined) ?? [];

const loading = ref(true);
const types = ref<ExportedTypeDefinition[]>([]);
const providers = ref<IExportProvider[]>([]);
const typeName = ref<string | undefined>(prefill?.exportTypeName);
const providerName = ref<string | undefined>(storageGet(PROVIDER_KEY) ?? "JsonExportProvider");
// the caller's search criteria (catalogIds, currencies...) apply to everything the data step picks
const base = criteriaOf(seedQuery);
const selection = ref<DataSelection>({
  keyword: seedQuery.keyword as string | undefined,
  objectIds: seedIds.length ? seedIds : undefined,
  isAllSelected: seedIds.length ? false : ((seedQuery.isAllSelected as boolean | undefined) ?? true),
});
const includedProperties = ref<ExportedTypePropertyInfo[]>([]);
const dataTotal = ref(0);
const dataSelected = ref(options.value?.totalItemsCount ?? 0);

const type = computed(() => types.value.find((x) => x.typeName === typeName.value));
const typeOptions = computed(() =>
  types.value.map((x) => ({ id: x.typeName!, title: typeLabel(x.typeName) })).sort((a, b) => a.title.localeCompare(b.title)),
);
// a type without tabular metadata can't go to CSV
const providerOptions = computed(() =>
  providers.value
    .filter((p) => !type.value || type.value.isTabularExportSupported || !p.isTabular)
    .map((p) => ({ id: p.typeName!, title: providerLabel(p.typeName) })),
);
const provider = computed(() => providerOptions.value.find((x) => x.id === providerName.value) && providers.value.find((x) => x.typeName === providerName.value));
const allProperties = computed(
  () => (provider.value?.isTabular && type.value?.tabularMetaData ? type.value.tabularMetaData : type.value?.metaData)?.propertyInfos ?? [],
);

function dataQuery(extra: Record<string, unknown> = {}) {
  // exportTypeName here is the query's type name: the server picks the query class by it
  return {
    ...base,
    ...filterCriteria(selection.value.filter),
    keyword: selection.value.keyword,
    objectIds: selection.value.objectIds,
    exportTypeName: type.value?.exportDataQueryType,
    includedProperties: includedProperties.value,
    ...extra,
  } as ExportDataRequest["dataQuery"];
}

async function count(query: ExportDataRequest["dataQuery"]) {
  const client = await getApiClient();
  return (await client.getData({ exportTypeName: typeName.value, dataQuery: query })).totalCount ?? 0;
}

// the type's total changes only with the type; the selected count with each data pick
async function countSelected() {
  if (!type.value) return;
  const { objectIds, isAllSelected, filter, keyword } = selection.value;
  dataSelected.value = objectIds?.length
    ? objectIds.length
    : isAllSelected === false
      ? 0
      : !filter && !keyword && !Object.keys(base).length
        ? dataTotal.value // everything, unfiltered: the total
        : await count(dataQuery({ objectIds: undefined, includedProperties: [], skip: 0, take: 0 }));
}

async function recount() {
  if (!type.value) return;
  dataTotal.value = await count({ exportTypeName: type.value.exportDataQueryType, skip: 0, take: 0 } as ExportDataRequest["dataQuery"]);
  await countSelected();
}

function ensureProvider() {
  if (!providerOptions.value.some((x) => x.id === providerName.value)) providerName.value = providerOptions.value[0]?.id;
  includedProperties.value = [...allProperties.value];
}

// a new type resets the data and property choice, as in the AngularJS blade (a pre-filled type can't change)
watch(typeName, () => {
  selection.value = { isAllSelected: true };
  ensureProvider();
  recount().catch((e) => showError(errorText(e)));
});
watch(providerName, (name) => {
  includedProperties.value = [...allProperties.value];
  if (name) storageSet(PROVIDER_KEY, name);
});

const steps = computed(() => [
  {
    id: "data",
    icon: "lucide-table-2",
    title: t("EXPORT.PAGES.SETTINGS.DATA"),
    count: dataSelected.value,
    total: dataTotal.value,
    note: selection.value.filter ? t("EXPORT.PAGES.SETTINGS.FILTERED", { name: selection.value.filter.name }) : undefined,
    open: () =>
      openBlade({
        name: "ExportDataSelector",
        options: {
          typeName: typeName.value,
          queryType: type.value?.exportDataQueryType,
          restrict: !!type.value?.restrictDataSelectivity,
          base,
          selection: selection.value,
        },
      }),
  },
  {
    id: "properties",
    icon: "lucide-columns-3",
    title: t("EXPORT.PAGES.SETTINGS.PROPERTIES"),
    count: includedProperties.value.length,
    total: allProperties.value.length,
    note: undefined,
    open: () => openBlade({ name: "ExportPropertiesSelector", options: { all: allProperties.value, included: includedProperties.value } }),
  },
]);

async function init() {
  loadLegacyNames();
  try {
    const client = await getApiClient();
    [types.value, providers.value] = await Promise.all([client.getExportedKnownTypes(), client.getExportProviders()]);
    if (typeName.value) {
      ensureProvider();
      await recount();
    }
  } catch (e) {
    showError(errorText(e));
  } finally {
    loading.value = false;
  }
}
init();

const bladeToolbar = ref<IBladeToolbar[]>([
  {
    id: "start",
    title: t("EXPORT.PAGES.SETTINGS.TOOLBAR.START"),
    icon: "lucide-file-output",
    disabled: computed(() => !type.value || !provider.value || !dataSelected.value),
    clickHandler: () =>
      openBlade({
        name: "ExportProgress",
        options: {
          request: { exportTypeName: typeName.value, providerName: providerName.value, dataQuery: dataQuery() } as ExportDataRequest,
        },
      }),
  },
]);

exposeToChildren({
  async applyData(value: DataSelection) {
    selection.value = value;
    await countSelected();
  },
  applyProperties(value: ExportedTypePropertyInfo[]) {
    includedProperties.value = value;
  },
});
</script>

<style lang="scss">
.export-form {
  padding: 20px 22px 36px;
}
.export-steps {
  display: flex;
  flex-direction: column;
  border: 1px solid var(--neutrals-200);
  border-radius: 8px;
  &__row {
    display: flex;
    align-items: center;
    gap: 14px;
    padding: 14px 16px;
    & + & {
      border-top: 1px solid var(--neutrals-200);
    }
  }
  &__icon {
    color: var(--primary-500);
  }
  &__text {
    flex: 1;
  }
  &__title {
    font-size: 14px;
    font-weight: 500;
    color: var(--neutrals-900);
  }
  &__value {
    font-size: 13px;
    color: var(--neutrals-600);
  }
  &__note {
    font-size: 12px;
    color: var(--primary-600);
  }
}
</style>
