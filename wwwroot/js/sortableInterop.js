window.sortableInterop = {
    lists: {},

    init: function (elementId, dotNetRef) {
        const el = document.getElementById(elementId);
        if (!el) {
            return;
        }

        // 既に同じ場所に設定済みなら、一度破棄してから作り直す
        if (window.sortableInterop.lists[elementId]) {
            window.sortableInterop.lists[elementId].destroy();
        }

        window.sortableInterop.lists[elementId] = new Sortable(el, {
            handle: '.drag-handle',
            animation: 150,
            filter: '.row-deleted',
            onEnd: function () {
                const ids = Array.from(el.children).map(function (child) {
                    return child.getAttribute('data-id');
                });
                dotNetRef.invokeMethodAsync('OnReordered', ids);
            }
        });
    },

    destroy: function (elementId) {
        if (window.sortableInterop.lists[elementId]) {
            window.sortableInterop.lists[elementId].destroy();
            delete window.sortableInterop.lists[elementId];
        }
    }
};