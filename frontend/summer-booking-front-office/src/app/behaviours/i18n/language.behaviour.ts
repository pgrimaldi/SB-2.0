import { Direction, Directionality } from '@angular/cdk/bidi';
import { DOCUMENT } from '@angular/common';
import { Injectable, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { firstValueFrom, forkJoin } from 'rxjs';
import { Language, LanguageOption } from '../../entities/shared/language';

const DEFAULT_LANGUAGE = Language.It;
const STORAGE_KEY = 'sb.language';
const LANGUAGE_PREFIX = new RegExp(`^/(${Object.values(Language).join('|')})(?=[/?#]|$)`);

@Injectable({ providedIn: 'root' })
export class LanguageBehaviour {
  private readonly translateService = inject(TranslateService);
  private readonly router = inject(Router);
  private readonly document = inject(DOCUMENT);
  private readonly directionality = inject(Directionality);

  readonly languages: readonly LanguageOption[] = [
    { code: Language.It, flagSrc: '/assets/images/flag-it.svg', isRightToLeft: false },
    { code: Language.En, flagSrc: '/assets/images/flag-gb.svg', isRightToLeft: false },
    { code: Language.Fr, flagSrc: '/assets/images/flag-fr.svg', isRightToLeft: false },
    { code: Language.Es, flagSrc: '/assets/images/flag-es.svg', isRightToLeft: false },
    { code: Language.De, flagSrc: '/assets/images/flag-de.svg', isRightToLeft: false },
    { code: Language.Zh, flagSrc: '/assets/images/flag-cn.svg', isRightToLeft: false },
    { code: Language.Ar, flagSrc: '/assets/images/flag-sa.svg', isRightToLeft: true },
  ];
  private readonly currentCode = signal<Language>(DEFAULT_LANGUAGE);

  readonly current = this.currentCode.asReadonly();

  constructor() {
    // Components (e.g. appI18nText) read the available languages from ngx-translate.
    this.translateService.addLangs(this.languages.map(({ code }) => code));
  }

  isSupported(value: string | null | undefined): value is Language {
    return this.languages.some((language) => language.code === value);
  }

  preferred(): Language {
    const stored = this.readStoredLanguage();
    return this.isSupported(stored) ? stored : DEFAULT_LANGUAGE;
  }

  use(code: Language): void {
    this.translateService.use(code);
    this.document.documentElement.lang = code;
    this.useDirection(this.option(code).isRightToLeft ? 'rtl' : 'ltr');
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

  /**
   * Loads the active language and every other one. The first render waits for it: texts reserve the space
   * of their longest translation, so nothing moves when a language file arrives after the page is drawn.
   */
  loadAll(active: Language): Promise<void> {
    const loads = this.languages.map(({ code }) =>
      code === active ? this.translateService.use(code) : this.translateService.reloadLang(code),
    );
    // A missing file must not block the app: the texts fall back to the available language.
    return firstValueFrom(forkJoin(loads)).then(
      () => undefined,
      () => undefined,
    );
  }

  option(code: Language): LanguageOption {
    return this.languages.find((language) => language.code === code) ?? this.languages[0];
  }

  /**
   * `dir` on the page mirrors our layouts; Material and the CDK (dialogs, menus, calendars) read the
   * direction from `Directionality`, set at the start only: it follows a change of language here.
   */
  private useDirection(direction: Direction): void {
    this.document.documentElement.dir = direction;
    if (this.directionality.value !== direction) {
      this.directionality.valueSignal.set(direction);
      this.directionality.change.emit(direction);
    }
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
