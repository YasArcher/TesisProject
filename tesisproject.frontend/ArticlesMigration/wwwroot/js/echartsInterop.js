// wwwroot/js/echartsInterop.js

window.echartsInterop = (function () {
    const resizeHandlers = new Map();

    function ensureInstance(id) {
        if (!window.echarts) {
            console.error("ECharts no está cargado. Revisa el <script> del CDN en index.html.");
            return null;
        }

        const el = document.getElementById(id);
        if (!el) {
            console.warn("Elemento no encontrado para EChart con id:", id);
            return null;
        }

        let chart = echarts.getInstanceByDom(el);
        if (!chart) {
            chart = echarts.init(el);
        }

        return chart;
    }

    function init(id, option) {
        const chart = ensureInstance(id);
        if (!chart) return false;

        let config = option;

        // Por si alguna vez llega como string JSON
        if (typeof option === "string") {
            try {
                config = JSON.parse(option);
            } catch (e) {
                console.error("No se pudo parsear option de EChart:", e);
                return false;
            }
        }

        chart.setOption(config, true);
        chart.resize();

        const previousHandler = resizeHandlers.get(id);
        if (previousHandler) {
            window.removeEventListener("resize", previousHandler);
        }

        const resizeHandler = () => {
            try {
                chart.resize();
            } catch {
                // ignorar
            }
        };

        resizeHandlers.set(id, resizeHandler);
        window.addEventListener("resize", resizeHandler);

        return true;
    }

    function dispose(id) {
        const el = document.getElementById(id);
        const resizeHandler = resizeHandlers.get(id);
        if (resizeHandler) {
            window.removeEventListener("resize", resizeHandler);
            resizeHandlers.delete(id);
        }

        if (!el || !window.echarts) return;

        const chart = echarts.getInstanceByDom(el);
        if (chart) {
            chart.dispose();
        }
    }

    return {
        init,
        dispose
    };
})();
