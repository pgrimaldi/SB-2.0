import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';
import {
  Message,
  MessagePartKind,
  isMessage,
  parseMessage,
} from '../../components/shared/i18n/message-format';
import { API_ERROR_CODES } from '../../entities/errors/api-error-codes';
import { FRONTEND_ERROR_CODES } from '../../entities/errors/api-problem';
import { LanguageBehaviour } from './language.behaviour';

/** Translation file of a language, flattened: `{ 'home.hero.title': 'text', ... }`. */
type Texts = Readonly<Record<string, unknown>>;

/** Translations of every language the app offers (a new language is checked as soon as it is added). */
async function loadTranslations(): Promise<ReadonlyMap<string, Texts>> {
  TestBed.configureTestingModule({ providers: [provideRouter([]), provideTranslateService()] });
  const languages = TestBed.inject(LanguageBehaviour).languages.map(({ code }) => code);
  const files = await Promise.all(
    languages.map(
      (code) => import(`../../../assets/i18n/${code}.json`) as Promise<{ default: object }>,
    ),
  );
  return new Map(languages.map((code, index) => [code, flatten(files[index].default)]));
}

function flatten(node: object, prefix = ''): Texts {
  return Object.entries(node).reduce<Record<string, unknown>>((texts, [key, value]) => {
    const path = `${prefix}${key}`;
    return value !== null && typeof value === 'object'
      ? { ...texts, ...flatten(value, `${path}.`) }
      : { ...texts, [path]: value };
  }, {});
}

/** Arguments of a message with their type (`count:Plural`, `day:Date`…), sorted and without repeats. */
function messageArguments(message: Message): string[] {
  const found = message.flatMap((part): string[] =>
    typeof part === 'string' || part.kind === MessagePartKind.Count
      ? []
      : part.kind === MessagePartKind.Plural
        ? [`${part.name}:Plural`, ...Object.values(part.branches).flatMap(messageArguments)]
        : [`${part.name}:${MessagePartKind[part.kind]}`],
  );
  return [...new Set(found)].sort();
}

/** Names of the `{{placeholders}}` of a text, sorted. */
function placeholders(text: string): string[] {
  return [...text.matchAll(/\{\{\s*([^}\s]+)\s*\}\}/g)].map(([, name]) => name).sort();
}

describe('translation files', () => {
  let translations: ReadonlyMap<string, Texts>;

  beforeAll(async () => {
    translations = await loadTranslations();
  });

  it('should have one file for every language of the app', () => {
    expect(translations.size).toBeGreaterThan(0);
  });

  it('should have the same keys in every language, all with a text', () => {
    const allKeys = new Set([...translations.values()].flatMap((texts) => Object.keys(texts)));
    const problems = [...translations].map(([language, texts]) => ({
      language,
      missing: [...allKeys].filter((key) => !(key in texts)),
      empty: Object.keys(texts).filter(
        (key) => typeof texts[key] !== 'string' || !(texts[key] as string).trim(),
      ),
    }));

    expect(problems).toEqual(
      [...translations.keys()].map((language) => ({ language, missing: [], empty: [] })),
    );
  });

  it('should have the same placeholders in every language', () => {
    const [[, referenceTexts], ...others] = [...translations];
    // Compared with the first language: a different placeholder would lose its value.
    const problems = others.map(([language, texts]) => ({
      language,
      different: Object.keys(referenceTexts).filter(
        (key) =>
          typeof texts[key] === 'string' &&
          placeholders(String(referenceTexts[key])).join() !==
            placeholders(texts[key] as string).join(),
      ),
    }));

    expect(problems).toEqual(others.map(([language]) => ({ language, different: [] })));
  });

  it('should write plurals, numbers and dates as valid messages, with the same arguments in every language', () => {
    const [[, referenceTexts]] = [...translations];
    const keys = Object.keys(referenceTexts).filter((key) =>
      [...translations.values()].some((texts) => isMessage(String(texts[key]))),
    );
    const problems = keys.flatMap((key) => {
      const signatures = [...translations].map(([language, texts]) => {
        try {
          return messageArguments(parseMessage(String(texts[key]))).join();
        } catch (error) {
          return `${language}: ${(error as Error).message}`;
        }
      });
      return new Set(signatures).size === 1 && isMessage(String(referenceTexts[key]))
        ? []
        : [{ key, signatures }];
    });

    expect(problems).toEqual([]);
  });

  it('should translate every error code, and nothing else, under error.', () => {
    const codes = new Set<string>([
      'unknown',
      ...API_ERROR_CODES,
      ...Object.values(FRONTEND_ERROR_CODES),
    ]);
    const problems = [...translations].map(([language, texts]) => {
      const translated = Object.keys(texts)
        .filter((key) => key.startsWith('error.'))
        .map((key) => key.slice('error.'.length));
      return {
        language,
        missing: [...codes].filter((code) => !translated.includes(code)),
        unknown: translated.filter((code) => !codes.has(code)),
      };
    });

    expect(problems).toEqual(
      [...translations.keys()].map((language) => ({ language, missing: [], unknown: [] })),
    );
  });
});
