<template>
  <header class="chat-header">
    <div class="brand-mark" aria-hidden="true">PE</div>
    <div>
      <p class="eyebrow">Aug1 · AI-assisted feedback ecosystem</p>
      <h1 class="title">PE Syllabus Assistant</h1>
    </div>
    <p class="status" aria-label="Source library status">
      <span class="status-dot" aria-hidden="true"></span>
      Demo mode
    </p>
  </header>
</template>

<style scoped>
.chat-header {
  display: flex;
  align-items: center;
  gap: 0.9rem;
  padding: 1.25rem 1.4rem;
  border-bottom: 1px solid var(--border);
  background: var(--surface);
}

.brand-mark {
  display: grid;
  width: 2.65rem;
  height: 2.65rem;
  flex: 0 0 auto;
  place-items: center;
  border-radius: 0.85rem;
  background: var(--navy);
  color: #fff;
  font-size: 0.78rem;
  font-weight: 800;
  letter-spacing: 0.08em;
}

.eyebrow {
  margin: 0 0 0.15rem;
  color: var(--muted);
  font-size: 0.72rem;
  font-weight: 700;
  letter-spacing: 0.04em;
  text-transform: uppercase;
}

.title {
  margin: 0;
  color: var(--ink);
  font-size: 1.1rem;
  line-height: 1.15;
}

.status {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  margin: 0 0 0 auto;
  color: var(--muted);
  font-size: 0.8rem;
  font-weight: 650;
}

.status-dot {
  width: 0.55rem;
  height: 0.55rem;
  border-radius: 50%;
  background: #d89c30;
}
</style>
