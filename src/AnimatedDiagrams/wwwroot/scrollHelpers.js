// scrollHelpers.js
// Scrolls the path list so the item with the given id is at the top of the scroll container
window.scrollPathListItemToTop = function (itemId) {
    // Find the element by id
    var el = document.getElementById(itemId);
    if (!el) return;
    // Find the nearest scrollable parent (the path-list-scroll div)
    var parent = el.closest('.path-list-scroll');
    if (!parent) return;
    // Scroll so the element is at the top
    parent.scrollTop = el.offsetTop - parent.offsetTop;
};
