// The unit-test builder runs the spec files without isolation (`isolate: false`): files in the same
// worker share `window`, timers and spies. Whatever a test replaces is put back after it, so it can
// never break a later test of another file (an error that would come and go with the file order).
afterEach(() => {
  vi.restoreAllMocks();
  vi.unstubAllGlobals();
  vi.useRealTimers();
});
