<template>
  <VcBlade
    :title="$t('EXPORT.PAGES.DATA.TITLE')"
    :subtitle="typeLabel(options?.typeName)"
    :toolbar-items="bladeToolbar"
    :loading="loading"
    width="50%"
  >
    <VcDataTable
      v-model:search-value="searchValue"
      v-model:selection="selection"
      :selection-mode="options?.restrict ? undefined : 'multiple'"
      :items="items"
      :total-count="totalCount"
      :pagination="pagination"
      :searchable="true"
      state-key="EXPORT_DATA"
      :empty-state="{ icon: 'lucide-table-2', title: $t('EXPORT.PAGES.DATA.EMPTY') }"
      @pagination-click="pagination.goToPage"
    >
      <template #header>
        <div class="export-hint">{{ $t(options?.restrict ? "EXPORT.PAGES.DATA.RESTRICTED" : "EXPORT.PAGES.DATA.HINT") }}</div>
        <div class="export-filter-bar">
          <VcSelect
            v-model="filterId"
            class="export-filter-bar__select"
            :options="filterOptions"
            option-value="id"
            option-label="name"
            :placeholder="$t('EXPORT.PAGES.DATA.FILTER_NONE')"
            :aria-label="$t('EXPORT.PAGES.DATA.FILTER')"
            size="small"
            clearable
          />
          <VcButton
            variant="outline"
            size="sm"
            icon="lucide-filter"
            @click="editFilter()"
          >
            {{ $t("EXPORT.PAGES.DATA.NEW_FILTER") }}
          </VcButton>
          <VcButton
            v-if="current"
            variant="outline"
            size="sm"
            icon="lucide-pencil"
            @click="editFilter(current)"
          >
            {{ $t("EXPORT.PAGES.DATA.EDIT_FILTER") }}
          </VcButton>
        </div>
      </template>
      <VcColumn
        id="imageUrl"
        :title="$t('EXPORT.PAGES.DATA.COLUMNS.IMAGE')"
        width="80px"
      >
        <template #body="{ data: row }">
          <span class="export-thumb"><img
            v-if="row.imageUrl"
            :src="row.imageUrl"
            :alt="row.name"
            loading="lazy"
          /></span>
        </template>
      </VcColumn>
      <VcColumn
        id="name"
        :title="$t('EXPORT.PAGES.DATA.COLUMNS.NAME')"
        body-class="export-list__name"
        always-visible
      />
      <VcColumn
        id="code"
        :title="$t('EXPORT.PAGES.DATA.COLUMNS.CODE')"
      />
      <VcColumn
        id="parent"
        :title="$t('EXPORT.PAGES.DATA.COLUMNS.PARENT')"
      />
      <VcColumn
        id="type"
        :title="$t('EXPORT.PAGES.DATA.COLUMNS.TYPE')"
      />
    </VcDataTable>
  </VcBlade>
</template>

<script setup lang="ts">
// The AngularJS "generic viewer": browse the data of the chosen type, filter it (saved filters), search it, pick items
import { useApiClient, useAsync, useBlade, useDataTablePagination, useFunctions, type IBladeToolbar } from "@vc-shell/framework";
import { VcBlade, VcButton, VcColumn, VcDataTable, VcSelect } from "@vc-shell/framework/ui";
import { computed, ref, watch } from "vue";
import { useI18n } from "vue-i18n";
import { ExportClient, type ExportDataRequest } from "../../api_client/virtocommerce.export";
import { filterCriteria, loadFilters, typeLabel, type DataSelection, type SavedFilter } from "../utils";

defineBlade({
  url: "/generic-export-data",
  name: "ExportDataSelector",
  // the type and the selection come from the export settings (options): nothing to restore after a reload
  routable: false,
});

type Item = { id: string; name?: string; code?: string; parent?: string; type?: string; imageUrl?: string };

