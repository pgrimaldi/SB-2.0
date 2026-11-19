// The unit-test builder runs the spec files without isolation (`isolate: false`): files in the same
// worker share `window` (storage and the language of `<html>` too), timers and spies. Whatever a test replaces is put back after it, so it can
// never break a later test of another file (an error that would come and go with the file order).
const { lang, dir } = document.documentElement;

// jsdom has `<dialog>` without its methods (e.g. `app-page-spinner` opens a modal one): only `open` changes.
if (!Reflect.has(HTMLDialogElement.prototype, 'showModal')) {
  Object.assign(HTMLDialogElement.prototype, {
    showModal(this: HTMLDialogElement) {
      this.open = true;
    },
    close(this: HTMLDialogElement) {
      this.open = false;
    },
  });
}

afterEach(() => {
  vi.restoreAllMocks();
  vi.unstubAllGlobals();
  vi.useRealTimers();
  localStorage.clear();
  sessionStorage.clear();
  document.documentElement.lang = lang;
  document.documentElement.dir = dir;
});
