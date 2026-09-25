# Feature Specification: SG PE Syllabus Bot

**Feature Directory**: `specs/001-aug1-ai-assisted-feedback-ecosystem-for-physical-education`  
**Created**: 2026-09-24  
**Status**: Prototype  
**Input**: User description: "SG PE Syllabus Bot is a web app that answers questions on the 2024 MOE PE Syllabus and grades pupils' Fundamental Movement Skills (FMS) from video."

## Current Prototype Boundary

The current implementation provides a syllabus-chat interface with the supplied `upload/2024 Physical Education Primary Secondary and PreUniversity Syllabus (1).pdf` permanently connected. All 239 PDF pages are extracted by the development/build process and included with the app; the answer backend independently reads this same document. Visitors do not select, upload, activate, replace, or remove a document. Browser PDF parsing and PDF worker support are not required. A link opens the supplied PDF for reference; other project uploads are not exposed.

The running prototype uses a prepared, syllabus-grounded library of 18 answers without an API key or live model connection. It covers kicking; 3v3 net-barrier and touch rules; safe landing; athletics; swimming; throwing/catching; dance; gymnastics; outdoor education; FMS; movement concepts; primary learning areas; camp; neighbourhood trips; competitions; practice time; and personal health practice. Visitors can submit common paraphrases or browse/filter available questions. The UI explicitly labels the limited-coverage library and does not imply that these are live AI-generated answers. Answers are concise, with PDF/printed page references, expandable evidence, and PDF page links.

Matching requires recognised meaningful words, a subject match and an unambiguous best match; it must not return an answer merely because an unrelated question mentions a known skill. Unsupported questions receive a library-coverage limitation and clickable available questions, not invented facts or a claim that the full syllabus lacks the information. Source/details follow-ups use the last user question. The existing backend is registered on port 15100. `/health` reports `answerAvailable`, `answerMode: syllabus-library`, and `answerCount` independently of `llmConfigured: false`; `/topics` publishes the browsable questions. Network errors retain a retry action without duplicating the question; controls disable while an answer is pending. The old LLM adapter is retained but not invoked by startup, and no provider credentials are required or consumed.

Updating the project PDF regenerates frontend content during development or the next build. The backend validates the extracted document fingerprint and all library quotes at startup; a changed document requires reviewing answers/citations and updating the fingerprint before restarting. A missing, unreadable, empty or unreviewed changed PDF prevents backend startup. Conversations stay in browser memory for the session, with the most recent four messages sent to this backend as context per question. No questions or syllabus content are sent to an external model, logged, or stored by the backend. The syllabus Q&A screen does not require a visitor account, although external access remains subject to Ignite deployment/access settings. Protected pupil-video features below remain future scope; the prototype does not assess videos.

### Project syllabus acceptance checks

- Opening or refreshing the chatbot shows the syllabus as ready with its page count and no upload, source-selection, replace, or remove controls.
- With no LLM configuration, covered questions return prepared direct answers with supporting syllabus references, not lists of raw excerpts.
- **When do students learn kicking?** identifies **Primary 2**, with PDF page 37 (printed page 33).
- **When is 3v3 net and barrier taught?** identifies grouped **Primary 5 and 6** outcomes, not an exclusive year, citing PDF pages 40 and 44 (printed pages 36 and 40).
- Safe landing answers explain balls-of-feet contact, bending ankles/knees/hips, abdominal control and arms for balance, citing PDF page 77 (printed page 73).
- Every listed question and alias returns its intended prepared answer. Ambiguous, unrelated and unsupported variants are declined with helpful alternatives and no invented citations.
- Changed source content or invalid quotations prevent stale answers from being served. Network errors are reported clearly; retrying does not duplicate the user's message.
- Questions work even when PDF downloads or browser PDF workers are unavailable, since extraction occurs before the app reaches the browser.
- The reference link opens the supplied PDF.
- Both live preview and production builds include extracted syllabus content and require the registered backend route for library answers; public deployment/access is a separate hosting concern.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Ask a syllabus question (Priority: P1)

