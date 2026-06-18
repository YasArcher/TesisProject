const tesisPdfPreviewState = new WeakMap();

async function tesisPaintPdfPreview(container, pdf) {
  if (!container || !pdf) {
    return false;
  }

  await new Promise(resolve => window.requestAnimationFrame(resolve));

  const availableWidth =
    container.clientWidth ||
    container.parentElement?.clientWidth ||
    container.closest(".reports-pdf-composer__preview")?.clientWidth ||
    1200;
  const maxWidth = Math.max(availableWidth - 40, 560);
  const deviceScale = Math.max(window.devicePixelRatio || 1, 1);

  container.innerHTML = "";

  for (let pageNumber = 1; pageNumber <= pdf.numPages; pageNumber++) {
    const page = await pdf.getPage(pageNumber);
    const baseViewport = page.getViewport({ scale: 1 });
    const scale = maxWidth / baseViewport.width;
    const viewport = page.getViewport({ scale });

    const wrapper = document.createElement("section");
    wrapper.className = "reports-pdf-composer__page";

    const badge = document.createElement("span");
    badge.className = "reports-pdf-composer__page-badge";
    badge.textContent = `Página ${pageNumber}`;
    wrapper.appendChild(badge);

    const canvas = document.createElement("canvas");
    canvas.style.width = `${viewport.width}px`;
    canvas.style.height = `${viewport.height}px`;
    canvas.width = Math.floor(viewport.width * deviceScale);
    canvas.height = Math.floor(viewport.height * deviceScale);

    const context = canvas.getContext("2d");
    context.setTransform(deviceScale, 0, 0, deviceScale, 0, 0);

    wrapper.appendChild(canvas);
    container.appendChild(wrapper);

    await page.render({
      canvasContext: context,
      viewport
    }).promise;
  }

  return true;
}

window.tesisExport = {
  exportCsv: function (filename, csv) {
    const blob = new Blob([csv], { type: "text/csv;charset=utf-8;" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    setTimeout(() => {
      URL.revokeObjectURL(url);
      a.remove();
    }, 0);
  },
  downloadFileFromBase64: function (filename, contentType, base64) {
    const byteCharacters = atob(base64);
    const byteNumbers = new Array(byteCharacters.length);
    for (let i = 0; i < byteCharacters.length; i++) {
      byteNumbers[i] = byteCharacters.charCodeAt(i);
    }

    const byteArray = new Uint8Array(byteNumbers);
    const blob = new Blob([byteArray], { type: contentType || "application/octet-stream" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    setTimeout(() => {
      URL.revokeObjectURL(url);
      a.remove();
    }, 0);
  },
  createObjectUrlFromBase64: function (contentType, base64) {
    const byteCharacters = atob(base64);
    const byteNumbers = new Array(byteCharacters.length);
    for (let i = 0; i < byteCharacters.length; i++) {
      byteNumbers[i] = byteCharacters.charCodeAt(i);
    }

    const byteArray = new Uint8Array(byteNumbers);
    const blob = new Blob([byteArray], { type: contentType || "application/octet-stream" });
    return URL.createObjectURL(blob);
  },
  revokeObjectUrl: function (url) {
    if (url) {
      URL.revokeObjectURL(url);
    }
  },
  collectReportChartImages: async function (charts) {
    if (!Array.isArray(charts) || !window.echarts) {
      return [];
    }

    await new Promise(resolve => window.requestAnimationFrame(resolve));
    await new Promise(resolve => window.requestAnimationFrame(resolve));

    const images = [];
    const maxImages = 4;
    const maxDataUrlLength = 1800000;

    for (const chart of charts) {
      if (images.length >= maxImages) {
        break;
      }

      const id = chart?.chartId || chart?.ChartId;
      if (!id) {
        continue;
      }

      const el = document.getElementById(id);
      if (!el) {
        continue;
      }

      const bounds = el.getBoundingClientRect();
      if (bounds.width < 40 || bounds.height < 40) {
        continue;
      }

      const style = window.getComputedStyle(el);
      if (style.display === "none" || style.visibility === "hidden" || style.opacity === "0") {
        continue;
      }

      const instance = window.echarts.getInstanceByDom(el);
      if (!instance) {
        continue;
      }

      try {
        instance.resize();
        let base64Png = instance.getDataURL({
          type: "png",
          pixelRatio: 1,
          backgroundColor: "#ffffff"
        });

        if (base64Png && base64Png.length > maxDataUrlLength) {
          base64Png = instance.getDataURL({
            type: "jpeg",
            pixelRatio: 1,
            backgroundColor: "#ffffff",
            quality: 0.74
          });
        }

        const isSupportedImage =
          typeof base64Png === "string" &&
          (base64Png.startsWith("data:image/png;base64,") || base64Png.startsWith("data:image/jpeg;base64,"));

        if (isSupportedImage && base64Png.length <= maxDataUrlLength) {
          images.push({
            ChartId: id,
            Title: chart.title || chart.Title || id,
            Section: chart.section || chart.Section || "General",
            Base64Png: base64Png
          });
        } else if (base64Png) {
          console.warn("Gráfico omitido por tamaño para PDF", id, base64Png.length);
        }
      } catch (error) {
        console.warn("No se pudo exportar el gráfico", id, error);
      }
    }

    return images;
  },
  renderPdfPreview: async function (container, base64) {
    if (!container || !base64 || !window.pdfjsLib) {
      return false;
    }

    container.innerHTML = "";

    try {
      await new Promise(resolve => window.requestAnimationFrame(resolve));

      const byteCharacters = atob(base64);
      const byteArray = new Uint8Array(byteCharacters.length);
      for (let i = 0; i < byteCharacters.length; i++) {
        byteArray[i] = byteCharacters.charCodeAt(i);
      }

      const loadingTask = window.pdfjsLib.getDocument({
        data: byteArray,
        disableWorker: true
      });
      const pdf = await loadingTask.promise;

      let state = tesisPdfPreviewState.get(container);
      if (!state) {
        state = {
          base64: null,
          pdf: null,
          resizeObserver: null,
          resizeTimer: null
        };
        tesisPdfPreviewState.set(container, state);
      }

      state.base64 = base64;
      state.pdf = pdf;

      if (!state.resizeObserver && typeof ResizeObserver !== "undefined") {
        state.resizeObserver = new ResizeObserver(() => {
          if (state.resizeTimer) {
            window.clearTimeout(state.resizeTimer);
          }

          state.resizeTimer = window.setTimeout(() => {
            if (!container.isConnected || !state.pdf) {
              return;
            }

            window.tesisExport.refreshPdfPreviewLayout(container);
          }, 120);
        });

        state.resizeObserver.observe(container);
      }

      return await tesisPaintPdfPreview(container, pdf);
    } catch (error) {
      console.error("No se pudo renderizar la vista previa del PDF con pdf.js", error);
      container.innerHTML = "";
      return false;
    }
  },
  refreshPdfPreviewLayout: async function (container) {
    const state = container ? tesisPdfPreviewState.get(container) : null;
    if (!container || !state?.pdf) {
      return false;
    }

    return await tesisPaintPdfPreview(container, state.pdf);
  },
  printSection: function (id) {
    const el = document.getElementById(id);
    const html = el ? el.outerHTML : document.body.innerHTML;
    const w = window.open("", "_blank", "width=1024,height=768");
    w.document.write(`<html><head><title>Reporte</title>
      <link rel="stylesheet" href="css/bootstrap/bootstrap.min.css">
      <link rel="stylesheet" href="css/app.css"></head><body>${html}</body></html>`);
    w.document.close();
    w.focus();
    w.print();
    w.close();
  }
};
