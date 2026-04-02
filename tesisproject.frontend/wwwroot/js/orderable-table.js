window.OrderableTable = {
    _instances: {},

    init(tableBodyId, dotNetRef) {
        const el = document.getElementById(tableBodyId);
        if (!el) return;

        if (this._instances[tableBodyId]) {
            this._instances[tableBodyId].destroy();
        }

        this._instances[tableBodyId] = Sortable.create(el, {
            handle: '[data-drag-handle]',
            animation: 150,
            ghostClass: 'sortable-ghost',
            chosenClass: 'sortable-chosen',
            dragClass: 'sortable-drag',
            onEnd: (evt) => {
                if (evt.oldIndex === evt.newIndex) return;
                dotNetRef.invokeMethodAsync('OnSortEnd', evt.oldIndex, evt.newIndex);
            }
        });
    },

    destroy(tableBodyId) {
        if (this._instances[tableBodyId]) {
            this._instances[tableBodyId].destroy();
            delete this._instances[tableBodyId];
        }
    }
};