window.sidebarSectionStartResize = function(title) {
    let moveHandler = function(e) {
        DotNet.invokeMethodAsync('AnimatedDiagrams', 'SidebarSectionResize', title, e.clientY);
    };
    let upHandler = function(e) {
        DotNet.invokeMethodAsync('AnimatedDiagrams', 'SidebarSectionEndResize', title);
        window.removeEventListener('mousemove', moveHandler);
        window.removeEventListener('mouseup', upHandler);
    };
    window.addEventListener('mousemove', moveHandler);
    window.addEventListener('mouseup', upHandler);
};
console.debug('[interop] sidebarSectionStartResize loaded');
