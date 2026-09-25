/// <reference types="vite/client" />

declare module "virtual:pe-syllabus" {
  const syllabus: import("./features/chat/types").SyllabusSource;
  export default syllabus;
}
