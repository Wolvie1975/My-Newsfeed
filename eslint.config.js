const js = require('@eslint/js');
const globals = require('globals');

module.exports = [
    {
        ignores: [
            '**/bin/**',
            '**/obj/**',
            'src/myNewspaper.Shared/wwwroot/lib/**',
            'new development/**',
            'node_modules/**',
        ],
    },
    js.configs.recommended,
    {
        rules: {
            'no-var': 'error',
            'prefer-const': 'error',
            'prefer-arrow-callback': 'error',
            eqeqeq: ['error', 'smart'],
            'no-unused-vars': ['error', { caughtErrors: 'none' }],
            // Storage and optional browser APIs are wrapped in try/catch with an intentional no-op fallback.
            'no-empty': ['error', { allowEmptyCatch: true }],
        },
    },
    {
        files: ['src/*/wwwroot/**/*.js', 'src/myNewspaper.Shared/wwwroot/*.js'],
        languageOptions: {
            sourceType: 'script',
            globals: { ...globals.browser, Blazor: 'readonly', myNewspaperSolar: 'readonly' },
        },
    },
    {
        files: ['src/*/wwwroot/service-worker.js'],
        languageOptions: { globals: { ...globals.serviceworker } },
    },
    {
        files: ['src/*/Components/**/*.razor.js'],
        languageOptions: { sourceType: 'module', globals: { ...globals.browser, Blazor: 'readonly' } },
    },
    {
        // Browser checks run Node, plus page.evaluate callbacks that execute in the page.
        files: ['tests/browser/**/*.cjs', 'tools/**/*.cjs', 'eslint.config.js'],
        languageOptions: { sourceType: 'commonjs', globals: { ...globals.node, ...globals.browser } },
    },
    {
        // solar-times.js is shared with the Node unit test through a CommonJS guard.
        files: ['src/myNewspaper.Shared/wwwroot/solar-times.js'],
        languageOptions: { globals: { ...globals.browser, module: 'writable' } },
    },
];
