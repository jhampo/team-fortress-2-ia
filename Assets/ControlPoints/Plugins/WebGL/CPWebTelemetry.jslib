mergeInto(LibraryManager.library, {
  CPWebReport: function(message) {
    if (window.powerhousePerformance) window.powerhousePerformance(UTF8ToString(message));
  }
});
