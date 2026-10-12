import { TestBed } from '@angular/core/testing';
import { TranslateService, provideTranslateService } from '@ngx-translate/core';
import { standardRowActions } from './standard-row-actions';

interface Item {
  name: string;
}

describe('standardRowActions', () => {
  const setup = () => {
    TestBed.configureTestingModule({ providers: [provideTranslateService()] });
    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('it', {
      table: { actions: { delete: 'Elimina', duplicate: 'Duplica', edit: 'Modifica' } },
    });
    translate.setTranslation('en', {
      table: { actions: { delete: 'Delete', duplicate: 'Duplicate', edit: 'Edit' } },
    });
    translate.use('it');
    return translate;
  };

  it('should give only the buttons with a function, each calling it with the row', () => {
    setup();
    const edited: string[] = [];
    const actions = TestBed.runInInjectionContext(() =>
      standardRowActions<Item>({ edit: (item) => edited.push(item.name) }),
    );

    expect(actions().map(({ label }) => label)).toEqual(['Modifica']);
    actions()[0].action({ name: 'Ombrellone' });
    expect(edited).toEqual(['Ombrellone']);
  });
});
