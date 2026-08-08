// eslint.config.ts
import globals from 'globals'
import reactHooks from 'eslint-plugin-react-hooks'
import reactRefresh from 'eslint-plugin-react-refresh'
import tseslint from 'typescript-eslint'

export default tseslint.config(
  { ignores: ['dist'] },

  // Application source. tsconfig.json has `include: ["src"]`, so these are the only
  // files type information is available for — the type-checked rule sets belong here.
  {
    files: ['src/**/*.{ts,tsx}'],
    extends: [
      ...tseslint.configs.recommendedTypeChecked,
      ...tseslint.configs.stylisticTypeChecked,
    ],
    languageOptions: {
      parser: tseslint.parser,
      parserOptions: {
        ecmaFeatures: { jsx: true },
        sourceType: 'module',
        project: ['./tsconfig.json'],
      },
      globals: globals.browser,
    },
    plugins: {
      '@typescript-eslint': tseslint.plugin,
      'react-hooks': reactHooks,
      'react-refresh': reactRefresh,
    },
    rules: {
      ...reactHooks.configs['recommended-latest'].rules,
      ...reactRefresh.configs.vite.rules,

      'no-unused-vars': 'off',
      '@typescript-eslint/no-unused-vars': ['error', { varsIgnorePattern: '^[A-Z_]' }],
      'no-console': ['error', { allow: ['warn', 'error'] }],
    },
  },

  // Build tooling at the repo root. These run in Node and are outside tsconfig.json's
  // `include`, so type-aware linting is impossible for them.
  {
    files: ['*.{ts,mts,cts,js,mjs,cjs}'],
    extends: [tseslint.configs.disableTypeChecked],
    languageOptions: {
      globals: globals.node,
    },
  },
)
