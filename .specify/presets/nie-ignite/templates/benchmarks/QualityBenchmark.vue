<script setup lang="ts">
// Content-only visual benchmark. Procurement compositions are adapted from the
// exact pinned sources recorded by the installer; all actions use local data.
import { computed, ref } from "vue";
import BoardReference from "./BoardReference.vue";
import benchmark from "./cases.json";

const screenId = location.hash.split("?")[0].split("/").at(-1);
const selected = benchmark.cases.find((item) => item.id === screenId) ?? benchmark.cases[0]!;
const query = new URLSearchParams(location.hash.split("?")[1] ?? "");
const requestedState = query.get("igniteState") ?? "default";
const state = ref(requestedState);
const actorId = query.get("igniteActor") ?? selected.actorIds[0];
const actor = benchmark.actors.find((item) => item.id === actorId) ?? benchmark.actors[0]!;
const search = ref("");
const delivery = ref("");
const validation = ref(false);
const detail = ref("");
const confirmation = ref("");
const receipt = ref("");
const requests = ref([...benchmark.fixtures.requests]);
const suppliers = computed(() => benchmark.fixtures.suppliers.filter((item) =>
  `${item.name} ${item.id} ${item.category}`.toLowerCase().includes(search.value.toLowerCase())));
const detailRecord = computed(() => benchmark.fixtures.board.flatMap((column) => column.records).find((item) => item.id === detail.value));
const pendingRequest = computed(() => requests.value.find((item) => item.id === confirmation.value));

function reviewRequest() {
  validation.value = !delivery.value.trim();
  if (!validation.value) detail.value = "request-summary";
}
function approveRequest() {
  const id = confirmation.value;
  requests.value = requests.value.filter((item) => item.id !== id);
  confirmation.value = "";
  receipt.value = `${id} approved. The queue now contains ${requests.value.length} request.`;
}
</script>

