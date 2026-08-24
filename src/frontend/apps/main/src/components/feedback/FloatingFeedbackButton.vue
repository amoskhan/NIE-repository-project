<script setup lang="ts">
import { computed, shallowRef, watch } from "vue";
import {
  HandThumbDownIcon,
  HandThumbUpIcon,
} from "@heroicons/vue/24/outline";
import { NieButton, NieModal, NieTextarea, useToast } from "@nie/ui";
import feedbackService from "@/services/feedback/feedbackService";
import type { FeedbackRating } from "@/services/feedback/feedbackService";

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
const canSubmit = computed(() => !!selectedRating.value && !isSubmitting.value);

function openFeedback(rating: FeedbackRating) {
  selectedRating.value = rating;
  submitError.value = null;
  isPopupOpen.value = true;
}

function closePopup() {
  isPopupOpen.value = false;
  selectedRating.value = null;
  additionalFeedback.value = "";
  submitError.value = null;
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
    toast.success("Thank you! Your feedback helps us improve this application.");
  } catch {
    submitError.value = "Failed to submit feedback. Please try again.";
  } finally {
    isSubmitting.value = false;
  }
}

watch(
  () => props.functionId,
  () => {
    if (isPopupOpen.value) {
      closePopup();
    }
  },
);

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

  <NieModal
    v-model="isPopupOpen"
    :title="questionText"
    size="md"
    placement="mobile-sheet"
    @close="closePopup"
  >
          <div class="space-y-5">
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
              <NieTextarea
                v-model="additionalFeedback"
                label="Additional feedback"
                :rows="4"
                placeholder="Tell us more about your experience..."
                :disabled="isSubmitting"
              />
              <p class="feedback-modal__warning">
                Please refrain from entering any sensitive or personal
                information.
              </p>
            </section>
          </div>

    <template #footer>
          <div class="feedback-modal__footer">
            <p v-if="submitError" class="feedback-modal__error">
              {{ submitError }}
            </p>
            <div class="feedback-modal__footer-actions">
              <NieButton
                variant="outline"
                :disabled="isSubmitting"
                @click="closePopup"
              >
                Cancel
              </NieButton>
              <NieButton
                :loading="isSubmitting"
                :disabled="!canSubmit"
                @click="handleSubmit"
              >
                Submit Feedback
              </NieButton>
            </div>
          </div>
    </template>
  </NieModal>
</template>

<style scoped>
.feedback-actions {
  display: inline-flex;
  align-items: center;
  gap: var(--theme-space-2);
  flex-shrink: 0;
}

.feedback-actions__button {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 2rem;
  height: 2rem;
  border: 1px solid color-mix(in srgb, var(--color-border) 88%, transparent);
  border-radius: var(--theme-radius-pill);
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
.feedback-modal__rating-button:focus-visible {
  outline: 2px solid var(--color-primary);
  outline-offset: 2px;
}

.feedback-actions__button--positive {
  border-color: color-mix(in srgb, var(--theme-color-success-600) 24%, var(--color-border));
  color: var(--theme-color-success-700);
  background: color-mix(in srgb, var(--theme-color-success-50) 68%, var(--color-surface));
}

.feedback-actions__button--negative {
  border-color: color-mix(in srgb, var(--theme-color-danger-solid) 24%, var(--color-border));
  color: var(--theme-color-danger-700);
  background: color-mix(in srgb, var(--theme-color-danger-surface) 68%, var(--color-surface));
}

.feedback-actions__button--positive:hover {
  border-color: color-mix(in srgb, var(--theme-color-success-600) 28%, var(--color-border));
  background: color-mix(in srgb, var(--theme-color-success-100) 66%, var(--color-surface));
}

.feedback-actions__button--negative:hover {
  border-color: color-mix(in srgb, var(--theme-color-danger-solid) 28%, var(--color-border));
  background: color-mix(in srgb, var(--theme-color-danger-100) 66%, var(--color-surface));
}

.feedback-actions__icon {
  width: 1rem;
  height: 1rem;
}

.feedback-modal__section {
  display: flex;
  flex-direction: column;
  gap: var(--theme-space-2);
}

.feedback-modal__section-title {
  margin: 0;
  color: var(--color-text);
  font-size: var(--theme-font-size-label);
  font-weight: var(--theme-font-weight-bold);
}

.feedback-modal__hint {
  margin: 0;
  color: var(--color-text-muted);
  font-size: var(--theme-font-size-label);
}

.feedback-modal__rating-buttons {
  display: flex;
  gap: var(--theme-space-2);
}

.feedback-modal__rating-button {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 2.75rem;
  height: 2.75rem;
  border: 1px solid var(--color-border);
  border-radius: var(--theme-radius-panel);
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

.feedback-modal__warning {
  margin: 0;
  color: var(--theme-color-warning-600);
  font-size: var(--theme-font-size-caption);
}

.feedback-modal__footer {
  display: flex;
  flex-direction: column;
  gap: var(--theme-space-3);
}

.feedback-modal__footer-actions {
  display: flex;
  justify-content: flex-end;
  gap: var(--theme-space-3);
}

.feedback-modal__rating-button:disabled {
  cursor: not-allowed;
  opacity: 0.55;
}

.feedback-modal__error {
  margin: 0;
  color: var(--theme-color-danger-600);
  font-size: var(--theme-font-size-label);
  text-align: right;
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


  .feedback-modal__error {
    text-align: left;
  }
}
</style>
