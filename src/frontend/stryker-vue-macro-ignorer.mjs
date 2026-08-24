import { declareValuePlugin, PluginKind } from "@stryker-mutator/api/plugin";

const vueCompilerMacros = new Set([
  "defineEmits",
  "defineExpose",
  "defineModel",
  "defineOptions",
  "defineProps",
  "defineSlots",
  "withDefaults",
]);

const vueCompilerMacroIgnorer = {
  shouldIgnore(path) {
    if (
      path.isCallExpression() &&
      path.node.callee.type === "Identifier" &&
      vueCompilerMacros.has(path.node.callee.name)
    ) {
      return "Vue compiler-macro arguments must remain statically analyzable";
    }

    return undefined;
  },
};

export const strykerPlugins = [
  declareValuePlugin(
    PluginKind.Ignore,
    "vue-compiler-macros",
    vueCompilerMacroIgnorer,
  ),
];
