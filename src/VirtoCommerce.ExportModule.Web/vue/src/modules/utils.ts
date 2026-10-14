import { ref } from "vue";

// The server's own message instead of the generated client's generic one
export function errorText(e: unknown) {
  const err = e as { response?: string; message?: string };
  try {
    const body = JSON.parse(err.response ?? "");
    if (body?.message || body?.detail || body?.title) return body.message ?? body.detail ?? body.title;
  } catch {
    // not JSON
  }
  return err.message ?? String(e);
}

// The data type and provider names the AngularJS blades showed: the owning modules' legacy texts
// (export.types.<type>.name, export.provider-names.<provider>) in the platform's merged localization, loaded once
type LegacyNames = { types?: Record<string, { name?: string }>; "provider-names"?: Record<string, string> };
const legacy = ref<LegacyNames>({});
let legacyLoad: Promise<void> | undefined;
export function loadLegacyNames() {
  legacyLoad ??= fetch("/api/platform/localization?lang=en")
    .then((r) => (r.ok ? r.json() : {}))
    .then((x: { export?: LegacyNames }) => {
      legacy.value = x.export ?? {};
    })
    .catch(() => undefined);
  return legacyLoad;
}

// a type without a legacy name: "VirtoCommerce.PricingModule.Data.ExportImport.ExportablePricelist" -> "Pricing: Pricelist"
export function typeLabel(typeName?: string) {
  const known = legacy.value.types?.[typeName ?? ""]?.name;
  if (known) return known;
  const parts = (typeName ?? "").split(".");
  const module = parts.find((p) => p.endsWith("Module"))?.replace(/Module$/, "") ?? parts[1] ?? "";
  const name = (parts[parts.length - 1] ?? "").replace(/^Exportable/, "").replace(/([a-z])([A-Z])/g, "$1 $2");
  return module ? `${module}: ${name}` : name;
}

export const providerLabel = (typeName?: string) => legacy.value["provider-names"]?.[typeName ?? ""] ?? typeName ?? "";

// localStorage may be unavailable (blocked, private mode): then nothing is remembered
export function storageGet(key: string) {
  try {
    return localStorage.getItem(key) ?? undefined;
  } catch {
    return undefined;
  }
}
export function storageSet(key: string, value: string) {
  try {
    localStorage.setItem(key, value);
  } catch {
    // just not remembered
  }
}

// A saved search filter of a data type (AngularJS: $localStorage.exportSearchFilters[type]): a name plus the
// criteria the type's filter fields wrote (e.g. { currencies: ["USD"] }), merged into the export data query.
export type SavedFilter = { id: string; name: string } & Record<string, unknown>;

// What the data step picked: explicit items (objectIds), or everything matching filter + keyword (isAllSelected);
// isAllSelected false with no items means nothing (a caller opened the export with an empty selection).
export type DataSelection = { keyword?: string; objectIds?: string[]; isAllSelected?: boolean; filter?: SavedFilter };

export const filterCriteria = (filter?: SavedFilter) => {
  if (!filter) return {};
  const { id: _id, name: _name, ...criteria } = filter;
  return criteria;
};

const FILTERS_KEY = "vc-generic-export-filters";
function readFilters(): Record<string, SavedFilter[]> {
  try {
    return JSON.parse(storageGet(FILTERS_KEY) ?? "{}");
  } catch {
    return {};
  }
}
export const loadFilters = (typeName: string) => readFilters()[typeName] ?? [];
// storage unavailable: the filter still applies to this export, it just isn't kept
export const storeFilters = (typeName: string, filters: SavedFilter[]) =>
  storageSet(FILTERS_KEY, JSON.stringify({ ...readFilters(), [typeName]: filters }));

// The data query fields that aren't search criteria: the export settings add them when it sends the query
const NOT_CRITERIA = ["exportTypeName", "objectIds", "isAllSelected", "keyword", "includedProperties", "skip", "take", "isPreview"];
export const criteriaOf = (query: Record<string, unknown> = {}) =>
  Object.fromEntries(Object.entries(query).filter(([key]) => !NOT_CRITERIA.includes(key)));
