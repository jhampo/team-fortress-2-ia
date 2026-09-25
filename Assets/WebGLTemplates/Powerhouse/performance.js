// Diagnostic switch: ?perf=1 or F8. Measures browser frame delivery; Unity adds its own samples.
(() => {
  const panel = document.createElement('output');
  panel.id = 'web-performance';
  panel.style.cssText = 'position:fixed;left:12px;top:88px;z-index:8;background:#15191ddd;color:#f3e5c4;padding:7px 10px;font:12px monospace;white-space:pre;pointer-events:none';
  panel.hidden = !new URLSearchParams(location.search).has('perf');
  document.body.appendChild(panel);
  let previous = 0, elapsed = 0, samples = [], unity = '', browser = '';
  window.powerhousePerformance = message => { unity = message; panel.textContent = browser + '\n' + unity; };
  document.addEventListener('keydown', e => { if (e.code === 'F8') { e.preventDefault(); panel.hidden = !panel.hidden; } });
  function frame(now) {
    if (previous && !document.hidden) {
      const dt = now - previous; samples.push(dt); elapsed += dt;
      if (elapsed >= 2500) {
        const count = samples.length; samples.sort((a,b) => a-b);
        browser = 'Web: ' + (1000*count/elapsed).toFixed(1) + ' FPS | p95 ' + samples[Math.floor((count-1)*.95)].toFixed(1) + ' ms';
        panel.textContent = browser + (unity ? '\n'+unity : '');
        samples = []; elapsed = 0;
      }
    } else { samples = []; elapsed = 0; }
    previous = now; requestAnimationFrame(frame);
  }
  requestAnimationFrame(frame);
})();
