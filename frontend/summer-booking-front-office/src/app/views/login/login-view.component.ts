import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonComponent } from '../../components/shared/ui/buttons/button/button.component';
import { Language, LanguageService } from '../../services/i18n/language.service';

@Component({
  selector: 'app-login-view',
  imports: [ButtonComponent, TranslatePipe],
  templateUrl: './login-view.component.html',
  styleUrl: './login-view.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginViewComponent {
  private readonly languageService = inject(LanguageService);

  protected readonly currentLanguage = this.languageService.current;

  protected changeLanguage(language: Language): void {
    this.languageService.switchTo(language);
  }
}
