window.tesisExport = {
  exportCsv: function (filename, csv) {
    const blob = new Blob([csv], { type: "text/csv;charset=utf-8;" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url; a.download = filename;
    document.body.appendChild(a); a.click();
    setTimeout(() => { URL.revokeObjectURL(url); a.remove(); }, 0);
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
    setTimeout(() => { URL.revokeObjectURL(url); a.remove(); }, 0);
  },
  printSection: function (id) {
    const el = document.getElementById(id);
    const html = el ? el.outerHTML : document.body.innerHTML;
    const w = window.open("", "_blank", "width=1024,height=768");
    w.document.write(`<html><head><title>Reporte</title>
      <link rel="stylesheet" href="css/bootstrap/bootstrap.min.css">
      <link rel="stylesheet" href="css/app.css"></head><body>${html}</body></html>`);
    w.document.close(); w.focus(); w.print(); w.close();
  }
};
