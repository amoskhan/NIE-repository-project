<script setup lang="ts">
import { computed, ref } from "vue";
import { NieLoaderSymbol } from "@nie/ui";
import type { Conversation } from "../../services/chat/chatService";

const props = defineProps<{
  conversations: Conversation[];
  currentConversationId?: string;
  isLoading: boolean;
}>();

const emit = defineEmits<{
  (e: "select", conversation: Conversation): void;
  (e: "new"): void;
  (e: "delete", conversation: Conversation): void;
  (e: "rename", conversation: Conversation, newTitle: string): void;
}>();

const editingId = ref<string | null>(null);
const editTitle = ref("");
const menuOpenId = ref<string | null>(null);

const conversationCount = computed(() => props.conversations.length);

const selectConversation = (conv: Conversation) => {
  menuOpenId.value = null;
  emit("select", conv);
};

const startEdit = (conv: Conversation) => {
  editingId.value = conv.id;
  editTitle.value = conv.title || "";
  menuOpenId.value = null;
};

const saveEdit = (conv: Conversation) => {
  if (editingId.value !== conv.id) return;

  const title = editTitle.value.trim();
  if (title && title !== conv.title) {
    emit("rename", conv, title);
  }
  editingId.value = null;
  editTitle.value = "";
};

const requestDelete = (conv: Conversation) => {
  menuOpenId.value = null;
  emit("delete", conv);
};

const cancelEdit = () => {
  editingId.value = null;
  editTitle.value = "";
};

const formatDate = (dateStr: string) => {
  const d = new Date(dateStr);
  const now = new Date();
  const diff = Math.floor(
    (now.getTime() - d.getTime()) / (1000 * 60 * 60 * 24),
  );
  if (diff <= 0) return "Today";
  if (diff === 1) return "Yesterday";
  if (diff < 7) return `${diff}d ago`;
  return d.toLocaleDateString();
};
</script>

<template>
  <aside class="chat-sidebar" aria-label="Chat conversations">
    <div class="sidebar-head">
      <div class="sidebar-title">
        <span class="sidebar-icon material-symbols-outlined">chat</span>
        <div class="sidebar-copy">
          <p class="sidebar-heading">Conversations</p>
          <p class="sidebar-meta">
            {{ conversationCount }} saved chat{{ conversationCount === 1 ? "" : "s" }}
          </p>
        </div>
      </div>

      <button
        type="button"
        class="new-chat-btn"
        aria-label="Start a new chat"
        @click="emit('new')"
      >
        <span class="material-symbols-outlined text-card-title" aria-hidden="true">add</span>
        <span>New</span>
      </button>
    </div>

    <div class="conversations-list">
      <div v-if="isLoading" class="loading">
        <NieLoaderSymbol size="sm" label="Loading conversations" />
        <span>Loading conversations...</span>
      </div>
      <div v-else-if="conversations.length === 0" class="empty">
        <span class="material-symbols-outlined text-page-title" aria-hidden="true">forum</span>
        <span>No conversations yet</span>
      </div>

      <div
        v-for="conv in conversations"
        :key="conv.id"
        class="conv-item"
        :class="{ active: conv.id === currentConversationId }"
      >
        <template v-if="editingId === conv.id">
          <input
            v-model="editTitle"
            class="edit-input"
            aria-label="Rename conversation"
            @keydown.enter.prevent="saveEdit(conv)"
            @keydown.escape.prevent="cancelEdit"
            @blur="saveEdit(conv)"
            autofocus
          />
        </template>
        <template v-else>
          <button
            type="button"
            class="conv-main"
            :aria-current="conv.id === currentConversationId ? 'page' : undefined"
            @click="selectConversation(conv)"
          >
            <span class="conv-title">{{ conv.title }}</span>
            <span class="conv-meta">
              {{ formatDate(conv.lastMessageAt) }} &middot; {{ conv.messageCount }} msgs
            </span>
          </button>
          <button
            type="button"
            class="conv-menu-btn"
            :aria-label="`Open actions for ${conv.title}`"
            @click.stop="menuOpenId = menuOpenId === conv.id ? null : conv.id"
          >
            <span class="material-symbols-outlined text-card-title" aria-hidden="true">more_horiz</span>
          </button>
          <div v-if="menuOpenId === conv.id" class="conv-menu">
            <button type="button" @click.stop="startEdit(conv)">
              <span class="material-symbols-outlined text-body-lg" aria-hidden="true">edit</span>
              <span>Rename</span>
            </button>
            <button type="button" @click.stop="requestDelete(conv)">
              <span class="material-symbols-outlined text-body-lg" aria-hidden="true">delete</span>
              <span>Delete</span>
            </button>
          </div>
        </template>
      </div>
    </div>
  </aside>