An educator asks a natural-language question about the 2024 MOE Physical Education Syllabus and receives a concise, classroom-usable answer grounded in the approved syllabus content, with the relevant source location shown.

**Why this priority**: Educators need trustworthy curriculum guidance before they can confidently plan or assess learning.

**Independent Test**: An educator can ask questions covering curriculum aims, learning outcomes, or teaching guidance and receive an answer with at least one matching syllabus reference.

**Acceptance Scenarios**:

1. **Given** an educator is on the syllabus question screen, **When** they submit a question that is covered by the approved 2024 syllabus, **Then** they see a clear answer and the supporting syllabus section or page reference.
2. **Given** an educator asks a question that is not covered by the approved syllabus, **When** the question is submitted, **Then** the app clearly says it cannot answer from the syllabus and does not present an unsupported answer as syllabus guidance.
3. **Given** a question could have more than one relevant interpretation, **When** the app identifies the ambiguity, **Then** it asks a focused follow-up or states the interpretation used before giving its answer.

---

### User Story 2 - Assess an FMS video (Priority: P2)

An educator uploads a pupil's video, selects the Fundamental Movement Skill and age-appropriate assessment rubric, and receives a provisional performance result with observable strengths, improvement points, and video moments that support the result.

**Why this priority**: Timely, specific feedback helps teachers assess movement learning more consistently and gives pupils actionable next steps.

**Independent Test**: An educator can submit a consented video for a selected FMS, review the provisional result and evidence, revise it if needed, and save the educator-confirmed assessment.

**Acceptance Scenarios**:

1. **Given** an educator has selected an FMS and available rubric, **When** they upload a supported, sufficiently clear pupil video, **Then** the app returns a provisional rubric-aligned result, observations, and specific improvement feedback.
2. **Given** a video is incomplete, unclear, or does not show the selected skill well enough, **When** it is assessed, **Then** the app explains that it cannot make a reliable assessment and asks for a new or clearer video rather than assigning a result.
3. **Given** a provisional assessment is shown, **When** the educator changes its level or feedback and confirms it, **Then** the saved assessment records the educator-confirmed result separately from the original provisional result.

---

### User Story 3 - Review feedback and progress (Priority: P3)

An educator reviews a pupil's confirmed FMS assessments over time to identify strengths, recurring improvement needs, and the next useful practice focus.

**Why this priority**: A history of feedback lets educators use individual assessments to support ongoing learning rather than treating each video as an isolated grade.

**Independent Test**: An educator can open a pupil's assessment history and see confirmed results grouped by FMS, including the associated feedback and dates.

**Acceptance Scenarios**:

1. **Given** a pupil has one or more confirmed assessments, **When** an authorised educator views that pupil's history, **Then** they can see each assessment's selected FMS, confirmed result, feedback, and assessment date.
2. **Given** a pupil has no confirmed assessment for a selected FMS, **When** the educator views the history, **Then** the app clearly identifies the skill as not yet assessed rather than inferring progress.

### Edge Cases

