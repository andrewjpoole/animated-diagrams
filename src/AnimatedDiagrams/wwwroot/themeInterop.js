// themeInterop: reliable system theme reporting with retry
window.themeInterop = (function(){
  let initialized = false;
  let retries = 0;
  const maxRetries = 10;
  const retryDelay = 250; // ms
  function report(){
    const dark = !!(window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches);
    if (window.DotNet && window.DotNet.invokeMethodAsync){
      try { window.DotNet.invokeMethodAsync('AnimatedDiagrams','UpdateSystemTheme', dark); return true; } catch(e) { /* ignore */ }
    }
    return false;
  }
  function ensureReported(){
    if (report()) return; // success
    if (retries < maxRetries){
      retries++;
      setTimeout(ensureReported, retryDelay);
    }
  }
  function init(){
    if (initialized) return;
    initialized = true;
    if (window.matchMedia){
      const mq = window.matchMedia('(prefers-color-scheme: dark)');
      const handler = ()=>report();
      if (mq.addEventListener) mq.addEventListener('change', handler); else if (mq.addListener) mq.addListener(handler);
    }
    // defer initial to let Blazor wire DotNet object
    setTimeout(ensureReported, 0);
  }
  // auto-init if script loaded after Blazor
  setTimeout(init, 0);
  return { init };
})();
