import { DOCUMENT } from '@angular/common';
import { Injectable, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';

export type Language = 'it' | 'en';

export interface LanguageOption {
  code: Language;
  flagSrc: string;
}

const DEFAULT_LANGUAGE: Language = 'it';
const STORAGE_KEY = 'sb.language';
const LANGUAGE_PREFIX = /^\/(it|en)(?=[/?#]|$)/;

@Injectable({ providedIn: 'root' })
export class LanguageService {
  readonly languages: readonly LanguageOption[] = [
    { code: 'it', flagSrc: '/assets/images/flag-it.svg' },
    { code: 'en', flagSrc: '/assets/images/flag-gb.svg' },
  ];

  private readonly translateService = inject(TranslateService);
  private readonly router = inject(Router);
  private readonly document = inject(DOCUMENT);
  private readonly currentCode = signal<Language>(DEFAULT_LANGUAGE);

  readonly current = this.currentCode.asReadonly();

  isSupported(value: string | null | undefined): value is Language {
    return this.languages.some((language) => language.code === value);
  }

  /** Language saved by the user, or Italian when nothing valid is stored. */
  preferred(): Language {
    const stored = this.readStoredLanguage();
    return this.isSupported(stored) ? stored : DEFAULT_LANGUAGE;
  }

  use(code: Language): void {
    this.translateService.use(code);
    this.document.documentElement.lang = code;
    this.currentCode.set(code);
  }

  /** Explicit user choice: applies it, remembers it and moves localized URLs to the new language. */
  switchTo(code: Language): void {
    this.use(code);
    this.storeLanguage(code);

    if (LANGUAGE_PREFIX.test(this.router.url)) {
      void this.router.navigateByUrl(this.router.url.replace(LANGUAGE_PREFIX, `/${code}`));
    }
  }

  /** Loads every other language up front, so texts can reserve the space of their longest translation. */
  preloadOtherLanguages(active: Language): void {
    for (const { code } of this.languages) {
      if (code !== active) {
        this.translateService.reloadLang(code).subscribe({ error: () => undefined });
      }
    }
  }

  option(code: Language): LanguageOption {
    return this.languages.find((language) => language.code === code) ?? this.languages[0];
  }

  private readStoredLanguage(): string | null {
    try {
      return this.document.defaultView?.localStorage.getItem(STORAGE_KEY) ?? null;
    } catch {
      return null;
    }
  }

  private storeLanguage(code: Language): void {
    try {
      this.document.defaultView?.localStorage.setItem(STORAGE_KEY, code);
    } catch {
      // Storage may be unavailable (private mode, blocked site data): the URL still carries the language.
    }
  }
}