- A syllabus question includes content outside the 2024 MOE PE Syllabus, outdated material, or a request for advice not supported by the source.
- The approved syllabus content is unavailable, incomplete, or has not been updated to the required 2024 edition.
- A video contains more than one pupil, does not visibly show the selected FMS, is too short, is corrupted, or is uploaded without the required educator confirmation of consent.
- An educator is not authorised to view a pupil's existing video or assessment history.
- An educator attempts to finalise an assessment without reviewing the provisional result.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The app MUST let authorised educators submit natural-language questions about the 2024 MOE Physical Education Syllabus.
- **FR-002**: The app MUST answer a syllabus question only from the approved 2024 MOE PE Syllabus content available to the app and MUST identify the relevant source section or page for each substantive answer.
- **FR-003**: The app MUST state when it cannot answer a question from the approved syllabus content, including when the question is outside scope or the source does not support a conclusion.
- **FR-004**: The app MUST let an authorised educator submit a pupil video for assessment only after the educator confirms that the required permission to use the video has been obtained.
- **FR-005**: Before video submission, the app MUST require the educator to select the pupil, the FMS being assessed, and an applicable assessment rubric.
- **FR-006**: The app MUST produce a provisional FMS assessment only when the submitted video provides enough visible evidence for the selected skill and rubric; otherwise, it MUST explain why an assessment cannot be made and request a suitable replacement video.
- **FR-007**: A provisional FMS assessment MUST include a rubric-aligned performance result, at least one observed strength, at least one improvement focus when improvement is indicated, and references to the supporting moments in the submitted video.
- **FR-008**: The app MUST require an educator to review and explicitly confirm, edit, or reject a provisional FMS assessment before it is recorded as the pupil's final assessment.
- **FR-009**: The app MUST preserve the original provisional result and any educator changes as part of the assessment record so that the confirmed outcome is traceable.
- **FR-010**: The app MUST let authorised educators view a pupil's confirmed FMS assessment history, organised by FMS and including assessment dates, confirmed results, and feedback.
- **FR-011**: The app MUST restrict access to pupil videos and assessment records to authorised school users with a legitimate educational need.
- **FR-012**: The app MUST allow authorised users to remove a submitted pupil video and its related assessment record in accordance with the school's retention and deletion policy.
- **FR-013**: The app MUST not present an FMS assessment as a medical, diagnostic, or high-stakes decision, and it MUST present educator confirmation as required for a final recorded assessment.

### Key Entities

- **Syllabus source**: The approved 2024 MOE PE Syllabus content and its identifiable sections or pages used to support answers.
- **Syllabus question and answer**: An educator's question, the returned answer, any stated limitation, and the cited syllabus locations.
- **Pupil**: A learner whose FMS assessment history is maintained by authorised school users.
- **FMS rubric**: The school-approved, age-appropriate criteria and performance levels for one Fundamental Movement Skill.
- **Video submission**: A consent-confirmed pupil video submitted for one selected FMS and rubric.
- **Provisional assessment**: The initial rubric-aligned result, observations, supporting video moments, and feedback produced from a video submission.
- **Confirmed assessment**: The educator-reviewed final result, feedback, confirmation details, and any changes from the provisional assessment.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In representative educator testing, at least 90% of syllabus questions that are answerable from the approved content receive an answer with a correct supporting syllabus reference.
- **SC-002**: At least 90% of representative out-of-scope or unsupported syllabus questions are clearly declined or qualified without an unsupported syllabus claim.
- **SC-003**: An educator can complete a supported video submission, review its provisional assessment, and confirm or edit the final assessment in no more than 5 minutes, excluding video-recording time.
- **SC-004**: At least 90% of supported representative FMS videos produce a provisional assessment that contains a result, an observable strength, feedback, and supporting video moments.
- **SC-005**: 100% of confirmed FMS assessment records in acceptance testing show an educator confirmation and preserve the corresponding provisional result.
- **SC-006**: In usability testing, at least 85% of participating educators can find a pupil's confirmed assessment history and identify the next improvement focus without assistance.

## Assumptions

- The app's primary users are school educators and authorised administrators; pupils do not require direct accounts for the first release.
- Schools provide the approved 2024 MOE PE Syllabus source material and approve the FMS rubrics used for assessment.
- Educators obtain and confirm the permissions required by their school before uploading pupil videos.
- Educators, not the app, retain responsibility for the final educational assessment and feedback given to a pupil.
- Live camera assessment, public sharing of pupil videos, medical assessment, automated high-stakes decisions, and curriculum authoring are outside this feature's initial scope.
- Retention periods and deletion responsibilities follow each school's applicable policy; the app must support authorised deletion rather than imposing a universal period.