</template>

<style scoped>
.chat-sidebar {
  /* Tokens fall back to launchpad-ish defaults so the sidebar still looks    */
  /* good when used outside the chat shell.                                   */
  --chat-primary: var(--color-primary, var(--theme-color-brand-600));
  --chat-border: color-mix(
    in srgb,
    var(--chat-primary) 9%,
    var(--color-border, var(--theme-color-border-default)) 91%
  );
  --chat-panel: var(--color-surface, var(--theme-color-static-white));
  --chat-active: var(--color-sidebar-active, var(--theme-color-brand-50));

  display: flex;
  width: 100%;
  min-width: 0;
  max-height: 18rem;
  flex-direction: column;
  flex-shrink: 0;
  border-bottom: 1px solid color-mix(in srgb, var(--chat-border) 60%, transparent);
  background: color-mix(in srgb, var(--chat-panel) 78%, transparent);
  backdrop-filter: blur(14px);
}

.sidebar-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--theme-space-3);
  padding: var(--theme-space-3);
  border-bottom: 1px solid color-mix(in srgb, var(--chat-border) 55%, transparent);
}

.sidebar-title {
  display: flex;
  min-width: 0;
  align-items: center;
  gap: var(--theme-space-2);
}

.sidebar-icon {
  display: inline-flex;
  width: 34px;
  height: 34px;
  flex-shrink: 0;
  align-items: center;
  justify-content: center;
  border-radius: var(--theme-radius-control);
  background: color-mix(in srgb, var(--color-primary, var(--theme-color-info-solid)) 12%, var(--theme-color-static-white) 88%);
  color: var(--color-primary, var(--theme-color-info-solid));
}

.sidebar-copy {
  min-width: 0;
}

.sidebar-heading {
  margin: 0;
  color: var(--color-text, var(--theme-color-text-strong));
  font-size: var(--theme-font-size-label);
  font-weight: var(--theme-font-weight-bold);
}

.sidebar-meta {
  margin: var(--theme-space-0-5) 0 0;
  color: var(--color-text-muted, var(--theme-color-text-muted));
  font-size: var(--theme-font-size-caption);
}

.new-chat-btn {
  display: inline-flex;
  min-height: 40px;
  align-items: center;
  justify-content: center;
  gap: var(--theme-space-1);
  padding: var(--theme-space-2) var(--theme-space-3);
  border-radius: var(--theme-radius-control);
  border: 1px solid color-mix(in srgb, var(--color-primary, var(--theme-color-info-solid)) 30%, var(--color-border, var(--theme-color-border-strong)));
  background: var(--color-primary, var(--theme-color-info-solid));
  color: var(--theme-color-on-brand);
  cursor: pointer;
  font-size: var(--theme-font-size-label);
  font-weight: var(--theme-font-weight-bold);
}

.new-chat-btn:hover {
  background: color-mix(in srgb, var(--color-primary, var(--theme-color-info-solid)) 86%, var(--theme-color-static-black) 14%);
}

