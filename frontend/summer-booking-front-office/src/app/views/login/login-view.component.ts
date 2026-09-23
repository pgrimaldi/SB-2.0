import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ButtonComponent } from '../../components/shared/ui/buttons/button/button.component';

type SupportedLanguage = 'it' | 'en';

@Component({
  selector: 'app-login-view',
  imports: [ButtonComponent, TranslatePipe],
  templateUrl: './login-view.component.html',
  styleUrl: './login-view.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginViewComponent {
  protected readonly currentLanguage = signal<SupportedLanguage>('it');

  constructor(private readonly translateService: TranslateService) {}

  protected changeLanguage(language: SupportedLanguage): void {
    this.currentLanguage.set(language);
    this.translateService.use(language);
    document.documentElement.lang = language;
  }
}
