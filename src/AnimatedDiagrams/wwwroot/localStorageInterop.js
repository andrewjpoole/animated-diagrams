// Maintain backward compatibility with earlier usage expecting window.localStorageInterop.setItem
window.localStorageInterop = {
    setItem: function (key, value) {
        localStorage.setItem(key, value);
        console.debug('[localStorageInterop] setItem', key, value);
    }
};

window.canvasInterop = {
    getRect: function (el) {
        if (!el) return { x:0, y:0, width:0, height:0 };
        const r = el.getBoundingClientRect();
        return { x: r.x, y: r.y, width: r.width, height: r.height };
    }
};

// Log to confirm script loaded
console.debug('[interop] localStorageInterop & canvasInterop loaded');