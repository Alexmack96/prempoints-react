// eslint.config.js
import js from "@eslint/js";
import globals from "globals";
import eslintConfigPrettier from "eslint-config-prettier";
import tseslint from "typescript-eslint";
import reactPlugin from "eslint-plugin-react";
import reactHooks from "eslint-plugin-react-hooks";
import jsxA11y from "eslint-plugin-jsx-a11y";
import importPlugin from "eslint-plugin-import";

export default [
  { ignores: ["dist", "build", "coverage"] },

  // Base JS rules
  js.configs.recommended,

  // TypeScript + parser
  ...tseslint.configs.recommendedTypeChecked.map((cfg) => ({
    ...cfg,
    languageOptions: {
      ...cfg.languageOptions,
      parserOptions: {
        ...cfg.languageOptions?.parserOptions,
        project: true, // uses your tsconfig.json
        tsconfigRootDir: import.meta.dirname,
      },
      globals: {
        ...globals.browser,
        ...globals.node,
      },
    },
  })),

  // React
  {
    plugins: {
      react: reactPlugin,
      "react-hooks": reactHooks,
      "jsx-a11y": jsxA11y,
      import: importPlugin,
    },
    rules: {
      ...reactPlugin.configs.recommended.rules,
      ...reactPlugin.configs["jsx-runtime"].rules, // for React 17+
      ...reactHooks.configs.recommended.rules,

      // Common quality rules
      "import/order": ["warn", { "newlines-between": "always" }],
      "import/no-unresolved": "off", // TS handles this
      "react/prop-types": "off", // using TS anyway
    },
    settings: {
      react: { version: "detect" },
    },
  },

  // 🚫 Turn off ALL formatting-related ESLint rules
  // This prevents conflicts with Prettier
  eslintConfigPrettier,
];
