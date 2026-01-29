mergeInto(LibraryManager.library, {

  ShowHtmlInputWithPlaceholder: function (placeholderPtr, idPtr, valuePtr) {

    // --- Mobile-only gate ---
    // Touch-capable + small-screen heuristic (works well for WebGL)
    const hasTouch =
      (navigator.maxTouchPoints && navigator.maxTouchPoints > 0) ||
      ('ontouchstart' in window) ||
      (navigator.msMaxTouchPoints && navigator.msMaxTouchPoints > 0);

    const smallScreen = Math.min(window.innerWidth, window.innerHeight) <= 1024;

    if (!(hasTouch && smallScreen)) {
      // Desktop: do nothing (Unity TMP_InputField should behave normally)
      return;
    }
    // --- end gate ---

    const placeholder = UTF8ToString(placeholderPtr);
    const inputId = UTF8ToString(idPtr);
    const defaultValue = UTF8ToString(valuePtr);

    if (typeof window.ShowHtmlInput === "function") {
      window.ShowHtmlInput(placeholder, inputId, defaultValue);
    } else {
      // Fail silently to avoid breaking builds if template JS isn't loaded for some reason
      // (Optional) console.warn("ShowHtmlInput is not defined on window.");
    }
  }

});