.conversations-list {
  flex: 1;
  overflow-y: auto;
  padding: var(--theme-space-2);
}

.loading,
.empty {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: var(--theme-space-2);
  min-height: 96px;
  color: var(--color-text-muted, var(--theme-color-text-muted));
  font-size: var(--theme-font-size-label);
}

.conv-item {
  position: relative;
  width: 100%;
  margin-bottom: var(--theme-space-0-5);
  border: 1px solid transparent;
  border-radius: var(--theme-radius-control);
  background: transparent;
  color: inherit;
}

.conv-item:hover,
.conv-item.active {
  border-color: color-mix(in srgb, var(--chat-primary) 18%, var(--chat-border));
  background: color-mix(in srgb, var(--chat-active) 56%, var(--chat-panel) 44%);
}

.conv-main {
  display: block;
  width: 100%;
  min-height: 60px;
  padding: var(--theme-space-2) var(--theme-space-10) var(--theme-space-2) var(--theme-space-3);
  border: 0;
  background: transparent;
  color: inherit;
  cursor: pointer;
  font: inherit;
  text-align: left;
}

.conv-title {
  display: block;
  overflow: hidden;
  color: var(--color-text, var(--theme-color-text-strong));
  font-size: var(--theme-font-size-label);
  font-weight: var(--theme-font-weight-semibold);
  text-overflow: ellipsis;
  white-space: nowrap;
}

.conv-meta {
  margin-top: var(--theme-space-0-5);
  color: var(--color-text-muted, var(--theme-color-neutral-400));
  font-size: var(--theme-font-size-caption);
}

.conv-menu-btn {
  position: absolute;
  top: 9px;
  right: 7px;
  display: inline-flex;
  width: 32px;
  height: 32px;
  align-items: center;
  justify-content: center;
  padding: 0;
  border: none;
  border-radius: var(--theme-radius-control);
  background: transparent;
  color: var(--color-text-muted, var(--theme-color-neutral-400));
  cursor: pointer;
}

.conv-menu-btn:hover {
  background: color-mix(in srgb, var(--color-border, var(--theme-color-border-default)) 50%, transparent);
  color: var(--color-text, var(--theme-color-text-strong));
}

.conv-menu {
  position: absolute;
  top: 34px;
  right: 8px;
  z-index: 10;
  overflow: hidden;
  min-width: 8.5rem;
  border: 1px solid var(--color-border, var(--theme-color-border-default));
  border-radius: var(--theme-radius-control);
  background: var(--color-surface, var(--theme-color-static-white));
  box-shadow: var(--theme-shadow-float);
}

.conv-menu button {
  display: flex;
  width: 100%;
  align-items: center;
  gap: var(--theme-space-2);
  padding: var(--theme-space-2) var(--theme-space-3);
  border: none;
  background: none;
  color: var(--color-text, var(--theme-color-text-strong));
  cursor: pointer;
  font-size: var(--theme-font-size-caption);
  text-align: left;
}

.conv-menu button:hover {
  background: color-mix(in srgb, var(--color-primary, var(--theme-color-info-solid)) 6%, var(--theme-color-static-white) 94%);
}

.edit-input {
  width: calc(100% - 24px);
  min-height: 38px;
  margin: var(--theme-space-2-5) var(--theme-space-3);
  padding: var(--theme-space-1) var(--theme-space-2);
  border: 1px solid var(--color-primary, var(--theme-color-info-solid));
  border-radius: var(--theme-radius-control);
  font-family: inherit;
  font-size: var(--theme-font-size-label);
}

@media (min-width: 900px) {
  .chat-sidebar {
    width: 280px;
    max-height: none;
    border-right: 1px solid color-mix(in srgb, var(--chat-border) 60%, transparent);
    border-bottom: 0;
  }

  .sidebar-head {
    align-items: stretch;
    flex-direction: column;
  }

  .new-chat-btn {
    width: 100%;
  }
}
</style>
