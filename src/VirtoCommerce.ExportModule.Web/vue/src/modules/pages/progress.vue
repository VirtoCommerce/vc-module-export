<template>
  <VcBlade
    :title="job?.title ?? $t('EXPORT.PAGES.PROGRESS.TITLE')"
    :toolbar-items="bladeToolbar"
    :loading="!job"
    width="50%"
  >
    <VcContainer>
      <JobProgress
        v-if="job"
        :job="job"
        :finished="finished"
        :percent="percent"
        prefix="EXPORT"
      >
        <div v-if="finished && job.downloadUrl">
          <VcButton
            icon="lucide-download"
            @click="download"
          >
            {{ $t("EXPORT.PAGES.PROGRESS.DOWNLOAD") }}
          </VcButton>
        </div>
      </JobProgress>
    </VcContainer>
  </VcBlade>
</template>

<script setup lang="ts">
import { useApiClient, useBlade, usePopup, type IBladeToolbar } from "@vc-shell/framework";
import { VcBlade, VcButton, VcContainer } from "@vc-shell/framework/ui";
import { computed, ref } from "vue";
import { useI18n } from "vue-i18n";
import { ExportClient, type ExportDataRequest } from "../../api_client/virtocommerce.export";
import JobProgress from "../components/JobProgress.vue";
import { useJob, type JobNotification } from "../composables/useJob";
import { errorText } from "../utils";

defineBlade({
  url: "/generic-export-progress",
  name: "ExportProgress",
  // opening it starts the export (options.request): a reload must not start another
  routable: false,
});

const { t } = useI18n({ useScope: "global" });
const { options } = useBlade<{ request?: ExportDataRequest }>();
const { showError } = usePopup();
const { getApiClient } = useApiClient(ExportClient);
const { job, finished, percent, follow } = useJob<JobNotification & { downloadUrl?: string }>();
const cancelling = ref(false);

// the AngularJS progress blade started the export when it opened
(async () => {
  try {
    const client = await getApiClient();
    follow((await client.runExport(options.value?.request)) as JobNotification);
  } catch (e) {
    // the run never started: show it as a finished job with the error, instead of loading forever
    const error = errorText(e);
    follow({ created: new Date(), finished: new Date(), description: error, errors: [error], errorCount: 1 } as JobNotification);
  }
})();

function download() {
  const link = document.createElement("a");
  link.href = new URL(job.value!.downloadUrl!, `${location.origin}/`).href;
  link.download = "";
  document.body.appendChild(link);
  link.click();
  link.remove();
}

const bladeToolbar = ref<IBladeToolbar[]>([
  {
    id: "cancel",
    title: t("EXPORT.PAGES.PROGRESS.TOOLBAR.CANCEL"),
    icon: "lucide-circle-x",
    disabled: computed(() => finished.value || cancelling.value || !job.value?.jobId),
    async clickHandler() {
      cancelling.value = true;
      try {
        const client = await getApiClient();
        await client.cancelExport({ jobId: job.value!.jobId });
      } catch (e) {
        cancelling.value = false;
        showError(errorText(e));
      }
    },
  },
]);
</script>