<template>
  <main class="benchmark" :data-preview-actor-id="actor.id"
    :data-preview-role-codes="actor.roleCodes.join(' ')"
    :data-preview-capabilities="actor.capabilities.join(' ')"
    :data-preview-state="requestedState">
    <header class="benchmark__heading">
      <h1>{{ selected.name }}</h1>
      <p>{{ selected.outcome }}</p>
    </header>

    <section v-if="state === 'loading'" class="benchmark__state" aria-busy="true">
      <h2>Loading {{ selected.name.toLowerCase() }}</h2>
      <p>The layout remains stable while records are being prepared.</p>
      <div class="benchmark__skeleton" aria-hidden="true"></div>
      <div class="benchmark__skeleton benchmark__skeleton--short" aria-hidden="true"></div>
      <button data-benchmark-finish-loading @click="state = 'default'">Finish loading fixture</button>
    </section>
    <section v-else-if="state === 'error'" class="benchmark__state" role="alert">
      <h2>This content could not be loaded</h2>
      <p>Your request is preserved. Try loading the records again.</p>
      <button data-benchmark-retry @click="state = 'default'">Try again</button>
    </section>
    <section v-else-if="state === 'empty'" class="benchmark__state">
      <h2>No records yet</h2>
      <p>Start with the first item when you are ready. There are no hidden results or unexplained counts.</p>
      <button data-benchmark-populate @click="state = 'default'">Add example records</button>
    </section>

    <section v-else data-benchmark-content class="benchmark__content">
      <BoardReference v-if="selected.layoutRecipe === 'board'" :columns="benchmark.fixtures.board" @open="detail = $event" />

      <!-- VendorManagement reference: aligned toolbar, explicit results and a
           horizontally scrollable table; long supplier names must wrap. -->
      <template v-else-if="selected.layoutRecipe === 'table'">
        <div class="benchmark__toolbar">
          <label class="benchmark__search">Search suppliers
            <input v-model="search" data-benchmark-search type="search" placeholder="Name, code or category" />
          </label>
          <p data-benchmark-result-count>{{ suppliers.length }} {{ suppliers.length === 1 ? 'supplier' : 'suppliers' }}</p>
        </div>
        <div class="benchmark__table-scroll" role="region" aria-label="Supplier results" tabindex="0" :data-preview-total="suppliers.length">
          <table>
            <thead><tr><th>Supplier</th><th>Category</th><th>Contact</th><th>Status</th></tr></thead>
            <tbody><tr v-for="supplier in suppliers" :key="supplier.id" :data-preview-record-id="supplier.id">
              <td><strong>{{ supplier.name }}</strong><small>{{ supplier.id }}</small></td>
              <td>{{ supplier.category }}</td><td>{{ supplier.contact }}</td><td><span class="benchmark__status">{{ supplier.status }}</span></td>
            </tr></tbody>
          </table>
        </div>
      </template>

      <!-- NewPurchaseRequest + PurchaseOrderDetail references: related fields
           are grouped, line totals explicit, and review precedes submission. -->
      <form v-else-if="selected.layoutRecipe === 'form'" class="benchmark__panel" @submit.prevent="reviewRequest">
        <section class="benchmark__section">
          <h2>Request details</h2>
          <div class="benchmark__fields">
            <label>Supplier<input value="Horizon Learning Resources" readonly /></label>
            <label>Requested by<input value="Amira Tan" readonly /></label>
          </div>
          <div class="benchmark__summary"><span>4 portable workshop displays</span><strong>$1,240.00</strong></div>
        </section>
        <section class="benchmark__section">
          <h2>Delivery</h2>
          <div class="benchmark__fields">
            <label>Delivery location
              <input v-model="delivery" data-benchmark-delivery :aria-invalid="validation" :aria-describedby="validation ? 'delivery-error' : undefined" />
              <span v-if="validation" id="delivery-error" data-benchmark-validation role="alert">Enter a delivery location.</span>
            </label>
            <label>Expected delivery<input value="18 September 2026" readonly /></label>
          </div>
          <label>Notes<textarea rows="3">Please use the accessible delivery entrance and contact the resource centre before arrival.</textarea></label>
        </section>
        <footer class="benchmark__actions"><button type="submit" data-benchmark-review>Review request</button></footer>
      </form>

      <!-- ApprovalQueue reference: identity/status, owner, decision facts and
           an explicit confirmation remain grouped at narrow widths. -->
      <template v-else>
        <p v-if="receipt" data-benchmark-receipt role="status" class="benchmark__receipt">{{ receipt }}</p>
        <div class="benchmark__queue" :data-preview-total="requests.length">
          <article v-for="request in requests" :key="request.id" :data-preview-record-id="request.id" class="benchmark__panel">
            <div class="benchmark__approval-heading">
              <div><span class="benchmark__status">Awaiting review</span><h2>{{ request.id }} · {{ request.title }}</h2></div>
              <strong class="benchmark__amount">{{ request.value }}</strong>
            </div>
            <dl class="benchmark__facts"><div><dt>Requested by</dt><dd>{{ request.owner }}</dd></div><div><dt>Supplier</dt><dd>{{ request.supplier }}</dd></div><div><dt>Requested delivery</dt><dd>{{ request.date }}</dd></div></dl>
            <footer class="benchmark__actions"><button :data-benchmark-approve="request.id" @click="confirmation = request.id">Review approval</button></footer>
          </article>
        </div>
      </template>
    </section>

    <div v-if="detail || confirmation" class="benchmark__overlay">
      <section class="benchmark__dialog" role="dialog" aria-modal="true" aria-label="Review selected item">
        <template v-if="confirmation && pendingRequest">
          <div data-benchmark-confirmation><h2>Approve {{ pendingRequest.id }}?</h2><p>{{ pendingRequest.title }}</p><p>{{ pendingRequest.items }} · {{ pendingRequest.value }}</p><p>Requested by {{ pendingRequest.owner }}.</p></div>
          <div class="benchmark__actions"><button @click="confirmation = ''">Cancel</button><button data-benchmark-confirm @click="approveRequest">Confirm approval</button></div>
        </template>
        <template v-else>
          <div data-benchmark-detail>
            <template v-if="detailRecord"><h2>{{ detailRecord.title }}</h2><p>{{ detailRecord.id }} · {{ detailRecord.owner }}</p><p>{{ detailRecord.dueLabel }} · {{ detailRecord.priority }} priority</p></template>
            <template v-else><h2>Review purchase request</h2><p>Horizon Learning Resources · $1,240.00</p><dl><dt>Delivery location</dt><dd>{{ delivery }}</dd></dl></template>
          </div>
          <footer class="benchmark__actions"><button data-benchmark-close @click="detail = ''">Back to {{ selected.name.toLowerCase() }}</button></footer>
        </template>
      </section>
    </div>
  </main>
</template>

