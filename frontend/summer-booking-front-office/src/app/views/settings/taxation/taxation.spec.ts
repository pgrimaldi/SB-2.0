import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS } from '@angular/material/core';
import { provideTranslateService } from '@ngx-translate/core';
import { of } from 'rxjs';
import { AuthBehaviour } from '../../../behaviours/auth/auth.behaviour';
import { TaxationService } from '../../../services/api/taxation/taxation.service';
import { Taxation } from './taxation';

describe('Taxation', () => {
  const PRINTERS = [
    { idPrinter: 'pr1', printerName: 'EPSON - 1', printerIP: '192.0.2.10', isEnabled: false },
    { idPrinter: 'pr2', printerName: 'EPSON - 2', printerIP: '192.0.2.11', isEnabled: true },
  ];

  const setup = async (termsAndConditions: string) => {
    const printerList = vi.fn(() => of(PRINTERS));
    const electronicReceipt = vi.fn(() => of({ termsAndConditions }));
    TestBed.configureTestingModule({
      providers: [
        provideTranslateService(),
        { provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } },
        { provide: TaxationService, useValue: { printerList, electronicReceipt } },
        { provide: AuthBehaviour, useValue: { user: signal({ idProperty: 'p1' }) } },
      ],
    });
    const fixture = TestBed.createComponent(Taxation);
    await fixture.whenStable();
    const element: HTMLElement = fixture.nativeElement;
    return { element, printerList, electronicReceipt };
  };

  it('should load the printers and the terms of the property', async () => {
    const { element, printerList, electronicReceipt } = await setup('<p>Termini</p>');

    expect(printerList).toHaveBeenCalledWith('p1');
    expect(electronicReceipt).toHaveBeenCalledWith('p1');
    expect(
      [...element.querySelectorAll('.taxation__printer__name')].map((name) => name.textContent),
    ).toEqual(['EPSON - 1', 'EPSON - 2']);
    expect(element.querySelector('.taxation__terms p')?.textContent).toBe('Termini');
  });

  it('should show the terms without scripts or event handlers', async () => {
    const { element } = await setup(
      '<p onclick="alert(1)">Termini</p><script>alert(2)</script><img src="x" onerror="alert(3)">',
    );
    const terms = element.querySelector('.taxation__terms')!;

    expect(terms.querySelector('script')).toBeNull();
    expect(terms.querySelector('[onclick], [onerror]')).toBeNull();
    expect(terms.querySelector('p')?.textContent).toBe('Termini');
  });
});
