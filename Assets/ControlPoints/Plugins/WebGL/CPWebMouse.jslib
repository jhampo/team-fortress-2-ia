mergeInto(LibraryManager.library, {
  CPWebMouseRead: function(x, y, accept) {
    if (!Module.powerhouseMouse) {
      var state = Module.powerhouseMouse = { x: 0, y: 0, accept: false };
      var clear = function() { state.x = 0; state.y = 0; state.accept = false; };
      document.addEventListener('mousemove', function(event) {
        if (!state.accept || document.pointerLockElement !== Module.canvas || document.hidden) return;
        if (Number.isFinite(event.movementX)) state.x += event.movementX;
        if (Number.isFinite(event.movementY)) state.y += event.movementY;
      }, true);
      document.addEventListener('pointerlockchange', clear);
      document.addEventListener('visibilitychange', clear);
      window.addEventListener('blur', clear);
    }
    var mouse = Module.powerhouseMouse;
    var active = !!accept && document.pointerLockElement === Module.canvas && !document.hidden;
    HEAPF32[x >> 2] = active && mouse.accept ? mouse.x : 0;
    HEAPF32[y >> 2] = active && mouse.accept ? mouse.y : 0;
    mouse.x = 0; mouse.y = 0; mouse.accept = active;
  }
});
