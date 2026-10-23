import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const configuration = process.env.BUILD_CONFIGURATION;
const apiKey = process.env.GOOGLE_MAPS_API_KEY_DEV?.trim();

if (configuration !== 'dev' && configuration !== 'production') {
  console.error('BUILD_CONFIGURATION must be dev or production.');
  process.exit(1);
}

if (!apiKey || !/^AIza[\w-]{35}$/.test(apiKey)) {
  console.error('GOOGLE_MAPS_API_KEY_DEV is missing or invalid. Check the CodeBuild secret reference.');
  process.exit(1);
}

// Do not log these arguments: the key belongs in the browser bundle, not in build logs.
const result = spawnSync(
  process.execPath,
  [
    fileURLToPath(new URL('../node_modules/@angular/cli/bin/ng.js', import.meta.url)),
    'build',
    '--configuration',
    configuration,
    '--define',
    `GOOGLE_MAPS_API_KEY_DEV=${JSON.stringify(apiKey)}`,
  ],
  { cwd: fileURLToPath(new URL('../', import.meta.url)), stdio: 'inherit', shell: false },
);

if (result.error) {
  console.error('Unable to start the Angular build.');
}
process.exit(result.status ?? 1);
