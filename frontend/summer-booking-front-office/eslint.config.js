// @ts-check
const { defineConfig } = require('eslint/config');
const angular = require('angular-eslint');
const tseslint = require('typescript-eslint');

/**
 * Lint of the TypeScript code, with the rules agreed with the team:
 * - a class that uses an Angular lifecycle hook declares it (`ngOnDestroy()` → `implements OnDestroy`);
 * - members in this order: fields, then the constructor, then the methods (arrow-function
 *   properties count as methods). The finer order (API, injections, state, hooks, listeners,
 *   ngOnDestroy last) is in FRONTEND_WORKING_RULES.md.
 */
module.exports = defineConfig([
  {
    files: ['src/**/*.ts'],
    languageOptions: { parser: tseslint.parser },
    plugins: { '@angular-eslint': angular.tsPlugin, '@typescript-eslint': tseslint.plugin },
    rules: {
      '@angular-eslint/use-lifecycle-interface': 'error',
      '@typescript-eslint/member-ordering': [
        'error',
        { default: ['field', 'constructor', 'method'] },
      ],
    },
  },
]);
