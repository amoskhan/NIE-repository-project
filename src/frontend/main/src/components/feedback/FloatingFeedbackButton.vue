<script setup lang="ts">
import { computed, onMounted, onUnmounted, shallowRef, watch } from "vue";
import { HandThumbDownIcon, HandThumbUpIcon } from "@heroicons/vue/24/outline";
import { XMarkIcon } from "@heroicons/vue/24/solid";
import { useToast } from "@/composables/useToast";
import feedbackService from "@/services/feedbackService";
import type { FeedbackRating } from "@/services/feedbackService";

interface Props {
  functionId: string;
  questionText?: string;
}

const props = withDefaults(defineProps<Props>(), {
  questionText: "How is your experience with this page?",
});

const toast = useToast();

const isPopupOpen = shallowRef(false);
const selectedRating = shallowRef<FeedbackRating | null>(null);
const additionalFeedback = shallowRef("");
const isSubmitting = shallowRef(false);
const submitError = shallowRef<string | null>(null);
const isMobileViewport = shallowRef(false);

const canSubmit = computed(() => !!selectedRating.value && !isSubmitting.value);

function syncViewport() {
  isMobileViewport.value = window.innerWidth < 640;
}

function openFeedback(rating: FeedbackRating) {
  selectedRating.value = rating;
  submitError.value = null;
  isPopupOpen.value = true;
  document.body.style.overflow = "hidden";
}

function closePopup() {
  isPopupOpen.value = false;
  selectedRating.value = null;
  additionalFeedback.value = "";
  submitError.value = null;
  document.body.style.overflow = "";
}

function selectRating(rating: FeedbackRating) {
  selectedRating.value = rating;
  submitError.value = null;
}

async function handleSubmit() {
  if (!selectedRating.value) return;

  isSubmitting.value = true;
  submitError.value = null;

  try {
    const submittedRating = selectedRating.value;
    await feedbackService.submit({
      function_id: props.functionId,
      rating: submittedRating,
      feedback: additionalFeedback.value.trim(),
      page: window.location.href,
    });

    closePopup();
    toast.success(
      "Thank you! Your feedback helps us improve this application.",
    );
  } catch {
    submitError.value = "Failed to submit feedback. Please try again.";
  } finally {
    isSubmitting.value = false;
  }
}

function onKeyDown(event: KeyboardEvent) {
  if (event.key === "Escape" && isPopupOpen.value) {
    closePopup();
  }
}

onMounted(() => {
  syncViewport();
  window.addEventListener("resize", syncViewport);
  document.addEventListener("keydown", onKeyDown);
});

watch(
  () => props.functionId,
  () => {
    if (isPopupOpen.value) {
      closePopup();
    }
  },
);

onUnmounted(() => {
  window.removeEventListener("resize", syncViewport);
  document.removeEventListener("keydown", onKeyDown);
  document.body.style.overflow = "";
});
</script>

<template>
  <div class="feedback-actions" data-testid="feedback-actions">
    <button
      class="feedback-actions__button feedback-actions__button--positive"
      type="button"
      aria-label="Share positive feedback"
      title="Share positive feedback"
      @click="openFeedback('5')"
    >
      <HandThumbUpIcon class="feedback-actions__icon" />
    </button>
    <button
      class="feedback-actions__button feedback-actions__button--negative"
      type="button"
      aria-label="Share negative feedback"
      title="Share negative feedback"
      @click="openFeedback('1')"
    >
      <HandThumbDownIcon class="feedback-actions__icon" />
    </button>
  </div>

  <Teleport to="body">
    <Transition name="feedback-modal">
      <div
        v-if="isPopupOpen"
        class="feedback-modal"
        :class="{ 'feedback-modal--mobile': isMobileViewport }"
        @click="closePopup"
      >
        <section
          class="feedback-modal__dialog"
          :class="{
            'feedback-modal__sheet': isMobileViewport,
            'feedback-modal__card': !isMobileViewport,
          }"
          role="dialog"
          aria-modal="true"
          aria-label="Feedback"
          @click.stop
        >
          <div
            v-if="isMobileViewport"
            class="feedback-modal__grabber"
            aria-hidden="true"
          ></div>

          <header class="feedback-modal__header">
            <div class="feedback-modal__title-section">
              <p class="feedback-modal__eyebrow">Feedback</p>
              <h2 class="feedback-modal__title">{{ questionText }}</h2>
            </div>
            <button
              type="button"
              class="feedback-modal__close"
              aria-label="Close feedback"
              @click="closePopup"
            >
              <XMarkIcon class="feedback-modal__close-icon" />
            </button>
          </header>

          <div class="feedback-modal__body">
            <section class="feedback-modal__section">
              <p class="feedback-modal__section-title">Rating</p>
              <p class="feedback-modal__hint">Tap a rating to continue.</p>
              <div class="feedback-modal__rating-buttons">
                <button
                  type="button"
                  class="feedback-modal__rating-button"
                  :class="{ active: selectedRating === '5' }"
                  :disabled="isSubmitting"
                  aria-label="Thumbs up"
                  @click="selectRating('5')"
                >
                  <HandThumbUpIcon class="feedback-modal__rating-icon" />
                </button>
                <button
                  type="button"
                  class="feedback-modal__rating-button"
                  :class="{ active: selectedRating === '1' }"
                  :disabled="isSubmitting"
                  aria-label="Thumbs down"
                  @click="selectRating('1')"
                >
                  <HandThumbDownIcon class="feedback-modal__rating-icon" />
                </button>
              </div>
            </section>

            <section class="feedback-modal__section">
              <p class="feedback-modal__section-title">Additional Feedback</p>
              <textarea
                v-model="additionalFeedback"
                rows="4"
                class="feedback-modal__textarea"
                placeholder="Tell us more about your experience..."
                :disabled="isSubmitting"
              />
              <p class="feedback-modal__warning">
                Please refrain from entering any sensitive or personal
                information.
              </p>
            </section>
          </div>

          <footer class="feedback-modal__footer">
            <p v-if="submitError" class="feedback-modal__error">
              {{ submitError }}
            </p>
            <div class="feedback-modal__footer-actions">
              <button
                type="button"
                class="feedback-modal__cancel"
                :disabled="isSubmitting"
                @click="closePopup"
              >
                Cancel
              </button>
              <button
                type="button"
                class="feedback-modal__submit"
                :disabled="!canSubmit"
                @click="handleSubmit"
              >
                {{ isSubmitting ? "Submitting..." : "Submit Feedback" }}
              </button>
            </div>
          </footer>
        </section>
      </div>
    </Transition>
  </Teleport>
