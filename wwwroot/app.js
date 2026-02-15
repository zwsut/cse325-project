window.appLayout = window.appLayout || {};

window.appLayout.getViewportWidth = function () {
    return window.innerWidth || document.documentElement.clientWidth || 0;
};
