(function(){
  if(!window.exportInterop){
    window.exportInterop = {
      set: function(svg){
        if(typeof svg !== 'string'){ return false; }
        window.__lastExportedSvg = svg;
        try{ localStorage.setItem('lastExportedSvg', svg); }catch(e){}
        return true;
      },
      get: function(){
        return window.__lastExportedSvg || localStorage.getItem('lastExportedSvg') || '';
      },
      ensure: function(svg){
        if(!window.exportInterop.get()){
          window.exportInterop.set(svg);
        }
        return window.exportInterop.get();
      },
      download: function(svg, filename){
        try {
          if(!svg) return false;
          var blob = new Blob([svg], { type: 'image/svg+xml' });
          var url = URL.createObjectURL(blob);
          var a = document.createElement('a');
          a.href = url;
          a.download = filename || 'diagram.svg';
          document.body.appendChild(a);
          a.click();
          setTimeout(function(){
            document.body.removeChild(a);
            URL.revokeObjectURL(url);
          }, 100);
          return true;
        } catch (e) {
          console.error('[exportInterop.download] failed', e);
          return false;
        }
      },
      setAndDownload: function(svg, filename){
        var ok = window.exportInterop.set(svg);
        window.exportInterop.download(svg, filename);
        return ok;
      },
      open: function(svg){
        try {
            if(!svg) return false;
            var w = window.open('about:blank','_blank');
            if(!w) return false;
            var safe = svg.replace(/</g,'&lt;').replace(/>/g,'&gt;');
            w.document.write('<pre style="white-space:pre-wrap;font:12px monospace;">'+safe+'</pre>');
            return true;
        } catch(e){ console.error('[exportInterop.open] failed', e); return false; }
      },
      openRendered: function(svg){
        try {
            if(!svg) return false;
            var url = 'data:image/svg+xml;utf8,' + encodeURIComponent(svg);
            window.open(url,'_blank');
            return true;
        } catch(e){ console.error('[exportInterop.openRendered] failed', e); return false; }
      },
      copy: async function(svg){
        try {
            if(!svg || !navigator.clipboard) return false;
            await navigator.clipboard.writeText(svg);
            return true;
        } catch(e){ console.error('[exportInterop.copy] failed', e); return false; }
      }
    };
  }
})();

// ES module exports for Blazor dynamic import
export function set(svg) { return window.exportInterop.set(svg); }
export function get() { return window.exportInterop.get(); }
export function download(svg, filename) { return window.exportInterop.download(svg, filename); }
export function setAndDownload(svg, filename) { return window.exportInterop.setAndDownload(svg, filename); }
export function openRendered(svg) { return window.exportInterop.openRendered(svg); }
export async function copy(svg) { return window.exportInterop.copy(svg); }
// Provides robust export interop functions used by Blazor tests
(function(){
  if(!window.exportInterop){
    window.exportInterop = {
      set: function(svg){
        if(typeof svg !== 'string'){ return false; }
        window.__lastExportedSvg = svg;
        try{ localStorage.setItem('lastExportedSvg', svg); }catch(e){}
        return true;
      },
      get: function(){
        return window.__lastExportedSvg || localStorage.getItem('lastExportedSvg') || '';
      },
      ensure: function(svg){
        if(!window.exportInterop.get()){
          window.exportInterop.set(svg);
        }
        return window.exportInterop.get();
      },
      download: function(svg, filename){
        try {
          if(!svg) return false;
          var blob = new Blob([svg], { type: 'image/svg+xml' });
          var url = URL.createObjectURL(blob);
          var a = document.createElement('a');
          a.href = url;
          a.download = filename || 'diagram.svg';
          document.body.appendChild(a);
          a.click();
          setTimeout(function(){
            document.body.removeChild(a);
            URL.revokeObjectURL(url);
          }, 100);
          return true;
        } catch (e) {
          console.error('[exportInterop.download] failed', e);
          return false;
        }
      },
      setAndDownload: function(svg, filename){
        var ok = window.exportInterop.set(svg);
        window.exportInterop.download(svg, filename);
        return ok;
      },
      open: function(svg){
        try {
            if(!svg) return false;
            var w = window.open('about:blank','_blank');
            if(!w) return false;
            // Basic escaping; avoid script execution by not injecting untrusted content directly
            var safe = svg.replace(/</g,'&lt;').replace(/>/g,'&gt;');
            w.document.write('<pre style="white-space:pre-wrap;font:12px monospace;">'+safe+'</pre>');
            return true;
        } catch(e){ console.error('[exportInterop.open] failed', e); return false; }
      },
      openRendered: function(svg){
        try {
            if(!svg) return false;
            var url = 'data:image/svg+xml;utf8,' + encodeURIComponent(svg);
            window.open(url,'_blank');
            return true;
        } catch(e){ console.error('[exportInterop.openRendered] failed', e); return false; }
      },
      copy: async function(svg){
        try {
            if(!svg || !navigator.clipboard) return false;
            await navigator.clipboard.writeText(svg);
            return true;
        } catch(e){ console.error('[exportInterop.copy] failed', e); return false; }
      }
    };
  }
})();