</template>

<style scoped>
.feedback-actions {
  display: inline-flex;
  align-items: center;
  gap: 0.45rem;
  flex-shrink: 0;
}

.feedback-actions__button {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 2rem;
  height: 2rem;
  border: 1px solid color-mix(in srgb, var(--color-border) 88%, transparent);
  border-radius: 999px;
  background: color-mix(in srgb, var(--color-surface) 82%, transparent);
  color: var(--color-text-muted);
  transition:
    border-color 0.18s ease,
    color 0.18s ease,
    background-color 0.18s ease,
    transform 0.18s ease;
}

.feedback-actions__button:hover {
  transform: translateY(-1px);
}

.feedback-actions__button:focus-visible,
.feedback-modal__close:focus-visible,
.feedback-modal__rating-button:focus-visible,
.feedback-modal__cancel:focus-visible,
.feedback-modal__submit:focus-visible {
  outline: 2px solid var(--color-primary);
  outline-offset: 2px;
}

.feedback-actions__button--positive {
  border-color: color-mix(in srgb, #059669 24%, var(--color-border));
  color: #047857;
  background: color-mix(in srgb, #ecfdf5 68%, var(--color-surface));
}

.feedback-actions__button--negative {
  border-color: color-mix(in srgb, #e11d48 24%, var(--color-border));
  color: #be123c;
  background: color-mix(in srgb, #fff1f2 68%, var(--color-surface));
}

.feedback-actions__button--positive:hover {
  border-color: color-mix(in srgb, #059669 28%, var(--color-border));
  background: color-mix(in srgb, #d1fae5 66%, var(--color-surface));
}

.feedback-actions__button--negative:hover {
  border-color: color-mix(in srgb, #e11d48 28%, var(--color-border));
  background: color-mix(in srgb, #ffe4e6 66%, var(--color-surface));
}

.feedback-actions__icon {
  width: 1rem;
  height: 1rem;
}

.feedback-modal {
  position: fixed;
  inset: 0;
  z-index: 100;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 1rem;
  background-color: rgba(15, 23, 42, 0.54);
}

.feedback-modal--mobile {
  align-items: flex-end;
  padding: 0.75rem;
  padding-bottom: max(0.75rem, env(safe-area-inset-bottom));
}

.feedback-modal__dialog {
  display: flex;
  flex-direction: column;
  width: 100%;
  max-height: calc(100dvh - 2rem);
  overflow: hidden;
  border: 1px solid color-mix(in srgb, var(--color-border) 90%, transparent);
  background: color-mix(in srgb, var(--color-surface) 98%, transparent);
  color: var(--color-text);
  box-shadow: 0 28px 60px -32px rgba(15, 23, 42, 0.42);
}

.feedback-modal__card {
  max-width: 28rem;
  border-radius: 1rem;
}

.feedback-modal__sheet {
  max-width: 28rem;
  max-height: 86dvh;
  border-radius: 1.5rem 1.5rem 1rem 1rem;
}

.feedback-modal__grabber {
  align-self: center;
  width: 3.5rem;
  height: 0.35rem;
  margin-top: 0.75rem;
  border-radius: 999px;
  background: color-mix(
    in srgb,
    var(--color-border) 78%,
    var(--color-text-muted) 22%
  );
}

.feedback-modal__header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 1rem;
  padding: 1.2rem 1.2rem 0;
}

.feedback-modal__title-section {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
  min-width: 0;
}

.feedback-modal__eyebrow {
  margin: 0;
  color: var(--color-text-muted);
  font-size: 0.74rem;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.feedback-modal__title {
  margin: 0;
  color: var(--color-text);
  font-size: 1rem;
  font-weight: 700;
  line-height: 1.5;
}

.feedback-modal__close {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 2.25rem;
  height: 2.25rem;
  border: 0;
  border-radius: 999px;
  background: transparent;
  color: var(--color-text-muted);
}

.feedback-modal__close:hover {
  background: var(--color-surface-alt);
  color: var(--color-text);
}

.feedback-modal__close-icon {
  width: 1.25rem;
  height: 1.25rem;
}

.feedback-modal__body {
  display: flex;
  flex: 1 1 auto;
  flex-direction: column;
  gap: 1.2rem;
  min-height: 0;
  overflow-y: auto;
  padding: 1.2rem;
}

.feedback-modal__section {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.feedback-modal__section-title {
  margin: 0;
  color: var(--color-text);
  font-size: 0.82rem;
  font-weight: 700;
}

.feedback-modal__hint {
  margin: 0;
  color: var(--color-text-muted);
  font-size: 0.78rem;
}

.feedback-modal__rating-buttons {
  display: flex;
  gap: 0.55rem;
}

.feedback-modal__rating-button {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 2.75rem;
  height: 2.75rem;
  border: 1px solid var(--color-border);
  border-radius: 0.9rem;
  background: var(--color-surface);
  color: var(--color-text-muted);
}

.feedback-modal__rating-button.active {
  border-color: color-mix(
    in srgb,
    var(--color-primary) 56%,
    var(--color-border)
  );
  background: var(--color-sidebar-active);
  color: var(--color-primary);
}

.feedback-modal__rating-icon {
  width: 1.35rem;
  height: 1.35rem;
}

.feedback-modal__textarea {
  width: 100%;
  padding: 0.8rem 0.9rem;
  border: 1px solid var(--color-border);
  border-radius: 0.9rem;
  background: var(--color-surface);
  color: var(--color-text);
  font-size: 0.88rem;
  resize: vertical;
}

.feedback-modal__textarea:focus {
  outline: none;
  border-color: var(--color-primary);
  box-shadow: 0 0 0 3px
    color-mix(in srgb, var(--color-primary) 12%, transparent);
}

.feedback-modal__warning {
  margin: 0;
  color: #d97706;
  font-size: 0.74rem;
}

.feedback-modal__footer {
  display: flex;
  flex-direction: column;
  gap: 0.7rem;
  padding: 1rem 1.2rem 1.2rem;
  border-top: 1px solid var(--color-border);
}

.feedback-modal__footer-actions {
  display: flex;
  justify-content: flex-end;
  gap: 0.75rem;
}

.feedback-modal__cancel,
.feedback-modal__submit {
  min-height: 2.75rem;
  padding: 0.7rem 1rem;
  border-radius: 0.85rem;
  font-size: 0.88rem;
  font-weight: 600;
}

.feedback-modal__cancel {
  border: 1px solid var(--color-border);
  background: var(--color-surface);
  color: var(--color-text);
}

.feedback-modal__submit {
  border: 0;
  background: linear-gradient(
    135deg,
    var(--color-primary),
    var(--color-primary-dark)
  );
  color: #fff;
}

.feedback-modal__submit:disabled,
.feedback-modal__cancel:disabled,
.feedback-modal__rating-button:disabled {
  cursor: not-allowed;
  opacity: 0.55;
}

.feedback-modal__error {
  margin: 0;
  color: #dc2626;
  font-size: 0.82rem;
  text-align: right;
}

.feedback-modal-enter-active,
.feedback-modal-leave-active {
  transition: opacity 0.2s ease;
}

.feedback-modal-enter-from,
.feedback-modal-leave-to {
  opacity: 0;
}

@media (min-width: 640px) {
  .feedback-actions__button {
    width: 2.125rem;
    height: 2.125rem;
  }
}

@media (max-width: 640px) {
  .feedback-modal__footer-actions {
    flex-direction: column-reverse;
  }

  .feedback-modal__cancel,
  .feedback-modal__submit {
    width: 100%;
  }

  .feedback-modal__error {
    text-align: left;
  }
}
</style>
