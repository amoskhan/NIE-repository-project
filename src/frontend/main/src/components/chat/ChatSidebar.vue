<script setup lang="ts">
import { computed, ref } from "vue";
import type { Conversation } from "../../services/chatService";

const props = defineProps<{
  conversations: Conversation[];
  currentConversationId?: number;
  isLoading: boolean;
}>();

const emit = defineEmits<{
  (e: "select", conversation: Conversation): void;
  (e: "new"): void;
  (e: "delete", conversation: Conversation): void;
  (e: "rename", conversation: Conversation, newTitle: string): void;
}>();

const editingId = ref<number | null>(null);
const editTitle = ref("");
const menuOpenId = ref<number | null>(null);

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
            {{ conversationCount }} saved chat{{
              conversationCount === 1 ? "" : "s"
            }}
          </p>
        </div>
      </div>

      <button
        type="button"
        class="new-chat-btn"
        aria-label="Start a new chat"
        @click="emit('new')"
      >
        <span class="material-symbols-outlined text-[18px]" aria-hidden="true"
          >add</span
        >
        <span>New</span>
      </button>
    </div>

    <div class="conversations-list">
      <div v-if="isLoading" class="loading">
        <span class="material-symbols-outlined text-[20px]"
          >progress_activity</span
        >
        <span>Loading conversations...</span>
      </div>
      <div v-else-if="conversations.length === 0" class="empty">
        <span class="material-symbols-outlined text-[24px]" aria-hidden="true"
          >forum</span
        >
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
            :aria-current="
              conv.id === currentConversationId ? 'page' : undefined
            "
            @click="selectConversation(conv)"
          >
            <span class="conv-title">{{ conv.title }}</span>
            <span class="conv-meta">
              {{ formatDate(conv.lastMessageAt) }} &middot;
              {{ conv.messageCount }} msgs
            </span>
          </button>
          <button
            type="button"
            class="conv-menu-btn"
            :aria-label="`Open actions for ${conv.title}`"
            @click.stop="menuOpenId = menuOpenId === conv.id ? null : conv.id"
          >
            <span
              class="material-symbols-outlined text-[18px]"
              aria-hidden="true"
              >more_horiz</span
            >
          </button>
          <div v-if="menuOpenId === conv.id" class="conv-menu">
            <button type="button" @click.stop="startEdit(conv)">
              <span
                class="material-symbols-outlined text-[16px]"
                aria-hidden="true"
                >edit</span
              >
              <span>Rename</span>
            </button>
            <button type="button" @click.stop="requestDelete(conv)">
              <span
                class="material-symbols-outlined text-[16px]"
                aria-hidden="true"
                >delete</span
              >
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
  --chat-primary: var(--color-primary, #4f46e5);
  --chat-border: color-mix(
    in srgb,
    var(--chat-primary) 9%,
    var(--color-border, #e2e8f0) 91%
  );
  --chat-panel: var(--color-surface, #ffffff);
  --chat-active: var(--color-sidebar-active, #eef0fe);

  display: flex;
  width: 100%;
  min-width: 0;
  max-height: 18rem;
  flex-direction: column;
  flex-shrink: 0;
  border-bottom: 1px solid
    color-mix(in srgb, var(--chat-border) 60%, transparent);
  background: color-mix(in srgb, var(--chat-panel) 78%, transparent);
  backdrop-filter: blur(14px);
}

.sidebar-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 12px;
  border-bottom: 1px solid
    color-mix(in srgb, var(--chat-border) 55%, transparent);
}

.sidebar-title {
  display: flex;
  min-width: 0;
  align-items: center;
  gap: 10px;
}

.sidebar-icon {
  display: inline-flex;
  width: 34px;
  height: 34px;
  flex-shrink: 0;
  align-items: center;
  justify-content: center;
  border-radius: 10px;
  background: color-mix(
    in srgb,
    var(--color-primary, #3b82f6) 12%,
    #ffffff 88%
  );
  color: var(--color-primary, #3b82f6);
}

.sidebar-copy {
  min-width: 0;
}

.sidebar-heading {
  margin: 0;
  color: var(--color-text, #111827);
  font-size: 13px;
  font-weight: 700;
}

.sidebar-meta {
  margin: 2px 0 0;
  color: var(--color-text-muted, #6b7280);
  font-size: 11px;
}

.new-chat-btn {
  display: inline-flex;
  min-height: 40px;
  align-items: center;
  justify-content: center;
  gap: 6px;
  padding: 8px 12px;
  border-radius: 10px;
  border: 1px solid
    color-mix(
      in srgb,
      var(--color-primary, #3b82f6) 30%,
      var(--color-border, #d1d5db)
    );
  background: var(--color-primary, #3b82f6);
  color: #ffffff;
  cursor: pointer;
  font-size: 13px;
  font-weight: 700;
}

.new-chat-btn:hover {
  background: color-mix(in srgb, var(--color-primary, #3b82f6) 86%, #000 14%);
}

.conversations-list {
  flex: 1;
  overflow-y: auto;
  padding: 8px;
}

.loading,
.empty {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  min-height: 96px;
  color: var(--color-text-muted, #6b7280);
  font-size: 13px;
}

.loading .material-symbols-outlined {
  animation: spin 1s linear infinite;
}

.conv-item {
  position: relative;
  width: 100%;
  margin-bottom: 2px;
  border: 1px solid transparent;
  border-radius: 8px;
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
  padding: 10px 44px 10px 12px;
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
  color: var(--color-text, #111827);
  font-size: 13px;
  font-weight: 650;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.conv-meta {
  margin-top: 2px;
  color: var(--color-text-muted, #9ca3af);
  font-size: 11px;
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
  border-radius: 8px;
  background: transparent;
  color: var(--color-text-muted, #9ca3af);
  cursor: pointer;
}

.conv-menu-btn:hover {
  background: color-mix(in srgb, var(--color-border, #e5e7eb) 50%, transparent);
  color: var(--color-text, #111827);
}

.conv-menu {
  position: absolute;
  top: 34px;
  right: 8px;
  z-index: 10;
  overflow: hidden;
  min-width: 8.5rem;
  border: 1px solid var(--color-border, #e5e7eb);
  border-radius: 8px;
  background: var(--color-surface, #ffffff);
  box-shadow: 0 14px 28px -18px rgba(15, 23, 42, 0.28);
}

.conv-menu button {
  display: flex;
  width: 100%;
  align-items: center;
  gap: 8px;
  padding: 9px 14px;
  border: none;
  background: none;
  color: var(--color-text, #111827);
  cursor: pointer;
  font-size: 12px;
  text-align: left;
}

.conv-menu button:hover {
  background: color-mix(in srgb, var(--color-primary, #3b82f6) 6%, #ffffff 94%);
}

.edit-input {
  width: calc(100% - 24px);
  min-height: 38px;
  margin: 10px 12px;
  padding: 6px 8px;
  border: 1px solid var(--color-primary, #3b82f6);
  border-radius: 4px;
  font-family: inherit;
  font-size: 13px;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

@media (min-width: 900px) {
  .chat-sidebar {
    width: 280px;
    max-height: none;
    border-right: 1px solid
      color-mix(in srgb, var(--chat-border) 60%, transparent);
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
