import { DOCUMENT, NgTemplateOutlet } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  afterNextRender,
  computed,
  inject,
  signal,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable } from 'rxjs';
import { LanguageBehaviour } from '../../../behaviours/i18n/language.behaviour';
import { Language } from '../../../entities/shared/language';
import { I18nText } from '../../i18n/i18n-text/i18n-text';
import { Button } from '../../shared/ui/buttons/button/button';
import { LoginDialog } from '../login/login-dialog';
import { DropdownMenu, DropdownMenuItem } from '../../shared/ui/menus/dropdown-menu/dropdown-menu';

@Component({
  selector: 'app-header',
  imports: [Button, DropdownMenu, I18nText, LoginDialog, NgTemplateOutlet, TranslatePipe],
  templateUrl: './header.html',
  styleUrl: './header.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '(window:scroll)': 'updateHeaderState()',
    '(document:keydown.escape)': 'closeMenu()',
  },
})
export class Header {
  private readonly document = inject(DOCUMENT);
  private readonly languageBehaviour = inject(LanguageBehaviour);
  private readonly translateService = inject(TranslateService);

  protected readonly isCompact = signal(false);
  protected readonly isMenuOpen = signal(false);
  protected readonly loginOpen = signal(false);
  protected readonly navItems = ['advantages', 'features', 'booking', 'pricing'] as const;

  private readonly languageNames = toSignal(
    this.translateService.stream('language.names') as Observable<Record<string, string>>,
    { initialValue: {} as Record<string, string> },
  );

  protected readonly currentLanguage = computed(() =>
    this.languageBehaviour.option(this.languageBehaviour.current()),
  );
  protected readonly languageItems = computed<DropdownMenuItem[]>(() =>
    this.languageBehaviour.languages.map((language) => ({
      value: language.code,
      label: this.languageNames()[language.code] ?? language.code,
    })),
  );
  /** In the order of the menu items. */
  protected readonly languageFlags = this.languageBehaviour.languages.map(
    (language) => language.flagSrc,
  );

  constructor() {
    afterNextRender(() => this.updateHeaderState());
  }

  protected closeMenu(): void {
    this.isMenuOpen.set(false);
  }

  protected changeLanguage(code: string): void {
    this.languageBehaviour.switchTo(code as Language);
  }

  protected openLogin(): void {
    this.closeMenu();
    this.loginOpen.set(true);
  }

  protected toggleMenu(): void {
    this.isMenuOpen.update((isOpen) => !isOpen);
  }

  /** Called by the `host` scroll listener. */
  protected updateHeaderState(): void {
    this.isCompact.set((this.document.defaultView?.scrollY ?? 0) >= 64);
  }
}
