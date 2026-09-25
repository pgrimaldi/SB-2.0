import { DOCUMENT } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  HostListener,
  afterNextRender,
  computed,
  inject,
  signal,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable } from 'rxjs';
import { Language, LanguageService } from '../../../services/i18n/language.service';
import { I18nText } from '../../shared/i18n/i18n-text/i18n-text';
import { ButtonComponent } from '../../shared/ui/buttons/button/button.component';
import { LoginDialogService } from '../login/login-dialog.service';
import { DropdownMenu, DropdownMenuItem } from '../../shared/ui/menus/dropdown-menu/dropdown-menu';

@Component({
  selector: 'app-header',
  imports: [ButtonComponent, DropdownMenu, I18nText, TranslatePipe],
  templateUrl: './header.html',
  styleUrl: './header.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Header {
  protected readonly isCompact = signal(false);
  protected readonly isMenuOpen = signal(false);
  protected readonly navItems = ['advantages', 'features', 'booking', 'pricing'] as const;

  private readonly document = inject(DOCUMENT);
  private readonly languageService = inject(LanguageService);
  private readonly loginDialog = inject(LoginDialogService);
  private readonly languageNames = toSignal(
    inject(TranslateService).stream('language.names') as Observable<Record<string, string>>,
    { initialValue: {} as Record<string, string> },
  );

  protected readonly currentLanguage = computed(() =>
    this.languageService.option(this.languageService.current()),
  );
  protected readonly languageItems = computed<DropdownMenuItem[]>(() =>
    this.languageService.languages.map((language) => ({
      value: language.code,
      label: this.languageNames()[language.code] ?? language.code,
      iconSrc: language.flagSrc,
    })),
  );

  constructor() {
    afterNextRender(() => this.updateHeaderState());
  }

  @HostListener('window:scroll')
  protected updateHeaderState(): void {
    this.isCompact.set((this.document.defaultView?.scrollY ?? 0) >= 64);
  }

  @HostListener('document:keydown.escape')
  protected closeMenu(): void {
    this.isMenuOpen.set(false);
  }

  protected changeLanguage(code: string): void {
    this.languageService.switchTo(code as Language);
  }

  protected openLogin(): void {
    this.closeMenu();
    this.loginDialog.open();
  }

  protected toggleMenu(): void {
    this.isMenuOpen.update((isOpen) => !isOpen);
  }
}