const { t } = useI18n({ useScope: "global" });
const { options, openBlade, callParent, closeSelf, exposeToChildren } = useBlade<{
  typeName?: string;
  queryType?: string;
  restrict?: boolean;
  base?: Record<string, unknown>;
  selection?: DataSelection;
}>();
const { debounce } = useFunctions();
const { getApiClient } = useApiClient(ExportClient);

const PAGE_SIZE = 20;
const typeName = options.value?.typeName ?? "";
const items = ref<Item[]>([]);
const totalCount = ref(0);
const searchValue = ref<string | undefined>(options.value?.selection?.keyword);
const selection = ref<Item[]>([]);
const pagination = useDataTablePagination({ pageSize: PAGE_SIZE, totalCount });
const keep = new Set(options.value?.selection?.objectIds ?? []);

// saved filters of this type, plus the current one when it was applied without saving
const saved = ref<SavedFilter[]>(loadFilters(typeName));
const current = ref<SavedFilter | undefined>(options.value?.selection?.filter);
const filterOptions = computed(() => (current.value && !saved.value.some((x) => x.id === current.value!.id) ? [...saved.value, current.value] : saved.value));
const filterId = computed<string | undefined>({
  get: () => current.value?.id,
  set: (id) => (current.value = filterOptions.value.find((x) => x.id === id)),
});

const { loading, action: load } = useAsync(async () => {
  const client = await getApiClient();
  const result = await client.getData({
    exportTypeName: typeName,
    dataQuery: {
      ...options.value?.base,
      ...filterCriteria(current.value),
      exportTypeName: options.value?.queryType,
      keyword: searchValue.value || undefined,
      skip: pagination.skip,
      take: PAGE_SIZE,
      isPreview: true,
    } as ExportDataRequest["dataQuery"],
  });
  items.value = (result.results ?? []) as Item[];
  totalCount.value = result.totalCount ?? 0;
  // keep the items picked earlier selected when they come into view
  selection.value = items.value.filter((i) => keep.has(i.id));
});
// the picks across pages (and from the last time) live in keep; the table's selection is the visible page's share
watch(selection, (rows) => items.value.forEach((i) => (rows.some((r) => r.id === i.id) ? keep.add(i.id) : keep.delete(i.id))));

function editFilter(filter?: SavedFilter) {
  openBlade({ name: "ExportFilterDetails", options: { typeName, filter } });
}

// one (debounced) load for a search, filter or page change
watch([searchValue, current], () => pagination.setPage(1));
watch([searchValue, current, () => pagination.skip], debounce(load, 300));
load();

const bladeToolbar = ref<IBladeToolbar[]>([
  {
    id: "apply",
    title: t("EXPORT.PAGES.DATA.TOOLBAR.APPLY"),
    icon: "lucide-check",
    async clickHandler() {
      const ids = [...keep];
      const picked: DataSelection = ids.length
        ? { filter: current.value, objectIds: ids, isAllSelected: false }
        : { filter: current.value, keyword: searchValue.value || undefined, isAllSelected: true };
      await callParent("applyData", picked);
      await closeSelf();
    },
  },
  { id: "refresh", title: t("EXPORT.PAGES.DATA.TOOLBAR.REFRESH"), icon: "lucide-refresh-cw", clickHandler: () => load() },
]);

// the filter blade reports back here
exposeToChildren({
  applyFilter(filter: SavedFilter) {
    saved.value = loadFilters(typeName);
    current.value = filter;
  },
  deleteFilter(id: string) {
    saved.value = loadFilters(typeName);
    if (current.value?.id === id) current.value = undefined;
  },
});
</script>

<style lang="scss">
.export-hint {
  padding: 12px 16px 4px;
  font-size: 13px;
  color: var(--neutrals-600);
}
.export-filter-bar {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 16px 0;
  &__select {
    flex: 0 1 320px;
  }
}
.export-list__name {
  color: var(--neutrals-900);
}
.export-thumb {
  display: inline-flex;
  width: 40px;
  height: 40px;
  border-radius: 6px;
  overflow: hidden;
  background: var(--neutrals-100);
  img {
    width: 100%;
    height: 100%;
    object-fit: contain;
  }
}
</style>