<style scoped>
.benchmark { max-width: 80rem; margin: 0 auto; padding: 2rem; color: var(--color-text); background: var(--color-surface-alt); min-height: 100vh; }
.benchmark__heading { margin-bottom: 1.5rem; max-width: 60rem; }
h1 { font-size: var(--theme-font-size-3xl, 2rem); line-height: 1.2; font-weight: 700; margin: 0 0 .75rem; }
h2 { font-size: var(--theme-font-size-xl, 1.2rem); line-height: 1.4; font-weight: 700; margin: 0 0 .75rem; }
p, label, input, textarea, button, td, th, dt, dd { font-size: var(--theme-font-size-md, 1rem); line-height: 1.5; }
p { margin: .5rem 0; } strong { font-weight: 700; } small { display: block; margin-top: .25rem; }
.benchmark__panel, .benchmark__state, .benchmark__table-scroll { border: 1px solid var(--color-border); border-radius: 1rem; background: var(--color-surface); }
.benchmark__panel, .benchmark__state { padding: 1.5rem; }
.benchmark__state { max-width: 42rem; margin: 2rem auto; }
.benchmark__toolbar, .benchmark__approval-heading, .benchmark__summary, .benchmark__actions { display: flex; align-items: center; justify-content: space-between; gap: 1rem; flex-wrap: wrap; }
.benchmark__toolbar { margin-bottom: 1rem; } .benchmark__search { flex: 1; max-width: 36rem; }
label { display: grid; gap: .5rem; min-width: 0; font-weight: 600; }
input, textarea { min-width: 0; width: 100%; border: 1px solid var(--color-border); border-radius: .5rem; padding: .65rem .75rem; color: var(--color-text); background: var(--color-surface); font-weight: 400; }
button { min-height: 2.75rem; padding: .65rem 1rem; border-radius: .5rem; border: 1px solid var(--color-primary); background: var(--color-surface); color: var(--color-text); font-weight: 600; cursor: pointer; }
button:hover { background: var(--color-surface-alt); } button:focus-visible, input:focus-visible, textarea:focus-visible { outline: 2px solid var(--color-primary); outline-offset: 3px; }
.benchmark__table-scroll { overflow-x: auto; } table { width: 100%; border-collapse: collapse; min-width: 42rem; table-layout: fixed; }
th, td { padding: 1rem; vertical-align: top; text-align: left; border-bottom: 1px solid var(--color-border); overflow-wrap: anywhere; } th:first-child { width: 34%; } th { background: var(--color-surface-alt); font-weight: 700; }
.benchmark__status { display: inline-block; border: 1px solid var(--color-border); padding: .2rem .5rem; border-radius: .35rem; font-size: var(--theme-font-size-sm, .875rem); }
.benchmark__section + .benchmark__section { border-top: 1px solid var(--color-border); margin-top: 1.5rem; padding-top: 1.5rem; }
.benchmark__fields { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 1.25rem; margin: 1rem 0; }
.benchmark__summary { padding: 1rem; margin-top: 1rem; background: var(--color-surface-alt); border-radius: .5rem; }
.benchmark__actions { margin-top: 1.5rem; justify-content: flex-end; }
.benchmark__queue { display: grid; gap: 1rem; } .benchmark__approval-heading { align-items: flex-start; } .benchmark__approval-heading > div { flex: 1; min-width: 14rem; } .benchmark__approval-heading h2 { margin-top: .75rem; }
.benchmark__amount { font-size: var(--theme-font-size-xl, 1.2rem); white-space: nowrap; }
.benchmark__facts { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 1rem; margin: 1rem 0 0; } dt { font-weight: 600; } dd { margin: .25rem 0 0; overflow-wrap: anywhere; }
.benchmark__receipt { border: 1px solid var(--color-border); border-radius: .5rem; padding: 1rem; }
.benchmark__skeleton { height: 1rem; margin: 1rem 0; border-radius: .5rem; background: var(--color-border); } .benchmark__skeleton--short { width: 65%; }
.benchmark__overlay { position: fixed; inset: 0; z-index: 100; display: grid; place-items: center; padding: 1rem; background: rgb(0 0 0 / .35); }
.benchmark__dialog { width: min(36rem, 100%); max-height: calc(100vh - 2rem); overflow-y: auto; padding: 1.5rem; border-radius: 1rem; background: var(--color-surface); color: var(--color-text); box-shadow: 0 1rem 3rem rgb(0 0 0 / .2); }
@media (max-width: 767px) { .benchmark { padding: 1rem; } .benchmark__panel { padding: 1rem; } .benchmark__fields, .benchmark__facts { grid-template-columns: 1fr; } .benchmark__toolbar { align-items: stretch; } .benchmark__search { flex-basis: 100%; } .benchmark__actions > button { width: 100%; } }
</style>
