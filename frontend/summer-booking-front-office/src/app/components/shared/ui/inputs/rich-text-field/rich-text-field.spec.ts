import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { RichTextField } from './rich-text-field';

@Component({
  imports: [RichTextField],
  template: `<app-rich-text-field [(value)]="value" />`,
})
class RichTextFieldHost {
  readonly value = signal<string | null>(null);
}

describe('RichTextField', () => {
  it('should not let an undo take away a value read from outside, nor bring back an older one', async () => {
    const fixture = TestBed.createComponent(RichTextFieldHost);
    await fixture.whenStable();
    const field = fixture.debugElement.query(By.directive(RichTextField))
      .componentInstance as RichTextField;

    fixture.componentInstance.value.set('<p>Primo</p>');
    await fixture.whenStable();
    field['editor']!.commands.undo();
    await fixture.whenStable();
    expect(fixture.componentInstance.value()).toBe('<p>Primo</p>');

    field['editor']!.commands.insertContent('!');
    await fixture.whenStable();
    expect(fixture.componentInstance.value()).not.toBe('<p>Primo</p>');
    fixture.componentInstance.value.set('<p>Secondo</p>');
    await fixture.whenStable();
    field['editor']!.commands.undo();
    await fixture.whenStable();
    expect(fixture.componentInstance.value()).toBe('<p>Secondo</p>');
  });
});
