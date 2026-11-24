import { readFileSync, readdirSync } from 'node:fs';
import { join, relative } from 'node:path';
import { fileURLToPath } from 'node:url';

/**
 * Every translation key written in full in the app (templates, components, validation kinds) must
 * exist in it.json; the translation test already keeps the 7 languages equal. Keys composed at run
 * time (e.g. `key + '.one.title'`) are not written in full, so they are not checked here.
 */
const root = fileURLToPath(new URL('..', import.meta.url));
const appFolder = join(root, 'src', 'app');
const texts = JSON.parse(readFileSync(join(root, 'src', 'assets', 'i18n', 'it.json'), 'utf8'));

const knownKeys = new Set();
const collect = (node, path) => {
  knownKeys.add(path);
  if (node && typeof node === 'object') {
    Object.entries(node).forEach(([name, child]) =>
      collect(child, path ? `${path}.${name}` : name),
    );
  }
};
collect(texts, '');

const groups = Object.keys(texts).join('|');
const keyLiteral = new RegExp(`(['"])((?:${groups})(?:\\.[a-z0-9_]+)+)\\1`, 'g');
const validationKind = /\bkind:\s*'([a-z0-9_.]+)'/g;

const sourceFiles = (folder) =>
  readdirSync(folder, { withFileTypes: true }).flatMap((entry) => {
    const path = join(folder, entry.name);
    if (entry.isDirectory()) {
      return sourceFiles(path);
    }
    const isSource = /\.(ts|html)$/.test(entry.name) && !entry.name.endsWith('.spec.ts');
    return isSource ? [path] : [];
  });

const missing = [];
for (const file of sourceFiles(appFolder)) {
  const lines = readFileSync(file, 'utf8').split(/\r?\n/);
  lines.forEach((line, index) => {
    const where = `${relative(root, file).replaceAll('\\', '/')}:${index + 1}`;
    for (const [, , key] of line.matchAll(keyLiteral)) {
      if (!knownKeys.has(key)) {
        missing.push(`${where}  ${key}`);
      }
    }
    for (const [, kind] of line.matchAll(validationKind)) {
      if (!knownKeys.has(`invalidate.${kind}`)) {
        missing.push(`${where}  invalidate.${kind}`);
      }
    }
  });
}

if (missing.length) {
  console.error(`Translation keys not in src/assets/i18n/it.json (${missing.length}):`);
  missing.forEach((entry) => console.error(`  ${entry}`));
  process.exit(1);
}
console.log('Translation keys: all found in it.json.');
