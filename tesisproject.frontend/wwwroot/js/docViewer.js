window.docViewer = {
    bytesToBlobUrl: function (bytes, contentType) {
        let u8;

        if (typeof bytes === "string") {
            const bin = atob(bytes);
            u8 = new Uint8Array(bin.length);
            for (let i = 0; i < bin.length; i++) u8[i] = bin.charCodeAt(i);
        } else if (bytes instanceof Uint8Array) {
            u8 = bytes;
        } else {
            u8 = new Uint8Array(bytes);
        }

        const blob = new Blob([u8], { type: contentType || "application/octet-stream" });
        return URL.createObjectURL(blob);
    },

    revoke: function (url) {
        try { URL.revokeObjectURL(url); } catch { }
    },

    downloadUrl: function (url, fileName) {
        const a = document.createElement("a");
        a.href = url;
        a.download = fileName || "document";
        a.click();
        a.remove();
    }
};