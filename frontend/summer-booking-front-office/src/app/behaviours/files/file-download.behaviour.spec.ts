import { HttpHeaders, HttpResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { FileDownloadBehaviour, fileNameOf } from './file-download.behaviour';

describe('fileNameOf', () => {
  it('should read the name, preferring the UTF-8 one', () => {
    expect(fileNameOf('attachment; filename="guida.pdf"')).toBe('guida.pdf');
    expect(
      fileNameOf(`attachment; filename="guida.pdf"; filename*=UTF-8''guida%20%C3%A8.pdf`),
    ).toBe('guida è.pdf');
    expect(fileNameOf(null)).toBeNull();
    expect(fileNameOf('attachment')).toBeNull();
  });

  it('should never let the server choose a folder or unsafe characters', () => {
    expect(fileNameOf('attachment; filename="../../etc/passwd"')).toBe('passwd');
    expect(fileNameOf(`attachment; filename*=UTF-8''..%5C..%5Cwin.ini`)).toBe('win.ini');
    expect(fileNameOf('attachment; filename="gu<i>da:?.pdf"')).toBe('guida.pdf');
    expect(fileNameOf('attachment; filename=".."')).toBeNull();
  });
});

describe('FileDownloadBehaviour', () => {
  it('should save the body with the name of the server, or the fallback one', () => {
    vi.useFakeTimers();
    const createObjectURL = vi.spyOn(URL, 'createObjectURL').mockReturnValue('blob:guide');
    const revokeObjectURL = vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => {});
    const names: string[] = [];
    vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(function (
      this: HTMLAnchorElement,
    ) {
      names.push(this.download);
    });
    const behaviour = TestBed.inject(FileDownloadBehaviour);
    const body = new Blob(['guida']);

    behaviour.save(
      new HttpResponse({
        body,
        headers: new HttpHeaders({ 'Content-Disposition': 'attachment; filename="guida.pdf"' }),
      }),
      'riserva',
    );
    behaviour.save(new HttpResponse({ body }), 'riserva');
    vi.runAllTimers();

    expect(createObjectURL).toHaveBeenCalledWith(body);
    expect(names).toEqual(['guida.pdf', 'riserva']);
    expect(revokeObjectURL).toHaveBeenCalledWith('blob:guide');
  });
});
